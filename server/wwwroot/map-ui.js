/* ── CyberClub Admin Map – map-ui.js ─────────────────────────────────────── */
// UI helpers: right-panel tabs, selected-item panel, palette, zone list.

import { state, ELEMENT_TYPES } from './map-state.js';
import { requestDraw } from './map-render.js';

// ── Right-panel tab switching ─────────────────────────────────────────────────
const _panel = document.querySelector('.right-panel');

export function activateTab(tabId) {
  _panel.querySelectorAll('.tab-btn').forEach(
    b => b.classList.toggle('active', b.dataset.tab === tabId));
  _panel.querySelectorAll('.tab-content').forEach(
    c => c.classList.toggle('active', c.id === 'tab-' + tabId));
  try { sessionStorage.setItem('map-active-tab', tabId); } catch (_) {}
}

// Restore persisted tab on load (default: settings)
(function restoreTab() {
  const saved = sessionStorage.getItem('map-active-tab') || 'settings';
  activateTab(saved);
})();

// Wire tab buttons
_panel.querySelectorAll('.tab-btn').forEach(btn => {
  btn.addEventListener('click', () => activateTab(btn.dataset.tab));
});

// ── Selected item panel ───────────────────────────────────────────────────────
const _selectedPanel = document.getElementById('selected-panel');

export function showSelectedPanel(item) {
  _selectedPanel.style.display = 'block';
  document.getElementById('sel-label').value        = item.label || '';
  document.getElementById('sel-rotation').value     = String(item.rotation || 0);
  document.getElementById('sel-workstation').value  = item.workstationId || '';
  state.isDirty = false;

  // Only switch to Elements tab if we're on a tab that hides the panel
  const current = sessionStorage.getItem('map-active-tab') || 'settings';
  if (current === 'settings' || current === 'zones') activateTab('elements');
}

export function hideSelectedPanel() {
  _selectedPanel.style.display = 'none';
  state.isDirty = false;
}

// Track dirty state on form inputs
['sel-label', 'sel-rotation', 'sel-workstation'].forEach(id => {
  document.getElementById(id).addEventListener('input', () => { state.isDirty = true; });
});

// ── Palette ───────────────────────────────────────────────────────────────────
export function buildPalette() {
  const grid = document.getElementById('element-palette');
  ELEMENT_TYPES.forEach(def => {
    const tile = document.createElement('div');
    tile.className      = 'el-tile';
    tile.draggable      = true;
    tile.innerHTML      = `<span class="icon">${def.icon}</span>${def.label}`;
    tile.dataset.type   = def.type;
    tile.addEventListener('dragstart', de => {
      de.dataTransfer.setData('application/cybermap', def.type);
    });
    grid.appendChild(tile);
  });
}

// ── Zone list ─────────────────────────────────────────────────────────────────
export function renderZoneList() {
  const ul = document.getElementById('zone-list');
  ul.innerHTML = '';
  state.zones.forEach(zone => {
    const li  = document.createElement('li');
    const dot = document.createElement('span');
    dot.className        = 'zone-dot';
    dot.style.background = zone.color;
    const name = document.createElement('span');
    name.className   = 'zone-name';
    name.textContent = zone.name;
    const del = document.createElement('button');
    del.className   = 'zone-del';
    del.textContent = '✕';
    del.onclick     = () => import('./map-api.js').then(m => m.deleteZone(zone.id));
    li.append(dot, name, del);
    ul.appendChild(li);
  });
}

// ── Populate workstation <select> ─────────────────────────────────────────────
export function populateWsSelect() {
  const sel = document.getElementById('sel-workstation');
  const cur = sel.value;
  sel.innerHTML = '<option value="">— none —</option>';
  state.workstations.forEach(ws => {
    const opt = document.createElement('option');
    opt.value       = ws.id;
    opt.textContent = ws.name;
    sel.appendChild(opt);
  });
  // Restore selection if still valid
  if (cur && state.workstations.some(w => w.id === cur)) sel.value = cur;
}

// ── Unsaved changes warning ───────────────────────────────────────────────────
window.addEventListener('beforeunload', e => {
  if (state.isDirty) {
    e.preventDefault();
    e.returnValue = 'You have unsaved property changes. Leave anyway?';
  }
});
