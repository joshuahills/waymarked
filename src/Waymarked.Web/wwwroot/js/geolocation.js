// ── Geolocation — "Use my location" ─────────────────────────────────

const geolocBtn   = document.getElementById('geolocBtn');
const geolocError = document.getElementById('geolocError');

let youAreHereMarker = null;

// Graceful degradation — hide button entirely on unsupported browsers
if (!navigator.geolocation) {
    geolocBtn.style.display = 'none';
}

function createYouAreHereIcon() {
    return L.divIcon({
        className: 'you-are-here-icon',
        html: '<div class="yah-outer"><div class="yah-inner"></div></div>',
        iconSize:   [22, 22],
        iconAnchor: [11, 11]
    });
}

function clearGeolocError() {
    geolocError.textContent = '';
    geolocError.classList.remove('visible');
}

function showGeolocError(msg) {
    geolocError.textContent = msg;
    geolocError.classList.add('visible');
}

geolocBtn.addEventListener('click', () => {
    if (!navigator.geolocation) return;

    clearGeolocError();
    geolocBtn.classList.add('geoloc-btn--loading');
    geolocBtn.disabled = true;

    navigator.geolocation.getCurrentPosition(
        async (position) => {
            const lat = position.coords.latitude;
            const lon = position.coords.longitude;

            // Pan/zoom to user location
            map.setView([lat, lon], 14);

            // Place distinct "you are here" marker (blue pulsing dot)
            if (youAreHereMarker) map.removeLayer(youAreHereMarker);
            youAreHereMarker = L.marker([lat, lon], {
                icon: createYouAreHereIcon(),
                zIndexOffset: 500,
                title: 'You are here'
            }).addTo(map);

            // Reverse-geocode for a friendly label
            let name = coordLabel(lat, lon);
            try {
                const result = await reverseGeocode(lat, lon);
                if (result.display_name) name = result.display_name;
            } catch { /* fall back to coordinate label */ }

            // Populate the start point (uses the existing setter from geocoder.js)
            setStartPoint(lat, lon, name);

            geolocBtn.classList.remove('geoloc-btn--loading');
            geolocBtn.disabled = false;
        },
        (error) => {
            geolocBtn.classList.remove('geoloc-btn--loading');
            geolocBtn.disabled = false;

            let msg = 'Location unavailable — please enable location access';
            if (error.code === error.TIMEOUT) {
                msg = 'Location request timed out — please try again';
            } else if (error.code === error.POSITION_UNAVAILABLE) {
                msg = 'Location unavailable — GPS signal lost';
            }
            showGeolocError(msg);
        },
        { timeout: 10000, maximumAge: 60000 }
    );
});
