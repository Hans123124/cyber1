/* ── CyberClub Admin Map – map.js ─────────────────────────────────────────── */
// ES-module entry point for the map editor.
// Imports sub-modules (which each self-register their event listeners on import)
// and boots the initial data load.

import { buildPalette } from './map-ui.js';
import { loadAll }       from './map-api.js';
import './map-events.js'; // registers all canvas / panel event listeners

buildPalette();
loadAll();
