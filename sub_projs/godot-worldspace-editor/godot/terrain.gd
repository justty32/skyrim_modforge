class_name TerrainGrid
extends Node3D
## Editable terrain mesh based on a (verts_x × verts_y) height grid.
##
## Coordinate mapping (Skyrim ↔ Godot):
##   col 0 = west,  col (verts_x-1) = east
##   row 0 = south, row (verts_y-1) = north
##   Godot X = col × step  (east)
##   Godot Z = -row × step (north = -Z because Godot +Z is south)
##   Godot Y = height_game_units × METERS_PER_UNIT
##
## Heights stored in Skyrim game units (e.g. 4000–4500).
## step = 128 game_units × 0.014286 m/unit ≈ 1.83 m (vertex spacing).

const METERS_PER_UNIT := 0.014286
const VERT_STEP_UNITS := 128.0  # game units between adjacent vertices
const STEP := VERT_STEP_UNITS * METERS_PER_UNIT  # ≈ 1.829 m

# ── Configuration (set via configure()) ─────────────────────────────────────
var cells_x   := 3
var cells_y   := 2
var min_height := 4000.0  # game units
var max_height := 4500.0

# ── Derived ─────────────────────────────────────────────────────────────────
var verts_x: int  # = cells_x * 32 + 1
var verts_y: int  # = cells_y * 32 + 1
var step: float = STEP

# ── Height data ─────────────────────────────────────────────────────────────
var heights: PackedFloat32Array   # [row * verts_x + col], game units

# ── Brush ────────────────────────────────────────────────────────────────────
enum BrushMode { RAISE = 0, LOWER = 1, FLATTEN = 2, SMOOTH = 3 }
var brush_mode: int = BrushMode.RAISE
var brush_radius   := 4.0   # vertices
var brush_strength := 50.0  # game units / second at full strength
var flatten_height := 4000.0

# ── Visuals ──────────────────────────────────────────────────────────────────
var _mesh_inst: MeshInstance3D

signal terrain_changed


func _ready() -> void:
	_mesh_inst = MeshInstance3D.new()
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(0.38, 0.55, 0.28)
	mat.roughness = 0.85
	_mesh_inst.material_override = mat
	add_child(_mesh_inst)


func configure(cx: int, cy: int, min_h: float, max_h: float) -> void:
	cells_x = cx; cells_y = cy
	min_height = min_h; max_height = max_h
	verts_x = cells_x * 32 + 1
	verts_y = cells_y * 32 + 1
	var mid := (min_h + max_h) * 0.5
	heights = PackedFloat32Array()
	heights.resize(verts_x * verts_y)
	heights.fill(mid)
	rebuild_mesh()


# ── Accessors ────────────────────────────────────────────────────────────────

func get_height(col: int, row: int) -> float:
	return heights[row * verts_x + col]


func set_height(col: int, row: int, h: float) -> void:
	heights[row * verts_x + col] = clampf(h, min_height, max_height)


## Convert Godot world position → nearest vertex (col, row). Unclamped.
func world_to_vert(world_pos: Vector3) -> Vector2i:
	return Vector2i(
		int(roundf(world_pos.x / step)),
		int(roundf(-world_pos.z / step))
	)


## Convert vertex (col, row) → Godot world position at that vertex's height.
func vert_to_world(col: int, row: int) -> Vector3:
	return Vector3(
		col * step,
		get_height(col, row) * METERS_PER_UNIT,
		-row * step
	)


## Cast a camera ray and return the approximate terrain hit (plane intersection
## at mid-height). Returns Vector3.ZERO if the ray misses or points upward.
func get_hit_position(camera: Camera3D, screen_pos: Vector2) -> Vector3:
	if camera == null: return Vector3.ZERO
	var from := camera.project_ray_origin(screen_pos)
	var dir  := camera.project_ray_normal(screen_pos)
	if absf(dir.y) < 0.001: return Vector3.ZERO
	var mid_y := (min_height + max_height) * 0.5 * METERS_PER_UNIT
	var t     := (mid_y - from.y) / dir.y
	if t < 0.0: return Vector3.ZERO
	var hit := from + dir * t
	# Clamp to terrain bounds.
	hit.x = clampf(hit.x, 0.0, (verts_x - 1) * step)
	hit.z = clampf(hit.z, -(verts_y - 1) * step, 0.0)
	return hit


# ── Brush ─────────────────────────────────────────────────────────────────────

func apply_brush(hit_world: Vector3, delta: float) -> void:
	var cv := world_to_vert(hit_world)
	var r  := int(ceilf(brush_radius))
	var changed := false
	for dr in range(-r, r + 1):
		for dc in range(-r, r + 1):
			var row := cv.y + dr
			var col := cv.x + dc
			if row < 0 or row >= verts_y or col < 0 or col >= verts_x:
				continue
			var dist := Vector2(float(dc), float(dr)).length()
			if dist > brush_radius:
				continue
			var falloff := 1.0 - smoothstep(0.0, brush_radius, dist)
			match brush_mode:
				BrushMode.RAISE:
					set_height(col, row, get_height(col, row) + delta * falloff * brush_strength)
				BrushMode.LOWER:
					set_height(col, row, get_height(col, row) - delta * falloff * brush_strength)
				BrushMode.FLATTEN:
					var h := get_height(col, row)
					set_height(col, row, lerpf(h, flatten_height, minf(delta * falloff * 3.0, 1.0)))
				BrushMode.SMOOTH:
					_smooth_vertex(col, row, delta * falloff)
			changed = true
	if changed:
		rebuild_mesh()
		terrain_changed.emit()


func _smooth_vertex(col: int, row: int, t: float) -> void:
	var sum := 0.0; var count := 0
	for dr in [-1, 0, 1]:
		for dc in [-1, 0, 1]:
			var nr := row + dr; var nc := col + dc
			if nr >= 0 and nr < verts_y and nc >= 0 and nc < verts_x:
				sum += get_height(nc, nr); count += 1
	if count > 0:
		set_height(col, row, lerpf(get_height(col, row), sum / count, minf(t * 4.0, 1.0)))


# ── Mesh generation ──────────────────────────────────────────────────────────

func rebuild_mesh() -> void:
	var verts   := PackedVector3Array(); verts.resize(verts_x * verts_y)
	var normals := PackedVector3Array(); normals.resize(verts_x * verts_y)
	var uvs     := PackedVector2Array(); uvs.resize(verts_x * verts_y)
	var indices := PackedInt32Array();   indices.resize((verts_x - 1) * (verts_y - 1) * 6)

	# Vertices + UVs
	for row in verts_y:
		for col in verts_x:
			var idx := row * verts_x + col
			verts[idx] = Vector3(col * step, get_height(col, row) * METERS_PER_UNIT, -row * step)
			uvs[idx]   = Vector2(float(col) / (verts_x - 1), float(row) / (verts_y - 1))

	# Normals (central difference; edges clamped)
	for row in verts_y:
		for col in verts_x:
			var he := _h(col + 1, row); var hw := _h(col - 1, row)
			var hn := _h(col, row + 1); var hs := _h(col, row - 1)
			# dH/dX in Godot space: east = +X, north = -Z → dZ/col = -1
			var dh_east  := (he - hw) * METERS_PER_UNIT / (2.0 * step)
			var dh_north := (hn - hs) * METERS_PER_UNIT / (2.0 * step)
			normals[row * verts_x + col] = Vector3(-dh_east, 1.0, dh_north).normalized()

	# Quad indices (two triangles per quad, consistent winding)
	var i := 0
	for row in (verts_y - 1):
		for col in (verts_x - 1):
			var base := row * verts_x + col
			indices[i]     = base;              i += 1
			indices[i]     = base + verts_x;    i += 1
			indices[i]     = base + 1;           i += 1
			indices[i]     = base + 1;           i += 1
			indices[i]     = base + verts_x;    i += 1
			indices[i]     = base + verts_x + 1; i += 1

	var arrays := []
	arrays.resize(Mesh.ARRAY_MAX)
	arrays[Mesh.ARRAY_VERTEX] = verts
	arrays[Mesh.ARRAY_NORMAL] = normals
	arrays[Mesh.ARRAY_TEX_UV] = uvs
	arrays[Mesh.ARRAY_INDEX]  = indices

	var am := ArrayMesh.new()
	am.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arrays)
	_mesh_inst.mesh = am


## Clamped height lookup for normal computation.
func _h(col: int, row: int) -> float:
	return get_height(clampi(col, 0, verts_x - 1), clampi(row, 0, verts_y - 1))
