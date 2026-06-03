PLACEHOLDER TEXTURE TREE — examples/textures/Textures/ModForge/rubble/

The two .dds files here are minimal valid 4x4 BGRA8 uncompressed DDS placeholders:
  gilded_rubble_d.dds  — solid gold diffuse (R=200 G=160 B=50)
  gilded_rubble_n.dds  — flat normal map (X=128 Y=128 Z=255)

They demonstrate the asset pipeline (assets field in spec + Textures/ sub-tree layout)
and will show a solid-color gold surface in-game on NorRubblePiece03.

Replace with real authored .dds files (DXT1/BC1 for diffuse, BC5/ATI2N for normal)
from GIMP/Photoshop + the Intel/NVIDIA DDS plugin for production quality.
