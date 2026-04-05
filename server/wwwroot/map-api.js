/* ── CyberClub Admin Map – map-api.js ────────────────────────────────────── */
// All API interactions for the map editor: load, save, delete, poll.

import { state, setStatus }                 from './map-state.js';
import { requestDraw }                       from './map-render.js';
import { populateWsSelect, renderZoneList,
         hideSelectedPanel }                 from './map-ui.js';
import { apiFetch }                          from './api.js';

// ── Load initial data ─────────────────────────────────────────────────────────
export async function loadAll() {
  setStatus('Loading…');
  try {
    const [layoutRes, wsRes] = await Promise.all([
      apiFetch('/api/admin/map'),
      apiFetch('/api/admin/workstations'),
    ]);
    if (!layoutRes.ok) throw new Error(`Failed to load map layout (${layoutRes.status})`);
    const layout = await layoutRes.json();

    state.layoutId = layout.id;
    state.gridSize = layout.gridSize;
    state.mapW     = layout.width;
    state.mapH     = layout.height;
    state.items    = layout.items || [];
    state.zones    = layout.zones || [];

    document.getElementById('cfg-grid-size').value = state.gridSize;
    document.getElementById('cfg-width').value      = state.mapW;
    document.getElementById('cfg-height').value     = state.mapH;

    if (wsRes.ok) {
      state.workstations = await wsRes.json();
      populateWsSelect();
    }
    renderZoneList();
    requestDraw();
    setStatus(`Loaded layout "${layout.name}" – ${state.items.length} items, ${state.zones.length} zones`);
  } catch (e) {
    setStatus('Error: ' + e.message);
  }
}

// ── Save / update a single item ───────────────────────────────────────────────
export async function saveItem(item) {
  const res = await apiFetch(`/api/admin/map/items/${item.id}`, {
    method: 'PUT',
    body:   JSON.stringify(item),
  });
  setStatus(res.ok ? 'Item saved' : `Error saving item (${res.status})`);
}

export async function saveItemPosition(item) {
  await saveItem(item);
}

// ── Delete an item ────────────────────────────────────────────────────────────
export async function deleteItem(item) {
  const res = await apiFetch(`/api/admin/map/items/${item.id}`, { method: 'DELETE' });
  if (res.ok) {
    state.items    = state.items.filter(i => i.id !== item.id);
    state.selectedId = null;
    hideSelectedPanel();
    requestDraw();
    setStatus(`Deleted ${item.type}`);
  }
}

// ── Create a new zone from a drag-draw ───────────────────────────────────────
export async function createZoneFromDraw(x, y, w, h) {
  if (!state.layoutId) return;
  const name  = document.getElementById('zone-name').value.trim() || 'Zone';
  const color = document.getElementById('zone-color').value || '#3b82f6';

  const res = await apiFetch('/api/admin/map/zones', {
    method: 'POST',
    body:   JSON.stringify({ layoutId: state.layoutId, name, color, x, y, w, h, metaJson: null }),
  });
  if (res.ok) {
    state.zones.push(await res.json());
    renderZoneList();
    requestDraw();
    setStatus(`Zone "${name}" created`);
  } else {
    setStatus('Error creating zone');
  }
}

// ── Delete a zone ─────────────────────────────────────────────────────────────
export async function deleteZone(id) {
  const res = await apiFetch(`/api/admin/map/zones/${id}`, { method: 'DELETE' });
  if (res.ok) {
    state.zones = state.zones.filter(z => z.id !== id);
    renderZoneList();
    requestDraw();
  }
}

// ── Send a command to a workstation ──────────────────────────────────────────
export async function sendWsCommand(wsId, action) {
  const cmdMap = { lock: 'Lock', unlock: 'Unlock', reboot: 'Reboot', shutdown: 'Shutdown' };
  const cmd    = cmdMap[action];
  if (!cmd) return;
  const res = await apiFetch(`/api/admin/workstations/${wsId}/commands`, {
    method: 'POST',
    body:   JSON.stringify({ command: cmd, issuedBy: 'MapUI', notes: '' }),
  });
  setStatus(res.ok ? `Command ${cmd} sent` : `Error sending ${cmd}`);
}

// ── Poll workstation statuses ─────────────────────────────────────────────────
export async function pollStatuses() {
  try {
    const res = await apiFetch('/api/admin/workstations');
    if (res.ok) {
      state.workstations = await res.json();
      populateWsSelect();  // refresh dropdown in case new stations registered
      requestDraw();
    }
  } catch (_) {}
}

// ── Save-all (top-bar button) ─────────────────────────────────────────────────
export async function saveAll() {
  setStatus('Saving…');
  await Promise.all(state.items.map(item =>
    apiFetch(`/api/admin/map/items/${item.id}`, {
      method: 'PUT',
      body:   JSON.stringify(item),
    })
  ));
  setStatus('Saved ✓');
}

// ── Apply layout settings ─────────────────────────────────────────────────────
export async function applyMapSettings() {
  if (!state.layoutId) return;
  const newGrid = parseInt(document.getElementById('cfg-grid-size').value, 10);
  const newW    = parseInt(document.getElementById('cfg-width').value,     10);
  const newH    = parseInt(document.getElementById('cfg-height').value,    10);
  const res = await apiFetch('/api/admin/map', {
    method: 'PUT',
    body:   JSON.stringify({ name: 'Main Hall', width: newW, height: newH, gridSize: newGrid }),
  });
  if (res.ok) {
    const layout   = await res.json();
    state.gridSize = layout.gridSize;
    state.mapW     = layout.width;
    state.mapH     = layout.height;
    requestDraw();
    setStatus('Map settings updated');
  }
}
