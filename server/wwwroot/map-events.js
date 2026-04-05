/* ── CyberClub Admin Map – map-events.js ─────────────────────────────────── */
// All DOM event wiring: canvas mouse/keyboard, palette drop, context menu,
// panel buttons, zoom controls, poll scheduling.

import { state, canvas, wrapper, setStatus, ELEMENT_TYPES } from './map-state.js';
import { requestDraw }                                        from './map-render.js';
import { activateTab, showSelectedPanel,
         hideSelectedPanel }                        from './map-ui.js';
import { saveItem, saveItemPosition, deleteItem,
         createZoneFromDraw, sendWsCommand,
         saveAll, applyMapSettings, pollStatuses }  from './map-api.js';
import { apiFetch }                                 from './api.js';

// ── Coordinate helper ─────────────────────────────────────────────────────────
// Returns logical (un-scaled, un-panned) canvas coordinates.
function getCanvasPos(e) {
  const rect = canvas.getBoundingClientRect();
  return {
    cx: (e.clientX - rect.left - state.panX) / state.scale,
    cy: (e.clientY - rect.top  - state.panY) / state.scale,
  };
}

// ── Rotation-aware hit testing ────────────────────────────────────────────────
function hitItem(cx, cy) {
  const gs = state.gridSize;
  for (let i = state.items.length - 1; i >= 0; i--) {
    const it = state.items[i];
    // Centre of item in logical space
    const cxItem = (it.x + it.w / 2) * gs;
    const cyItem = (it.y + it.h / 2) * gs;
    // Rotate mouse point about item centre by -rotation
    const angle = -(it.rotation * Math.PI) / 180;
    const dx    = cx - cxItem;
    const dy    = cy - cyItem;
    const rx    = dx * Math.cos(angle) - dy * Math.sin(angle);
    const ry    = dx * Math.sin(angle) + dy * Math.cos(angle);
    // AABB test in item-local space
    if (rx >= -it.w * gs / 2 && rx < it.w * gs / 2 &&
        ry >= -it.h * gs / 2 && ry < it.h * gs / 2) {
      return it;
    }
  }
  return null;
}

// ── Space-key panning flag ────────────────────────────────────────────────────
let isSpaceHeld = false;
window.addEventListener('keydown', e => {
  if (e.code === 'Space' && document.activeElement === document.body) {
    isSpaceHeld = true;
    canvas.style.cursor = 'grab';
    e.preventDefault();
  }
});
window.addEventListener('keyup', e => {
  if (e.code === 'Space') {
    isSpaceHeld = false;
    canvas.style.cursor = state.isDrawingZone ? 'crosshair' : 'default';
  }
});

// ── Canvas: mousedown ─────────────────────────────────────────────────────────
canvas.addEventListener('mousedown', e => {
  hideCtxMenu();

  // Middle-mouse OR Space+left-click → start panning
  if (e.button === 1 || (e.button === 0 && isSpaceHeld)) {
    e.preventDefault();
    state.isPanning    = true;
    state.panStartX    = e.clientX;
    state.panStartY    = e.clientY;
    state.panStartOffX = state.panX;
    state.panStartOffY = state.panY;
    canvas.style.cursor = 'grabbing';
    return;
  }

  if (e.button !== 0) return;
  const { cx, cy } = getCanvasPos(e);

  if (state.isDrawingZone) {
    state.zoneDrawStart = { x: Math.floor(cx / state.gridSize), y: Math.floor(cy / state.gridSize) };
    state.zoneDrawEnd   = { ...state.zoneDrawStart };
    return;
  }

  const hit = hitItem(cx, cy);
  if (hit) {
    state.selectedId = hit.id;
    state.draggingId = hit.id;
    state.dragOffX   = cx - hit.x * state.gridSize;
    state.dragOffY   = cy - hit.y * state.gridSize;
    showSelectedPanel(hit);
    requestDraw();
  } else {
    state.selectedId = null;
    hideSelectedPanel();
    requestDraw();
  }
});

// ── Canvas: mousemove ─────────────────────────────────────────────────────────
canvas.addEventListener('mousemove', e => {
  if (state.isPanning) {
    state.panX = state.panStartOffX + (e.clientX - state.panStartX);
    state.panY = state.panStartOffY + (e.clientY - state.panStartY);
    requestDraw();
    return;
  }

  const { cx, cy } = getCanvasPos(e);

  if (state.isDrawingZone && state.zoneDrawStart) {
    state.zoneDrawEnd = { x: Math.floor(cx / state.gridSize), y: Math.floor(cy / state.gridSize) };
    requestDraw();
    return;
  }

  if (state.draggingId) {
    const item = state.items.find(i => i.id === state.draggingId);
    if (!item) return;
    item.x = Math.max(0, Math.min(
      Math.floor((cx - state.dragOffX) / state.gridSize),
      Math.floor(state.mapW / state.gridSize) - item.w,
    ));
    item.y = Math.max(0, Math.min(
      Math.floor((cy - state.dragOffY) / state.gridSize),
      Math.floor(state.mapH / state.gridSize) - item.h,
    ));
    requestDraw();
    setStatus(`${item.label || item.type} → (${item.x}, ${item.y})`);
  }
});

// ── Canvas: mouseup ───────────────────────────────────────────────────────────
canvas.addEventListener('mouseup', async e => {
  if (state.isPanning) {
    state.isPanning = false;
    canvas.style.cursor = isSpaceHeld ? 'grab' : (state.isDrawingZone ? 'crosshair' : 'default');
    return;
  }

  if (e.button !== 0) return;
  const { cx, cy } = getCanvasPos(e);

  if (state.isDrawingZone && state.zoneDrawStart) {
    const x = Math.min(state.zoneDrawStart.x, state.zoneDrawEnd?.x ?? state.zoneDrawStart.x);
    const y = Math.min(state.zoneDrawStart.y, state.zoneDrawEnd?.y ?? state.zoneDrawStart.y);
    const w = Math.abs((state.zoneDrawEnd?.x ?? state.zoneDrawStart.x) - state.zoneDrawStart.x) + 1;
    const h = Math.abs((state.zoneDrawEnd?.y ?? state.zoneDrawStart.y) - state.zoneDrawStart.y) + 1;
    await createZoneFromDraw(x, y, w, h);
    state.isDrawingZone = false;
    state.zoneDrawStart = null;
    state.zoneDrawEnd   = null;
    canvas.style.cursor = '';
    return;
  }

  if (state.draggingId) {
    const item = state.items.find(i => i.id === state.draggingId);
    if (item) await saveItemPosition(item);
    state.draggingId = null;
  }
});

canvas.addEventListener('mouseleave', () => {
  if (state.isPanning) state.isPanning = false;
  state.draggingId = null;
});

// ── Mouse-wheel zoom (centred on cursor) ──────────────────────────────────────
wrapper.addEventListener('wheel', e => {
  e.preventDefault();
  const factor   = e.deltaY < 0 ? 1.15 : 1 / 1.15;
  const newScale = Math.max(0.25, Math.min(3, state.scale * factor));
  if (newScale === state.scale) return;

  const rect = canvas.getBoundingClientRect();
  const mx   = e.clientX - rect.left;
  const my   = e.clientY - rect.top;

  // Keep the point under the cursor stationary
  state.panX = mx - (mx - state.panX) * (newScale / state.scale);
  state.panY = my - (my - state.panY) * (newScale / state.scale);
  state.scale = newScale;
  requestDraw();
}, { passive: false });

// ── Context menu ──────────────────────────────────────────────────────────────
const ctxMenu = document.getElementById('ctx-menu');

canvas.addEventListener('contextmenu', e => {
  e.preventDefault();
  const { cx, cy } = getCanvasPos(e);
  const hit = hitItem(cx, cy);
  if (!hit) { hideCtxMenu(); return; }
  state.selectedId = hit.id;
  showSelectedPanel(hit);
  requestDraw();
  showCtxMenu(e.clientX, e.clientY, hit);
});

function showCtxMenu(x, y, item) {
  // Show menu at initial position to measure size
  ctxMenu.style.left    = '-9999px';
  ctxMenu.style.display = 'block';
  ctxMenu.dataset.itemId = item.id;

  // Show/hide workstation-action items based on item type
  const hasWs = !!item.workstationId;
  ['lock', 'unlock', 'reboot', 'shutdown'].forEach(action => {
    const el = ctxMenu.querySelector(`[data-action="${action}"]`);
    if (el) el.style.display = hasWs ? '' : 'none';
  });

  // Overflow correction: flip left/up if needed
  const mw = ctxMenu.offsetWidth;
  const mh = ctxMenu.offsetHeight;
  const finalX = x + mw > window.innerWidth  ? x - mw : x;
  const finalY = y + mh > window.innerHeight ? y - mh : y;
  ctxMenu.style.left = finalX + 'px';
  ctxMenu.style.top  = finalY + 'px';
}

function hideCtxMenu() { ctxMenu.style.display = 'none'; }
document.addEventListener('click', e => {
  if (!ctxMenu.contains(e.target)) hideCtxMenu();
});

ctxMenu.querySelectorAll('.ctx-item').forEach(el => {
  el.addEventListener('click', async e => {
    e.stopPropagation();
    const action = el.dataset.action;
    const itemId = ctxMenu.dataset.itemId;
    const item   = state.items.find(i => i.id === itemId);
    if (!item) return;
    if (action === 'delete') {
      if (confirm(`Delete "${item.label || item.type}"?`)) await deleteItem(item);
    } else if (item.workstationId) {
      await sendWsCommand(item.workstationId, action);
    }
    hideCtxMenu();
  });
});

// ── Palette drag-to-canvas ────────────────────────────────────────────────────
canvas.addEventListener('dragover', e => { e.preventDefault(); });
canvas.addEventListener('drop', async e => {
  e.preventDefault();
  const type = e.dataTransfer.getData('application/cybermap');
  if (!type || !state.layoutId) return;

  const { cx, cy } = getCanvasPos(e);
  const gx  = Math.max(0, Math.floor(cx / state.gridSize));
  const gy  = Math.max(0, Math.floor(cy / state.gridSize));
  const def = ELEMENT_TYPES.find(d => d.type === type) || { w: 1, h: 1 };

  const res = await apiFetch('/api/admin/map/items', {
    method: 'POST',
    body:   JSON.stringify({
      layoutId:     state.layoutId,
      type,
      x:            gx,
      y:            gy,
      w:            def.w || 1,
      h:            def.h || 1,
      rotation:     0,
      label:        '',
      workstationId: null,
      zoneId:       null,
      metaJson:     null,
    }),
  });
  if (res.ok) {
    const item = await res.json();
    state.items.push(item);
    state.selectedId = item.id;
    showSelectedPanel(item);
    requestDraw();
    setStatus(`Placed ${type}`);
  } else {
    setStatus('Error placing element');
  }
});

// ── Panel: Save / Delete selected item ────────────────────────────────────────
document.getElementById('btn-sel-save').addEventListener('click', async () => {
  const item = state.items.find(i => i.id === state.selectedId);
  if (!item) return;
  item.label         = document.getElementById('sel-label').value;
  item.rotation      = parseInt(document.getElementById('sel-rotation').value, 10) || 0;
  item.workstationId = document.getElementById('sel-workstation').value || null;
  state.isDirty      = false;
  await saveItem(item);
  requestDraw();
});

document.getElementById('btn-sel-delete').addEventListener('click', async () => {
  const item = state.items.find(i => i.id === state.selectedId);
  if (!item) return;
  if (!confirm(`Delete "${item.label || item.type}"?`)) return;
  await deleteItem(item);
});

// ── Panel: Draw zone ──────────────────────────────────────────────────────────
document.getElementById('btn-add-zone').addEventListener('click', () => {
  state.isDrawingZone = true;
  state.zoneDrawStart = null;
  state.zoneDrawEnd   = null;
  canvas.style.cursor = 'crosshair';
  setStatus('Click and drag on the map to draw a zone');
  activateTab('zones');
});

// ── Top-bar: Zoom buttons ─────────────────────────────────────────────────────
document.getElementById('btn-zoom-in').addEventListener('click', () => {
  state.scale = Math.min(state.scale + 0.25, 3);
  requestDraw();
});
document.getElementById('btn-zoom-out').addEventListener('click', () => {
  state.scale = Math.max(state.scale - 0.25, 0.25);
  requestDraw();
});
document.getElementById('btn-zoom-reset').addEventListener('click', () => {
  state.scale = 1;
  state.panX  = 0;
  state.panY  = 0;
  requestDraw();
});

// ── Top-bar: Save all ─────────────────────────────────────────────────────────
document.getElementById('btn-save').addEventListener('click', saveAll);

// ── Settings panel: Apply map settings ───────────────────────────────────────
document.getElementById('btn-apply-map').addEventListener('click', applyMapSettings);

// ── Polling: pause when tab is hidden, resume when visible ────────────────────
let _pollTimer = null;

function startPoll() {
  if (_pollTimer) return;
  _pollTimer = setInterval(pollStatuses, 5000);
}

function stopPoll() {
  if (_pollTimer) { clearInterval(_pollTimer); _pollTimer = null; }
}

document.addEventListener('visibilitychange', () => {
  if (document.hidden) stopPoll(); else { pollStatuses(); startPoll(); }
});

startPoll();
