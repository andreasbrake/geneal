var ip_addr = process.env.OPENSHIFT_NODEJS_IP || '127.0.0.1';
var port    = process.env.OPENSHIFT_NODEJS_PORT || '3000'

var express = require('express')
var fs = require('fs')
var db = require('./db.js')

/* Initialize express */
var site = express()
site.use(express.static('static'));

site.get('/',function(req, res){
    res.sendFile(__dirname + '/index.html')
})
site.get('/family/:root&type=:type',function(req, res){
    console.log('getting family of ' + req.params.root)
    var data = {
    	"name":"test",
    	"member":"test"
    }
    res.send(data)	
})
site.get('/export/:name',function(req, res){
    console.log('exporting data of ' + req.params.name)

	fs.readFile(__dirname +  '/data/brake.json', 'utf8', function (err, data) {
		if (err) throw err;
		var data = JSON.parse(data);
		res.send(data)
	});
    
})
site.post('/import',function(req, res){
	console.log('importing data')
	res.redirect('/')
})
site.get('*',function(req, res){
    res.sendFile(__dirname + req.params[0])
})

/* Run on port 3000 */
site.listen(port, ip_addr);