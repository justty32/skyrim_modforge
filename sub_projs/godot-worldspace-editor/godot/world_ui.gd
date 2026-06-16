class_name WorldUI
extends PanelContainer

var lbl_pos: Label
var lbl_brush: Label

var _height_sl: HSlider
var _surface_sl: HSlider


func setup(terrain: TerrainGrid, ui_width: int, on_cursor_update: Callable,
		on_display_sync: Callable, on_export: Callable, on_import: Callable,
		on_walk: Callable) -> void:
	custom_minimum_size.x = ui_width

	var scroll := ScrollContainer.new()
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	add_child(scroll)

	var vbox := VBoxContainer.new()
	vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	vbox.add_theme_constant_override("separation", 4)
	scroll.add_child(vbox)

	# ── View modes ────────────────────────────────────────────────────────────
	var btn_edit := Button.new()
	btn_edit.text = "Editor View (reset scale)"
	btn_edit.pressed.connect(func(): _apply_preset(10.0, 1.0))
	vbox.add_child(btn_edit)

	var btn_walk := Button.new()
	btn_walk.text = "▶ Walk Mode (WASD, ESC exits)"
	btn_walk.pressed.connect(on_walk)
	vbox.add_child(btn_walk)
	_add_sep(vbox)

	# ── Info ──────────────────────────────────────────────────────────────────
	_add_lbl(vbox, "Worldspace Editor", true)
	_add_lbl(vbox, "%d×%d cells  |  %d×%d verts" % [
		terrain.cells_x, terrain.cells_y, terrain.verts_x, terrain.verts_y])
	_add_lbl(vbox, "H range: %.0f–%.0f units" % [terrain.min_height, terrain.max_height])
	_add_sep(vbox)

	# ── Display scale ─────────────────────────────────────────────────────────
	_add_lbl(vbox, "DISPLAY SCALE")
	_add_lbl(vbox, "Height (Y):")
	_height_sl = _add_slider_spin(vbox, 1.0, 50.0, terrain.vis_height_scale, 1.0, "×",
		func(v: float):
			terrain.vis_height_scale = v
			terrain.rebuild_mesh()
			on_display_sync.call())

	_add_lbl(vbox, "Surface (X/Z):")
	_surface_sl = _add_slider_spin(vbox, 0.1, 10.0, terrain.vis_surface_scale, 0.1, "×",
		func(v: float):
			terrain.vis_surface_scale = v
			terrain.rebuild_mesh()
			on_display_sync.call())
	_add_sep(vbox)

	# ── Brush ─────────────────────────────────────────────────────────────────
	_add_lbl(vbox, "BRUSH MODE  (R/L/F/S)")
	var bg := ButtonGroup.new()
	for info: Array in [["Raise (R)", 0], ["Lower (L)", 1], ["Flatten (F)", 2], ["Smooth (S)", 3]]:
		var btn := Button.new()
		btn.text = info[0]; btn.toggle_mode = true; btn.button_group = bg
		btn.button_pressed = (int(info[1]) == 0)
		var mode: int = info[1]
		btn.toggled.connect(func(on: bool): if on: terrain.brush_mode = mode)
		vbox.add_child(btn)
	_add_sep(vbox)

	_add_lbl(vbox, "Radius (verts):")
	_add_slider_spin(vbox, 0.5, 32.0, terrain.brush_radius, 0.5, "v",
		func(v: float): terrain.brush_radius = v; on_cursor_update.call())

	_add_lbl(vbox, "Strength (units/s):")
	_add_slider_spin(vbox, 1.0, 100.0, terrain.brush_strength, 1.0, "u/s",
		func(v: float): terrain.brush_strength = v)
	_add_sep(vbox)

	# ── Status ────────────────────────────────────────────────────────────────
	_add_lbl(vbox, "POSITION")
	lbl_pos   = _add_lbl(vbox, "Hover over terrain")
	lbl_brush = _add_lbl(vbox, "")
	_add_sep(vbox)

	# ── Export ────────────────────────────────────────────────────────────────
	_add_lbl(vbox, "EXPORT")
	var btn_exp := Button.new(); btn_exp.text = "Save PNG (16-bit)"
	btn_exp.pressed.connect(on_export); vbox.add_child(btn_exp)
	var btn_imp := Button.new(); btn_imp.text = "Load PNG"
	btn_imp.pressed.connect(on_import); vbox.add_child(btn_imp)


# Setting slider.value triggers the value_changed chain → terrain + on_display_sync.
func _apply_preset(h_scale: float, s_scale: float) -> void:
	_height_sl.value  = h_scale
	_surface_sl.value = s_scale


func _add_lbl(parent: Control, text: String, bold: bool = false) -> Label:
	var lbl := Label.new(); lbl.text = text
	if bold: lbl.add_theme_font_size_override("font_size", 13)
	parent.add_child(lbl)
	return lbl


func _add_sep(parent: Control) -> void:
	parent.add_child(HSeparator.new())


func _add_slider_spin(parent: Control, min_v: float, max_v: float,
		init_v: float, step_v: float, suffix: String, cb: Callable) -> HSlider:
	var hbox := HBoxContainer.new(); parent.add_child(hbox)
	var sl := HSlider.new()
	sl.min_value = min_v; sl.max_value = max_v; sl.value = init_v; sl.step = step_v
	sl.size_flags_horizontal = Control.SIZE_EXPAND_FILL; hbox.add_child(sl)
	var sp := SpinBox.new()
	sp.min_value = min_v; sp.max_value = max_v; sp.value = init_v; sp.step = step_v
	sp.suffix = suffix; sp.custom_minimum_size.x = 80; hbox.add_child(sp)
	sl.value_changed.connect(func(v: float): sp.value = v; cb.call(v))
	sp.value_changed.connect(func(v: float): sl.value = v)
	return sl
