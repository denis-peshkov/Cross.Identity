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
import { createLocalAgentOptions } from './cursor-agent-local.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = join(__dirname, '../..');
const GH = join(ROOT, '.cursor/triage/rtk-gh.sh');
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

function gh(args, { json = false, maxBuffer = 8 * 1024 * 1024 } = {}) {
  const out = execFileSync(GH, args, {
    cwd: ROOT,
    encoding: 'utf8',
    maxBuffer,
  });
  return json ? JSON.parse(out) : out;
}

function buildPrompt(pr, diff) {
  const checklist = existsSync(CHECKLIST) ? readFileSync(CHECKLIST, 'utf8') : '';
  const files = (pr.files || []).map((f) => f.path).join('\n');

  return `You triage pull request #${pr.number} for Cross.Identity (NuGet identity/auth library: JWT, OAuth, process engine).

## PR metadata
- Title: ${pr.title}
- Author: ${pr.author?.login ?? 'unknown'}
- +${pr.additions}/-${pr.deletions}, ${pr.changedFiles} files
- Draft: ${pr.isDraft}

## Body
${pr.body || '(empty)'}

## Changed files
${files}

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

  let diff = gh(['pr', 'diff', String(prNumber)]);
  const maxDiff = 48_000;
  if (diff.length > maxDiff) {
    diff = `${diff.slice(0, maxDiff)}\n\n…(diff truncated for triage)`;
  }

  console.log(`Running agent triage for PR #${prNumber}...`);

  try {
    const result = await Agent.prompt(buildPrompt(pr, diff), {
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
  } catch (err) {
    if (err instanceof CursorAgentError) {
      console.error('Cursor SDK error:', err.message);
      process.exit(1);
    }
    throw err;
  }
}

main();
