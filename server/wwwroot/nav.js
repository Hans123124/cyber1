/* ── CyberClub Admin – nav.js ────────────────────────────────────────────── */
// Role-based sidebar navigation and authentication guard.
// Include on every admin page via <script type="module" src="nav.js">.
//
// Nav link classes (must be present on <a> elements in every sidebar):
//   .nav-map        – Map page link
//   .nav-config     – Configuration page link
//   .nav-ws         – Workstations page link
//   .nav-users      – Users page link  (hidden for non-SuperAdmin)
//   .nav-logout     – Logout link      (handled here)
//
// Role visibility:
//   SuperAdmin  → all links visible
//   Admin       → Map, Config, Workstations, Logout (no Users)
//   Cashier     → Workstations, Logout only
//   (unauthenticated) → redirect to /login

export let currentUser = null;

async function initNav() {
  try {
    const r = await fetch('/api/account/me');
    if (r.status === 401) { location.href = '/login'; return; }
    if (!r.ok) return;
    currentUser = await r.json();
    applyVisibility(currentUser.roles || []);
  } catch (_) {
    // Network error – leave links as-is; server will still enforce auth
  }

  // Logout handler – works for any link with .nav-logout class
  document.querySelectorAll('.nav-logout').forEach(el => {
    el.addEventListener('click', async e => {
      e.preventDefault();
      try { await fetch('/api/account/logout', { method: 'POST' }); } catch (_) { /* ignore */ }
      location.href = '/login';
    });
  });
}

function applyVisibility(roles) {
  const isSuperAdmin = roles.includes('SuperAdmin');
  const isAdmin      = roles.includes('Admin');

  if (!isSuperAdmin) {
    // Admin, Cashier, Player: hide Users link
    document.querySelectorAll('.nav-users').forEach(el => el.style.display = 'none');
  }

  if (!isSuperAdmin && !isAdmin) {
    // Cashier and Player: hide Map and Configuration
    document.querySelectorAll('.nav-map, .nav-config').forEach(el => el.style.display = 'none');

    // If we're currently ON the map or config page, redirect to workstations
    const page = location.pathname.replace(/^\//, '') || 'index.html';
    if (page === '' || page === 'index.html' || page === 'configuration.html') {
      location.href = '/workstations.html';
    }
  }
}

initNav();
