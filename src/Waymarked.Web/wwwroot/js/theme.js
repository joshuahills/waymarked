// ── Theme management ─────────────────────────────────────────────────
// The inline script in <head> already applied the initial theme to prevent
// flash of wrong theme. This file wires up the toggle button once the DOM
// is ready.
(function () {
    const root   = document.documentElement;
    const toggle = document.getElementById('theme-toggle');

    const TILE_CONFIG = {
        light: {
            url: 'https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png',
            options: {
                maxZoom: 17,
                attribution: 'Map data: \u00a9 OpenStreetMap contributors, SRTM | Map style: \u00a9 OpenTopoMap'
            }
        },
        dark: {
            url: 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png',
            options: {
                maxZoom: 19,
                attribution: '\u00a9 <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors \u00a9 <a href="https://carto.com/attributions">CARTO</a>'
            }
        }
    };

    function swapTileLayer(theme) {
        if (!window.map || !window.tileLayer) return;
        window.map.removeLayer(window.tileLayer);
        const cfg = TILE_CONFIG[theme] || TILE_CONFIG.light;
        window.tileLayer = L.tileLayer(cfg.url, cfg.options).addTo(window.map);
        window.tileLayer.bringToBack();
    }

    function applyTheme(theme) {
        if (theme === 'dark') {
            root.setAttribute('data-theme', 'dark');
            toggle.innerHTML = '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="12" r="5"></circle><line x1="12" y1="1" x2="12" y2="3"></line><line x1="12" y1="21" x2="12" y2="23"></line><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"></line><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"></line><line x1="1" y1="12" x2="3" y2="12"></line><line x1="21" y1="12" x2="23" y2="12"></line><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"></line><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"></line></svg>';
            toggle.setAttribute('aria-label', 'Switch to light mode');
        } else {
            root.removeAttribute('data-theme');
            toggle.innerHTML = '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"></path></svg>';
            toggle.setAttribute('aria-label', 'Switch to dark mode');
        }
        swapTileLayer(theme);
    }

    function currentTheme() {
        return root.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
    }

    toggle.addEventListener('click', () => {
        const next = currentTheme() === 'dark' ? 'light' : 'dark';
        applyTheme(next);
        localStorage.setItem('theme', next);
    });

    // Sync button label/icon to whatever the inline <head> script already applied.
    applyTheme(currentTheme());
})();
