#!/usr/bin/env node
/*
 * ASD framework updater. Invoked by the /asd-update skill.
 * Fetches the latest framework files from the configured repo's branch and
 * replaces only the managed paths, leaving consumer-owned files untouched.
 *
 * Safety order: fetch + extract + validate FIRST, then delete-by-LOCAL-manifest,
 * then copy-by-NEW-manifest. A failed/partial download mutates nothing.
 *
 * Run from the consumer project root:  node .claude/skills/asd-update/update.js [--dry-run]
 * No npm deps. Requires Node >= 16.7 (fs.cpSync) and `tar` on PATH (Win10+/macOS/Linux ship it).
 */
'use strict';
const fs = require('fs');
const path = require('path');
const os = require('os');
const https = require('https');
const { execFileSync } = require('child_process');

const ROOT = process.cwd();
const DRY = process.argv.includes('--dry-run');
const LOCAL_MANIFEST = path.join(ROOT, '.asd', 'update-manifest.json');
const log = (m) => process.stdout.write(m + '\n');
const die = (m) => { process.stderr.write('asd-update: ' + m + '\n'); process.exit(1); };

function parseRepo(url) {
  const m = String(url).replace(/\.git$/, '').match(/github\.com[/:]([^/]+)\/([^/]+)/);
  if (!m) die(`cannot parse owner/repo from repo URL: ${url}`);
  return { owner: m[1], name: m[2] };
}

function get(url, opts, cb) {
  https.get(url, { headers: { 'User-Agent': 'asd-update' }, ...opts }, (res) => {
    if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
      res.resume();
      return get(res.headers.location, opts, cb);
    }
    cb(res);
  }).on('error', (e) => die(`network error: ${e.message}`));
}

function download(url, dest) {
  return new Promise((resolve) => {
    get(url, {}, (res) => {
      if (res.statusCode !== 200) die(`HTTP ${res.statusCode} fetching ${url}`);
      const f = fs.createWriteStream(dest);
      res.pipe(f);
      f.on('finish', () => f.close(() => resolve()));
      f.on('error', (e) => die(`write error: ${e.message}`));
    });
  });
}

function checkTar() {
  try { execFileSync('tar', ['--version'], { stdio: 'ignore' }); }
  catch { die('`tar` not found on PATH. Install tar (bundled with Win10 1803+, macOS, Linux) and retry.'); }
}

async function main() {
  if (!fs.existsSync(LOCAL_MANIFEST)) die(`no .asd/update-manifest.json in ${ROOT}. Run from the project root.`);
  checkTar();

  const local = JSON.parse(fs.readFileSync(LOCAL_MANIFEST, 'utf8'));
  const { owner, name } = parseRepo(local.repo);
  const branch = local.branch || 'main';
  const tarUrl = `https://codeload.github.com/${owner}/${name}/tar.gz/refs/heads/${branch}`;

  const work = fs.mkdtempSync(path.join(os.tmpdir(), 'asd-update-'));
  const exdir = path.join(work, 'x');
  fs.mkdirSync(exdir);

  log(`Fetching ${owner}/${name}@${branch} ...`);
  await download(tarUrl, path.join(work, 'src.tar.gz'));
  // Run tar with cwd + relative names: a Windows path like C:\...\src.tar.gz makes
  // GNU tar treat the drive-letter colon as a remote host. Relative names avoid that.
  execFileSync('tar', ['-xzf', 'src.tar.gz', '-C', 'x'], { cwd: work });

  // GitHub tarball extracts to a single top dir: <name>-<branch>/
  const top = fs.readdirSync(exdir).filter((d) => fs.statSync(path.join(exdir, d)).isDirectory());
  if (top.length !== 1) die(`unexpected tarball layout (${top.length} top dirs)`);
  const SRC = path.join(exdir, top[0]);

  // New manifest from the freshly fetched source = authoritative copy list.
  const newManifestPath = path.join(SRC, '.asd', 'update-manifest.json');
  if (!fs.existsSync(newManifestPath)) die('fetched source has no .asd/update-manifest.json — aborting, nothing changed.');
  const remote = JSON.parse(fs.readFileSync(newManifestPath, 'utf8'));
  const newList = [...(remote.managed.trees || []), ...(remote.managed.paths || [])];

  // Pre-validate: every new managed path must exist in the fetched source
  // BEFORE we delete anything locally. Abort cleanly otherwise.
  const missing = newList.filter((p) => !fs.existsSync(path.join(SRC, p)));
  if (missing.length) die(`fetched source missing managed paths, aborting:\n  ${missing.join('\n  ')}`);

  const oldList = [...(local.managed.trees || []), ...(local.managed.paths || [])];

  log(`Version: ${local.version || '?'} -> ${remote.version || '?'}${DRY ? '  (dry run)' : ''}`);
  log(`Will remove ${oldList.length} managed path(s), copy ${newList.length}.`);

  if (DRY) {
    log('\n[dry-run] would delete (local list):');
    oldList.forEach((p) => log('  - ' + p));
    log('[dry-run] would copy (new list):');
    newList.forEach((p) => log('  + ' + p));
    cleanup(work);
    return;
  }

  // DELETE by LOCAL list (handles paths removed upstream: present in old, absent in new -> stay deleted).
  for (const p of oldList) fs.rmSync(path.join(ROOT, p), { recursive: true, force: true });

  // COPY by NEW list.
  let copied = 0;
  for (const p of newList) {
    const dest = path.join(ROOT, p);
    fs.mkdirSync(path.dirname(dest), { recursive: true });
    fs.cpSync(path.join(SRC, p), dest, { recursive: true });
    copied++;
  }

  // Refresh the local manifest itself (lives at .asd/, outside managed trees/paths).
  fs.copyFileSync(newManifestPath, LOCAL_MANIFEST);

  cleanup(work);
  log(`\nUpdated ${copied} managed path(s) to version ${remote.version || '?'}.`);
  log('Review `.claude/settings.json` manually — it is NOT auto-updated (consumer permissions + hook registration live there).');
}

function cleanup(dir) {
  try { fs.rmSync(dir, { recursive: true, force: true }); } catch { /* best effort */ }
}

main().catch((e) => die(e && e.message ? e.message : String(e)));
