/**
 * Apply GitHub labels from automated PR triage (category + priority).
 * Manages only known triage labels; other labels on the PR are left untouched.
 */

export const CATEGORY_LABELS = Object.freeze([
  'feature',
  'bug',
  'enhancement',
  'security',
  'docs',
  'chore',
  'question',
]);

export const PRIORITY_VALUES = Object.freeze(['critical', 'high', 'medium', 'low']);

/** @type {ReadonlyArray<string>} */
export const MANAGED_TRIAGE_LABELS = Object.freeze([
  ...CATEGORY_LABELS,
  ...PRIORITY_VALUES.map((p) => `priority:${p}`),
]);

const LABEL_META = Object.freeze({
  feature: { color: '0e8a16', description: 'New capability or flow' },
  bug: { color: 'd73a4a', description: 'Something is broken' },
  enhancement: { color: 'a2eeef', description: 'Improvement without major behavior change' },
  security: { color: 'b60205', description: 'Auth / JWT / OAuth / token security' },
  docs: { color: '0075ca', description: 'Documentation only' },
  chore: { color: 'fef2c0', description: 'Build, CI, tooling, deps' },
  question: { color: 'd876e3', description: 'Question / clarification' },
  'priority:critical': { color: 'b60205', description: 'Triage priority: critical' },
  'priority:high': { color: 'd93f0b', description: 'Triage priority: high' },
  'priority:medium': { color: 'fbca04', description: 'Triage priority: medium' },
  'priority:low': { color: '0e8a16', description: 'Triage priority: low' },
});

/**
 * @param {unknown} value
 * @returns {string | null}
 */
export function normalizeCategoryLabel(value) {
  const raw = String(value || '')
    .trim()
    .toLowerCase();
  if (!raw || raw === 'unknown') {
    return null;
  }
  return CATEGORY_LABELS.includes(raw) ? raw : null;
}

/**
 * @param {unknown} value
 * @returns {string | null} label like priority:medium
 */
export function normalizePriorityLabel(value) {
  const raw = String(value || '')
    .trim()
    .toLowerCase()
    .replace(/^priority:/, '');
  if (!PRIORITY_VALUES.includes(raw)) {
    return null;
  }
  return `priority:${raw}`;
}

/**
 * @param {{ category?: unknown; priority?: unknown }} data
 * @returns {{ toAdd: string[]; managed: readonly string[] }}
 */
export function labelsFromTriage(data) {
  const category = normalizeCategoryLabel(data?.category);
  const priority = normalizePriorityLabel(data?.priority);
  return {
    toAdd: [category, priority].filter(Boolean),
    managed: MANAGED_TRIAGE_LABELS,
  };
}

/**
 * @param {(args: string[], opts?: { json?: boolean }) => unknown} gh
 * @param {string} name
 */
function ensureLabel(gh, name) {
  const meta = LABEL_META[name] || { color: 'ededed', description: 'Triage label' };
  try {
    gh([
      'label',
      'create',
      name,
      '--color',
      meta.color,
      '--description',
      meta.description,
      '--force',
    ]);
  } catch (err) {
    console.warn(`ensureLabel(${name}):`, err?.message ?? err);
  }
}

/**
 * Sync triage labels on a PR: drop previous managed triage labels, set current ones.
 *
 * @param {(args: string[], opts?: { json?: boolean }) => unknown} gh
 * @param {number|string} prNumber
 * @param {{ category?: unknown; priority?: unknown }} data
 * @returns {{ added: string[]; removed: string[] }}
 */
export function applyPrTriageLabels(gh, prNumber, data) {
  const { toAdd, managed } = labelsFromTriage(data);
  const pr = String(prNumber);

  if (toAdd.length === 0) {
    console.warn('No valid triage category/priority — skipping label sync');
    return { added: [], removed: [] };
  }

  const view = /** @type {{ labels?: { name: string }[] }} */ (
    gh(['pr', 'view', pr, '--json', 'labels'], { json: true })
  );
  const currentNames = (view.labels ?? []).map((l) => l.name);

  const toRemove = currentNames.filter(
    (name) => managed.includes(name) && !toAdd.includes(name)
  );

  for (const name of toAdd) {
    ensureLabel(gh, name);
  }

  gh(['pr', 'edit', pr, ...toAdd.flatMap((name) => ['--add-label', name])]);

  if (toRemove.length > 0) {
    gh(['pr', 'edit', pr, ...toRemove.flatMap((name) => ['--remove-label', name])]);
  }

  return { added: toAdd, removed: toRemove };
}
