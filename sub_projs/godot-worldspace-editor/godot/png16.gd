class_name Png16
## 16-bit grayscale PNG encoder/decoder for ModForge heightmaps.
##
## Encoding:  heights (game units, row0=south) → L16 PNG (top=north, big-endian 16-bit).
## Decoding:  L16 PNG → heights (game units, row0=south).
##
## PNG IDAT uses zlib (RFC 1950).  Godot's PackedByteArray.compress(DEFLATE)
## wraps miniz mz_compress2() which produces the zlib stream directly — no
## manual header/checksum needed.

const METERS_PER_UNIT := 0.014286

## Save heightmap as 16-bit grayscale PNG.
## heights: PackedFloat32Array, size = verts_x × verts_y, row-major row0=south, game units.
static func save(path: String, verts_x: int, verts_y: int,
		heights: PackedFloat32Array, min_h: float, max_h: float) -> bool:
	var range_h := max_h - min_h
	if range_h < 1.0:
		range_h = 1.0

	# Build raw scanlines: 1 filter byte + 2 bytes per pixel (big-endian 16-bit).
	# PNG top = world north = row (verts_y-1); img_y=0 → world_row = verts_y-1.
	var raw := PackedByteArray()
	raw.resize((1 + verts_x * 2) * verts_y)
	var w := 0
	for img_y in verts_y:
		var world_row := (verts_y - 1) - img_y
		raw[w] = 0; w += 1  # filter type: None
		for col in verts_x:
			var h := heights[world_row * verts_x + col]
			var px := int(clampf((h - min_h) / range_h, 0.0, 1.0) * 65535.0 + 0.5)
			raw[w] = (px >> 8) & 0xFF; w += 1
			raw[w] = px & 0xFF;        w += 1

	# Godot COMPRESSION_DEFLATE = zlib (header + deflate + Adler-32) — PNG-ready.
	var idat := raw.compress(FileAccess.COMPRESSION_DEFLATE)
	if idat.is_empty():
		push_error("Png16.save: compression failed")
		return false

	var file := FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		push_error("Png16.save: cannot open " + path)
		return false

	# PNG signature
	file.store_buffer(PackedByteArray([137, 80, 78, 71, 13, 10, 26, 10]))

	# IHDR: width, height, bit_depth=16, color_type=0 (grayscale), compress=0, filter=0, interlace=0
	var ihdr := PackedByteArray()
	ihdr.append_array(_u32(verts_x))
	ihdr.append_array(_u32(verts_y))
	ihdr.append_array(PackedByteArray([16, 0, 0, 0, 0]))
	_write_chunk(file, [73, 72, 68, 82], ihdr)  # "IHDR"

	_write_chunk(file, [73, 68, 65, 84], idat)  # "IDAT"
	_write_chunk(file, [73, 69, 78, 68], PackedByteArray())  # "IEND"

	file.close()
	return true


## Load a PNG heightmap and return heights (game units, row0=south).
## Uses Godot's Image to load (8-bit precision sufficient for re-editing).
## Returns empty array on failure.
static func load_heights(path: String, verts_x: int, verts_y: int,
		min_h: float, max_h: float) -> PackedFloat32Array:
	var img := Image.load_from_file(path)
	if img == null:
		push_error("Png16.load: cannot load " + path)
		return PackedFloat32Array()
	if img.get_width() != verts_x or img.get_height() != verts_y:
		push_error("Png16.load: size mismatch (expected %dx%d, got %dx%d)" % [
			verts_x, verts_y, img.get_width(), img.get_height()])
		return PackedFloat32Array()

	img.convert(Image.FORMAT_L8)
	var range_h := max_h - min_h
	var result := PackedFloat32Array()
	result.resize(verts_x * verts_y)
	for img_y in verts_y:
		var world_row := (verts_y - 1) - img_y
		for col in verts_x:
			# get_pixel returns Color with r in [0,1] for grayscale
			var norm := img.get_pixel(col, img_y).r
			result[world_row * verts_x + col] = min_h + norm * range_h
	return result


# ── PNG internals ────────────────────────────────────────────────────────────

static func _write_chunk(file: FileAccess, type_4b: Array, data: PackedByteArray) -> void:
	file.store_buffer(_u32(data.size()))
	var tb := PackedByteArray(type_4b)
	file.store_buffer(tb)
	file.store_buffer(data)
	var crc_src := PackedByteArray()
	crc_src.append_array(tb)
	crc_src.append_array(data)
	file.store_buffer(_u32(_crc32(crc_src)))


static func _u32(v: int) -> PackedByteArray:
	return PackedByteArray([(v >> 24) & 0xFF, (v >> 16) & 0xFF, (v >> 8) & 0xFF, v & 0xFF])


static func _crc32(data: PackedByteArray) -> int:
	var crc := 0xFFFFFFFF
	for byte in data:
		crc ^= byte
		for _i in 8:
			crc = (crc >> 1) ^ (0xEDB88320 if (crc & 1) else 0)
	return (crc ^ 0xFFFFFFFF) & 0xFFFFFFFF
