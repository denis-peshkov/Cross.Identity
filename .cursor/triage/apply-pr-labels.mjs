/**
 * Apply GitHub labels from automated PR triage (category + priority).
 * Manages only known triage labels; other labels on the PR are left untouched.
 *
 * Labels sync by default when confidence ≥ `TRIAGE_LABEL_MIN_CONFIDENCE`
 * (default 70). Set `TRIAGE_APPLY_LABELS=false` to disable. Category/priority
 * stay allowlisted. Comment suggestions are independent of this gate.
 *
 * Sticky rules across synchronize re-runs:
 * - **category**: accumulate (union) — a PR may carry several category labels;
 *   new runs add the proposed category, never drop ones already on the PR
 * - **priority**: exactly one label via {@link PRIORITY_RANK} — only raise or keep;
 *   every other `priority:*` is removed in the same `gh pr edit` as the add
 */

export const CATEGORY_LABELS = Object.freeze([
  'feature',
  'bug',
  'enhancement',
  'security',
  'docs',
  'chore',
]);

export const PRIORITY_VALUES = Object.freeze(['critical', 'high', 'medium', 'low']);

/** Higher number = more severe. Used so priority never decreases on re-triage. */
export const PRIORITY_RANK = Object.freeze({
  critical: 4,
  high: 3,
  medium: 2,
  low: 1,
});

/** @type {ReadonlyArray<string>} */
export const MANAGED_TRIAGE_LABELS = Object.freeze([
  ...CATEGORY_LABELS,
  ...PRIORITY_VALUES.map((p) => `priority:${p}`),
]);

export const DEFAULT_LABEL_MIN_CONFIDENCE = 70;

const LABEL_META = Object.freeze({
  feature: { color: '0e8a16', description: 'New API or behavior' },
  bug: { color: 'e8372a', description: 'Something is broken' },
  enhancement: { color: 'a2eeef', description: 'Polish, perf, or DX without new public contract' },
  security: {
    color: '5319e7',
    description: '🔒 Auth/JWT/OAuth, secrets, licensing, PII, payment, or token security',
  },
  docs: { color: '006b75', description: 'Improvements or additions to documentation' },
  chore: { color: '1d76db', description: 'Build, CI, tooling, deps' },
  'priority:critical': { color: 'b60205', description: 'Triage priority: critical' },
  'priority:high': { color: 'd93f0b', description: 'Triage priority: high' },
  'priority:medium': { color: 'fb8500', description: 'Triage priority: medium' },
  'priority:low': { color: 'fbca04', description: 'Triage priority: low' },
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
 * @param {string | null | undefined} priorityLabel
 * @returns {number}
 */
export function priorityRank(priorityLabel) {
  if (!priorityLabel) {
    return 0;
  }
  const raw = String(priorityLabel)
    .trim()
    .toLowerCase()
    .replace(/^priority:/, '');
  return PRIORITY_RANK[raw] || 0;
}

/**
 * @param {string | null | undefined} a
 * @param {string | null | undefined} b
 * @returns {string | null}
 */
export function maxPriorityLabel(a, b) {
  const left = normalizePriorityLabel(a);
  const right = normalizePriorityLabel(b);
  if (!left) {
    return right;
  }
  if (!right) {
    return left;
  }
  return priorityRank(left) >= priorityRank(right) ? left : right;
}

/**
 * Stable order for accumulated category labels (same order as {@link CATEGORY_LABELS}).
 * @param {Iterable<string>} categories
 * @returns {string[]}
 */
export function sortCategoryLabels(categories) {
  const set = new Set(
    [...categories].map((c) => normalizeCategoryLabel(c)).filter(Boolean)
  );
  return CATEGORY_LABELS.filter((c) => set.has(c));
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
 * Merge agent proposal with labels already on the PR.
 * Categories accumulate (union). Priority is a single sticky max; lower
 * `priority:*` labels are dropped when applying.
 *
 * @param {{ category?: unknown; priority?: unknown }} data
 * @param {string[]} currentNames
 * @returns {{ toAdd: string[]; managed: readonly string[] }}
 */
export function resolveStickyTriageLabels(data, currentNames = []) {
  const { toAdd: proposed, managed } = labelsFromTriage(data);
  const names = Array.isArray(currentNames) ? currentNames : [];

  const currentCategories = names.filter((n) => CATEGORY_LABELS.includes(n));
  // Max across *all* current priority:* — never pick .find() first (dual labels / race).
  const currentPriority = names
    .filter((n) => typeof n === 'string' && n.startsWith('priority:') && managed.includes(n))
    .reduce((best, name) => maxPriorityLabel(best, name), null);

  const proposedCategory = proposed.find((n) => CATEGORY_LABELS.includes(n)) || null;
  const proposedPriority = proposed.find((n) => n.startsWith('priority:')) || null;

  const categories = sortCategoryLabels([
    ...currentCategories,
    ...(proposedCategory ? [proposedCategory] : []),
  ]);
  const priority = maxPriorityLabel(currentPriority, proposedPriority);

  return {
    toAdd: [...categories, priority].filter(Boolean),
    managed,
  };
}

/**
 * Gate: labels on by default; opt-out via TRIAGE_APPLY_LABELS=false|0|no|off.
 * Also requires confidence floor (allowlist is in labelsFromTriage).
 *
 * @param {{ confidence?: unknown }} data
 * @param {NodeJS.ProcessEnv} [env]
 * @returns {{ apply: boolean; reason: string; confidence: number; minConfidence: number }}
 */
export function shouldApplyTriageLabels(data, env = process.env) {
  const flag = String(env.TRIAGE_APPLY_LABELS ?? '1')
    .trim()
    .toLowerCase();
  const disabled = flag === 'false' || flag === '0' || flag === 'no' || flag === 'off';
  const enabled = !disabled;
  const parsedMin = Number(env.TRIAGE_LABEL_MIN_CONFIDENCE);
  const minConfidence =
    Number.isFinite(parsedMin) && parsedMin >= 0
      ? Math.min(100, parsedMin)
      : DEFAULT_LABEL_MIN_CONFIDENCE;
  const confidence = Math.min(100, Math.max(0, Number(data?.confidence) || 0));

  if (!enabled) {
    return {
      apply: false,
      reason: 'TRIAGE_APPLY_LABELS is disabled (comment-only suggestions)',
      confidence,
      minConfidence,
    };
  }

  if (confidence < minConfidence) {
    return {
      apply: false,
      reason: `confidence ${confidence} < min ${minConfidence}`,
      confidence,
      minConfidence,
    };
  }

  return {
    apply: true,
    reason: 'enabled and confidence gate passed',
    confidence,
    minConfidence,
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
 * Sync triage labels on a PR: set sticky-resolved labels, drop other managed ones
 * (including superseded lower `priority:*`).
 * Caller must gate with {@link shouldApplyTriageLabels} before invoking.
 *
 * @param {(args: string[], opts?: { json?: boolean }) => unknown} gh
 * @param {number|string} prNumber
 * @param {{ category?: unknown; priority?: unknown }} data
 * @returns {{ added: string[]; removed: string[] }}
 */
export function applyPrTriageLabels(gh, prNumber, data) {
  const pr = String(prNumber);

  const view = /** @type {{ labels?: { name: string }[] }} */ (
    gh(['pr', 'view', pr, '--json', 'labels'], { json: true })
  );
  const currentNames = (view.labels ?? []).map((l) => l.name);

  const { toAdd, managed } = resolveStickyTriageLabels(data, currentNames);

  if (toAdd.length === 0) {
    console.warn('No valid triage category/priority — skipping label sync');
    return { added: [], removed: [] };
  }

  // Drop every managed label not in the sticky set — including all lower priority:*.
  const toRemove = currentNames.filter(
    (name) => managed.includes(name) && !toAdd.includes(name)
  );

  for (const name of toAdd) {
    ensureLabel(gh, name);
  }

  // One `pr edit`: add winners and remove losers together (avoids a dual-priority window).
  const editArgs = [
    'pr',
    'edit',
    pr,
    ...toAdd.flatMap((name) => ['--add-label', name]),
    ...toRemove.flatMap((name) => ['--remove-label', name]),
  ];
  gh(editArgs);

  return { added: toAdd, removed: toRemove };
}
