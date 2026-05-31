PLACEHOLDER TEXTURE TREE — examples/textures/ModForge/rubble/

texture_set_spec.json references two textures by their Data-relative paths:
    ModForge\rubble\gilded_rubble_d.dds   (diffuse / color)
    ModForge\rubble\gilded_rubble_n.dds   (normal + gloss)

In a real mod these go under Data/Textures/ModForge/rubble/ (the TXST slot path is
relative to Data\Textures\, so it OMITS the leading "Textures\"). The .gilded_rubble_*.dds
files here are ZERO-BYTE PLACEHOLDERS — they are NOT valid DDS images. ModForge only writes
the TXST record + the path references into the .esp; it cannot author or render texture
content. Replace these placeholders with real authored .dds files (e.g. from GIMP/Photoshop +
the Intel/NVIDIA DDS plugin, or xLODGen/CK output) before the retexture will actually show
in-game. Until then, the mesh renders with the .nif's original textures (or purple/black if
the placeholder paths are loaded as-is).
