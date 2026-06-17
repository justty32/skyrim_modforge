class_name PlacedObject
extends Node3D
## One placed worldspace object: a thin metadata carrier + a box proxy visual.
##
## The box is a placeholder. Real .nif → glTF proxies arrive via the
## model-converter sub_proj; until then a colour-coded box marks the base ref.
## Only position / rotation / uniform_scale feed placements.json — the box size
## is purely cosmetic and never exported.

var skyrim_base: String = ""    # "<master>:0xFORMID" or in-spec editorId
var instance_id: String = ""    # "" = anonymous REFR (omitted from export)
var uniform_scale: float = 1.0  # Skyrim REFR scale, independent of display scales

const _BOX_SIZE := Vector3(1.5, 3.0, 1.5)  # rough humanoid-ish proxy, metres


func setup(base: String, inst_id: String, color: Color) -> void:
	skyrim_base = base
	instance_id = inst_id
	var box := MeshInstance3D.new()
	var bm := BoxMesh.new()
	bm.size = _BOX_SIZE
	box.mesh = bm
	var mat := StandardMaterial3D.new()
	mat.albedo_color = color
	box.material_override = mat
	box.position = Vector3(0, _BOX_SIZE.y * 0.5, 0)  # base sits at the node origin (feet)
	add_child(box)
