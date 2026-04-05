/* ── CyberClub Admin Map – map-render.js ─────────────────────────────────── */
// Canvas rendering: redraw, drawItem, drawZone.
// Uses ctx.setTransform so all drawing is in logical (un-scaled) pixel space.

import { state, canvas, ctx, wrapper, ELEMENT_TYPES } from './map-state.js';

// ── RAF-debounced redraw ──────────────────────────────────────────────────────
let _drawScheduled = false;

export function requestDraw() {
  if (_drawScheduled) return;
  _drawScheduled = true;
  requestAnimationFrame(() => { _drawScheduled = false; redraw(); });
}

// ── Canvas resize (called by ResizeObserver on wrapper) ───────────────────────
export function resizeCanvas() {
  canvas.width  = wrapper.clientWidth  || 800;
  canvas.height = wrapper.clientHeight || 600;
}

// ── Main redraw ───────────────────────────────────────────────────────────────
export function redraw() {
  const { scale, panX, panY, mapW, mapH, gridSize, isDrawingZone,
          zoneDrawStart, zoneDrawEnd, items, zones, selectedId } = state;

  // Apply pan + scale transform for all subsequent draws
  ctx.setTransform(scale, 0, 0, scale, panX, panY);

  // Clear only the visible area (in logical space)
  const visX = -panX / scale;
  const visY = -panY / scale;
  const visW =  canvas.width  / scale;
  const visH =  canvas.height / scale;
  ctx.clearRect(visX, visY, visW, visH);

  // Map background
  ctx.fillStyle = '#0f1117';
  ctx.fillRect(0, 0, mapW, mapH);

  // ── Grid lines (batched into one path per direction) ──────────────────────
  ctx.strokeStyle = '#1e2233';
  ctx.lineWidth   = 1 / scale;  // always 1 screen-pixel regardless of zoom
  ctx.beginPath();
  for (let x = 0; x <= mapW; x += gridSize) {
    ctx.moveTo(x, 0); ctx.lineTo(x, mapH);
  }
  for (let y = 0; y <= mapH; y += gridSize) {
    ctx.moveTo(0, y); ctx.lineTo(mapW, y);
  }
  ctx.stroke();

  // ── Zones ─────────────────────────────────────────────────────────────────
  zones.forEach(drawZone);

  // Zone being drawn live
  if (isDrawingZone && zoneDrawStart && zoneDrawEnd) {
    const nx = Math.min(zoneDrawStart.x, zoneDrawEnd.x);
    const ny = Math.min(zoneDrawStart.y, zoneDrawEnd.y);
    const nw = Math.abs(zoneDrawEnd.x - zoneDrawStart.x) + 1;
    const nh = Math.abs(zoneDrawEnd.y - zoneDrawStart.y) + 1;
    const gs = gridSize;
    ctx.fillStyle   = 'rgba(99,102,241,0.25)';
    ctx.strokeStyle = '#6366f1';
    ctx.lineWidth   = 2 / scale;
    ctx.fillRect(nx * gs, ny * gs, nw * gs, nh * gs);
    ctx.strokeRect(nx * gs, ny * gs, nw * gs, nh * gs);
  }

  // ── Items ─────────────────────────────────────────────────────────────────
  items.forEach(item => drawItem(item, item.id === selectedId));
}

// ── Draw a zone ───────────────────────────────────────────────────────────────
function drawZone(zone) {
  const gs    = state.gridSize;
  const color = zone.color || '#6366f1';

  ctx.globalAlpha = 0.18;
  ctx.fillStyle   = color;
  ctx.fillRect(zone.x * gs, zone.y * gs, zone.w * gs, zone.h * gs);
  ctx.globalAlpha = 1;

  ctx.strokeStyle = color;
  ctx.lineWidth   = 1.5 / state.scale;
  ctx.setLineDash([4 / state.scale, 4 / state.scale]);
  ctx.strokeRect(zone.x * gs, zone.y * gs, zone.w * gs, zone.h * gs);
  ctx.setLineDash([]);

  ctx.fillStyle    = color;
  ctx.font         = '11px Segoe UI, sans-serif';
  ctx.textAlign    = 'left';
  ctx.textBaseline = 'top';
  ctx.fillText(zone.name, zone.x * gs + 4, zone.y * gs + 4);
}

// ── Draw an item ──────────────────────────────────────────────────────────────
export function drawItem(item, selected) {
  const gs  = state.gridSize;
  const px  = item.x * gs;
  const py  = item.y * gs;
  const pw  = item.w * gs;
  const ph  = item.h * gs;
  const def = ELEMENT_TYPES.find(e => e.type === item.type) || ELEMENT_TYPES[0];
  const ws  = state.workstations.find(w => w.id === item.workstationId);

  // Workstation state → tile colour
  let bg = def.color;
  if (item.type === 'Pc' || item.type === 'Console') {
    if (!ws)                          bg = '#334155'; // unlinked
    else if (!ws.isOnline)            bg = '#1e293b'; // offline
    else if (ws.state === 'Locked')   bg = '#7f1d1d'; // locked
    else if (ws.state === 'Unlocked') bg = '#14532d'; // in use
    else                              bg = '#1e3a5f';
  }

  ctx.save();
  ctx.translate(px + pw / 2, py + ph / 2);
  ctx.rotate((item.rotation * Math.PI) / 180);

  if (selected) { ctx.shadowColor = '#818cf8'; ctx.shadowBlur = 12 / state.scale; }

  // Tile background
  ctx.fillStyle = bg;
  _roundRect(ctx, -pw / 2, -ph / 2, pw, ph, 5);
  ctx.fill();

  // Border
  ctx.strokeStyle = selected ? '#818cf8' : 'rgba(255,255,255,0.12)';
  ctx.lineWidth   = (selected ? 2 : 1) / state.scale;
  _roundRect(ctx, -pw / 2, -ph / 2, pw, ph, 5);
  ctx.stroke();
  ctx.shadowBlur = 0;

  // Icon
  if (pw >= 24 && ph >= 24) {
    ctx.font         = `${Math.min(pw * 0.42, ph * 0.42, 24)}px Segoe UI Emoji, sans-serif`;
    ctx.textAlign    = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(def.icon, 0, ph >= 40 ? -ph * 0.12 : 0);
  }

  // Label
  if (item.label && pw >= 20) {
    ctx.font         = `bold ${Math.min(11, pw * 0.3)}px Segoe UI, sans-serif`;
    ctx.fillStyle    = 'rgba(255,255,255,0.9)';
    ctx.textAlign    = 'center';
    ctx.textBaseline = 'alphabetic';
    ctx.fillText(item.label, 0, ph / 2 - 4);
  }

  // Online indicator dot
  if (ws) {
    const dotR = 5;
    ctx.beginPath();
    ctx.arc(pw / 2 - dotR - 2, -ph / 2 + dotR + 2, dotR, 0, Math.PI * 2);
    ctx.fillStyle = ws.isOnline ? '#22c55e' : '#475569';
    ctx.fill();
  }

  ctx.restore();
}

// ── Helper: rounded rectangle path ───────────────────────────────────────────
function _roundRect(ctx, x, y, w, h, r) {
  ctx.beginPath();
  ctx.moveTo(x + r, y);
  ctx.arcTo(x + w, y,     x + w, y + h, r);
  ctx.arcTo(x + w, y + h, x,     y + h, r);
  ctx.arcTo(x,     y + h, x,     y,     r);
  ctx.arcTo(x,     y,     x + w, y,     r);
  ctx.closePath();
}

// ── ResizeObserver ────────────────────────────────────────────────────────────
const ro = new ResizeObserver(() => { resizeCanvas(); requestDraw(); });
ro.observe(wrapper);
resizeCanvas();
