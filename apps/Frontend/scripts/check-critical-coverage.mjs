#!/usr/bin/env node
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const CRITICAL_PREFIXES = [
  'src/app/core/auth/',
  'src/app/core/utils/',
  'src/app/core/strategies/',
  'src/app/core/router/',
  'src/app/shared/pipes/',
];

const METRIC_FLOORS = {
  statements: 100,
  functions: 100,
  lines: 100,
  branches: 100,
};

const BRANCH_RELAXED_PATTERNS = [/shared\/pipes\/.*\.ts$/];

const RELAXED_BRANCH_FLOOR = 0;

const projectRoot = resolve(import.meta.dirname, '..');
const coverageJsonPath = resolve(projectRoot, 'coverage', 'Frontend', 'coverage-final.json');

function loadCoverage() {
  try {
    return JSON.parse(readFileSync(coverageJsonPath, 'utf8'));
  } catch (error) {
    console.error(`Could not read ${coverageJsonPath}.`);
    console.error('Run `node ./node_modules/.bin/ng test --watch=false --coverage` first.');
    console.error(`Underlying error: ${error.message}`);
    process.exit(2);
  }
}

function isCritical(relativePath) {
  return CRITICAL_PREFIXES.some((prefix) => relativePath.startsWith(prefix));
}

function relaxedBranchOk(relativePath, branchPct) {
  return (
    BRANCH_RELAXED_PATTERNS.some((pattern) => pattern.test(relativePath)) &&
    branchPct >= RELAXED_BRANCH_FLOOR
  );
}

function summariseFile(fileData) {
  const statements = Object.values(fileData.s);
  const functions = Object.values(fileData.f);
  const branchArrays = Object.values(fileData.b);
  const branchHits = branchArrays.flat();
  const lineMap = fileData.lineMap ?? {};

  const lineHits = new Map();
  for (const [statementId, hitCount] of Object.entries(fileData.s)) {
    const startLine = fileData.statementMap[statementId]?.start?.line;
    if (startLine === undefined) {
      continue;
    }
    const previous = lineHits.get(startLine) ?? 0;
    lineHits.set(startLine, previous + hitCount);
  }
  const lineTotal = lineHits.size;
  const lineCovered = [...lineHits.values()].filter((hits) => hits > 0).length;

  return {
    statementPct: percent(statements.filter((hit) => hit > 0).length, statements.length),
    functionPct: percent(functions.filter((hit) => hit > 0).length, functions.length),
    branchPct: percent(branchHits.filter((hit) => hit > 0).length, branchHits.length),
    linePct: percent(lineCovered, lineTotal),
  };
}

function percent(numerator, denominator) {
  if (denominator === 0) {
    return 100;
  }
  return (numerator / denominator) * 100;
}

function relativeFromAbsolute(absolutePath) {
  return absolutePath.startsWith(projectRoot)
    ? absolutePath.slice(projectRoot.length + 1)
    : absolutePath;
}

const coverage = loadCoverage();

const failures = [];
const summaries = [];

for (const [absolutePath, fileData] of Object.entries(coverage)) {
  const relativePath = relativeFromAbsolute(absolutePath);
  if (!isCritical(relativePath)) {
    continue;
  }
  const stats = summariseFile(fileData);
  summaries.push({ relativePath, stats });

  const gaps = [];
  if (stats.statementPct < METRIC_FLOORS.statements) {
    gaps.push(`statements ${stats.statementPct.toFixed(2)}%`);
  }
  if (stats.functionPct < METRIC_FLOORS.functions) {
    gaps.push(`functions ${stats.functionPct.toFixed(2)}%`);
  }
  if (stats.linePct < METRIC_FLOORS.lines) {
    gaps.push(`lines ${stats.linePct.toFixed(2)}%`);
  }
  if (stats.branchPct < METRIC_FLOORS.branches && !relaxedBranchOk(relativePath, stats.branchPct)) {
    gaps.push(`branches ${stats.branchPct.toFixed(2)}%`);
  }
  if (gaps.length > 0) {
    failures.push({ relativePath, gaps });
  }
}

summaries.sort((left, right) => left.relativePath.localeCompare(right.relativePath));
for (const { relativePath, stats } of summaries) {
  const flag =
    stats.statementPct === 100 &&
    stats.functionPct === 100 &&
    stats.linePct === 100 &&
    (stats.branchPct === 100 || relaxedBranchOk(relativePath, stats.branchPct))
      ? 'ok'
      : 'GAP';
  console.log(
    `[${flag}] ${relativePath}  ` +
      `S=${stats.statementPct.toFixed(2)}%  ` +
      `B=${stats.branchPct.toFixed(2)}%  ` +
      `F=${stats.functionPct.toFixed(2)}%  ` +
      `L=${stats.linePct.toFixed(2)}%`,
  );
}

if (failures.length > 0) {
  console.error('\nCritical coverage gate FAILED:');
  for (const { relativePath, gaps } of failures) {
    console.error(`  - ${relativePath}: ${gaps.join(', ')}`);
  }
  console.error(`\n${failures.length} file(s) below the 100% floor for critical paths.`);
  process.exit(1);
}

console.log('\nCritical coverage gate passed.');
