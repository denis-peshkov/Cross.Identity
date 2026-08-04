#!/usr/bin/env node
/**
 * Post wshm-style automated triage comment on a pull request.
 * Env: CURSOR_API_KEY, GH_TOKEN, PR_NUMBER
 */
import { execFileSync } from 'node:child_process';
import { existsSync, readFileSync, writeFileSync, unlinkSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { Agent, CursorAgentError } from '@cursor/sdk';
import { formatPrTriageComment, parseAgentJson } from './format-pr-comment.mjs';
import { applyPrTriageLabels } from './apply-pr-labels.mjs';
import { createLocalAgentOptions } from './cursor-agent-local.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = join(__dirname, '../..');
const GH = join(ROOT, '.cursor/triage/gh-wrapper.sh');
const CHECKLIST = join(ROOT, '.cursor/skills/pr-triage/references/dotnet-checklist.md');

const prNumber = process.env.PR_NUMBER || process.argv[2];
const apiKey = process.env.CURSOR_API_KEY;

if (!prNumber) {
  console.error('PR_NUMBER is required');
  process.exit(1);
}

if (!apiKey) {
  console.error('CURSOR_API_KEY is required');
  process.exit(1);
}

const MAX_DIFF_CHARS = 48_000;

function gh(args, { json = false, maxBuffer = 8 * 1024 * 1024 } = {}) {
  const out = execFileSync(GH, args, {
    cwd: ROOT,
    encoding: 'utf8',
    maxBuffer,
  });
  return json ? JSON.parse(out) : out;
}

function tryGh(args, options = {}) {
  try {
    return { ok: true, data: gh(args, options) };
  } catch (err) {
    const stderr = err.stderr?.toString?.() ?? '';
    const message = err.message ?? String(err);
    return { ok: false, stderr, message };
  }
}

function isPrDiffTooLarge({ stderr, message }) {
  const text = `${stderr}\n${message}`;
  return /too_large|PullRequest\.diff|exceeded maximum|HTTP 406/i.test(text);
}

function fetchAllPrFiles(repo, prNumber) {
  return gh(['api', `repos/${repo}/pulls/${prNumber}/files`, '--paginate'], {
    json: true,
    maxBuffer: 32 * 1024 * 1024,
  });
}

const PATCH_PRIORITY = [
  /^Cross\.Identity\//,
  /^Sample\.Api\//,
  /^\.github\/workflows\//,
  /\.cs$/,
];

function patchPriority(filename) {
  const idx = PATCH_PRIORITY.findIndex((re) => re.test(filename));
  return idx === -1 ? PATCH_PRIORITY.length : idx;
}

function buildDiffFromFilePatches(files, maxChars = MAX_DIFF_CHARS) {
  const sorted = [...files].sort(
    (a, b) => patchPriority(a.filename) - patchPriority(b.filename)
  );
  const parts = [];
  let used = 0;

  for (const file of sorted) {
    if (!file.patch) {
      continue;
    }

    const chunk = `diff --git a/${file.filename} b/${file.filename}\n${file.patch}\n`;
    if (used + chunk.length > maxChars) {
      break;
    }

    parts.push(chunk);
    used += chunk.length;
  }

  const header =
    `(GitHub \`gh pr diff\` unavailable — PR exceeds diff file limit; ` +
    `${files.length} files total, ${parts.length} patch excerpt(s) below)\n\n`;

  return header + parts.join('\n');
}

function mapApiFileStatus(status) {
  switch (status) {
    case 'added':
      return 'ADDED';
    case 'removed':
      return 'DELETED';
    case 'renamed':
      return 'RENAMED';
    default:
      return 'MODIFIED';
  }
}

function normalizePrFiles(files) {
  return files.map((f) => ({
    path: f.path ?? f.filename,
    additions: f.additions,
    deletions: f.deletions,
    changeType: f.changeType ?? mapApiFileStatus(f.status),
  }));
}

function formatFileList(files) {
  return files
    .map((f) => {
      const stats =
        f.additions != null ? ` (+${f.additions}/-${f.deletions ?? 0})` : '';
      const kind = f.changeType ? ` [${f.changeType}]` : '';
      return `${f.path}${kind}${stats}`;
    })
    .join('\n');
}

function fetchPrDiff(repo, prNumber, pr) {
  const diffResult = tryGh(['pr', 'diff', String(prNumber)]);

  if (diffResult.ok) {
    let diff = diffResult.data;
    if (diff.length > MAX_DIFF_CHARS) {
      diff = `${diff.slice(0, MAX_DIFF_CHARS)}\n\n…(diff truncated for triage)`;
    }

    return { diff, files: normalizePrFiles(pr.files ?? []) };
  }

  if (!isPrDiffTooLarge(diffResult)) {
    throw new Error(
      `gh pr diff failed: ${diffResult.stderr || diffResult.message}`
    );
  }

  console.warn(
    'PR diff too large for gh pr diff — using per-file patches from GitHub API'
  );

  const apiFiles = fetchAllPrFiles(repo, prNumber);
  const files = normalizePrFiles(apiFiles);
  let diff = buildDiffFromFilePatches(apiFiles);

  if (!apiFiles.some((f) => f.patch)) {
    diff =
      `(Full diff unavailable: PR exceeds GitHub diff limit (${apiFiles.length} files). ` +
      `Triage from PR metadata and changed-file paths only.)`;
  } else if (diff.length > MAX_DIFF_CHARS) {
    diff = `${diff.slice(0, MAX_DIFF_CHARS)}\n\n…(patch excerpts truncated for triage)`;
  }

  return { diff, files };
}

function buildPrompt(pr, diff, files) {
  const checklist = existsSync(CHECKLIST) ? readFileSync(CHECKLIST, 'utf8') : '';
  const fileList = formatFileList(files);

  return `You triage pull request #${pr.number} for Cross.Identity (NuGet identity/auth library: JWT, OAuth, process engine).

## PR metadata
- Title: ${pr.title}
- Author: ${pr.author?.login ?? 'unknown'}
- +${pr.additions}/-${pr.deletions}, ${pr.changedFiles} files
- Draft: ${pr.isDraft}

## Body
${pr.body || '(empty)'}

## Changed files (${files.length})
${fileList}

## Diff (may be truncated)
${diff}

## Review checklist
${checklist}

Return ONLY a single JSON object (no markdown prose) with this schema:
{
  "category": "feature|bug|enhancement|security|docs|chore|question",
  "priority": "critical|high|medium|low",
  "confidence": <integer 0-100>,
  "summary": "<2-4 sentences English: what the PR does and triage takeaway>",
  "maintainerHint": "<optional line, e.g. This looks like a simple fix suitable for quick review>",
  "relevantFiles": ["path/from/diff.cs", "..."],
  "securityNotes": "<optional; JWT/OAuth/token risks for identity library>"
}

Rules:
- security + high/critical for auth/token vulnerabilities
- confidence reflects how clear the PR intent is from title/body/diff
- relevantFiles: max 8 paths, only from this PR
- English only
`;
}

function findExistingCommentId(repo, issueNumber) {
  try {
    const id = execFileSync(
      GH,
      [
        'api',
        `repos/${repo}/issues/${issueNumber}/comments`,
        '--jq',
        '.[] | select(.body | contains("cross-identity-triage")) | .id',
      ],
      { cwd: ROOT, encoding: 'utf8' }
    ).trim();
    return id ? Number(id.split('\n')[0]) : undefined;
  } catch {
    return undefined;
  }
}

function patchComment(repo, commentId, body) {
  const tmp = join(tmpdir(), `ci-triage-comment-${commentId}.json`);
  writeFileSync(tmp, JSON.stringify({ body }), 'utf8');
  try {
    execFileSync(
      GH,
      ['api', '-X', 'PATCH', `repos/${repo}/issues/comments/${commentId}`, '--input', tmp],
      { cwd: ROOT, stdio: 'inherit' }
    );
  } finally {
    unlinkSync(tmp);
  }
}

function upsertPrComment(repo, issueNumber, body) {
  if (!body?.trim()) {
    throw new Error('Generated comment body is empty');
  }

  const existingId = findExistingCommentId(repo, issueNumber);

  if (existingId) {
    patchComment(repo, existingId, body);
    console.log(`Updated triage comment ${existingId} on PR #${issueNumber}`);
    return;
  }

  const tmp = join(tmpdir(), `ci-triage-pr-comment-${issueNumber}.md`);
  writeFileSync(tmp, body, 'utf8');
  try {
    execFileSync(GH, ['pr', 'comment', String(issueNumber), '--body-file', tmp], {
      cwd: ROOT,
      stdio: 'inherit',
    });
  } finally {
    unlinkSync(tmp);
  }
  console.log(`Posted triage comment on PR #${issueNumber}`);
}

async function main() {
  const repo = gh(['repo', 'view', '--json', 'nameWithOwner', '-q', '.nameWithOwner']).trim();

  const pr = gh(
    [
      'pr',
      'view',
      String(prNumber),
      '--json',
      'number,title,body,author,additions,deletions,changedFiles,isDraft,files,headRefName,baseRefName',
    ],
    { json: true }
  );

  if (pr.isDraft) {
    console.log(`PR #${prNumber} is draft — skipping triage comment`);
    return;
  }

  const { diff, files } = fetchPrDiff(repo, prNumber, pr);

  console.log(`Running agent triage for PR #${prNumber}...`);

  try {
    const result = await Agent.prompt(buildPrompt(pr, diff, files), {
      apiKey,
      model: { id: 'auto' },
      local: createLocalAgentOptions(ROOT),
    });

    if (result.status === 'error' || result.status === 'cancelled') {
      console.error('Agent failed:', result.status, result.id);
      process.exit(2);
    }

    const data = parseAgentJson(result.result || '{}');
    const defaultBranch = gh([
      'repo',
      'view',
      '--json',
      'defaultBranchRef',
      '-q',
      '.defaultBranchRef.name',
    ]).trim();
    const comment = formatPrTriageComment(data, { repo, defaultBranch });
    upsertPrComment(repo, prNumber, comment);

    try {
      const { added, removed } = applyPrTriageLabels(gh, prNumber, data);
      console.log(
        `Triage labels on PR #${prNumber}: +[${added.join(', ')}] -[${removed.join(', ')}]`
      );
    } catch (labelErr) {
      console.error('Failed to apply triage labels:', labelErr.message ?? labelErr);
      // Comment already posted — do not fail the job solely on label sync.
    }
  } catch (err) {
    if (err instanceof CursorAgentError) {
      console.error('Cursor SDK error:', err.message);
      process.exit(1);
    }
    throw err;
  }
}

main();
