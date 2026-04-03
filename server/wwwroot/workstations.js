/* ── CyberClub Admin – workstations.js ─────────────────────────────────── */
'use strict';

function adminKey() {
  return document.getElementById('admin-key-input').value.trim();
}

function apiFetch(url, opts = {}) {
  const headers = { 'Content-Type': 'application/json', ...(opts.headers || {}) };
  const k = adminKey();
  if (k) headers['X-Admin-Key'] = k;
  return fetch(url, { ...opts, headers });
}

function fmtDate(dt) {
  if (!dt) return '—';
  return new Date(dt).toLocaleString();
}

function stateBadge(ws) {
  if (!ws.isOnline) return '<span class="badge badge-offline">Offline</span>';
  if (ws.state === 'Locked') return '<span class="badge badge-locked">Locked</span>';
  return '<span class="badge badge-online">Online</span>';
}

async function sendCommand(wsId, cmd, btn) {
  btn.disabled = true;
  try {
    const res = await apiFetch(`/api/admin/workstations/${wsId}/commands`, {
      method: 'POST',
      body: JSON.stringify({ command: cmd, issuedBy: 'WebAdmin', notes: '' }),
    });
    btn.textContent = res.ok ? '✓' : '✗';
  } finally {
    setTimeout(() => { btn.disabled = false; }, 1500);
  }
}

async function loadWorkstations() {
  const tbody = document.getElementById('ws-tbody');
  try {
    const res = await apiFetch('/api/admin/workstations');
    if (!res.ok) { tbody.innerHTML = `<tr><td colspan="6" style="color:#ef4444">Error: ${res.status}</td></tr>`; return; }
    const list = await res.json();
    if (!list.length) { tbody.innerHTML = '<tr><td colspan="6" style="text-align:center;color:var(--muted)">No workstations registered</td></tr>'; return; }

    tbody.innerHTML = '';
    list.forEach(ws => {
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${ws.name}</td>
        <td>${stateBadge(ws)}</td>
        <td>${fmtDate(ws.lastSeen)}</td>
        <td>${ws.ipAddress || '—'}</td>
        <td>${ws.agentVersion || '—'}</td>
        <td>
          <button class="btn btn-sm btn-secondary" data-cmd="Lock">🔒</button>
          <button class="btn btn-sm btn-secondary" data-cmd="Unlock">🔓</button>
          <button class="btn btn-sm btn-secondary" data-cmd="Reboot">🔄</button>
          <button class="btn btn-sm btn-secondary" data-cmd="Shutdown">⏻</button>
        </td>
      `;
      tr.querySelectorAll('[data-cmd]').forEach(btn => {
        btn.addEventListener('click', () => sendCommand(ws.id, btn.dataset.cmd, btn));
      });
      tbody.appendChild(tr);
    });
  } catch (e) {
    tbody.innerHTML = `<tr><td colspan="6" style="color:#ef4444">${e.message}</td></tr>`;
  }
}

document.getElementById('btn-refresh').addEventListener('click', loadWorkstations);
