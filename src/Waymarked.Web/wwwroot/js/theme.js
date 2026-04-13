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
            toggle.textContent = '☀️';
            toggle.setAttribute('aria-label', 'Switch to light mode');
        } else {
            root.removeAttribute('data-theme');
            toggle.textContent = '🌙';
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
