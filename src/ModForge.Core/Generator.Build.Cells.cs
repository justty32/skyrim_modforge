namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // Copy a master cell's ENVIRONMENT data (water height/type/textures, lighting + template,
        // region list, imagespace, music, acoustic space, encounter zone, location, ownership,
        // sky/weather-from-region) onto an override cell. CRITICAL: an override CELL that omits
        // these does NOT inherit them from the master at runtime — the engine resets them to
        // defaults. The worst offender is WaterHeight -> 0, which floods any terrain below sea
        // level (the "whole world is underwater" bug); a missing interior LightingTemplate -> a
        // pitch-black room. We DELIBERATELY skip the localized Name (copying it needs the BSA/
        // load-order string lookup, absent headless) and the child reference lists (we ADD our ref
        // to Temporary; vanilla refs stay in the master). Every field copied here is inline or a
        // FormLink — no string resolution, so no plugins.txt dependency.
        private void CopyCellEnv(ICellGetter src, Cell dst)
        {
            dst.Flags = src.Flags;
            if (src.Grid is { } g)
                dst.Grid = new CellGrid { Point = new Noggog.P2Int(g.Point.X, g.Point.Y), Flags = g.Flags };
            dst.Lighting = src.Lighting?.DeepCopy();
            dst.WaterHeight = src.WaterHeight;
            dst.WaterNoiseTexture = src.WaterNoiseTexture;
            dst.WaterEnvironmentMap = src.WaterEnvironmentMap;
            dst.LightingTemplate.SetTo(src.LightingTemplate);
            dst.Water.SetTo(src.Water);
            dst.Location.SetTo(src.Location);
            dst.Owner.SetTo(src.Owner);
            dst.SkyAndWeatherFromRegion.SetTo(src.SkyAndWeatherFromRegion);
            dst.AcousticSpace.SetTo(src.AcousticSpace);
            dst.EncounterZone.SetTo(src.EncounterZone);
            dst.Music.SetTo(src.Music);
            dst.ImageSpace.SetTo(src.ImageSpace);
            if (src.Regions is { } regions)
            {
                dst.Regions = new Noggog.ExtendedList<IFormLinkGetter<IRegionGetter>>();
                foreach (var rg in regions) dst.Regions.Add(rg);
            }
        }

        // Interior cells nest CellBlock(type 2, label=block) -> CellSubBlock(type 3, label=sub) ->
        // Cell, and Skyrim groups them BY FORMID: block = id % 10, sub = (id / 10) % 10 (decimal,
        // 24-bit ID — verified by walking Skyrim.esm, e.g. WhiterunBanneredMare 0x01605E/dec 90206
        // is in block 6 / sub 0). This is CRITICAL for OVERRIDES: a vanilla-cell override placed in
        // the wrong block GRUP is never matched against the master cell, so the engine SILENTLY
        // IGNORES it (the It.10 bug — placed objects + lighting didn't apply; we'd hardcoded 0/0).
        // get-or-add the correct (block, sub) GRUP for any cell's FormID.
        private CellSubBlock InteriorSubFor(FormKey fk)
        {
            int id = (int)fk.ID;
            int blk = id % 10, sub = (id / 10) % 10;
            if (interiorSubs.TryGetValue((blk, sub), out var cached)) return cached;
            var block = mod.Cells.Records.FirstOrDefault(
                b => b.BlockNumber == blk && b.GroupType == GroupTypeEnum.InteriorCellBlock);
            if (block is null)
            {
                block = new CellBlock { BlockNumber = blk, GroupType = GroupTypeEnum.InteriorCellBlock };
                mod.Cells.Records.Add(block);
            }
            var subBlock = new CellSubBlock { BlockNumber = sub, GroupType = GroupTypeEnum.InteriorCellSubBlock };
            block.SubBlocks.Add(subBlock);
            interiorSubs[(blk, sub)] = subBlock;
            return subBlock;
        }

        // --- pass 1: interior (new) cells ---
        public void BuildCells()
        {
            foreach (var c in spec.Cells)
            {
                var cell = new Cell(mod, c.EditorId) { Flags = Cell.Flag.IsInteriorCell };
                // A cell with no Lighting/LightingTemplate renders PITCH BLACK in-game. Optionally copy a
                // vanilla interior cell's lighting/water ENV (same `template` pattern as It.8 item models):
                // point `template` at a known-good vanilla interior (e.g. a player home). Floor still comes
                // from a placed static — without one the player falls into the void.
                if (!string.IsNullOrWhiteSpace(c.Template))
                {
                    if (TryResolveTemplate<ICellGetter>(c.Template, out var tmplCell) && tmplCell is not null)
                    {
                        if (tmplCell.Flags.HasFlag(Cell.Flag.IsInteriorCell)) CopyCellEnv(tmplCell, cell);
                        else Warn($"  ! cell '{c.EditorId}' template '{c.Template}' is exterior — ignored (need an interior cell)");
                    }
                    else Warn($"  ! cell '{c.EditorId}' template '{c.Template}' unresolved — created without lighting (may render black)");
                }
                cell.Flags |= Cell.Flag.IsInteriorCell;   // CopyCellEnv overwrote Flags — keep it interior
                if (!string.IsNullOrEmpty(c.Name)) cell.Name = c.Name;
                InteriorSubFor(cell.FormKey).Cells.Add(cell);
                if (!string.IsNullOrEmpty(c.EditorId)) cellsByEd[c.EditorId] = cell;
            }
        }
    }
}
