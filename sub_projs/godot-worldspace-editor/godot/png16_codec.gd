class_name Png16Codec

static func write_chunk(file: FileAccess, type_4b: Array, data: PackedByteArray) -> void:
	file.store_buffer(u32(data.size()))
	var tb := PackedByteArray(type_4b)
	file.store_buffer(tb)
	file.store_buffer(data)
	var crc_src := PackedByteArray()
	crc_src.append_array(tb)
	crc_src.append_array(data)
	file.store_buffer(u32(crc32(crc_src)))


static func u32(v: int) -> PackedByteArray:
	return PackedByteArray([(v >> 24) & 0xFF, (v >> 16) & 0xFF, (v >> 8) & 0xFF, v & 0xFF])


static func crc32(data: PackedByteArray) -> int:
	var crc := 0xFFFFFFFF
	for byte in data:
		crc ^= byte
		for _i in 8:
			crc = (crc >> 1) ^ (0xEDB88320 if (crc & 1) else 0)
	return (crc ^ 0xFFFFFFFF) & 0xFFFFFFFF
