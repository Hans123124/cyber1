/* ── CyberClub Admin – api.js ────────────────────────────────────────────── */
// Shared helpers. Imported as an ES module by map, config, and workstations.

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

/** Escape a string for safe insertion into HTML. */
export function escHtml(s) {
  return String(s)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}
