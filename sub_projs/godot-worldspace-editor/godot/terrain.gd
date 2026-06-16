class_name TerrainGrid
extends Node3D
## Editable terrain mesh based on a (verts_x × verts_y) height grid.
##
## Coordinate mapping (Skyrim ↔ Godot):
##   col 0 = west,  col (verts_x-1) = east
##   row 0 = south, row (verts_y-1) = north
##   Godot X = col × step  (east)
##   Godot Z = -row × step (north = -Z because Godot +Z is south)
##   Godot Y = (height - min_height) × METERS_PER_UNIT × vis_height_scale
##
## Heights stored in Skyrim game units (e.g. 4000–4500).
## step = 128 game_units × 0.014286 m/unit ≈ 1.83 m (vertex spacing).

const METERS_PER_UNIT := 0.014286
const VERT_STEP_UNITS := 128.0
const STEP := VERT_STEP_UNITS * METERS_PER_UNIT

# ── Configuration ────────────────────────────────────────────────────────────
var cells_x    := 3
var cells_y    := 2
var min_height := 4000.0
var max_height := 4500.0

# ── Derived ──────────────────────────────────────────────────────────────────
var verts_x: int
var verts_y: int
var step: float = STEP
var vis_height_scale   := 10.0  # vertical exaggeration for display only
var vis_surface_scale  := 1.0   # horizontal (X/Z) exaggeration for display only

# ── Height data ──────────────────────────────────────────────────────────────
var heights: PackedFloat32Array  # [row * verts_x + col], game units

# ── Brush ─────────────────────────────────────────────────────────────────────
enum BrushMode { RAISE = 0, LOWER = 1, FLATTEN = 2, SMOOTH = 3 }
var brush_mode: int     = BrushMode.RAISE
var brush_radius        := 4.0
var brush_strength      := 50.0
var flatten_height      := 4000.0

var _mesh_inst: MeshInstance3D
var _collision: CollisionShape3D  # terrain walk collision (lazily refreshed)

signal terrain_changed


func _ready() -> void:
	_mesh_inst = MeshInstance3D.new()
	var mat := StandardMaterial3D.new()
	mat.vertex_color_use_as_albedo = true  # height gradient is baked into vertex colors
	mat.roughness = 0.9
	_mesh_inst.material_override = mat
	add_child(_mesh_inst)

	var body := StaticBody3D.new()
	add_child(body)
	_collision = CollisionShape3D.new()
	body.add_child(_collision)


func configure(cx: int, cy: int, min_h: float, max_h: float) -> void:
	cells_x = cx; cells_y = cy
	min_height = min_h; max_height = max_h
	verts_x = cells_x * 32 + 1
	verts_y = cells_y * 32 + 1
	heights = PackedFloat32Array()
	heights.resize(verts_x * verts_y)
	heights.fill((min_h + max_h) * 0.5)
	rebuild_mesh()


# ── Accessors ────────────────────────────────────────────────────────────────

func get_height(col: int, row: int) -> float:
	return heights[row * verts_x + col]


func set_height(col: int, row: int, h: float) -> void:
	heights[row * verts_x + col] = clampf(h, min_height, max_height)


func world_to_vert(world_pos: Vector3) -> Vector2i:
	var ds := step * vis_surface_scale
	return Vector2i(
		int(roundf(world_pos.x / ds)),
		int(roundf(-world_pos.z / ds))
	)


func vert_to_world(col: int, row: int) -> Vector3:
	var ds := step * vis_surface_scale
	return Vector3(
		col * ds,
		(get_height(col, row) - min_height) * METERS_PER_UNIT * vis_height_scale,
		-row * ds
	)


func get_hit_position(camera: Camera3D, screen_pos: Vector2) -> Vector3:
	if camera == null: return Vector3.ZERO
	var from := camera.project_ray_origin(screen_pos)
	var dir  := camera.project_ray_normal(screen_pos)
	if absf(dir.y) < 0.001: return Vector3.ZERO
	var mid_y := (max_height - min_height) * 0.5 * METERS_PER_UNIT * vis_height_scale
	var t     := (mid_y - from.y) / dir.y
	if t < 0.0: return Vector3.ZERO
	var hit := from + dir * t
	var ds  := step * vis_surface_scale
	hit.x = clampf(hit.x, 0.0, (verts_x - 1) * ds)
	hit.z = clampf(hit.z, -(verts_y - 1) * ds, 0.0)
	return hit


# ── Brush / Mesh (delegated) ──────────────────────────────────────────────────

func apply_brush(hit_world: Vector3, delta: float) -> void:
	if TerrainBrush.apply(self, hit_world, delta):
		rebuild_mesh()
		terrain_changed.emit()


func rebuild_mesh() -> void:
	_mesh_inst.mesh = TerrainMeshBuilder.build(self)


# Regenerate walk collision from the current mesh. Call before entering walk mode
# (skipped during editing so brush strokes stay cheap).
func refresh_collision() -> void:
	if _collision and _mesh_inst.mesh:
		_collision.shape = _mesh_inst.mesh.create_trimesh_shape()
