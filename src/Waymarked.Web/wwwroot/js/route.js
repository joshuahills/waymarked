// ── Field-state management (mutual exclusion: end point ↔ distance) ─

function updateFieldStates() {
    const endFilled  = endLatInput.value.trim()  !== '' || endLonInput.value.trim() !== '';
    const distFilled = distanceInput.value.trim() !== '' && parseFloat(distanceInput.value) > 0;

    // Disable end-point search while distance is in use; disable distance
    // inputs while an end point is set
    endSearch.disabled          = !endFilled && distFilled;
    distanceInput.disabled      = endFilled;
    distanceUnitInput.disabled  = endFilled;
}

distanceInput.addEventListener('input', updateFieldStates);

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

        routeLayer = L.polyline(coordinates, {
            color: '#007bff',
            weight: 4,
            opacity: 0.7
        }).addTo(map);

        map.fitBounds(routeLayer.getBounds(), { padding: [50, 50] });

        addRouteArrows(data.points.coordinates, data.instructions);

        const instructionCount = data.instructions ? data.instructions.length : 0;
        showStats(data.distanceKm, data.distanceMiles, data.durationFormatted, instructionCount);
        showSteps(data.instructions);

    } catch (error) {
        console.error('Route planning error:', error);
        showError(error.message);
    } finally {
        planButton.disabled    = false;
        planButton.textContent = 'Plan Route';
    }
});
