// ── Map initialisation ──────────────────────────────────────────────
const map = L.map('map').setView([54.0, -2.0], 6);

L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png', {
    maxZoom: 17,
    attribution: 'Map data: © OpenStreetMap contributors, SRTM | Map style: © OpenTopoMap'
}).addTo(map);

let routeLayer = null;
let arrowMarkers = [];

// ── DOM refs ────────────────────────────────────────────────────────
const form              = document.getElementById('routeForm');
const planButton        = document.getElementById('planButton');
const errorDiv          = document.getElementById('error');
const statsDiv          = document.getElementById('stats');

const startSearch       = document.getElementById('startSearch');
const startDropdown     = document.getElementById('startDropdown');
const startLatInput     = document.getElementById('startLat');
const startLonInput     = document.getElementById('startLon');

const endSearch         = document.getElementById('endSearch');
const endDropdown       = document.getElementById('endDropdown');
const endLatInput       = document.getElementById('endLat');
const endLonInput       = document.getElementById('endLon');

const distanceInput     = document.getElementById('distance');
const distanceUnitInput = document.getElementById('distanceUnit');

const modeStartBtn      = document.getElementById('modeStartBtn');
const modeEndBtn        = document.getElementById('modeEndBtn');
const modeOffBtn        = document.getElementById('modeOffBtn');
const mapEl             = document.getElementById('map');

const profileSelect      = document.getElementById('profile');
const profileDescription = document.getElementById('profile-description');

const stepsToggle       = document.getElementById('stepsToggle');
const stepsList         = document.getElementById('stepsList');

const exportSection     = document.getElementById('exportSection');
const exportGpx         = document.getElementById('exportGpx');
const exportKml         = document.getElementById('exportKml');
const exportGeoJson     = document.getElementById('exportGeoJson');

// ── Marker state ────────────────────────────────────────────────────
let startMarker = null;
let endMarker   = null;
let clickMode   = 'off'; // 'start' | 'end' | 'off'

// Pin-shaped DivIcon — colour + single letter label
function createMarkerIcon(color, letter) {
    return L.divIcon({
        className: '',
        html: `<div style="
            width:28px;height:34px;
            background:${color};
            border:2px solid rgba(255,255,255,0.9);
            border-radius:50% 50% 50% 0;
            transform:rotate(-45deg);
            box-shadow:0 2px 6px rgba(0,0,0,0.35);
            display:flex;align-items:center;justify-content:center;
        "><span style="
            transform:rotate(45deg);
            color:white;font-size:12px;font-weight:700;line-height:1;
        ">${letter}</span></div>`,
        iconSize:    [28, 34],
        iconAnchor:  [14, 34],
        popupAnchor: [0, -36]
    });
}

