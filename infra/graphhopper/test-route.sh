#!/usr/bin/env bash
# Test GraphHopper routing API with a sample Edinburgh route
# Route: Edinburgh city centre (Waverley) to Arthur's Seat viewpoint

set -e

# API endpoint
GRAPHHOPPER_URL="http://localhost:8989"

# Route coordinates (lat,lon)
START="55.9533,-3.1883"  # Edinburgh Waverley Station
END="55.9444,-3.1618"    # Arthur's Seat viewpoint

# Profile to test
PROFILE="hike"

echo "=========================================="
echo "GraphHopper Routing Test"
echo "=========================================="
echo "Route: Edinburgh Waverley → Arthur's Seat"
echo "Profile: $PROFILE"
echo ""

# Check if GraphHopper is running
echo "Checking GraphHopper health..."
if ! curl -sf "$GRAPHHOPPER_URL/health" > /dev/null; then
    echo "❌ GraphHopper is not responding at $GRAPHHOPPER_URL"
    echo "   Start it with: docker-compose up -d"
    echo "   Check logs: docker-compose logs graphhopper"
    exit 1
fi
echo "✅ GraphHopper is running"
echo ""

# Make routing request
echo "Requesting route..."
RESPONSE=$(curl -s "$GRAPHHOPPER_URL/route?point=$START&point=$END&profile=$PROFILE&points_encoded=false&elevation=true&instructions=true&locale=en-GB")

# Check for errors in response
if echo "$RESPONSE" | grep -q '"message"'; then
    echo "❌ Routing failed:"
    echo "$RESPONSE" | jq '.message'
    exit 1
fi

echo "✅ Route found!"
echo ""

# Extract key metrics
DISTANCE=$(echo "$RESPONSE" | jq -r '.paths[0].distance')
TIME=$(echo "$RESPONSE" | jq -r '.paths[0].time')
ASCENT=$(echo "$RESPONSE" | jq -r '.paths[0].ascend // 0')
DESCENT=$(echo "$RESPONSE" | jq -r '.paths[0].descend // 0')

# Convert distance from meters to km
DISTANCE_KM=$(echo "scale=2; $DISTANCE / 1000" | bc)

# Convert time from milliseconds to minutes
TIME_MIN=$(echo "scale=1; $TIME / 60000" | bc)

echo "=========================================="
echo "Route Summary"
echo "=========================================="
echo "Distance:  ${DISTANCE_KM} km"
echo "Time:      ${TIME_MIN} minutes"
echo "Ascent:    ${ASCENT} m"
echo "Descent:   ${DESCENT} m"
echo ""

# Show first few instructions
echo "=========================================="
echo "Turn-by-Turn Instructions (first 5)"
echo "=========================================="
echo "$RESPONSE" | jq -r '.paths[0].instructions[:5] | .[] | "\(.distance)m - \(.text)"'
echo ""

# Optionally save full response
OUTPUT_FILE="test-route-response.json"
echo "$RESPONSE" | jq '.' > "$OUTPUT_FILE"
echo "Full response saved to: $OUTPUT_FILE"
echo ""

# Show coordinate count (useful for route complexity)
COORD_COUNT=$(echo "$RESPONSE" | jq '.paths[0].points.coordinates | length')
echo "Route has $COORD_COUNT coordinate points"

echo ""
echo "✅ Test complete!"
