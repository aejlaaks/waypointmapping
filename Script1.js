// JavaScript source code
var selectedShape;

function WaypointEditorSave() {
    for (var i = 0; i < map.flags.length; i++) {
        if (map.flags[i].id == document.getElementById("selectedWaypointId").innerHTML) {
            if (map.flags[i].heading != document.getElementById("editWaypointHeading").value) {
                const updateMarker = {
                    path: 'M 230 80 A 45 45, 0, 1, 0, 275 125 L 275 80 Z',
                    fillOpacity: 0.8,
                    fillColor: 'blue',
                    anchor: new google.maps.Point(228, 125),
                    strokeWeight: 3,
                    strokeColor: 'white',
                    scale: 0.5,
                    rotation: document.getElementById("editWaypointHeading").value - 45,
                    labelOrigin: new google.maps.Point(228, 125),
                };

                map.flags[i].setIcon(updateMarker);
            }


            map.flags[i].altitude = document.getElementById("editWaypointAltitude").value;
            map.flags[i].speed = document.getElementById("editWaypointSpeed").value;
            map.flags[i].angle = document.getElementById("editWaypointAngle").value;
            map.flags[i].heading = document.getElementById("editWaypointHeading").value;
            map.flags[i].action = document.getElementById("editWaypointAction").value

            if (document.getElementById("editWaypointID").value > map.flags.length) {
                document.getElementById("editWaypointID").value = map.flags.length;
            }

            if (map.flags[i].id != document.getElementById("editWaypointID").value) { //if old id value not equal to new one
                var perc = 0;
                for (var x = 0; x < map.flags.length; x++) { //push
                    var originalId = map.flags[i].id;
                    if (map.flags[x].id == document.getElementById("editWaypointID").value || perc == 1) {
                        if (map.flags[x].id < originalId) {
                            map.flags[x].id = parseInt(map.flags[x].id) + parseInt('1');
                            perc = 1;
                        }
                        else if (map.flags[x].id >= originalId) {
                            var temp = map.flags[x].id;
                            for (var y = temp - 1; y >= originalId; y--) {
                                map.flags[y].id = parseInt(map.flags[y].id) - parseInt('1');
                                perc = 1;
                            }

                            map.flags[i].id = document.getElementById("editWaypointID").value  //changed the id

                            RedrawMarkers();

                            map.flags.sort(function (a, b) {
                                return a.id - b.id;
                            });

                            redrawFlightPaths();

                            return;
                        }
                    }
                }

                map.flags[i].id = document.getElementById("editWaypointID").value  //changed the id

                RedrawMarkers();

                map.flags.sort(function (a, b) {
                    return a.id - b.id;
                });

                redrawFlightPaths();
            }
            return;
        }
    }
}

function WaypointEditiorRemove() {
    for (var i = 0; i < map.flags.length; i++) {
        if (map.flags[i].id == document.getElementById("selectedWaypointId").innerHTML) {

            for (var x = 0; x < map.flags.length; x++) {
                if (map.flags[i].id < map.flags[x].id)
                    map.flags[x].id -= 1;
            }

            RedrawMarkers(map);

            map.flags[i].setMap(null);
            map.flags.splice(i, 1);

            redrawFlightPaths();

            flagCount -= 1;
            return;
        }
    }
}

function ShapeEditiorRemove() {
    selectedShape.setMap(null);
}

function GenerateWaypointInfoboxText(waypointMarker) {
    var select = '';
    if (waypointMarker.action == "noAction") {
        select = '<option selected value="noAction">No Action</option><option value="takePhoto">Take Picture</option><option value="startRecord">Start Recording</option><option value="stopRecord">Stop Recording</option>';
    }
    else if (waypointMarker.action == "takePhoto") {
        select = '<option value="noAction">No Action</option><option selected value="takePhoto">Take Picture</option><option value="startRecord">Start Recording</option><option value="stopRecord">Stop Recording</option>';
    }
    else if (waypointMarker.action == "startRecord") {
        select = '<option value="noAction">No Action</option><option value="takePhoto">Take Picture</option><option selected value="startRecord">Start Recording</option><option value="stopRecord">Stop Recording</option>';
    }
    else if (waypointMarker.action == "stopRecord") {
        select = '<option value="noAction">No Action</option><option value="takePhoto">Take Picture</option><option value="startRecord">Start Recording</option><option selected value="stopRecord">Stop Recording</option>';
    }

    return '<div><h2 class="text-center" id="selectedWaypointId">'
        + waypointMarker.id + '</h2><div class="text-center">'
        + waypointMarker.lat
        + ', '
        + waypointMarker.lng
        + '</div><br/>Altitude:<br/><input type="text" id="editWaypointAltitude" value="'
        + waypointMarker.altitude
        + '" /><span class="unitsLabel">Meters</span><br/>Speed:<br/><input type="text" id="editWaypointSpeed" value="'
        + waypointMarker.speed
        + '" /><span class="unitsLabel">Meters</span>/s<br/>Gimbal Angle:<br/><input type="text" id="editWaypointAngle" value="'
        + waypointMarker.angle
        + '" />Degrees<br/>Heading:<br/><input type="text" id="editWaypointHeading" value="'
        + waypointMarker.heading
        + '" />Degrees North<br/>Action:<br/><select id="editWaypointAction" selected="'
        + waypointMarker.action
        + '" value="'
        + waypointMarker.action
        + '">'
        + select
        + '</select > <br />Waypoint Number:<br/><input type="text" id="editWaypointID" value="'
        + waypointMarker.id
        + '" /><br/><br/><div class="text-center">'
        + '<button class="btn btn-success" onclick="WaypointEditorSave()">Save</button><span> </span>'
        + '<button class="btn btn-danger" onclick="WaypointEditiorRemove()">Remove</button></div></div>';
}

function GenerateShapeInfoboxText(shape) {
    return '<div class="text-center"><h4>Generate Waypoints For Shape?</h4>'
        + '<button class="btn btn-success" onclick="submitFormFetch()">Generate</button><span>   </span>'
        + '<button class="btn btn-danger" onclick="ShapeEditiorRemove()">Remove</button></div>';
}

function RedrawMarkers() {
    var tempFlags = [];

    for (var i = 0; i < map.flags.length; i++) {
        map.flags[i].setLabel(map.flags[i].id.toString() + "");
    }

    return tempFlags;
}

function UpdateTimeEstimate() {
    var totalDistance = 0;
    var totalTime = 0;
    for (var i = 0; i < map.flags.length - 2; i++) {
        var distance = measure(map.flags[i].lat, map.flags[i].lng, map.flags[i + 1].lat, map.flags[i + 1].lng);

        //Math.sqrt(Math.pow((map.flags[i].lng - map.flags[i + 1].lng) * (40075 * Math.cos(map.flags[i].lat) / 360), 2) + Math.pow((map.flags[i].lat - map.flags[i + 1].lat) * 111132.954, 2));
        totalDistance += distance;
        totalTime += (distance / ((map.flags[i].speed + map.flags[i + 1].speed) / 2)) / 60;
    }

    document.getElementById("timeEstimate").innerHTML = "Total Distance of: " + Math.round(totalDistance, 2) + " meters with a total flight time of: " + Math.round(totalTime) + " minutes.";
}

function measure(lat1, lon1, lat2, lon2) {  // generally used geo measurement function
    var R = 6378.137; // Radius of earth in KM
    var dLat = lat2 * Math.PI / 180 - lat1 * Math.PI / 180;
    var dLon = lon2 * Math.PI / 180 - lon1 * Math.PI / 180;
    var a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
        Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
        Math.sin(dLon / 2) * Math.sin(dLon / 2);
    var c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    var d = R * c;
    return d * 1000; // meters
}