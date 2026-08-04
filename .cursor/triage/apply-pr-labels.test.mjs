import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  labelsFromTriage,
  normalizeCategoryLabel,
  normalizePriorityLabel,
  applyPrTriageLabels,
  MANAGED_TRIAGE_LABELS,
} from './apply-pr-labels.mjs';

describe('normalizeCategoryLabel', () => {
  it('accepts known categories', () => {
    assert.equal(normalizeCategoryLabel('Enhancement'), 'enhancement');
    assert.equal(normalizeCategoryLabel('bug'), 'bug');
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

describe('applyPrTriageLabels', () => {
  it('creates missing labels, removes stale triage labels, adds current', () => {
    /** @type {string[][]} */
    const calls = [];
    const gh = (args, opts) => {
      calls.push(args);
      if (args[0] === 'pr' && args[1] === 'view') {
        assert.equal(opts?.json, true);
        return {
          labels: [
            { name: 'priority:high' },
            { name: 'bug' },
            { name: 'needs-human' },
          ],
        };
      }
      return '';
    };

    const result = applyPrTriageLabels(gh, 42, {
      category: 'enhancement',
      priority: 'medium',
    });

    assert.deepEqual(result.added, ['enhancement', 'priority:medium']);
    assert.deepEqual(result.removed.sort(), ['bug', 'priority:high']);

    const createCalls = calls.filter((c) => c[0] === 'label' && c[1] === 'create');
    assert.equal(createCalls.length, 2);

    const removeCall = calls.find(
      (c) => c[0] === 'pr' && c[1] === 'edit' && c.includes('--remove-label')
    );
    assert.ok(removeCall);
    assert.ok(removeCall.includes('bug'));
    assert.ok(removeCall.includes('priority:high'));
    assert.ok(!removeCall.includes('needs-human'));

    const addCall = calls.find(
      (c) => c[0] === 'pr' && c[1] === 'edit' && c.includes('--add-label')
    );
    assert.ok(addCall);
    assert.ok(addCall.includes('enhancement'));
    assert.ok(addCall.includes('priority:medium'));
  });
});
