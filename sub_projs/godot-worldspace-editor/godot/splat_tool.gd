class_name SplatTool
extends Node
## Per-vertex terrain texture-alpha painting (the front end for ModForge
## worldspace.textureLayers). Holds N layers, each = an LTEX ref + an alpha grid sized to the
## terrain vertex grid (verts_x × verts_y). LMB in Splat Mode paints the ACTIVE layer's alpha;
## export writes each layer to an 8-bit grayscale PNG that feeds textureLayers[].splatmap.
##
## Alpha grid is [row * verts_x + col], row 0 = south, col 0 = west — the SAME layout the
## heightmap uses, so a splatmap PNG co-registers with terrain.png vertex-for-vertex.
## Visual feedback: the active layer's alpha tints the terrain vertex colors (terrain_mesh.gd).

# Display tints cycled per layer index — purely for in-editor feedback, not exported.
const LAYER_TINTS := [
	Color(0.30, 0.62, 0.25),  # grass green
	Color(0.62, 0.34, 0.20),  # dirt brown
	Color(0.70, 0.70, 0.74),  # rock grey
	Color(0.92, 0.92, 0.96),  # snow white
]

var _terrain: TerrainGrid
var _tex_fetch: TexFetch       # resolves LTEX refs → real ground textures (may be null/offline)
var layers: Array = []        # each: { "texture": String, "alpha": PackedFloat32Array }
var active: int = 0

# The un-painted ground (BTXT base layer). WYSIWYG shows this everywhere a layer's alpha is 0.
var base_texture := "Skyrim.esm:0x000C14"   # LDirt01

var radius   := 4.0
var strength := 3.0           # alpha gained per second at full falloff
var erase    := false

signal changed                # layer set / active / paint changed


func configure(terrain: TerrainGrid, tex_fetch: TexFetch = null) -> void:
	_terrain = terrain
	_tex_fetch = tex_fetch
	add_layer("Skyrim.esm:0x013428")   # LFieldGrass01 — a real grass layer to start
	refresh_textures(false)            # load disk-cached PNGs only; no blocking CLI fetch at startup


func set_base_texture(ref: String) -> void:
	base_texture = ref
	refresh_textures(true)             # explicit ref commit → fetch the real texture
	changed.emit()


# Resolve every layer (+ base) to a real Texture2D and push the WYSIWYG blend into the terrain.
# allow_fetch=true permits the (slow, blocking) CLI export for refs not yet cached; false only
# loads what's already on disk. When nothing resolves, apply() keeps the height-gradient fallback.
func refresh_textures(allow_fetch: bool = false) -> void:
	if _terrain == null:
		return
	var base_tex: Texture2D = _tex_fetch.get_texture(base_texture, allow_fetch) if _tex_fetch else null
	var resolved: Array = []
	for l in layers:
		var t: Texture2D = _tex_fetch.get_texture(l["texture"], allow_fetch) if _tex_fetch else null
		resolved.append({ "tex": t, "alpha": l["alpha"] })
	_terrain.apply_textures(base_tex, resolved)


func _has_real_textures() -> bool:
	return _tex_fetch != null and _tex_fetch.available()


func add_layer(texture: String = "") -> void:
	var a := PackedFloat32Array()
	a.resize(_terrain.verts_x * _terrain.verts_y)
	a.fill(0.0)
	layers.append({ "texture": texture, "alpha": a })
	set_active(layers.size() - 1)


func set_active(i: int) -> void:
	active = clampi(i, 0, layers.size() - 1)
	_refresh_visual()
	changed.emit()


func count() -> int:
	return layers.size()


func active_texture() -> String:
	return layers[active]["texture"]


func set_active_texture(t: String) -> void:
	layers[active]["texture"] = t
	refresh_textures(true)   # re-fetch the new LTEX's real ground texture


func clear_active() -> void:
	layers[active]["alpha"].fill(0.0)
	_refresh_visual()
	changed.emit()


# Paint the active layer's alpha under the cursor, toward 1.0 (or 0.0 when erasing).
func paint(hit_world: Vector3, delta: float) -> void:
	var cv := _terrain.world_to_vert(hit_world)
	var r  := int(ceilf(radius))
	var target := 0.0 if erase else 1.0
	var alpha: PackedFloat32Array = layers[active]["alpha"]
	var touched := false
	for dr in range(-r, r + 1):
		for dc in range(-r, r + 1):
			var row := cv.y + dr
			var col := cv.x + dc
			if row < 0 or row >= _terrain.verts_y or col < 0 or col >= _terrain.verts_x:
				continue
			var dist := Vector2(float(dc), float(dr)).length()
			if dist > radius:
				continue
			var falloff := 1.0 - smoothstep(0.0, radius, dist)
			var i := row * _terrain.verts_x + col
			var amt := minf(delta * strength * falloff, 1.0)
			alpha[i] = clampf(lerpf(alpha[i], target, amt), 0.0, 1.0)
			touched = true
	if touched:
		layers[active]["alpha"] = alpha
		_refresh_visual()
		changed.emit()


# Update the terrain's appearance after an alpha/active/base change. With real textures loaded,
# rebuild the (cheap) alpha textures and blend; otherwise fall back to the vertex-colour tint
# (which needs a mesh rebuild). This is why painting with textures is also lighter than before.
func _refresh_visual() -> void:
	if _has_real_textures():
		refresh_textures()
	else:
		_push_overlay()
		_terrain.rebuild_mesh()


# Share the active layer's alpha + tint into the terrain so the mesh builder can blend it (the
# height-gradient fallback used when no real ground textures are available).
func _push_overlay() -> void:
	_terrain.splat_overlay_alpha = layers[active]["alpha"]
	_terrain.splat_overlay_color = LAYER_TINTS[active % LAYER_TINTS.size()]
