var fs = require('fs')

function init(){
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