var fs = require('fs')
var pg = require('pg');
var conString = "postgres://postgres:admin@localhost/mydb";

function init(){
	var client = new pg.Client(conString);
	client.connect(function(err) {
		if(err) {
			return console.error('could not connect to postgres', err);
		}

		client.query('SELECT NOW() AS "theTime"', function(err, result) {
			if(err) {
				return console.error('error running query', err);
			}

			console.log(result.rows[0].theTime);
			client.end();
		});
	});
}
function get_files(){
	fs.readdir("./data",function(err, files){
		for(var i=0; i<files.length; i++){
			var file = files[i]
			if(file.split('.')[1] == "json"){
				add_file(file)
			}
		}
	})	
}
function add_file(filename){
	fs.readFile('./data/' + filename, 'utf8', function (err, data) {
		if (err) throw err
		var data = JSON.parse(data)
		console.log(JSON.stringify(data))
	});	
}
init()