#!/usr/bin/env node
/**
 * Post wshm-style automated triage comment on a pull request.
 * Env: CURSOR_API_KEY, GH_TOKEN, PR_NUMBER
 */
import { execFileSync } from 'node:child_process';
import { existsSync, readFileSync, readdirSync, writeFileSync, unlinkSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { Agent, CursorAgentError } from '@cursor/sdk';
import { formatPrTriageComment, parseAgentJson, TRIAGE_MARKER } from './format-pr-comment.mjs';
import { applyPrTriageLabels, shouldApplyTriageLabels } from './apply-pr-labels.mjs';
import { createLocalAgentOptions } from './cursor-agent-local.mjs';
import { formatPrScopeSection } from './pr-scope.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = join(__dirname, '../..');
const GH = join(ROOT, '.cursor/triage/gh-wrapper.sh');
const CHECKLISTS_DIR = join(ROOT, '.cursor/skills/triage-pr/references');
const DOTNET_CHECKLIST = join(CHECKLISTS_DIR, 'dotnet-checklist.md');
const ANGULAR_CHECKLIST = join(CHECKLISTS_DIR, 'angular-checklist.md');

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

function escapeRegExp(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

/**
 * Prefer env TRIAGE_PATCH_PRIORITY_PREFIXES (comma-separated top-level dirs),
 * else top-level dirs from the PR file list (by frequency), then workflows / .cs.
 * @param {string[]} filenames
 * @returns {RegExp[]}
 */
function buildPatchPriorityMatchers(filenames) {
  const fromEnv = (process.env.TRIAGE_PATCH_PRIORITY_PREFIXES || '')
    .split(',')
    .map((s) => s.trim().replace(/\/?$/, '/'))
    .filter(Boolean);

  let prefixes = fromEnv;
  if (prefixes.length === 0) {
    const counts = new Map();
    for (const name of filenames) {
      const slash = name.indexOf('/');
      if (slash <= 0) {
        continue;
      }
      const top = name.slice(0, slash + 1);
      if (top.startsWith('.')) {
        continue;
      }
      counts.set(top, (counts.get(top) || 0) + 1);
    }
    prefixes = [...counts.entries()]
      .sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0]))
      .map(([p]) => p)
      .slice(0, 8);
  }

  return [
    ...prefixes.map((p) => new RegExp(`^${escapeRegExp(p)}`)),
    /^\.github\/workflows\//,
    /\.cs$/,
  ];
}

function patchPriority(filename, matchers) {
  const idx = matchers.findIndex((re) => re.test(filename));
  return idx === -1 ? matchers.length : idx;
}

function buildDiffFromFilePatches(files, maxChars = MAX_DIFF_CHARS) {
  const matchers = buildPatchPriorityMatchers(files.map((f) => f.filename).filter(Boolean));
  const sorted = [...files].sort(
    (a, b) => patchPriority(a.filename, matchers) - patchPriority(b.filename, matchers)
  );
  const parts = [];
  let used = 0;

  for (const file of sorted) {
    if (!file.patch) {
      continue;
    }

    const chunk = `diff --git a/${file.filename} b/${file.filename}\n${file.patch}\n`;
    if (used + chunk.length > maxChars) {
      continue;
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

function loadReviewChecklists(files) {
  const paths = (files || []).map((f) => f.path ?? f.filename ?? '');
  const hasDotnet = paths.some((p) => /\.(cs|csproj)$/i.test(p));
  const hasFrontend = paths.some((p) => /\.(ts|tsx|html|scss|css)$/i.test(p));

  const sections = [];
  const wantDotnet =
    existsSync(DOTNET_CHECKLIST) &&
    (paths.length === 0 || hasDotnet || !hasFrontend);
  const wantAngular =
    existsSync(ANGULAR_CHECKLIST) &&
    (paths.length === 0 || hasFrontend || !hasDotnet);

  if (wantDotnet) {
    sections.push(readFileSync(DOTNET_CHECKLIST, 'utf8'));
  }
  if (wantAngular) {
    sections.push(readFileSync(ANGULAR_CHECKLIST, 'utf8'));
  }

  if (sections.length > 0) {
    return sections.join('\n\n---\n\n');
  }

  // Neither signal matched, but files may exist — include whatever is present.
  if (existsSync(DOTNET_CHECKLIST)) {
    sections.push(readFileSync(DOTNET_CHECKLIST, 'utf8'));
  }
  if (existsSync(ANGULAR_CHECKLIST)) {
    sections.push(readFileSync(ANGULAR_CHECKLIST, 'utf8'));
  }
  if (sections.length > 0) {
    return sections.join('\n\n---\n\n');
  }

  if (!existsSync(CHECKLISTS_DIR)) {
    return '';
  }

  try {
    const names = readdirSync(CHECKLISTS_DIR)
      .filter((name) => name.endsWith('.md'))
      .sort();
    return names
      .map((name) => readFileSync(join(CHECKLISTS_DIR, name), 'utf8'))
      .join('\n\n---\n\n');
  } catch {
    return '';
  }
}

function buildPrompt(pr, diff, files) {
  const checklist = loadReviewChecklists(files);
  const fileList = formatFileList(files);
  const scope = formatPrScopeSection(pr);

  return `You triage pull request #${pr.number} for this repository (see README.md for layout).

${scope}

## PR metadata
- Title: ${pr.title}
- Author: ${pr.author?.login ?? 'unknown'}
- Base → head: ${pr.baseRefName} → ${pr.headRefName}
- +${pr.additions}/-${pr.deletions}, ${pr.changedFiles} files
- Draft: ${pr.isDraft}

## Body
${pr.body || '(empty)'}

## Changed files (full PR, ${files.length})
${fileList}

## Diff (full PR base...head; may be truncated for size — still classify whole PR)
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
  "relevantFiles": ["path/from/this-pr.cs", "..."],
  "securityNotes": "<optional; secrets/auth/licensing/token/PII/payment risks if applicable>"
}

Rules:
- security + high/critical for secret leaks, auth/licensing bypass, token misuse, PII exposure, payment issues
- confidence reflects how clear the PR intent is from title/body/diff
- relevantFiles: max 8 paths, only from this PR
- English only
- category/priority/summary MUST describe the cumulative PR (all commits/files), never the tip commit alone
`;
}

/**
 * Comments posted with Actions `GITHUB_TOKEN` are always authored by this login.
 * (Local `yarn pr-triage` with a PAT would create comments as that user — use CI
 * token / bot for upsert, or post once and edit manually.)
 */
const TRIAGE_COMMENT_AUTHOR = 'github-actions[bot]';

/**
 * Find our prior triage comment: marker + bot author (ignore contributor copies of the marker).
 * @param {string} repo
 * @param {string|number} issueNumber
 * @returns {number|undefined}
 */
function findExistingCommentId(repo, issueNumber) {
  try {
    const comments = gh(
      ['api', `repos/${repo}/issues/${issueNumber}/comments`, '--paginate'],
      { json: true }
    );
    const list = Array.isArray(comments) ? comments : [];
    const match = list.find(
      (c) =>
        typeof c?.body === 'string' &&
        c.body.includes(TRIAGE_MARKER) &&
        c.user?.login === TRIAGE_COMMENT_AUTHOR
    );
    return match?.id != null ? Number(match.id) : undefined;
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
    console.log(
      `Updated triage comment ${existingId} on PR #${issueNumber} (author=${TRIAGE_COMMENT_AUTHOR})`
    );
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
      'number,title,body,author,additions,deletions,changedFiles,isDraft,files,headRefName,baseRefName,commits',
    ],
    { json: true }
  );

  if (pr.isDraft) {
    console.log(`PR #${prNumber} is draft — skipping triage comment`);
    return;
  }

  const { diff, files } = fetchPrDiff(repo, prNumber, pr);
  const commitCount = Array.isArray(pr.commits) ? pr.commits.length : 0;
  console.log(
    `PR #${prNumber} scope: ${pr.baseRefName}...${pr.headRefName}, ${commitCount} commit(s), ${files.length} file(s)`
  );

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
      const gate = shouldApplyTriageLabels(data);
      if (!gate.apply) {
        console.log(`Skipping triage labels on PR #${prNumber}: ${gate.reason}`);
      } else {
        const { added, removed } = applyPrTriageLabels(gh, prNumber, data);
        console.log(
          `Triage labels on PR #${prNumber}: +[${added.join(', ')}] -[${removed.join(', ')}] (confidence=${gate.confidence})`
        );
      }
    } catch (labelErr) {
      console.error('Failed to apply triage labels:', labelErr.message ?? labelErr);
      process.exit(1);
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
