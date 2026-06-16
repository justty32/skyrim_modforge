extends Node3D
## WorldspaceEditor — root scene.
##
## Adjust these constants for your worldspace before starting:
##   CELLS_X, CELLS_Y  — number of Skyrim cells
##   MIN_HEIGHT, MAX_HEIGHT — Skyrim game units (e.g. 4000–4500)
##
## Controls:
##   Left-click drag  — paint with current brush
##   Middle-click drag — orbit camera
##   Scroll            — zoom
##   Right-click drag  — pan
##   R / L / F / S     — Raise / Lower / Flatten / Smooth

const CELLS_X    := 3
const CELLS_Y    := 2
const MIN_HEIGHT := 4000.0
const MAX_HEIGHT := 4500.0

var terrain: TerrainGrid
var camera_rig: CameraRig

# UI
var _lbl_pos: Label
var _lbl_brush: Label
var _cursor: MeshInstance3D

# Input
var _painting  := false
var _ui_width  := 200


func _ready() -> void:
	_setup_environment()
	_setup_terrain()
	_setup_camera()
	_setup_cursor()
	_setup_ui()
	_setup_grid_outlines()


# ── Scene setup ───────────────────────────────────────────────────────────────

func _setup_environment() -> void:
	var env := Environment.new()
	env.background_mode = Environment.BG_SKY
	var sky_mat := ProceduralSkyMaterial.new()
	sky_mat.sky_top_color     = Color(0.2, 0.4, 0.7)
	sky_mat.sky_horizon_color = Color(0.6, 0.7, 0.8)
	sky_mat.ground_bottom_color = Color(0.3, 0.28, 0.25)
	var sky := Sky.new(); sky.sky_material = sky_mat
	env.sky = sky
	var we := WorldEnvironment.new(); we.environment = env
	add_child(we)

	var sun := DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-50, -30, 0)
	sun.light_energy = 1.4
	sun.shadow_enabled = true
	add_child(sun)


func _setup_terrain() -> void:
	terrain = TerrainGrid.new()
	add_child(terrain)
	terrain.configure(CELLS_X, CELLS_Y, MIN_HEIGHT, MAX_HEIGHT)


func _setup_camera() -> void:
	camera_rig = CameraRig.new()
	add_child(camera_rig)
	# Focus center of terrain, height at midpoint
	var cx := (terrain.verts_x - 1) * terrain.step * 0.5
	var cz := -(terrain.verts_y - 1) * terrain.step * 0.5
	var cy := (MIN_HEIGHT + MAX_HEIGHT) * 0.5 * TerrainGrid.METERS_PER_UNIT
	camera_rig.target   = Vector3(cx, cy, cz)
	camera_rig.distance = maxf((terrain.verts_x + terrain.verts_y) * terrain.step * 0.4, 40.0)


func _setup_cursor() -> void:
	_cursor = MeshInstance3D.new()
	var m := TorusMesh.new()
	m.inner_radius = 0.0; m.outer_radius = 1.0; m.rings = 32; m.ring_segments = 8
	_cursor.mesh = m
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(1, 1, 0, 0.7)
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	_cursor.material_override = mat
	_cursor.visible = false
	add_child(_cursor)


# Draw thin boxes marking each cell boundary (visual guide only).
func _setup_grid_outlines() -> void:
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(1, 1, 1, 0.3)
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED

	var line_h := (MIN_HEIGHT + MAX_HEIGHT) * 0.5 * TerrainGrid.METERS_PER_UNIT

	# Vertical lines (column dividers, every 32 verts)
	for cx in (CELLS_X + 1):
		var mesh := MeshInstance3D.new()
		var bm := BoxMesh.new()
		var depth := (terrain.verts_y - 1) * terrain.step
		bm.size = Vector3(0.2, 2.0, depth)
		mesh.mesh = bm
		mesh.material_override = mat
		mesh.position = Vector3(cx * 32 * terrain.step, line_h, -depth * 0.5)
		add_child(mesh)

	# Horizontal lines (row dividers)
	for cy in (CELLS_Y + 1):
		var mesh := MeshInstance3D.new()
		var bm := BoxMesh.new()
		var width := (terrain.verts_x - 1) * terrain.step
		bm.size = Vector3(width, 2.0, 0.2)
		mesh.mesh = bm
		mesh.material_override = mat
		mesh.position = Vector3(width * 0.5, line_h, -cy * 32 * terrain.step)
		add_child(mesh)


# ── UI ────────────────────────────────────────────────────────────────────────

func _setup_ui() -> void:
	var canvas := CanvasLayer.new()
	add_child(canvas)

	# ─ Left panel ─
	var panel := PanelContainer.new()
	panel.set_anchors_and_offsets_preset(Control.PRESET_LEFT_WIDE, Control.PRESET_MODE_MINSIZE)
	panel.custom_minimum_size.x = _ui_width
	canvas.add_child(panel)

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 4)
	panel.add_child(vbox)

	# Config info
	_add_lbl(vbox, "Worldspace Editor", true)
	_add_lbl(vbox, "%d×%d cells  |  %d×%d verts" % [
		terrain.cells_x, terrain.cells_y, terrain.verts_x, terrain.verts_y])
	_add_lbl(vbox, "H range: %.0f–%.0f units" % [terrain.min_height, terrain.max_height])
	_add_sep(vbox)

	# Tool buttons
	_add_lbl(vbox, "BRUSH MODE  (R/L/F/S)")
	var bg := ButtonGroup.new()
	for info: Array in [["Raise (R)", 0], ["Lower (L)", 1], ["Flatten (F)", 2], ["Smooth (S)", 3]]:
		var btn := Button.new()
		btn.text = info[0]
		btn.toggle_mode = true
		btn.button_group = bg
		btn.button_pressed = (int(info[1]) == 0)
		var mode: int = info[1]
		btn.toggled.connect(func(on: bool): if on: terrain.brush_mode = mode)
		vbox.add_child(btn)
	_add_sep(vbox)

	# Brush radius slider
	_add_lbl(vbox, "Radius (verts):")
	var s_radius := HSlider.new()
	s_radius.min_value = 1.0; s_radius.max_value = 24.0; s_radius.value = terrain.brush_radius
	s_radius.step = 0.5; s_radius.custom_minimum_size.x = _ui_width - 20
	s_radius.value_changed.connect(func(v: float):
		terrain.brush_radius = v; _update_cursor())
	vbox.add_child(s_radius)

	# Brush strength slider
	_add_lbl(vbox, "Strength (units/s):")
	var s_strength := HSlider.new()
	s_strength.min_value = 5.0; s_strength.max_value = 500.0; s_strength.value = terrain.brush_strength
	s_strength.step = 5.0; s_strength.custom_minimum_size.x = _ui_width - 20
	s_strength.value_changed.connect(func(v: float): terrain.brush_strength = v)
	vbox.add_child(s_strength)
	_add_sep(vbox)

	# Status labels
	_add_lbl(vbox, "POSITION")
	_lbl_pos   = _add_lbl(vbox, "Hover over terrain")
	_lbl_brush = _add_lbl(vbox, "")
	_add_sep(vbox)

	# Export/import
	_add_lbl(vbox, "EXPORT")
	var btn_exp := Button.new(); btn_exp.text = "Save PNG (16-bit)"
	btn_exp.pressed.connect(_on_export_png); vbox.add_child(btn_exp)

	var btn_imp := Button.new(); btn_imp.text = "Load PNG"
	btn_imp.pressed.connect(_on_import_png); vbox.add_child(btn_imp)

	# ─ Bottom status bar ─
	var bottom := Label.new()
	bottom.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_WIDE)
	bottom.add_theme_color_override("font_color", Color.WHITE)
	bottom.text = "Middle-drag: orbit  |  Scroll: zoom  |  Right-drag: pan  |  LMB: paint"
	bottom.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	canvas.add_child(bottom)


func _add_lbl(parent: Control, text: String, bold: bool = false) -> Label:
	var lbl := Label.new(); lbl.text = text
	if bold: lbl.add_theme_font_size_override("font_size", 13)
	parent.add_child(lbl)
	return lbl


func _add_sep(parent: Control) -> void:
	parent.add_child(HSeparator.new())


# ── Input ─────────────────────────────────────────────────────────────────────

func _input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and not event.echo:
		match event.keycode:
			KEY_R: terrain.brush_mode = TerrainGrid.BrushMode.RAISE
			KEY_L: terrain.brush_mode = TerrainGrid.BrushMode.LOWER
			KEY_F:
				terrain.brush_mode = TerrainGrid.BrushMode.FLATTEN
				terrain.flatten_height = _mid_height_at_mouse()
			KEY_S: terrain.brush_mode = TerrainGrid.BrushMode.SMOOTH

	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_LEFT:
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

		var vc := terrain.world_to_vert(hit)
		var col := clampi(vc.x, 0, terrain.verts_x - 1)
		var row := clampi(vc.y, 0, terrain.verts_y - 1)
		var h   := terrain.get_height(col, row)
		if _lbl_pos:
			_lbl_pos.text = "col %d  row %d\nH: %.0f units" % [col, row, h]

	if _painting and hit != Vector3.ZERO and not _over_ui(mouse):
		terrain.apply_brush(hit, delta)


func _update_cursor() -> void:
	var r := terrain.brush_radius * terrain.step
	_cursor.scale = Vector3(r, 1.0, r)


func _mid_height_at_mouse() -> float:
	var cam := camera_rig.get_camera()
	var hit := terrain.get_hit_position(cam, get_viewport().get_mouse_position())
	if hit == Vector3.ZERO: return (terrain.min_height + terrain.max_height) * 0.5
	var vc := terrain.world_to_vert(hit)
	return terrain.get_height(clampi(vc.x, 0, terrain.verts_x - 1),
	                          clampi(vc.y, 0, terrain.verts_y - 1))


func _over_ui(screen_pos: Vector2) -> bool:
	return screen_pos.x < _ui_width


# ── Export / Import ───────────────────────────────────────────────────────────

func _on_export_png() -> void:
	var dlg := FileDialog.new()
	dlg.access       = FileDialog.ACCESS_FILESYSTEM
	dlg.file_mode    = FileDialog.FILE_MODE_SAVE_FILE
	dlg.filters      = PackedStringArray(["*.png ; 16-bit Grayscale PNG"])
	dlg.current_file = "terrain.png"
	dlg.confirmed.connect(func(): _do_export(dlg.current_path); dlg.queue_free())
	dlg.canceled.connect(func(): dlg.queue_free())
	add_child(dlg)
	dlg.popup_centered_ratio(0.7)


func _do_export(path: String) -> void:
	var ok := Png16.save(path, terrain.verts_x, terrain.verts_y,
	                      terrain.heights, terrain.min_height, terrain.max_height)
	if ok:
		print("Exported heightmap → " + path)
		if _lbl_brush: _lbl_brush.text = "Saved: " + path.get_file()
	else:
		push_error("Export failed: " + path)


func _on_import_png() -> void:
	var dlg := FileDialog.new()
	dlg.access    = FileDialog.ACCESS_FILESYSTEM
	dlg.file_mode = FileDialog.FILE_MODE_OPEN_FILE
	dlg.filters   = PackedStringArray(["*.png ; PNG Heightmap"])
	dlg.confirmed.connect(func():
		var h := Png16.load_heights(dlg.current_path, terrain.verts_x, terrain.verts_y,
		                             terrain.min_height, terrain.max_height)
		if not h.is_empty():
			terrain.heights = h
			terrain.rebuild_mesh()
			print("Imported: " + dlg.current_path)
		dlg.queue_free())
	dlg.canceled.connect(func(): dlg.queue_free())
	add_child(dlg)
	dlg.popup_centered_ratio(0.7)
