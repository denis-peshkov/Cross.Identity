import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  labelsFromTriage,
  normalizeCategoryLabel,
  normalizePriorityLabel,
  applyPrTriageLabels,
  shouldApplyTriageLabels,
  resolveStickyTriageLabels,
  buildDesiredLabelNames,
  maxPriorityLabel,
  priorityRank,
  sortCategoryLabels,
  MANAGED_TRIAGE_LABELS,
  DEFAULT_LABEL_MIN_CONFIDENCE,
} from './apply-pr-labels.mjs';

describe('normalizeCategoryLabel', () => {
  it('accepts known categories', () => {
    assert.equal(normalizeCategoryLabel('Enhancement'), 'enhancement');
    assert.equal(normalizeCategoryLabel('bug'), 'bug');
    assert.equal(normalizeCategoryLabel('docs'), 'docs');
  });

  it('rejects unknown', () => {
    assert.equal(normalizeCategoryLabel('unknown'), null);
    assert.equal(normalizeCategoryLabel(''), null);
  });
});

describe('normalizePriorityLabel', () => {
  it('prefixes priority', () => {
    assert.equal(normalizePriorityLabel('medium'), 'priority:medium');
    assert.equal(normalizePriorityLabel('priority:high'), 'priority:high');
  });

  it('rejects unknown', () => {
    assert.equal(normalizePriorityLabel('urgent'), null);
  });
});

describe('priorityRank / maxPriorityLabel', () => {
  it('orders critical > high > medium > low', () => {
    assert.ok(priorityRank('priority:critical') > priorityRank('priority:high'));
    assert.ok(priorityRank('priority:high') > priorityRank('priority:medium'));
    assert.ok(priorityRank('priority:medium') > priorityRank('priority:low'));
  });

  it('keeps the higher priority', () => {
    assert.equal(maxPriorityLabel('priority:high', 'medium'), 'priority:high');
    assert.equal(maxPriorityLabel('low', 'priority:critical'), 'priority:critical');
    assert.equal(maxPriorityLabel(null, 'high'), 'priority:high');
  });
});

describe('sortCategoryLabels', () => {
  it('dedupes and orders by CATEGORY_LABELS', () => {
    assert.deepEqual(sortCategoryLabels(['chore', 'security', 'enhancement', 'security']), [
      'enhancement',
      'security',
      'chore',
    ]);
  });
});

describe('labelsFromTriage', () => {
  it('returns category and priority labels', () => {
    const { toAdd, managed } = labelsFromTriage({
      category: 'enhancement',
      priority: 'medium',
    });
    assert.deepEqual(toAdd, ['enhancement', 'priority:medium']);
    assert.ok(managed.includes('priority:low'));
    assert.equal(managed.length, MANAGED_TRIAGE_LABELS.length);
  });
});

describe('resolveStickyTriageLabels', () => {
  it('accumulates enhancement onto existing security', () => {
    const { toAdd } = resolveStickyTriageLabels(
      { category: 'enhancement', priority: 'medium' },
      ['security', 'priority:high', 'needs-human']
    );
    assert.deepEqual(toAdd, ['enhancement', 'security', 'priority:high']);
  });

  it('accumulates chore onto bug and keeps higher priority', () => {
    const { toAdd } = resolveStickyTriageLabels(
      { category: 'chore', priority: 'low' },
      ['bug', 'priority:medium']
    );
    assert.deepEqual(toAdd, ['bug', 'chore', 'priority:medium']);
  });

  it('adds security beside enhancement and raises priority', () => {
    const { toAdd } = resolveStickyTriageLabels(
      { category: 'security', priority: 'high' },
      ['enhancement', 'priority:low']
    );
    assert.deepEqual(toAdd, ['enhancement', 'security', 'priority:high']);
  });

  it('never lowers priority (cancels previous lower priority on apply)', () => {
    const { toAdd } = resolveStickyTriageLabels(
      { category: 'bug', priority: 'low' },
      ['bug', 'priority:high']
    );
    assert.deepEqual(toAdd, ['bug', 'priority:high']);
  });

  it('when dual priority labels exist, keeps the higher and drops the lower', () => {
    const { toAdd } = resolveStickyTriageLabels(
      { category: 'enhancement', priority: 'medium' },
      ['enhancement', 'priority:medium', 'priority:high']
    );
    assert.deepEqual(toAdd, ['enhancement', 'priority:high']);
  });

  it('raises medium → high (toAdd has only the higher priority)', () => {
    const { toAdd } = resolveStickyTriageLabels(
      { category: 'bug', priority: 'high' },
      ['bug', 'priority:medium']
    );
    assert.deepEqual(toAdd, ['bug', 'priority:high']);
  });
});

describe('buildDesiredLabelNames', () => {
  it('keeps unmanaged labels and replaces managed with sticky winners', () => {
    const desired = buildDesiredLabelNames(
      ['needs-human', 'breaking-changes', 'priority:medium', 'bug'],
      ['bug', 'priority:high'],
      MANAGED_TRIAGE_LABELS
    );
    assert.deepEqual(desired.sort(), ['breaking-changes', 'bug', 'needs-human', 'priority:high'].sort());
    assert.ok(!desired.includes('priority:medium'));
  });
});

describe('shouldApplyTriageLabels', () => {
  it('defaults to applying labels when env unset (default 1)', () => {
    const gate = shouldApplyTriageLabels({ confidence: 95 }, {});
    assert.equal(gate.apply, true);
  });

  it('opt-out via TRIAGE_APPLY_LABELS=false', () => {
    const gate = shouldApplyTriageLabels(
      { confidence: 95 },
      { TRIAGE_APPLY_LABELS: 'false' }
    );
    assert.equal(gate.apply, false);
    assert.match(gate.reason, /disabled/);
  });

  it('requires confidence floor when enabled', () => {
    const low = shouldApplyTriageLabels({ confidence: 40 }, {});
    assert.equal(low.apply, false);
    assert.equal(low.minConfidence, DEFAULT_LABEL_MIN_CONFIDENCE);

    const ok = shouldApplyTriageLabels(
      { confidence: 80 },
      { TRIAGE_APPLY_LABELS: '1' }
    );
    assert.equal(ok.apply, true);
  });

  it('honors TRIAGE_LABEL_MIN_CONFIDENCE', () => {
    const gate = shouldApplyTriageLabels(
      { confidence: 85 },
      { TRIAGE_LABEL_MIN_CONFIDENCE: '90' }
    );
    assert.equal(gate.apply, false);
    assert.equal(gate.minConfidence, 90);
  });
});

describe('applyPrTriageLabels', () => {
  it('PUTs desired labels and drops lower priority atomically', () => {
    /** @type {{ args: string[]; opts?: object }[]} */
    const calls = [];
    const gh = (args, opts) => {
      calls.push({ args, opts });
      if (args[0] === 'pr' && args[1] === 'view') {
        assert.equal(opts?.json, true);
        return {
          labels: [
            { name: 'priority:low' },
            { name: 'chore' },
            { name: 'needs-human' },
          ],
        };
      }
      return '';
    };

    const result = applyPrTriageLabels(
      gh,
      42,
      { category: 'enhancement', priority: 'medium' },
      { GITHUB_REPOSITORY: 'denis-peshkov/Cross.Identity' }
    );

    assert.deepEqual(result.added, ['enhancement', 'chore', 'priority:medium']);
    assert.deepEqual(result.removed, ['priority:low']);
    assert.deepEqual(
      result.desired.sort(),
      ['needs-human', 'enhancement', 'chore', 'priority:medium'].sort()
    );

    const createCalls = calls.filter((c) => c.args[0] === 'label' && c.args[1] === 'create');
    assert.equal(createCalls.length, 3);

    const put = calls.find(
      (c) => c.args[0] === 'api' && c.args.includes('--method') && c.args.includes('PUT')
    );
    assert.ok(put);
    assert.ok(put.args.some((a) => String(a).includes('/issues/42/labels')));
    const body = JSON.parse(put.opts.input);
    assert.deepEqual(body.labels.sort(), result.desired.sort());
    assert.ok(!body.labels.includes('priority:low'));
    assert.ok(!calls.some((c) => c.args.includes('--add-label') || c.args.includes('--remove-label')));
  });

  it('raises priority:medium → priority:high and omits medium from PUT body', () => {
    /** @type {{ args: string[]; opts?: object }[]} */
    const calls = [];
    const gh = (args, opts) => {
      calls.push({ args, opts });
      if (args[0] === 'pr' && args[1] === 'view') {
        return {
          labels: [
            { name: 'bug' },
            { name: 'priority:medium' },
            { name: 'priority:high' },
            { name: 'breaking-changes' },
          ],
        };
      }
      return '';
    };

    const result = applyPrTriageLabels(
      gh,
      27,
      { category: 'bug', priority: 'high' },
      { GITHUB_REPOSITORY: 'denis-peshkov/Cross.Identity' }
    );

    assert.deepEqual(result.added, ['bug', 'priority:high']);
    assert.deepEqual(result.removed, ['priority:medium']);
    assert.ok(result.desired.includes('priority:high'));
    assert.ok(!result.desired.includes('priority:medium'));
    assert.ok(result.desired.includes('breaking-changes'));

    const put = calls.find((c) => c.args[0] === 'api' && c.args.includes('PUT'));
    const body = JSON.parse(put.opts.input);
    assert.equal(body.labels.filter((n) => n.startsWith('priority:')).length, 1);
    assert.equal(body.labels.filter((n) => n.startsWith('priority:'))[0], 'priority:high');
  });

  it('keeps security and adds enhancement without lowering priority', () => {
    /** @type {{ args: string[]; opts?: object }[]} */
    const calls = [];
    const gh = (args, opts) => {
      calls.push({ args, opts });
      if (args[0] === 'pr' && args[1] === 'view') {
        return {
          labels: [
            { name: 'security' },
            { name: 'priority:high' },
            { name: 'needs-human' },
          ],
        };
      }
      return '';
    };

    const result = applyPrTriageLabels(
      gh,
      6,
      { category: 'enhancement', priority: 'medium' },
      { GITHUB_REPOSITORY: 'denis-peshkov/Cross.Identity' }
    );

    assert.deepEqual(result.added, ['enhancement', 'security', 'priority:high']);
    assert.deepEqual(result.removed, []);

    const put = calls.find((c) => c.args[0] === 'api' && c.args.includes('PUT'));
    const body = JSON.parse(put.opts.input);
    assert.ok(body.labels.includes('priority:high'));
    assert.ok(!body.labels.includes('priority:medium'));
    assert.ok(body.labels.includes('needs-human'));
  });
});
