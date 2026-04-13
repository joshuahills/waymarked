// ── Profile description ──────────────────────────────────────────────

const profileDescriptions = {
    foot: 'Prefers footpaths and pavements. Good for shorter walks on mixed terrain.',
    hike: 'Prefers trails and bridleways, avoids steep terrain. Best for longer scenic routes.'
};

function updateProfileDescription() {
    profileDescription.textContent = profileDescriptions[profileSelect.value] || '';
}

profileSelect.addEventListener('change', updateProfileDescription);
updateProfileDescription();

// ── Route type toggle (Round Trip ↔ Point to Point) ─────────────────

let lastRouteRequest = null;

const roundTripBtn       = document.getElementById('routeTypeRoundTrip');
const pointToPointBtn    = document.getElementById('routeTypePtoP');
const roundTripSection   = document.getElementById('roundTripSection');
const ptoPSection        = document.getElementById('pointToPointSection');

function selectRoundTrip() {
    roundTripBtn.classList.add('active');
    roundTripBtn.setAttribute('aria-pressed', 'true');
    pointToPointBtn.classList.remove('active');
    pointToPointBtn.setAttribute('aria-pressed', 'false');
    roundTripSection.style.display = '';
    ptoPSection.style.display = 'none';
    // Clear end point so stale coords don't leak into round-trip requests
    endLatInput.value  = '';
    endLonInput.value  = '';
    endSearch.value    = '';
}

function selectPointToPoint() {
    pointToPointBtn.classList.add('active');
    pointToPointBtn.setAttribute('aria-pressed', 'true');
    roundTripBtn.classList.remove('active');
    roundTripBtn.setAttribute('aria-pressed', 'false');
    ptoPSection.style.display = '';
    roundTripSection.style.display = 'none';
}

roundTripBtn.addEventListener('click', selectRoundTrip);
pointToPointBtn.addEventListener('click', selectPointToPoint);

// Default: Round Trip
selectRoundTrip();

// ── Steps toggle event listener ──────────────────────────────────────
stepsToggle.addEventListener('click', () => {
    const open = stepsList.style.display !== 'none';
    stepsList.style.display = open ? 'none' : 'block';
    stepsToggle.textContent = open ? '▾ Show steps' : '▴ Hide steps';
});

// ── Error / stats helpers ────────────────────────────────────────────

function showError(message) {
    errorDiv.textContent = message;
    errorDiv.classList.add('visible');
}

function hideError() {
    errorDiv.classList.remove('visible');
}

function showStats(distanceKm, distanceMiles, duration, instructions) {
    document.getElementById('statDistance').textContent = `${distanceKm} km (${distanceMiles} mi)`;
    document.getElementById('statTime').textContent     = duration;
    document.getElementById('statInstructions').textContent = instructions;
    statsDiv.classList.add('visible');
}

function hideStats() {
    statsDiv.classList.remove('visible');
    stepsToggle.style.display = 'none';
    stepsList.style.display = 'none';
    stepsList.innerHTML = '';
    exportSection.style.display = 'none';
}

function showSteps(instructions) {
    stepsList.innerHTML = '';
    const filtered = instructions.filter(i => i.sign !== 4);
    filtered.forEach((inst, idx) => {
        const dist = inst.distance < 100
            ? `${Math.round(inst.distance)}m`
            : `${(inst.distance / 1000).toFixed(1)}km`;
        const li = document.createElement('li');
        li.innerHTML = `<span>${idx + 1}.</span> ${inst.text} <span class="step-dist">— ${dist}</span>`;
        stepsList.appendChild(li);
    });
    stepsToggle.style.display = 'block';
    stepsToggle.textContent = '▾ Show steps';
    stepsList.style.display = 'none';
}

// ── Form submission — reads hidden inputs, logic unchanged ───────────

form.addEventListener('submit', async e => {
    e.preventDefault();
    hideError();
    hideStats();

    const startLat  = parseFloat(startLatInput.value);
    const startLon  = parseFloat(startLonInput.value);
    const endLatVal = endLatInput.value.trim();
    const endLonVal = endLonInput.value.trim();
    const profile   = document.getElementById('profile').value;

    const isRoundTrip = endLatVal === '' && endLonVal === '';

    // Validate start point
    if (isNaN(startLat) || isNaN(startLon)) {
        showError('Please search for and select a start point');
        return;
    }

    // Build payload based on mode
    let payload;
    if (isRoundTrip) {
        const distance = parseFloat(distanceInput.value);
        if (isNaN(distance) || distance <= 0) {
            showError('Please enter a distance greater than 0 for a round trip');
            return;
        }
        payload = {
            from: [startLat, startLon],
            profile,
            distance,
            distanceUnit: distanceUnitInput.value
        };
    } else {
        const endLat = parseFloat(endLatVal);
        const endLon = parseFloat(endLonVal);
        if (isNaN(endLat) || isNaN(endLon)) {
            showError('Please select a valid end point, or leave it blank for a round trip');
            return;
        }
        payload = {
            from: [startLat, startLon],
            to:   [endLat, endLon],
            profile
        };
    }

    planButton.disabled    = true;
    planButton.textContent = 'Planning…';

    lastRouteRequest = payload;

    try {
        const response = await fetch('/api/routes', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`Failed to plan route: ${response.status} ${errorText}`);
        }

        const data = await response.json();

        if (!data.points || !data.points.coordinates) {
            throw new Error('No route found');
        }

        // GeoJSON coordinates are [lon, lat] — swap to [lat, lon] for Leaflet
        const coordinates = data.points.coordinates.map(coord => [coord[1], coord[0]]);

        if (routeLayer) map.removeLayer(routeLayer);
        clearArrowMarkers();

        // Two stacked polylines: dark outline underneath, magenta fill on top.
        // Magenta (#E0007A) sits in the hue gap between OpenTopoMap's blue/cyan
        // water, orange/red roads, and green woodland — readable at all zoom levels.
        const routeOutline = L.polyline(coordinates, {
            color: '#1a0800',
            weight: 7,
            opacity: 0.55
        });
        const routeFill = L.polyline(coordinates, {
            color: '#E0007A',
            weight: 4,
            opacity: 0.95
        });
        routeLayer = L.featureGroup([routeOutline, routeFill]).addTo(map);

        map.fitBounds(routeLayer.getBounds(), { padding: [50, 50] });

        addRouteArrows(data.points.coordinates, data.instructions);

        const instructionCount = data.instructions ? data.instructions.length : 0;
        showStats(data.distanceKm, data.distanceMiles, data.durationFormatted, instructionCount);
        showSteps(data.instructions);
        exportSection.style.display = 'block';

    } catch (error) {
        console.error('Route planning error:', error);
        showError(error.message);
    } finally {
        planButton.disabled    = false;
        planButton.textContent = 'Plan Route';
    }
});

// ── Export download handler ──────────────────────────────────────────

async function exportRoute(format, btn) {
    if (!lastRouteRequest) return;

    const originalText = btn.textContent;
    btn.disabled = true;
    btn.textContent = '…';

    try {
        const res = await fetch(`/api/routes/export/${format}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(lastRouteRequest)
        });

        if (!res.ok) throw new Error(`Export failed: ${res.status}`);

        const blob = await res.blob();
        const url  = URL.createObjectURL(blob);
        const a    = document.createElement('a');
        a.href     = url;
        a.download = `waymarked-route.${format}`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    } catch (err) {
        console.error('Export error:', err);
    } finally {
        btn.disabled = false;
        btn.textContent = originalText;
    }
}

exportGpx.addEventListener('click',     () => exportRoute('gpx',     exportGpx));
exportKml.addEventListener('click',     () => exportRoute('kml',     exportKml));
exportGeoJson.addEventListener('click', () => exportRoute('geojson', exportGeoJson));
