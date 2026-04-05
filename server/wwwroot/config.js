/* ── CyberClub Admin – config.js ────────────────────────────────────────── */

import { apiFetch } from './api.js';

// ── Load settings ─────────────────────────────────────────────────────────────
async function loadSettings() {
  try {
    const res = await apiFetch('/api/admin/settings');
    if (!res.ok) { console.warn('Could not load settings, status', res.status); return; }
    const s = await res.json();
    document.getElementById('idle-seconds').value           = s.shutdownIdlePcSeconds;
    document.getElementById('auto-restart-enabled').checked = s.autoRestartEnabled;
    document.getElementById('auto-restart-seconds').value   = s.autoRestartAfterSessionSeconds;
    document.getElementById('show-gamer-name').checked      = s.showGamerNameOnMap;
    document.getElementById('action-menu-mode').value       = s.actionMenuMode;
    syncRestartRow();
  } catch (e) {
    console.error('loadSettings error', e);
  }
}

// ── Toggle auto-restart row visibility ───────────────────────────────────────
function syncRestartRow() {
  const enabled = document.getElementById('auto-restart-enabled').checked;
  document.getElementById('auto-restart-seconds-row').style.display = enabled ? 'flex' : 'none';
}

document.getElementById('auto-restart-enabled').addEventListener('change', syncRestartRow);

// ── Save settings ─────────────────────────────────────────────────────────────
document.getElementById('btn-save-config').addEventListener('click', async () => {
  const body = {
    shutdownIdlePcSeconds:          parseInt(document.getElementById('idle-seconds').value, 10) || 0,
    autoRestartAfterSessionSeconds: parseInt(document.getElementById('auto-restart-seconds').value, 10) || 0,
    autoRestartEnabled:             document.getElementById('auto-restart-enabled').checked,
    showGamerNameOnMap:             document.getElementById('show-gamer-name').checked,
    actionMenuMode:                 document.getElementById('action-menu-mode').value,
  };

  const status = document.getElementById('save-status');
  try {
    const res = await apiFetch('/api/admin/settings', {
      method: 'PUT',
      body: JSON.stringify(body),
    });
    if (res.ok) {
      status.textContent = '✓ Saved';
      status.style.color = '#22c55e';
    } else {
      status.textContent = `Error: ${res.status}`;
      status.style.color = '#ef4444';
    }
  } catch (e) {
    status.textContent = 'Network error';
    status.style.color = '#ef4444';
  }
  setTimeout(() => { status.textContent = ''; }, 3000);
});

// ── Init ──────────────────────────────────────────────────────────────────────
loadSettings();
