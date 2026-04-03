/* ── CyberClub Admin Map – map.js ─────────────────────────────────────────── */
'use strict';

// ── Element palette definition ────────────────────────────────────────────────
const ELEMENT_TYPES = [
  { type: 'Pc',          icon: '🖥',  label: 'PC',       color: '#4f46e5', w: 1, h: 1 },
  { type: 'Console',     icon: '🎮',  label: 'Console',  color: '#7c3aed', w: 1, h: 1 },
  { type: 'Wall',        icon: '▬',   label: 'Wall',     color: '#475569', w: 2, h: 1 },
  { type: 'Corner',      icon: '◣',   label: 'Corner',   color: '#475569', w: 1, h: 1 },
  { type: 'WallT',       icon: '⊤',   label: 'Wall T',   color: '#475569', w: 1, h: 1 },
  { type: 'Triangle',    icon: '△',   label: 'Triangle', color: '#475569', w: 1, h: 1 },
  { type: 'Decoration',  icon: '🪴',  label: 'Decor',    color: '#15803d', w: 1, h: 1 },
  { type: 'Desk',        icon: '🗄',  label: 'Desk',     color: '#92400e', w: 2, h: 1 },
  { type: 'Chair',       icon: '🪑',  label: 'Chair',    color: '#78350f', w: 1, h: 1 },
];

// ── State ─────────────────────────────────────────────────────────────────────
let layoutId   = null;
let gridSize   = 40;
let mapW       = 1200;
let mapH       = 800;
let scale      = 1;
let items      = [];   // MapItem[]
let zones      = [];   // Zone[]
let workstations = []; // Workstation[] from /api/admin/workstations

let selectedId    = null;
let draggingId    = null;
let dragOffX      = 0;
let dragOffY      = 0;
let isDrawingZone = false;
let zoneDrawStart = null;
let zoneDrawEnd   = null;

let paletteGhost  = null;  // { type, w, h } when dragging from palette

// ── Canvas setup ──────────────────────────────────────────────────────────────
const canvas    = document.getElementById('map-canvas');
const ctx       = canvas.getContext('2d');
const wrapper   = document.getElementById('canvas-wrapper');
const statusBar = document.getElementById('status-bar');

function adminKey() {
  return document.getElementById('admin-key-input').value.trim();
}

function apiFetch(url, opts = {}) {
  const headers = { 'Content-Type': 'application/json', ...(opts.headers || {}) };
  const k = adminKey();
  if (k) headers['X-Admin-Key'] = k;
  return fetch(url, { ...opts, headers });
}

function setStatus(msg) { statusBar.textContent = msg; }

// ── Load initial data ─────────────────────────────────────────────────────────
async function loadAll() {
  setStatus('Loading…');
  try {
    const [layoutRes, wsRes] = await Promise.all([
      apiFetch('/api/admin/map'),
      apiFetch('/api/admin/workstations'),
    ]);
    if (!layoutRes.ok) throw new Error('Failed to load map layout');
    const layout = await layoutRes.json();
    layoutId = layout.id;
    gridSize = layout.gridSize;
    mapW     = layout.width;
    mapH     = layout.height;
    items    = layout.items || [];
    zones    = layout.zones || [];

    document.getElementById('cfg-grid-size').value = gridSize;
    document.getElementById('cfg-width').value      = mapW;
    document.getElementById('cfg-height').value     = mapH;

    if (wsRes.ok) {
      workstations = await wsRes.json();
      populateWsSelect();
    }
    renderZoneList();
    redraw();
    setStatus(`Loaded layout "${layout.name}" – ${items.length} items, ${zones.length} zones`);
  } catch (e) {
    setStatus('Error: ' + e.message);
  }
}

function populateWsSelect() {
  const sel = document.getElementById('sel-workstation');
  sel.innerHTML = '<option value="">— none —</option>';
  workstations.forEach(ws => {
    const opt = document.createElement('option');
    opt.value = ws.id;
    opt.textContent = ws.name;
    sel.appendChild(opt);
  });
}

// ── Rendering ─────────────────────────────────────────────────────────────────
function resizeCanvas() {
  canvas.width  = mapW * scale;
  canvas.height = mapH * scale;
}

function redraw() {
  resizeCanvas();
  ctx.clearRect(0, 0, canvas.width, canvas.height);

  // Background
  ctx.fillStyle = '#0f1117';
  ctx.fillRect(0, 0, canvas.width, canvas.height);

  // Grid lines
  ctx.strokeStyle = '#1e2233';
  ctx.lineWidth   = 1;
  const gs = gridSize * scale;
  for (let x = 0; x <= mapW; x += gridSize) {
    ctx.beginPath(); ctx.moveTo(x * scale, 0); ctx.lineTo(x * scale, canvas.height); ctx.stroke();
  }
  for (let y = 0; y <= mapH; y += gridSize) {
    ctx.beginPath(); ctx.moveTo(0, y * scale); ctx.lineTo(canvas.width, y * scale); ctx.stroke();
  }

  // Draw zones
  zones.forEach(drawZone);

  // Draw zone being drawn
  if (isDrawingZone && zoneDrawStart && zoneDrawEnd) {
    const nx = Math.min(zoneDrawStart.x, zoneDrawEnd.x);
    const ny = Math.min(zoneDrawStart.y, zoneDrawEnd.y);
    const nw = Math.abs(zoneDrawEnd.x - zoneDrawStart.x) + 1;
    const nh = Math.abs(zoneDrawEnd.y - zoneDrawStart.y) + 1;
    ctx.fillStyle = 'rgba(99,102,241,0.25)';
    ctx.strokeStyle = '#6366f1';
    ctx.lineWidth = 2;
    ctx.fillRect(nx * gs, ny * gs, nw * gs, nh * gs);
    ctx.strokeRect(nx * gs, ny * gs, nw * gs, nh * gs);
  }

  // Draw items
  items.forEach(item => drawItem(item, item.id === selectedId));
}

function drawZone(zone) {
  const gs = gridSize * scale;
  ctx.globalAlpha = 0.18;
  ctx.fillStyle = zone.color || '#6366f1';
  ctx.fillRect(zone.x * gs, zone.y * gs, zone.w * gs, zone.h * gs);
  ctx.globalAlpha = 1;
  ctx.strokeStyle = zone.color || '#6366f1';
  ctx.lineWidth = 1.5;
  ctx.setLineDash([4, 4]);
  ctx.strokeRect(zone.x * gs, zone.y * gs, zone.w * gs, zone.h * gs);
  ctx.setLineDash([]);

  // Label
  ctx.fillStyle = zone.color || '#6366f1';
  ctx.font = `${Math.max(10, 11 * scale)}px Segoe UI, sans-serif`;
  ctx.textAlign = 'left';
  ctx.fillText(zone.name, zone.x * gs + 4, zone.y * gs + 14 * scale);
}

function drawItem(item, selected) {
  const gs   = gridSize * scale;
  const px   = item.x * gs;
  const py   = item.y * gs;
  const pw   = item.w * gs;
  const ph   = item.h * gs;
  const def  = ELEMENT_TYPES.find(e => e.type === item.type) || ELEMENT_TYPES[0];
  const ws   = workstations.find(w => w.id === item.workstationId);

  // Workstation state → color
  let bg = def.color;
  if (item.type === 'Pc' || item.type === 'Console') {
    if (!ws)                            bg = '#334155'; // not linked
    else if (!ws.isOnline)              bg = '#1e293b'; // offline
    else if (ws.state === 'Locked')     bg = '#7f1d1d'; // locked (red)
    else if (ws.state === 'Unlocked')   bg = '#14532d'; // in use (green)
    else                                bg = '#1e3a5f'; // other
  }

  ctx.save();
  ctx.translate(px + pw / 2, py + ph / 2);
  ctx.rotate((item.rotation * Math.PI) / 180);

  // Shadow for selected
  if (selected) {
    ctx.shadowColor = '#818cf8';
    ctx.shadowBlur  = 12;
  }

  // Tile background
  ctx.fillStyle = bg;
  roundRect(ctx, -pw / 2, -ph / 2, pw, ph, 5 * scale);
  ctx.fill();

  // Border
  ctx.strokeStyle = selected ? '#818cf8' : 'rgba(255,255,255,0.12)';
  ctx.lineWidth   = selected ? 2 : 1;
  roundRect(ctx, -pw / 2, -ph / 2, pw, ph, 5 * scale);
  ctx.stroke();

  ctx.shadowBlur = 0;

  // Icon (only if large enough)
  if (pw >= 24 * scale && ph >= 24 * scale) {
    ctx.font = `${Math.min(pw * 0.42, ph * 0.42, 24 * scale)}px Segoe UI Emoji, sans-serif`;
    ctx.textAlign    = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(def.icon, 0, ph >= 40 * scale ? -ph * 0.12 : 0);
  }

  // Label
  if (item.label && pw >= 20 * scale) {
    ctx.font         = `bold ${Math.min(11 * scale, pw * 0.3)}px Segoe UI, sans-serif`;
    ctx.fillStyle    = 'rgba(255,255,255,0.9)';
    ctx.textAlign    = 'center';
    ctx.textBaseline = 'alphabetic';
    ctx.fillText(item.label, 0, ph / 2 - 4 * scale);
  }

  // Workstation status indicator dot
  if (ws) {
    const dotR = 5 * scale;
    ctx.beginPath();
    ctx.arc(pw / 2 - dotR - 2 * scale, -ph / 2 + dotR + 2 * scale, dotR, 0, Math.PI * 2);
    ctx.fillStyle = ws.isOnline ? '#22c55e' : '#475569';
    ctx.fill();
  }

  ctx.restore();
}

function roundRect(ctx, x, y, w, h, r) {
  ctx.beginPath();
  ctx.moveTo(x + r, y);
  ctx.arcTo(x + w, y, x + w, y + h, r);
  ctx.arcTo(x + w, y + h, x, y + h, r);
  ctx.arcTo(x, y + h, x, y, r);
  ctx.arcTo(x, y, x + w, y, r);
  ctx.closePath();
}

// ── Hit-testing ───────────────────────────────────────────────────────────────
function getCanvasPos(e) {
  const rect = canvas.getBoundingClientRect();
  return {
    cx: (e.clientX - rect.left) / scale,
    cy: (e.clientY - rect.top)  / scale,
  };
}

function hitItem(cx, cy) {
  for (let i = items.length - 1; i >= 0; i--) {
    const it = items[i];
    const gs = gridSize;
    if (cx >= it.x * gs && cx < (it.x + it.w) * gs &&
        cy >= it.y * gs && cy < (it.y + it.h) * gs) {
      return it;
    }
  }
  return null;
}

// ── Canvas mouse events ───────────────────────────────────────────────────────
canvas.addEventListener('mousedown', e => {
  if (e.button !== 0) return;
  const { cx, cy } = getCanvasPos(e);

  if (isDrawingZone) {
    zoneDrawStart = { x: Math.floor(cx / gridSize), y: Math.floor(cy / gridSize) };
    zoneDrawEnd   = { ...zoneDrawStart };
    return;
  }

  const hit = hitItem(cx, cy);
  if (hit) {
    selectedId = hit.id;
    draggingId = hit.id;
    dragOffX   = cx - hit.x * gridSize;
    dragOffY   = cy - hit.y * gridSize;
    showSelectedPanel(hit);
    redraw();
  } else {
    selectedId = null;
    hideSelectedPanel();
    redraw();
  }
});

canvas.addEventListener('mousemove', e => {
  const { cx, cy } = getCanvasPos(e);

  if (isDrawingZone && zoneDrawStart) {
    zoneDrawEnd = { x: Math.floor(cx / gridSize), y: Math.floor(cy / gridSize) };
    redraw();
    return;
  }

  if (draggingId) {
    const item = items.find(i => i.id === draggingId);
    if (!item) return;
    item.x = Math.max(0, Math.min(Math.floor((cx - dragOffX) / gridSize), Math.floor(mapW / gridSize) - item.w));
    item.y = Math.max(0, Math.min(Math.floor((cy - dragOffY) / gridSize), Math.floor(mapH / gridSize) - item.h));
    redraw();
    setStatus(`${item.label || item.type} → (${item.x}, ${item.y})`);
  }
});

canvas.addEventListener('mouseup', async e => {
  if (e.button !== 0) return;
  const { cx, cy } = getCanvasPos(e);

  if (isDrawingZone && zoneDrawStart) {
    const x = Math.min(zoneDrawStart.x, zoneDrawEnd?.x ?? zoneDrawStart.x);
    const y = Math.min(zoneDrawStart.y, zoneDrawEnd?.y ?? zoneDrawStart.y);
    const w = Math.abs((zoneDrawEnd?.x ?? zoneDrawStart.x) - zoneDrawStart.x) + 1;
    const h = Math.abs((zoneDrawEnd?.y ?? zoneDrawStart.y) - zoneDrawStart.y) + 1;
    await createZoneFromDraw(x, y, w, h);
    isDrawingZone  = false;
    zoneDrawStart  = null;
    zoneDrawEnd    = null;
    canvas.style.cursor = 'crosshair';
    return;
  }

  if (draggingId) {
    const item = items.find(i => i.id === draggingId);
    if (item) await saveItemPosition(item);
    draggingId = null;
  }
});

canvas.addEventListener('mouseleave', () => { draggingId = null; });

// ── Right-click context menu ──────────────────────────────────────────────────
canvas.addEventListener('contextmenu', e => {
  e.preventDefault();
  const { cx, cy } = getCanvasPos(e);
  const hit = hitItem(cx, cy);
  if (!hit) { hideCtxMenu(); return; }
  selectedId = hit.id;
  showSelectedPanel(hit);
  redraw();
  showCtxMenu(e.clientX, e.clientY, hit);
});

const ctxMenu = document.getElementById('ctx-menu');

function showCtxMenu(x, y, item) {
  ctxMenu.style.left    = x + 'px';
  ctxMenu.style.top     = y + 'px';
  ctxMenu.style.display = 'block';
  ctxMenu.dataset.itemId = item.id;
}
function hideCtxMenu() { ctxMenu.style.display = 'none'; }
document.addEventListener('click', hideCtxMenu);

ctxMenu.querySelectorAll('.ctx-item').forEach(el => {
  el.addEventListener('click', async e => {
    e.stopPropagation();
    const action = el.dataset.action;
    const itemId = ctxMenu.dataset.itemId;
    const item   = items.find(i => i.id === itemId);
    if (!item) return;

    if (action === 'delete') {
      await deleteItem(item);
    } else if (item.workstationId) {
      await sendWsCommand(item.workstationId, action);
    }
    hideCtxMenu();
  });
});

async function sendWsCommand(wsId, action) {
  const cmdMap = { lock: 'Lock', unlock: 'Unlock', reboot: 'Reboot', shutdown: 'Shutdown' };
  const cmd    = cmdMap[action];
  if (!cmd) return;
  const res = await apiFetch(`/api/admin/workstations/${wsId}/commands`, {
    method: 'POST',
    body: JSON.stringify({ command: cmd, issuedBy: 'MapUI', notes: '' }),
  });
  setStatus(res.ok ? `Command ${cmd} sent` : `Error sending ${cmd}`);
}

// ── Palette drag ──────────────────────────────────────────────────────────────
function buildPalette() {
  const grid = document.getElementById('element-palette');
  ELEMENT_TYPES.forEach(def => {
    const tile = document.createElement('div');
    tile.className  = 'el-tile';
    tile.draggable  = true;
    tile.innerHTML  = `<span class="icon">${def.icon}</span>${def.label}`;
    tile.dataset.type = def.type;

    tile.addEventListener('dragstart', de => {
      de.dataTransfer.setData('application/cybermap', def.type);
      paletteGhost = def;
    });
    grid.appendChild(tile);
  });
}

canvas.addEventListener('dragover', e => { e.preventDefault(); });
canvas.addEventListener('drop', async e => {
  e.preventDefault();
  const type = e.dataTransfer.getData('application/cybermap');
  if (!type || !layoutId) return;
  const rect = canvas.getBoundingClientRect();
  const cx   = (e.clientX - rect.left) / scale;
  const cy   = (e.clientY - rect.top)  / scale;
  const gx   = Math.floor(cx / gridSize);
  const gy   = Math.floor(cy / gridSize);
  const def  = ELEMENT_TYPES.find(d => d.type === type) || { w: 1, h: 1 };

  const body = {
    layoutId,
    type,
    x: gx,
    y: gy,
    w: def.w || 1,
    h: def.h || 1,
    rotation: 0,
    label: '',
    workstationId: null,
    zoneId: null,
    metaJson: null,
  };

  const res  = await apiFetch('/api/admin/map/items', { method: 'POST', body: JSON.stringify(body) });
  if (res.ok) {
    const item = await res.json();
    items.push(item);
    selectedId = item.id;
    showSelectedPanel(item);
    redraw();
    setStatus(`Placed ${type}`);
  } else {
    setStatus('Error placing element');
  }
});

// ── Selected item panel ───────────────────────────────────────────────────────
function showSelectedPanel(item) {
  const panel = document.getElementById('selected-panel');
  panel.style.display = 'block';
  document.getElementById('sel-label').value      = item.label || '';
  document.getElementById('sel-rotation').value   = String(item.rotation || 0);
  document.getElementById('sel-workstation').value = item.workstationId || '';
  // Switch to Elements tab
  activateTab('elements');
}
function hideSelectedPanel() {
  document.getElementById('selected-panel').style.display = 'none';
}

document.getElementById('btn-sel-save').addEventListener('click', async () => {
  const item = items.find(i => i.id === selectedId);
  if (!item) return;
  item.label        = document.getElementById('sel-label').value;
  item.rotation     = parseInt(document.getElementById('sel-rotation').value, 10);
  item.workstationId = document.getElementById('sel-workstation').value || null;
  await saveItem(item);
  redraw();
});

document.getElementById('btn-sel-delete').addEventListener('click', async () => {
  const item = items.find(i => i.id === selectedId);
  if (!item) return;
  if (!confirm(`Delete "${item.label || item.type}"?`)) return;
  await deleteItem(item);
});

async function saveItem(item) {
  const res = await apiFetch(`/api/admin/map/items/${item.id}`, {
    method: 'PUT',
    body: JSON.stringify(item),
  });
  setStatus(res.ok ? 'Item saved' : 'Error saving item');
}

async function saveItemPosition(item) {
  await saveItem(item);
}

async function deleteItem(item) {
  const res = await apiFetch(`/api/admin/map/items/${item.id}`, { method: 'DELETE' });
  if (res.ok) {
    items = items.filter(i => i.id !== item.id);
    selectedId = null;
    hideSelectedPanel();
    redraw();
    setStatus(`Deleted ${item.type}`);
  }
}

// ── Zone drawing ──────────────────────────────────────────────────────────────
document.getElementById('btn-add-zone').addEventListener('click', () => {
  isDrawingZone = true;
  zoneDrawStart = null;
  zoneDrawEnd   = null;
  canvas.style.cursor = 'crosshair';
  setStatus('Click and drag on the map to draw a zone');
  activateTab('zones'); // keep zones tab
});

async function createZoneFromDraw(x, y, w, h) {
  if (!layoutId) return;
  const name  = document.getElementById('zone-name').value.trim() || 'Zone';
  const color = document.getElementById('zone-color').value || '#3b82f6';

  const body = { layoutId, name, color, x, y, w, h, metaJson: null };
  const res  = await apiFetch('/api/admin/map/zones', { method: 'POST', body: JSON.stringify(body) });
  if (res.ok) {
    const zone = await res.json();
    zones.push(zone);
    renderZoneList();
    redraw();
    setStatus(`Zone "${name}" created`);
  } else {
    setStatus('Error creating zone');
  }
}

async function deleteZone(id) {
  const res = await apiFetch(`/api/admin/map/zones/${id}`, { method: 'DELETE' });
  if (res.ok) {
    zones = zones.filter(z => z.id !== id);
    renderZoneList();
    redraw();
  }
}

function renderZoneList() {
  const ul = document.getElementById('zone-list');
  ul.innerHTML = '';
  zones.forEach(zone => {
    const li  = document.createElement('li');
    const dot = document.createElement('span');
    dot.className = 'zone-dot';
    dot.style.background = zone.color;
    const name = document.createElement('span');
    name.className   = 'zone-name';
    name.textContent = zone.name;
    const del = document.createElement('button');
    del.className   = 'zone-del';
    del.textContent = '✕';
    del.onclick     = () => deleteZone(zone.id);
    li.append(dot, name, del);
    ul.appendChild(li);
  });
}

// ── Layout settings apply ─────────────────────────────────────────────────────
document.getElementById('btn-apply-map').addEventListener('click', async () => {
  if (!layoutId) return;
  const newGrid = parseInt(document.getElementById('cfg-grid-size').value, 10);
  const newW    = parseInt(document.getElementById('cfg-width').value,    10);
  const newH    = parseInt(document.getElementById('cfg-height').value,   10);
  const res = await apiFetch('/api/admin/map', {
    method: 'PUT',
    body: JSON.stringify({ name: 'Main Hall', width: newW, height: newH, gridSize: newGrid }),
  });
  if (res.ok) {
    const layout = await res.json();
    gridSize = layout.gridSize;
    mapW     = layout.width;
    mapH     = layout.height;
    redraw();
    setStatus('Map settings updated');
  }
});

// ── Save all (top-bar button) ─────────────────────────────────────────────────
document.getElementById('btn-save').addEventListener('click', async () => {
  setStatus('Saving…');
  // Save all items that are currently in state (positions may have changed by drag)
  await Promise.all(items.map(item => apiFetch(`/api/admin/map/items/${item.id}`, {
    method: 'PUT', body: JSON.stringify(item),
  })));
  setStatus('Saved ✓');
});

// ── Zoom ──────────────────────────────────────────────────────────────────────
document.getElementById('btn-zoom-in').addEventListener('click',    () => { scale = Math.min(scale + 0.25, 3); redraw(); });
document.getElementById('btn-zoom-out').addEventListener('click',   () => { scale = Math.max(scale - 0.25, 0.25); redraw(); });
document.getElementById('btn-zoom-reset').addEventListener('click', () => { scale = 1; redraw(); });

// ── Tabs ──────────────────────────────────────────────────────────────────────
function activateTab(tabId) {
  document.querySelectorAll('.tab-btn').forEach(b => b.classList.toggle('active', b.dataset.tab === tabId));
  document.querySelectorAll('.tab-content').forEach(c => c.classList.toggle('active', c.id === 'tab-' + tabId));
}

document.querySelectorAll('.tab-btn').forEach(btn => {
  btn.addEventListener('click', () => activateTab(btn.dataset.tab));
});

// ── Poll workstation statuses every 5 s ──────────────────────────────────────
async function pollStatuses() {
  if (!adminKey()) return;
  try {
    const res = await apiFetch('/api/admin/workstations');
    if (res.ok) { workstations = await res.json(); redraw(); }
  } catch (_) {}
}
setInterval(pollStatuses, 5000);

// ── Init ──────────────────────────────────────────────────────────────────────
buildPalette();
loadAll();
