#==============================================================================#
# Copyright (c) 2015 Franklin Sobrinho.                                        #
#                                                                              #
# Permission is hereby granted, free of charge, to any person obtaining        #
# a copy of this software and associated documentation files (the "Software"), #
# to deal in the Software without restriction, including without               #
# limitation the rights to use, copy, modify, merge, publish,                  #
# distribute, sublicense, and/or sell copies of the Software, and to           #
# permit persons to whom the Software is furnished to do so, subject to        #
# the following conditions:                                                    #
#                                                                              #
# The above copyright notice and this permission notice shall be               #
# included in all copies or substantial portions of the Software.              #
#                                                                              #
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,              #
# EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF           #
# MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.       #
# IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY         #
# CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,         #
# TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE            #
# SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.                       #
#==============================================================================#

tool
extends EditorPlugin

var add_primitives

var tree

static func get_name():
	return "add_primitives"
	
func set_state(state):
	add_primitives.set_state(state)
	
func get_state():
	var state = {}
	
	add_primitives.get_state(state)
	
	return state
	
func clear():
	add_primitives.clear_state()
	
func get_editor_dpi_scale():
	var dpi = get_editor_settings().get("global/hidpi_mode")
	
	# Auto detect screen dpi
	if dpi == 0:
		return int(OS.get_screen_dpi() > 150) + 1
	
	else:
		return dpi

func get_selected():
	var item = tree.get_selected()
	
	if not item:
		return
		
	var path = item.get_metadata(0)
	
	if has_node(path):
		return get_node(path)
		
	return null
	
func _find_node(type, node):
	if node.is_type(type):
		return node
		
	for i in range(node.get_child_count()):
		var n = _find_node(type, node.get_child(i))
		
		if n:
			return n
			
	return null
	
func _enter_tree():
	add_primitives = preload("res://addons/add_primitives/src/AddPrimitives.gd").new(self)
	add_control_to_container(CONTAINER_SPATIAL_EDITOR_MENU, add_primitives)
	
	add_primitives.get_parent().move_child(add_primitives, 5)
	
	var scene_tree_dock = _find_node('SceneTreeDock', get_parent())
	
	var scene_editor = _find_node('SceneTreeEditor', scene_tree_dock)
	tree = scene_editor.get_child(0)
	
	print("ADD PRIMITIVES INIT")
	
func _exit_tree():
	add_primitives.queue_free()
	

