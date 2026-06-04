#!/usr/bin/env node
// ASD Stop hook.
// Finalize state.json updated_at timestamp on session stop.
// Silent fail.

'use strict';

const fs = require('fs');
const path = require('path');

function findActiveStatePath() {
  const sprintsDir = path.join(process.cwd(), '.asd', 'sprints');
  if (!fs.existsSync(sprintsDir)) return null;
  const entries = fs.readdirSync(sprintsDir, { withFileTypes: true });
  for (const entry of entries) {
    if (!entry.isDirectory() || entry.name === 'archived') continue;
    const statePath = path.join(sprintsDir, entry.name, 'state.json');
    if (fs.existsSync(statePath)) return statePath;
  }
  return null;
}

(function main() {
  try {
    const statePath = findActiveStatePath();
    if (!statePath) {
      process.exit(0);
    }
    const state = JSON.parse(fs.readFileSync(statePath, 'utf8'));
    state.updated_at = new Date().toISOString();
    fs.writeFileSync(statePath, JSON.stringify(state, null, 2) + '\n', 'utf8');
    process.exit(0);
  } catch (_) {
    // silent fail
    process.exit(0);
  }
})();
