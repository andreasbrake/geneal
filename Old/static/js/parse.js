var treeLevel = 0

function parse_main(){
    draw_tree("brake,andreas,1993")
}
function draw_tree(id){
    display(id)

    document.getElementById("familyTree").innerHTML = ""
    treeLevel = 0
    var mem = find_by_id(id)
    insert("familyTree", mem, 2)

    var children = find_children(id)
    var cElem = "<div id='children'>"
    for(var i=0; i<children.length; i++){
        elemId = children[i].family + "," + children[i].name + "," + children[i].birth[0].split("-")[0]
        cElem += "<div class='child' onclick='display(\"" + elemId + "\")'>" + children[i].name + " " + children[i].family + "</div>"
    }
    cElem += "</div>"

    document.getElementById("familyTree").innerHTML = cElem + document.getElementById("familyTree").innerHTML

    var date = id.split(",")[2]
    if(document.getElementById("slider").value != date){
        document.getElementById("slider").value = date
        slide(date)       
    }

}
function slide(value){
	clearMarkers()
	document.getElementById("date").innerHTML = value
    var aliveList = find_by_alive(value)

    for(var i=0; i<aliveList.length; i++){
    	var mem = aliveList[i]
        var location = mem.birth[1]

        if(mem.misc.moved != undefined && mem.misc.moved.length > 0){
            for(var j=0; j<mem.misc.moved.length; j++){
                if(parseInt(mem.misc.moved[j].split(':')[0]) <= value)
                    location = mem.misc.moved[j].split(':')[1]  
            }    	   
        }

        add_marker(location, (mem.family + "," + mem.name + "," + mem.birth[0].split("-")[0]))//(mem.name + " " + mem.family))
    }
}
function insert(id, mem, type){
    treeLevel++
    if(treeLevel > 6) // Limit size of tree
        return

    if(mem == undefined || mem.name == undefined){
        return
    }

    var elemId = mem.family + "," + mem.name + "," + mem.birth[0].split("-")[0]


    if(type == 0)
        var elem = "<div class='member upper'"
    else if(type == 1)
        var elem = "<div class='member lower'"
    else
        var elem = "<div class='member first'"

    elem += " id='" + elemId + "'><div class='name' onclick='display(\"" + elemId + "\")'>" + mem.name + " " + mem.family + "</div></div>"

    document.getElementById(id).innerHTML = document.getElementById(id).innerHTML + elem

    if(mem.parents[0] != ""){
        var parent0 = find_by_id(mem.parents[0])
        insert(elemId, parent0, 0)
        treeLevel--
    }

    if(mem.parents[1] != ""){
        var parent1 = find_by_id(mem.parents[1])
        insert(elemId, parent1, 1)
        treeLevel--
    }
    
}
function display(memId){
    var mem = find_by_id(memId)

    document.getElementById("mem-info").style["opacity"] = 1

    document.getElementById("mem-name").innerHTML = mem.name + " " + mem.family
    document.getElementById("mem-birth").innerHTML = "Born: " + mem.birth[0] + " " + mem.birth[1]
    if(mem.death.length > 0)
        document.getElementById("mem-death").innerHTML = "Died: " + mem.death[0] + " " + mem.death[1]
    else
        document.getElementById("mem-death").innerHTML = "Still Alive"
    document.getElementById("mem-find").onclick = function(){draw_tree(memId)}
}
function find_by_id(id){
    last = id.split(",")[0]
    first = id.split(",")[1]
    byear = id.split(",")[2]

    for(var i=0; i<family.length; i++){
        if(last.toUpperCase() == family[i].name.toUpperCase()){
            for(var j=0; j<family[i].members.length; j++){
                if(first.toUpperCase() == family[i].members[j].name.toUpperCase() && byear == family[i].members[j].birth[0].split("-")[0]){
                    var mem = family[i].members[j]
                    mem.family = last
                    return mem
                }
            }
        }
    }
    return {}
}
function find_by_alive(y){
	var testYear = parseInt(y)
    var mems = []

    for(var i=0; i<family.length; i++){
        for(var j=0; j<family[i].members.length; j++){
        	var mem = family[i].members[j]
        	var born = parseInt(mem.birth[0].split("-")[0])

        	var died = -1
        	if(mem.death[0] != undefined){
        		died = parseInt(mem.death[0].split("-")[0])
        	}
        	
        	if(born <= testYear){
        		if(died < 0 || died > testYear){
        			mem.family = family[i].name
        			mems.push(mem)
        		}
        	}
        }
    }
    return mems
}
function find_children(memId){
    var children = []
    for(var i=0; i<family.length; i++){
        for(var j=0; j<family[i].members.length; j++){
            var p1 = family[i].members[j].parents[0].toUpperCase()
            var p2 = family[i].members[j].parents[1].toUpperCase()

            if(p1 == memId.toUpperCase() || p2 == memId.toUpperCase())
                children.push(family[i].members[j])
        }
    }
    return children
}