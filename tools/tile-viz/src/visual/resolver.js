// Port of AutotileResolver.cs (sprite id resolution without Unity Sprite objects).

import { ruleMatches, ruleMatchesColumns, patternToMask, masksEqual } from './rule.js';
import { getRulesForSpriteCount, getFallbackSpriteId, GROUND_SPRITE_COUNT } from './ruleTables.js';

const MatchPass = {
  Direct: 'direct',
  FlipRows: 'flipRows',
  FlipColumns: 'flipColumns',
};

function ruleMatchesPass(pattern, mask, pass) {
  switch (pass) {
    case MatchPass.Direct:
      return ruleMatches(pattern, mask, false);
    case MatchPass.FlipRows:
      return ruleMatches(pattern, mask, true);
    case MatchPass.FlipColumns:
      return ruleMatchesColumns(pattern, mask, true);
    default:
      return false;
  }
}

/**
 * @param {object[]} rules
 * @param {number[][]} mask
 * @param {string} pass
 * @returns {number}
 */
function findRuleIndex(rules, mask, pass) {
  const matches = [];
  for (let i = 0; i < rules.length; i++) {
    if (ruleMatchesPass(rules[i].pattern, mask, pass)) {
      matches.push(rules[i]);
    }
  }
  if (matches.length === 0) {
    return -1;
  }
  if (matches.length === 1) {
    return rules.indexOf(matches[0]);
  }
  const flipForHash = pass !== MatchPass.Direct;
  if (allSharePattern(matches)) {
    const selected = pickDeterministic(matches, mask, flipForHash);
    return rules.indexOf(selected);
  }
  for (let i = 0; i < rules.length; i++) {
    if (ruleMatchesPass(rules[i].pattern, mask, pass)) {
      return i;
    }
  }
  return -1;
}

function allSharePattern(matches) {
  const first = patternToMask(matches[0].pattern);
  for (let i = 1; i < matches.length; i++) {
    if (!masksEqual(first, patternToMask(matches[i].pattern))) {
      return false;
    }
  }
  return true;
}

function pickDeterministic(matches, mask, flipForHash) {
  if (matches.length === 1) {
    return matches[0];
  }
  let totalWeight = 0;
  for (const m of matches) {
    totalWeight += Math.max(1, m.weight ?? 1);
  }
  const pick = hashMask(mask, flipForHash) % totalWeight;
  let state = 0;
  for (const m of matches) {
    state += Math.max(1, m.weight ?? 1);
    if (pick < state) {
      return m;
    }
  }
  return matches[0];
}

function hashMask(mask, flipForHash) {
  let hash = flipForHash ? 17 : 31;
  for (let x = 0; x < 3; x++) {
    for (let y = 0; y < 3; y++) {
      hash = (hash * 31 + mask[x][y]) | 0;
    }
  }
  return Math.abs(hash);
}

/**
 * Resolve sprite id and flip for a tileset definition and neighbor mask.
 * @param {{ spriteCount: number, rules?: object[] }} tileset
 * @param {number[][]} mask
 * @returns {{ spriteId: string, flipX: boolean, resolved: boolean }}
 */
export function resolveSpriteId(tileset, mask) {
  if (!tileset || !mask) {
    return { spriteId: null, flipX: false, resolved: false };
  }
  const spriteCount = tileset.spriteCount ?? GROUND_SPRITE_COUNT;
  if (spriteCount === 1) {
    return { spriteId: '0', flipX: false, resolved: true };
  }
  const rules = tileset.rules ?? getRulesForSpriteCount(spriteCount);
  let index = findRuleIndex(rules, mask, MatchPass.Direct);
  if (index >= 0) {
    return { spriteId: rules[index].spriteId, flipX: false, resolved: true };
  }
  index = findRuleIndex(rules, mask, MatchPass.FlipRows);
  if (index >= 0) {
    return { spriteId: rules[index].spriteId, flipX: true, resolved: true };
  }
  index = findRuleIndex(rules, mask, MatchPass.FlipColumns);
  if (index >= 0) {
    return { spriteId: rules[index].spriteId, flipX: true, resolved: true };
  }
  return {
    spriteId: getFallbackSpriteId(spriteCount),
    flipX: false,
    resolved: true,
  };
}

/**
 * @param {object[]} rules
 * @param {number[][]} mask
 * @returns {string[]}
 */
export function findMatchingSpriteIds(rules, mask) {
  const ids = [];
  if (!rules || !mask) {
    return ids;
  }
  for (const rule of rules) {
    const matched =
      ruleMatches(rule.pattern, mask, false)
      || ruleMatches(rule.pattern, mask, true)
      || ruleMatchesColumns(rule.pattern, mask, true);
    if (matched && !ids.includes(rule.spriteId)) {
      ids.push(rule.spriteId);
    }
  }
  return ids;
}

export { hashMask };
