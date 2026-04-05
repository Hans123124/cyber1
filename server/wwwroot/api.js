/* ── CyberClub Admin – api.js ────────────────────────────────────────────── */
// Shared API fetch helper. Imported as an ES module by map, config, and
// workstations modules. Also exported as window globals for pages that still
// load scripts in classic (non-module) mode.

export function adminKey() {
  const el = document.getElementById('admin-key-input');
  return el ? el.value.trim() : '';
}

export function apiFetch(url, opts = {}) {
  const headers = { 'Content-Type': 'application/json', ...(opts.headers || {}) };
  const k = adminKey();
  if (k) headers['X-Admin-Key'] = k;
  return fetch(url, { ...opts, headers });
}
