class_name TerrainCoords
## Pure coordinate-system math for TerrainGrid: Skyrim↔Godot↔display-world↔vertex conversions and
## camera ray picking. Split out of terrain.gd so the grid node keeps just height data + brush/mesh
## state; TerrainGrid forwards its public coord methods here (same static-helper pattern as
## TerrainMeshBuilder / TerrainBrush). All funcs take the grid `t` and read its scales/bounds.

const MPU := TerrainGrid.METERS_PER_UNIT


static func world_to_vert(t: TerrainGrid, world_pos: Vector3) -> Vector2i:
	var ds := t.step * t.vis_surface_scale
	return Vector2i(int(roundf(world_pos.x / ds)), int(roundf(-world_pos.z / ds)))


static func vert_to_world(t: TerrainGrid, col: int, row: int) -> Vector3:
	var ds := t.step * t.vis_surface_scale
	return Vector3(col * ds, (t.get_height(col, row) - t.min_height) * MPU * t.vis_height_scale, -row * ds)


# Display world pos → canonical Godot-native metres (display scales divided out). placements.json
# positions are these unscaled metres; ModForge converts to game units. Y: surface height h (game
# units) maps to canonical y = h × MPU, so ModForge's skyrim_z = y / MPU recovers h (sits on ground).
static func world_to_canonical_meters(t: TerrainGrid, display_world: Vector3) -> Vector3:
	return Vector3(
		display_world.x / t.vis_surface_scale,
		display_world.y / t.vis_height_scale + t.min_height * MPU,
		display_world.z / t.vis_surface_scale)


static func canonical_meters_to_world(t: TerrainGrid, m: Vector3) -> Vector3:
	return Vector3(
		m.x * t.vis_surface_scale,
		(m.y - t.min_height * MPU) * t.vis_height_scale,
		m.z * t.vis_surface_scale)


# Display-world Y of the terrain surface at the vertex nearest a clicked point (placement snaps here;
# get_hit_position returns the mid-plane Y, not the surface).
static func surface_display_y(t: TerrainGrid, display_world: Vector3) -> float:
	var vc := world_to_vert(t, display_world)
	var col := clampi(vc.x, 0, t.verts_x - 1)
	var row := clampi(vc.y, 0, t.verts_y - 1)
	return (t.get_height(col, row) - t.min_height) * MPU * t.vis_height_scale


# Ray from screen → the terrain mid-height plane, clamped to the grid extent.
static func get_hit_position(t: TerrainGrid, camera: Camera3D, screen_pos: Vector2) -> Vector3:
	if camera == null: return Vector3.ZERO
	var from := camera.project_ray_origin(screen_pos)
	var dir  := camera.project_ray_normal(screen_pos)
	if absf(dir.y) < 0.001: return Vector3.ZERO
	var mid_y := (t.max_height - t.min_height) * 0.5 * MPU * t.vis_height_scale
	var tt    := (mid_y - from.y) / dir.y
	if tt < 0.0: return Vector3.ZERO
	var hit := from + dir * tt
	var ds  := t.step * t.vis_surface_scale
	hit.x = clampf(hit.x, 0.0, (t.verts_x - 1) * ds)
	hit.z = clampf(hit.z, -(t.verts_y - 1) * ds, 0.0)
	return hit
