function routeBearing(lat1, lon1, lat2, lon2) {
    const toRad = d => d * Math.PI / 180;
    const dLon  = toRad(lon2 - lon1);
    const y = Math.sin(dLon) * Math.cos(toRad(lat2));
    const x = Math.cos(toRad(lat1)) * Math.sin(toRad(lat2)) -
              Math.sin(toRad(lat1)) * Math.cos(toRad(lat2)) * Math.cos(dLon);
    return (Math.atan2(y, x) * 180 / Math.PI + 360) % 360;
}

function haversineKm(lat1, lon1, lat2, lon2) {
    const R    = 6371;
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLon = (lon2 - lon1) * Math.PI / 180;
    const a    = Math.sin(dLat / 2) ** 2 +
                 Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
                 Math.sin(dLon / 2) ** 2;
    return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
}

function chevronIcon(bearingDeg) {
    const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20">
        <polygon points="5,4 15,10 5,16" fill="rgba(255,255,255,0.9)" stroke="#2d5a27" stroke-width="1.5" stroke-linejoin="round"/>
    </svg>`;
    return L.divIcon({
        html:       `<div style="transform:rotate(${bearingDeg}deg);width:20px;height:20px;">${svg}</div>`,
        className:  '',
        iconSize:   [20, 20],
        iconAnchor: [10, 10]
    });
}

function turnDotIcon(sign) {
    const color = (sign < 0) ? '#c8590a' : (sign === 6 ? '#6b6b6b' : '#2d5a27');
    return L.divIcon({
        html:       `<div style="width:10px;height:10px;border-radius:50%;background:${color};border:2px solid white;box-shadow:0 1px 3px rgba(0,0,0,0.3);"></div>`,
        className:  '',
        iconSize:   [10, 10],
        iconAnchor: [5, 5]
    });
}

function clearArrowMarkers() {
    arrowMarkers.forEach(m => map.removeLayer(m));
    arrowMarkers = [];
}

function addRouteArrows(geoCoords, instructions) {
    clearArrowMarkers();

    // 1. Periodic bearing-aligned chevrons along the full route (~every 400 m)
    const CHEVRON_INTERVAL_KM = 0.4;
    let distAccum = 0;

    for (let i = 1; i < geoCoords.length; i++) {
        const [lon1, lat1] = geoCoords[i - 1];
        const [lon2, lat2] = geoCoords[i];
        const segKm = haversineKm(lat1, lon1, lat2, lon2);
        distAccum += segKm;

        if (distAccum >= CHEVRON_INTERVAL_KM) {
            const b      = routeBearing(lat1, lon1, lat2, lon2);
            const midLat = (lat1 + lat2) / 2;
            const midLon = (lon1 + lon2) / 2;
            const marker = L.marker([midLat, midLon], {
                icon:        chevronIcon(b),
                interactive: false,
                keyboard:    false
            }).addTo(map);
            arrowMarkers.push(marker);
            distAccum = 0;
        }
    }

    // 2. Coloured turn dots at instruction waypoints
    if (!instructions || !instructions.length) return;

    instructions.forEach(instr => {
        const sign = instr.sign;
        // Skip straight (0), finish (4), via (5)
        if (sign === 0 || sign === 4 || sign === 5) return;
        if (!instr.interval || instr.interval.length < 1) return;

        const ptIdx = instr.interval[0];
        const coord = geoCoords[ptIdx];
        if (!coord) return;

        const [lon, lat] = coord;
        const marker = L.marker([lat, lon], {
            icon:        turnDotIcon(sign),
            interactive: true,
            keyboard:    false
        }).addTo(map);

        if (instr.text) {
            marker.bindTooltip(instr.text, { sticky: false, direction: 'top', offset: [0, -8] });
        }

        arrowMarkers.push(marker);
    });
}

