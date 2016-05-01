var map = null
var markers = []
var locations = {}

function initialize() {
	var mapOptions = {
		zoom: 3,
		center: new google.maps.LatLng(45.439645, -75.718734)
	}
	map = new google.maps.Map(document.getElementById('map-canvas'), mapOptions);
}
function clearMarkers(){
	for (var i = 0; i < markers.length; i++) {
		markers[i].setMap(null);
	}
	markers = []
}
function add_marker(address, name){
	var addrId = address

	address = address.replace(/,/g, "")
	address = address.replace(/\//g, "+")
	address = address.replace(/ /g, "+")
	addrId = addrId.replace(/,/g, "")
	addrId = addrId.replace(/\//g, "")
	addrId = addrId.replace(/ /g, "")

	if(addrId == "")
		return

	if(locations[addrId] == undefined){
		var url = "http://maps.google.com/maps/api/geocode/json?address=" + address + "&sensor=false"
	    
	    var xmlHttp = null;
	    xmlHttp = new XMLHttpRequest();
	    xmlHttp.open( "GET", url, false );
	    xmlHttp.send( null );

	    var location = JSON.parse(xmlHttp.responseText).results[0]
	    if(location == undefined)
	    	return
	    location = location.geometry.location;	
	    locations[addrId] = location	
	}else{
		var location = locations[addrId]
	}


	var marker = new google.maps.Marker({
		position: new google.maps.LatLng(location.lat,location.lng),
		map: map,
		title: name
	})

	google.maps.event.addListener(marker, 'click', function() {
		display(marker.title)
	});

	markers.push(marker)
}
google.maps.event.addDomListener(window, 'load', initialize);
