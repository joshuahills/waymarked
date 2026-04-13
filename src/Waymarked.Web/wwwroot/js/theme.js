// ── Theme management ─────────────────────────────────────────────────
// The inline script in <head> already applied the initial theme to prevent
// flash of wrong theme. This file wires up the toggle button once the DOM
// is ready.
(function () {
    const root   = document.documentElement;
    const toggle = document.getElementById('theme-toggle');

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
