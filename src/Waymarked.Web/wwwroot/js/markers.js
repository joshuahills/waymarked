// ── Map-click mode ──────────────────────────────────────────────────

function setClickMode(mode) {
    clickMode = mode;
    modeStartBtn.className = 'mode-btn';
    modeEndBtn.className   = 'mode-btn';
    modeOffBtn.className   = 'mode-btn';
    mapEl.classList.remove('click-mode-start', 'click-mode-end');

    if (mode === 'start') {
        modeStartBtn.classList.add('active-start');
        mapEl.classList.add('click-mode-start');
    } else if (mode === 'end') {
        modeEndBtn.classList.add('active-end');
        mapEl.classList.add('click-mode-end');
    } else {
        modeOffBtn.classList.add('active-off');
    }
}

setClickMode('off'); // initialise to Off

// Clicking the active mode button a second time deactivates it
modeStartBtn.addEventListener('click', () => setClickMode(clickMode === 'start' ? 'off' : 'start'));
modeEndBtn.addEventListener('click',   () => setClickMode(clickMode === 'end'   ? 'off' : 'end'));
modeOffBtn.addEventListener('click',   () => setClickMode('off'));

map.on('click', async e => {
    if (clickMode === 'off') return;

    const { lat, lng } = e.latlng;
    const mode = clickMode;
    setClickMode('off'); // reset immediately so a second stray click is harmless

    let name = coordLabel(lat, lng);
    try {
        const result = await reverseGeocode(lat, lng);
        if (result.display_name) name = result.display_name;
    } catch { /* fall back to coordinate label */ }

    if (mode === 'start') setStartPoint(lat, lng, name);
    else                  setEndPoint(lat, lng, name);
});

