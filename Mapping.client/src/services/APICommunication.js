

function redrawFlightPaths() {
    updateDownloadablePoints();

    flightPoints = [];

    for (var i = 0; i < map.lines.length; i++) {
        map.lines[i].setMap(null);
    }

    for (var i = 0; i < map.flags.length; i++) {
        flightPoints.push({ lat: map.flags[i].getPosition().lat(), lng: map.flags[i].getPosition().lng() });
    }

    const flightPath = new google.maps.Polyline({
        path: flightPoints,
        geodesic: true,
        strokeColor: "#FF0000",
        strokeOpacity: 1.0,
        strokeWeight: 10,
    });

    flightPath.setMap(map);
    map.lines.push(flightPath);

    if (flightPoints.length != 0) {
        document.getElementById("downloadBtn").disabled = false;
        document.getElementById("saveBtn").disabled = false;
    }
}

export { redrawFlightPaths };
