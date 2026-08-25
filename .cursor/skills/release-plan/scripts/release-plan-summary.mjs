#!/usr/bin/env node
/**
 * Recalculate the "Checklist summary" line in docs/RELEASE-PLAN-dev-to-master.md.
 *
 * Includes:
 * - rows in §3, §6, §7, §8 with IDs like A1, CI1, DOC1, M1 (status — last emoji in the table row);
 * - §10 markers (release gate + go/no-go).
 *
 * Usage:
 *   node .cursor/skills/release-plan/scripts/release-plan-summary.mjs           # print the line
 *   node .cursor/skills/release-plan/scripts/release-plan-summary.mjs --write   # update the plan file
 */
import { readFileSync, writeFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '../../../..');
const PLAN = join(ROOT, 'docs/RELEASE-PLAN-dev-to-master.md');

const ID_ROW = /^\| ([A-Z]+[0-9]+) \|/;
const BULLET = /^- (✅|🟨|⬜|❌) /;

function primaryStatus(cell) {
  return (cell.match(/^(✅|🟨|⬜|❌)/) ?? [])[1] ?? null;
}

function statusFromTableRow(line) {
  const parts = line.split('|').map((c) => c.trim());
  const dataCols = parts.slice(2, -1);
  for (let i = dataCols.length - 1; i >= 0; i--) {
    const status = primaryStatus(dataCols[i]);
    if (status) {
      return status;
    }
  }
  return null;
}

export function collectChecklistStatuses(markdown) {
  const statuses = [];

  for (const line of markdown.split('\n')) {
    if (ID_ROW.test(line)) {
      const status = statusFromTableRow(line);
      if (!status) {
        throw new Error(`Status not found in checklist row: ${line.slice(0, 120)}`);
      }
      statuses.push(status);
      continue;
    }

    const bullet = line.match(BULLET);
    if (bullet) {
      statuses.push(bullet[1]);
    }
  }

  return statuses;
}

export function formatSummaryLine(statuses) {
  const total = statuses.length;
  const counts = { '✅': 0, '🟨': 0, '⬜': 0, '❌': 0 };
  for (const status of statuses) {
    counts[status] += 1;
  }

  const pct = (n) => (total ? Math.round((n / total) * 100) : 0);

  return (
    `**Checklist summary:** **${total}** items — ` +
    `✅ **${counts['✅']}** (${pct(counts['✅'])}%) · ` +
    `🟨 **${counts['🟨']}** (${pct(counts['🟨'])}%) · ` +
    `⬜ **${counts['⬜']}** (${pct(counts['⬜'])}%) · ` +
    `❌ **${counts['❌']}** (${pct(counts['❌'])}%)`
  );
}

function main() {
  const markdown = readFileSync(PLAN, 'utf8');
  const line = formatSummaryLine(collectChecklistStatuses(markdown));

  if (process.argv.includes('--write')) {
    const updated = markdown.replace(
      /^\*\*Checklist summary:\*\*.*$/m,
      line
    );
    if (updated === markdown) {
      console.log('Checklist summary is already up to date');
    } else {
      writeFileSync(PLAN, updated, 'utf8');
      console.log(`Updated ${PLAN}`);
    }
  }

  console.log(line);
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  main();
}
