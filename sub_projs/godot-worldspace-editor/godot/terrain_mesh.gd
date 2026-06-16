class_name TerrainMeshBuilder

static func build(t: TerrainGrid) -> ArrayMesh:
	var ds      := t.step * t.vis_surface_scale  # display step in X/Z
	var inv_rng := 1.0 / maxf(t.max_height - t.min_height, 1.0)
	var verts   := PackedVector3Array(); verts.resize(t.verts_x * t.verts_y)
	var normals := PackedVector3Array(); normals.resize(t.verts_x * t.verts_y)
	var uvs     := PackedVector2Array(); uvs.resize(t.verts_x * t.verts_y)
	var colors  := PackedColorArray();   colors.resize(t.verts_x * t.verts_y)
	var indices := PackedInt32Array();   indices.resize((t.verts_x - 1) * (t.verts_y - 1) * 6)

	# Y = (h - min_height) * MPU * vis_height_scale  → terrain floor sits at Y=0
	for row in t.verts_y:
		for col in t.verts_x:
			var idx := row * t.verts_x + col
			var h := t.get_height(col, row)
			verts[idx] = Vector3(
				col * ds,
				(h - t.min_height) * TerrainGrid.METERS_PER_UNIT * t.vis_height_scale,
				-row * ds)
			uvs[idx]    = Vector2(float(col) / (t.verts_x - 1), float(row) / (t.verts_y - 1))
			colors[idx] = _height_color(clampf((h - t.min_height) * inv_rng, 0.0, 1.0))

	# Normals: dH/dX and dH/dZ in display space (both scales applied).
	for row in t.verts_y:
		for col in t.verts_x:
			var he := _h(t, col + 1, row); var hw := _h(t, col - 1, row)
			var hn := _h(t, col, row + 1); var hs := _h(t, col, row - 1)
			var dh_east  := (he - hw) * TerrainGrid.METERS_PER_UNIT * t.vis_height_scale / (2.0 * ds)
			var dh_north := (hn - hs) * TerrainGrid.METERS_PER_UNIT * t.vis_height_scale / (2.0 * ds)
			normals[row * t.verts_x + col] = Vector3(-dh_east, 1.0, dh_north).normalized()

	var qi := 0
	for row in (t.verts_y - 1):
		for col in (t.verts_x - 1):
			var base := row * t.verts_x + col
			indices[qi]     = base;                  qi += 1
			indices[qi]     = base + t.verts_x;      qi += 1
			indices[qi]     = base + 1;              qi += 1
			indices[qi]     = base + 1;              qi += 1
			indices[qi]     = base + t.verts_x;      qi += 1
			indices[qi]     = base + t.verts_x + 1;  qi += 1

	var arrays := []
	arrays.resize(Mesh.ARRAY_MAX)
	arrays[Mesh.ARRAY_VERTEX] = verts
	arrays[Mesh.ARRAY_NORMAL] = normals
	arrays[Mesh.ARRAY_TEX_UV] = uvs
	arrays[Mesh.ARRAY_COLOR]  = colors
	arrays[Mesh.ARRAY_INDEX]  = indices

	var am := ArrayMesh.new()
	am.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arrays)
	return am


static func _h(t: TerrainGrid, col: int, row: int) -> float:
	return t.get_height(clampi(col, 0, t.verts_x - 1), clampi(row, 0, t.verts_y - 1))


# Normalized height [0,1] → terrain gradient, with the mid-height (0.5) baseline
# as the land/water divide: below it sinks through water → deep blue, above it
# rises through grass → rock → snow.
static func _height_color(nt: float) -> Color:
	var deep  := Color(0.05, 0.10, 0.30)  # deep water
	var water := Color(0.15, 0.35, 0.60)  # shallow water
	var grass := Color(0.40, 0.52, 0.24)  # baseline ground
	var rock  := Color(0.45, 0.38, 0.30)  # exposed rock
	var snow  := Color(0.95, 0.95, 0.97)  # snow cap
	if nt < 0.5:
		if nt < 0.25:
			return deep.lerp(water, nt / 0.25)
		return water.lerp(grass, (nt - 0.25) / 0.25)
	if nt < 0.8:
		return grass.lerp(rock, (nt - 0.5) / 0.3)
	return rock.lerp(snow, (nt - 0.8) / 0.2)
