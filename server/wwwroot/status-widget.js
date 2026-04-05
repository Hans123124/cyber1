/* ── CyberClub Admin – status-widget.js ─────────────────────────────────── */
// Self-contained status widget module.  Creates and injects its own DOM,
// then polls /api/account/me + clubs + tariffs every 10 s.
// Include on any admin page via <script type="module" src="status-widget.js">.

import { escHtml } from './api.js';

// ── Build DOM ─────────────────────────────────────────────────────────────────
const widget = document.createElement('div');
widget.id = 'sw';
widget.innerHTML = `
  <div id="sw-header">
    <span id="sw-title">Status</span>
    <span id="sw-health" title="API health">OK</span>
    <span id="sw-toggle" title="Toggle panel">▾</span>
  </div>
  <div id="sw-body">
    <div id="sw-user">
      <div id="sw-avatar">?</div>
      <div id="sw-user-info">
        <div id="sw-email">Loading…</div>
        <div id="sw-roles"></div>
      </div>
    </div>
    <div id="sw-clubs">
      <label for="sw-club-sel">Club</label>
      <select id="sw-club-sel"><option value="">—</option></select>
    </div>
    <div id="sw-tariffs">
      <div id="sw-tariffs-title">Tariff Plans</div>
      <div id="sw-tariff-list"><span id="sw-empty">Select a club…</span></div>
    </div>
    <div id="sw-footer">
      <span id="sw-updated">—</span>
      <button class="sw-btn" id="sw-btn-refresh" title="Refresh now">↻ Refresh</button>
      <button class="sw-btn sw-btn-danger" id="sw-btn-logout" title="Sign out">→ Logout</button>
    </div>
  </div>
`;
document.body.prepend(widget);

// ── Element refs ──────────────────────────────────────────────────────────────
const elHealth   = document.getElementById('sw-health');
const elAvatar   = document.getElementById('sw-avatar');
const elEmail    = document.getElementById('sw-email');
const elRoles    = document.getElementById('sw-roles');
const elClubSel  = document.getElementById('sw-club-sel');
const elTariffs  = document.getElementById('sw-tariff-list');
const elUpdated  = document.getElementById('sw-updated');
const btnRefresh = document.getElementById('sw-btn-refresh');
const btnLogout  = document.getElementById('sw-btn-logout');
const swBody     = document.getElementById('sw-body');
const swToggle   = document.getElementById('sw-toggle');
const swHeader   = document.getElementById('sw-header');

let swCollapsed = false;
let swClubs     = [];

// ── Collapse / Expand ─────────────────────────────────────────────────────────
swHeader.addEventListener('click', () => {
  swCollapsed = !swCollapsed;
  swBody.classList.toggle('collapsed', swCollapsed);
  swToggle.textContent = swCollapsed ? '▸' : '▾';
});

// ── Utilities ─────────────────────────────────────────────────────────────────
async function safeJson(r) {
  try { return await r.json(); } catch { return null; }
}

function setHealth(ok, code) {
  if (ok) {
    elHealth.textContent = 'OK';
    elHealth.classList.remove('err');
  } else {
    elHealth.textContent = code ? String(code) : 'ERR';
    elHealth.classList.add('err');
  }
}

// ── Load current user ─────────────────────────────────────────────────────────
async function loadMe() {
  const r = await fetch('/api/account/me');
  setHealth(r.ok, r.status);
  if (r.status === 401) {
    elEmail.textContent = 'Not authenticated';
    setTimeout(() => { location.href = '/login'; }, 400);
    return;
  }
  const me = await safeJson(r);
  if (!me) { elEmail.textContent = 'No user data'; return; }

  const display = me.email || me.userName || me.username || '—';
  elEmail.textContent = display;
  elAvatar.textContent = display.charAt(0).toUpperCase();

  const roles = Array.isArray(me.roles) ? me.roles : [];
  elRoles.innerHTML = roles.length
    ? roles.map(ro => `<span class="sw-role">${escHtml(ro)}</span>`).join('')
    : '<span class="sw-role" style="opacity:.5;">—</span>';
}

// ── Load clubs ────────────────────────────────────────────────────────────────
async function loadClubs() {
  const r = await fetch('/api/admin/clubs');
  if (!r.ok) return;
  const data = await safeJson(r);
  swClubs = Array.isArray(data) ? data : [];

  const saved = localStorage.getItem('sw-club-id');
  elClubSel.innerHTML = swClubs
    .map(c => `<option value="${escHtml(c.id)}">${escHtml(c.name)}</option>`)
    .join('');

  if (saved && swClubs.some(c => String(c.id) === saved)) {
    elClubSel.value = saved;
  } else if (swClubs.length) {
    elClubSel.value = swClubs[0].id;
  }
}

// ── Load tariff plans for selected club ───────────────────────────────────────
async function loadTariffs() {
  const clubId = elClubSel.value;
  if (!clubId) {
    elTariffs.innerHTML = '<span id="sw-empty">Select a club…</span>';
    return;
  }
  const r = await fetch(`/api/admin/clubs/${encodeURIComponent(clubId)}/tariff-plans`);
  if (!r.ok) {
    elTariffs.innerHTML = `<span style="color:var(--danger)">Error ${r.status}</span>`;
    return;
  }
  localStorage.setItem('sw-club-id', clubId);
  const data = await safeJson(r);
  if (!Array.isArray(data) || !data.length) {
    elTariffs.innerHTML = '<span id="sw-empty">No tariff plans</span>';
    return;
  }

  elTariffs.innerHTML = data.map(t => {
    const rate   = t.hourlyRateMdl ?? t.hourlyRate ?? t.price ?? '—';
    const active = t.isActive === false ? 'inactive' : 'active';
    return `<div class="sw-tariff-row">
      <span class="sw-tariff-name">${escHtml(t.name || '—')}</span>
      <span class="sw-tariff-rate">${escHtml(String(rate))} MDL/h</span>
      <span class="sw-tariff-badge ${escHtml(active)}">${escHtml(active)}</span>
    </div>`;
  }).join('');
}

// ── Full refresh ──────────────────────────────────────────────────────────────
async function refreshAll() {
  try {
    await loadMe();
    await loadClubs();
    await loadTariffs();
    elUpdated.textContent = `Updated ${new Date().toLocaleTimeString()}`;
  } catch (e) {
    elUpdated.textContent = `Error: ${e.message || e}`;
  }
}

// ── Events ────────────────────────────────────────────────────────────────────
btnRefresh.addEventListener('click', refreshAll);
elClubSel.addEventListener('change', loadTariffs);

btnLogout.addEventListener('click', async () => {
  try { await fetch('/api/account/logout', { method: 'POST' }); } catch (_) { /* ignore */ }
  location.href = '/login';
});

// Pause polling when tab is hidden, resume when visible
document.addEventListener('visibilitychange', () => {
  if (!document.hidden) refreshAll();
});

// ── Bootstrap ─────────────────────────────────────────────────────────────────
refreshAll();
setInterval(refreshAll, 10000);
