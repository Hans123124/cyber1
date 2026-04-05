/* ── CyberClub Admin Map – map-state.js ──────────────────────────────────── */
// Shared state and DOM references for the map editor.

export const ELEMENT_TYPES = [
  { type: 'Pc',         icon: '🖥',  label: 'PC',       color: '#4f46e5', w: 1, h: 1 },
  { type: 'Console',    icon: '🎮',  label: 'Console',  color: '#7c3aed', w: 1, h: 1 },
  { type: 'Wall',       icon: '▬',   label: 'Wall',     color: '#475569', w: 2, h: 1 },
  { type: 'Corner',     icon: '◣',   label: 'Corner',   color: '#475569', w: 1, h: 1 },
  { type: 'WallT',      icon: '⊤',   label: 'Wall T',   color: '#475569', w: 1, h: 1 },
  { type: 'Triangle',   icon: '△',   label: 'Triangle', color: '#475569', w: 1, h: 1 },
  { type: 'Decoration', icon: '🪴',  label: 'Decor',    color: '#15803d', w: 1, h: 1 },
  { type: 'Desk',       icon: '🗄',  label: 'Desk',     color: '#92400e', w: 2, h: 1 },
  { type: 'Chair',      icon: '🪑',  label: 'Chair',    color: '#78350f', w: 1, h: 1 },
];

// ── Shared mutable state ──────────────────────────────────────────────────────
export const state = {
  // Layout
  layoutId:    null,
  gridSize:    40,
  mapW:        1200,
  mapH:        800,

  // Viewport
  scale:       1,
  panX:        0,
  panY:        0,

  // Data
  items:        [],  // MapItem[]
  zones:        [],  // Zone[]
  workstations: [],  // Workstation[]

  // Selection / drag
  selectedId:   null,
  draggingId:   null,
  dragOffX:     0,
  dragOffY:     0,

  // Panning
  isPanning:     false,
  panStartX:     0,
  panStartY:     0,
  panStartOffX:  0,
  panStartOffY:  0,

  // Zone drawing
  isDrawingZone: false,
  zoneDrawStart: null,
  zoneDrawEnd:   null,

  // Unsaved property changes (set when panel form is dirty)
  isDirty: false,
};

// ── DOM refs ──────────────────────────────────────────────────────────────────
export const canvas    = document.getElementById('map-canvas');
export const ctx       = canvas.getContext('2d');
export const wrapper   = document.getElementById('canvas-wrapper');
export const statusBar = document.getElementById('status-bar');

export function setStatus(msg) { statusBar.textContent = msg; }
