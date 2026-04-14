const NOM_HEADERS = { 'User-Agent': 'Waymarked/1.0' };

async function geocode(query) {
    const url = `https://nominatim.openstreetmap.org/search?format=json&countrycodes=gb&limit=5&q=${encodeURIComponent(query)}`;
    const res = await fetch(url, { headers: NOM_HEADERS });
    if (!res.ok) throw new Error('Geocoding request failed');
    return res.json();
}

async function reverseGeocode(lat, lon) {
    const url = `https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lon}`;
    const res = await fetch(url, { headers: NOM_HEADERS });
    if (!res.ok) throw new Error('Reverse geocoding request failed');
    return res.json();
}

function debounce(fn, delay) {
    let timer;
    return (...args) => { clearTimeout(timer); timer = setTimeout(() => fn(...args), delay); };
}

function setStartPoint(lat, lon, name) {
    startLatInput.value = lat;
    startLonInput.value = lon;
    startSearch.value   = name;
    closeDropdown(startDropdown);

    if (startMarker) map.removeLayer(startMarker);
    startMarker = L.marker([lat, lon], {
        icon: createMarkerIcon('#28a745', 'S'),
        draggable: true
    }).addTo(map);

    startMarker.on('dragend', async () => {
        const { lat: newLat, lng: newLon } = startMarker.getLatLng();
        startLatInput.value = newLat;
        startLonInput.value = newLon;
        try {
            const result = await reverseGeocode(newLat, newLon);
            startSearch.value = result.display_name || coordLabel(newLat, newLon);
        } catch {
            startSearch.value = coordLabel(newLat, newLon);
        }
    });
}

function clearStartPoint() {
    startLatInput.value = '';
    startLonInput.value = '';
    if (startMarker) { map.removeLayer(startMarker); startMarker = null; }
}

function setEndPoint(lat, lon, name) {
    endLatInput.value = lat;
    endLonInput.value = lon;
    endSearch.value   = name;
    closeDropdown(endDropdown);

    if (endMarker) map.removeLayer(endMarker);
    endMarker = L.marker([lat, lon], {
        icon: createMarkerIcon('#dc3545', 'E'),
        draggable: true
    }).addTo(map);

    endMarker.on('dragend', async () => {
        const { lat: newLat, lng: newLon } = endMarker.getLatLng();
        endLatInput.value = newLat;
        endLonInput.value = newLon;
        try {
            const result = await reverseGeocode(newLat, newLon);
            endSearch.value = result.display_name || coordLabel(newLat, newLon);
        } catch {
            endSearch.value = coordLabel(newLat, newLon);
        }
    });
}

function clearEndPoint() {
    endLatInput.value = '';
    endLonInput.value = '';
    endSearch.value   = '';
    if (endMarker) { map.removeLayer(endMarker); endMarker = null; }
}

function coordLabel(lat, lon) {
    return `${parseFloat(lat).toFixed(5)}, ${parseFloat(lon).toFixed(5)}`;
}

function closeDropdown(dropdown) {
    dropdown.classList.remove('visible');
    dropdown.innerHTML = '';
}

function showDropdown(dropdown, results, onSelect) {
    dropdown.innerHTML = '';
    if (!results.length) {
        const item = document.createElement('div');
        item.className = 'search-dropdown-item no-results';
        item.textContent = 'No results found';
        dropdown.appendChild(item);
    } else {
        results.forEach(r => {
            const item = document.createElement('div');
            item.className = 'search-dropdown-item';
            item.textContent = r.display_name;
            // mousedown fires before blur, preventing the dropdown from
            // disappearing before the click is registered
            item.addEventListener('mousedown', e => {
                e.preventDefault();
                onSelect(parseFloat(r.lat), parseFloat(r.lon), r.display_name);
            });
            dropdown.appendChild(item);
        });
    }
    dropdown.classList.add('visible');
}

function setupSearch(searchInput, dropdown, latInput, lonInput, removeMarkerFn, setPointFn) {
    const debouncedSearch = debounce(async query => {
        try {
            const results = await geocode(query);
            showDropdown(dropdown, results, setPointFn);
        } catch {
            closeDropdown(dropdown);
        }
    }, 400);

    searchInput.addEventListener('input', () => {
        const val = searchInput.value.trim();

        // Coords are stale the moment the user edits the text — clear them
        latInput.value = '';
        lonInput.value = '';

        if (!val) {
            removeMarkerFn();
            closeDropdown(dropdown);
        } else {
            debouncedSearch(val);
        }
    });

    // Hide dropdown on blur; brief delay lets mousedown on items fire first
    searchInput.addEventListener('blur', () => {
        setTimeout(() => closeDropdown(dropdown), 160);
    });

    // Re-show dropdown on re-focus if results are still there
    searchInput.addEventListener('focus', () => {
        if (dropdown.children.length) dropdown.classList.add('visible');
    });
}

function removeStartMarker() {
    if (startMarker) { map.removeLayer(startMarker); startMarker = null; }
}

function removeEndMarker() {
    if (endMarker) { map.removeLayer(endMarker); endMarker = null; }
}

setupSearch(startSearch, startDropdown, startLatInput, startLonInput, removeStartMarker, setStartPoint);
setupSearch(endSearch,   endDropdown,   endLatInput,   endLonInput,   removeEndMarker,   setEndPoint);

document.addEventListener('click', e => {
    if (!e.target.closest('#startDropdown') && !e.target.closest('#startSearch')) {
        closeDropdown(startDropdown);
    }
    if (!e.target.closest('#endDropdown') && !e.target.closest('#endSearch')) {
        closeDropdown(endDropdown);
    }
});

