extends Node3D
## WorldspaceEditor — root scene.
##
## Adjust these constants before starting:
##   CELLS_X, CELLS_Y   — number of Skyrim cells
##   MIN_HEIGHT, MAX_HEIGHT — Skyrim game units (e.g. 4000–4500)
##
## Controls:
##   Left-click drag   — paint with current brush
##   Middle-click drag — orbit camera
##   Scroll            — zoom
##   Right-click drag  — pan
##   R / L / F / S     — Raise / Lower / Flatten / Smooth

const CELLS_X    := 3
const CELLS_Y    := 2
const MIN_HEIGHT := 4000.0
const MAX_HEIGHT := 4500.0

var terrain:    TerrainGrid
var camera_rig: CameraRig

var _lbl_pos:   Label
var _lbl_brush: Label
var _cursor:    MeshInstance3D
var _painting   := false
var _ui_width   := 200
var _grid_lines: Array[Node3D] = []
var _player: PlayerController = null


func _ready() -> void:
	SceneBuilder.environment(self)
	terrain = TerrainGrid.new()
	add_child(terrain)
	terrain.configure(CELLS_X, CELLS_Y, MIN_HEIGHT, MAX_HEIGHT)
	camera_rig  = SceneBuilder.camera(self, terrain)
	_cursor     = SceneBuilder.cursor(self)
	_grid_lines = SceneBuilder.grid_outlines(self, CELLS_X, CELLS_Y, terrain)
	_setup_ui()


func _setup_ui() -> void:
	var canvas := CanvasLayer.new()
	add_child(canvas)

	var panel := WorldUI.new()
	panel.set_anchors_and_offsets_preset(Control.PRESET_LEFT_WIDE, Control.PRESET_MODE_MINSIZE)
	canvas.add_child(panel)
	panel.setup(terrain, _ui_width, _update_cursor, _sync_display,
		_on_export_png, _on_import_png, _enter_walk_mode)
	_lbl_pos   = panel.lbl_pos
	_lbl_brush = panel.lbl_brush

	var bottom := Label.new()
	bottom.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_WIDE)
	bottom.add_theme_color_override("font_color", Color.WHITE)
	bottom.text = "Middle-drag: orbit  |  Scroll: zoom  |  Right-drag: pan  |  LMB: paint"
	bottom.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	canvas.add_child(bottom)


# Syncs camera and grid after any display-scale change (height or surface).
func _sync_display() -> void:
	var ds := terrain.step * terrain.vis_surface_scale
	var cx := (terrain.verts_x - 1) * ds * 0.5
	var cz := -(terrain.verts_y - 1) * ds * 0.5
	var cy := (terrain.max_height - terrain.min_height) * 0.5 \
		* TerrainGrid.METERS_PER_UNIT * terrain.vis_height_scale
	camera_rig.target = Vector3(cx, cy, cz)
	camera_rig.refresh()
	_rebuild_grid_lines()
	_update_cursor()


func _rebuild_grid_lines() -> void:
	for mesh in _grid_lines:
		mesh.queue_free()
	_grid_lines = SceneBuilder.grid_outlines(self, CELLS_X, CELLS_Y, terrain)


# ── Walk mode ─────────────────────────────────────────────────────────────────

func _enter_walk_mode() -> void:
	if _player != null:
		return
	terrain.refresh_collision()
	_player = PlayerController.new()
	add_child(_player)
	# Spawn at terrain center, dropped in from just above the surface.
	var ds := terrain.step * terrain.vis_surface_scale
	var cc := terrain.verts_x / 2
	var cr := terrain.verts_y / 2
	var gy := (terrain.get_height(cc, cr) - terrain.min_height) \
		* TerrainGrid.METERS_PER_UNIT * terrain.vis_height_scale
	_player.global_position = Vector3(cc * ds, gy + 3.0, -cr * ds)
	_player.exited.connect(_exit_walk_mode)
	_player.activate()
	# Suspend editor: stop orbit input, cursor/paint processing.
	camera_rig.set_process_input(false)
	set_process(false)
	_painting = false
	_cursor.visible = false


func _exit_walk_mode() -> void:
	if _player == null:
		return
	_player.queue_free()
	_player = null
	camera_rig.get_camera().current = true
	camera_rig.set_process_input(true)
	set_process(true)


# ── Input ─────────────────────────────────────────────────────────────────────

func _input(event: InputEvent) -> void:
	if _player != null:
		return  # walk mode owns input
	if event is InputEventKey and event.pressed and not event.echo:
		match event.keycode:
			KEY_R: terrain.brush_mode = TerrainGrid.BrushMode.RAISE
			KEY_L: terrain.brush_mode = TerrainGrid.BrushMode.LOWER
			KEY_F:
				terrain.brush_mode = TerrainGrid.BrushMode.FLATTEN
				terrain.flatten_height = _mid_height_at_mouse()
			KEY_S: terrain.brush_mode = TerrainGrid.BrushMode.SMOOTH

	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		if not _over_ui(event.position):
			_painting = event.pressed
			if _painting and terrain.brush_mode == TerrainGrid.BrushMode.FLATTEN:
				terrain.flatten_height = _mid_height_at_mouse()


func _process(delta: float) -> void:
	var mouse := get_viewport().get_mouse_position()
	var cam   := camera_rig.get_camera()
	var hit   := terrain.get_hit_position(cam, mouse)

	_cursor.visible = (hit != Vector3.ZERO and not _over_ui(mouse))
	if hit != Vector3.ZERO:
		_cursor.global_position = hit
		_update_cursor()
		var vc  := terrain.world_to_vert(hit)
		var col := clampi(vc.x, 0, terrain.verts_x - 1)
		var row := clampi(vc.y, 0, terrain.verts_y - 1)
		var h   := terrain.get_height(col, row)
		if _lbl_pos:
			_lbl_pos.text = "col %d  row %d\nH: %.0f units" % [col, row, h]

	if _painting and hit != Vector3.ZERO and not _over_ui(mouse):
		terrain.apply_brush(hit, delta)


func _update_cursor() -> void:
	var r := terrain.brush_radius * terrain.step * terrain.vis_surface_scale
	_cursor.scale = Vector3(r, 1.0, r)


func _mid_height_at_mouse() -> float:
	var cam := camera_rig.get_camera()
	var hit := terrain.get_hit_position(cam, get_viewport().get_mouse_position())
	if hit == Vector3.ZERO:
		return (terrain.min_height + terrain.max_height) * 0.5
	var vc := terrain.world_to_vert(hit)
	return terrain.get_height(clampi(vc.x, 0, terrain.verts_x - 1),
	                           clampi(vc.y, 0, terrain.verts_y - 1))


func _over_ui(screen_pos: Vector2) -> bool:
	return screen_pos.x < _ui_width


# ── Export / Import ───────────────────────────────────────────────────────────

func _on_export_png() -> void:
	IoPng.export_dialog(self, terrain, _lbl_brush)

func _on_import_png() -> void:
	IoPng.import_dialog(self, terrain)
