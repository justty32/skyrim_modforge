namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        private readonly Dictionary<(int Block, int Sub), CellSubBlock> interiorSubs = new();

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
            // EditorID (EDID): plain ASCII, not localized — the same "carry it or the override blanks
            // it" rule as CopyWorldspaceEnv. A CELL override is a WHOLE-RECORD replacement: at runtime
            // the cell object takes its EditorID from the WINNING record, so an override that omits
            // EDID leaves the cell nameless — FormID, interior flag and contents all still correct, so
            // nothing crashes and nothing warns. Found 2026-08-02 by the in-game QA runner: a
            // navmesh-only override of WhiterunBanneredMare (0x01605E) made the live cell's EditorID
            // come back "". A pure navmesh edit must not cost the cell its name.
            dst.EditorID = src.EditorID;
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
                // This is the one caller that BORROWS env from an unrelated cell instead of overriding
                // it, so it must keep its own identity: CopyCellEnv overwrote both Flags and EditorID
                // with the template's.
                cell.Flags |= Cell.Flag.IsInteriorCell;   // keep it interior
                cell.EditorID = c.EditorId;               // keep OUR name, not the template's
                // Custom/vanilla LightingTemplate (LGTM) + ImageSpace (IMGS) links + inline XCLL.
                if (!string.IsNullOrWhiteSpace(c.LightingTemplate))
                {
                    if (ResolveLightingRef(c.LightingTemplate, out var ltFk)) cell.LightingTemplate.SetTo(ltFk);
                    else Warn($"  ! cell '{c.EditorId}' lightingTemplate '{c.LightingTemplate}' unresolved");
                }
                if (!string.IsNullOrWhiteSpace(c.ImageSpace))
                {
                    if (ResolveLightingRef(c.ImageSpace, out var imgFk)) cell.ImageSpace.SetTo(imgFk);
                    else Warn($"  ! cell '{c.EditorId}' imageSpace '{c.ImageSpace}' unresolved");
                }
                ApplyCellLighting(cell, c);
                if (!string.IsNullOrEmpty(c.Name)) cell.Name = c.Name;
                InteriorSubFor(cell.FormKey).Cells.Add(cell);
                if (!string.IsNullOrEmpty(c.EditorId)) cellsByEd[c.EditorId] = cell;
            }
        }

        // An interior CELL MUST carry an XCLL (Lighting) or it renders pitch black. The Inherit flags
        // decide which fields come from the LightingTemplate vs the inline XCLL. Rules:
        //   * no inline `lighting` → if a LightingTemplate is present and there's no Lighting yet,
        //     create one that inherits ALL flags (fully template-driven).
        //   * inline `lighting` → write the authored fields; Inherits = the flags listed in `inherit`
        //     (those come from the template). A field set inline AND listed in `inherit` is inherited
        //     (template wins) + warned.
        private void ApplyCellLighting(Cell cell, CellSpec c)
        {
            if (c.Lighting is null)
            {
                if (!string.IsNullOrWhiteSpace(c.LightingTemplate))
                    cell.Lighting ??= new CellLighting { Inherits = AllInheritFlags() };
                return;
            }

            var s = c.Lighting;
            var lz = cell.Lighting ??= new CellLighting();

            CellLighting.Inherit inh = 0;
            foreach (var f in s.Inherit)
                if (Enum.TryParse<CellLighting.Inherit>(f, ignoreCase: true, out var fl)) inh |= fl;
                else Warn($"  ! cell '{c.EditorId}' invalid inherit flag '{f}'");
            lz.Inherits = inh;

            // helper: set inline value only if NOT inherited; warn on conflict.
            void Field(CellLighting.Inherit flag, bool authored, Action set)
            {
                if (!authored) return;
                if (inh.HasFlag(flag)) Warn($"  ! cell '{c.EditorId}' field for {flag} set inline but also inherited — template wins");
                else set();
            }

            Field(CellLighting.Inherit.AmbientColor, s.AmbientColor is not null, () => lz.AmbientColor = ToColor(s.AmbientColor!));
            Field(CellLighting.Inherit.DirectionalColor, s.DirectionalColor is not null, () => lz.DirectionalColor = ToColor(s.DirectionalColor!));
            Field(CellLighting.Inherit.DirectionalRotation, s.DirectionalRotationXY is not null, () => lz.DirectionalRotationXY = s.DirectionalRotationXY!.Value);
            Field(CellLighting.Inherit.DirectionalRotation, s.DirectionalRotationZ is not null, () => lz.DirectionalRotationZ = s.DirectionalRotationZ!.Value);
            Field(CellLighting.Inherit.DirectionalFade, s.DirectionalFade is not null, () => lz.DirectionalFade = s.DirectionalFade!.Value);
            Field(CellLighting.Inherit.FogColor, s.FogNearColor is not null, () => lz.FogNearColor = ToColor(s.FogNearColor!));
            Field(CellLighting.Inherit.FogColor, s.FogFarColor is not null, () => lz.FogFarColor = ToColor(s.FogFarColor!));
            Field(CellLighting.Inherit.FogNear, s.FogNear is not null, () => lz.FogNear = s.FogNear!.Value);
            Field(CellLighting.Inherit.FogFar, s.FogFar is not null, () => lz.FogFar = s.FogFar!.Value);
            Field(CellLighting.Inherit.FogMax, s.FogMax is not null, () => lz.FogMax = s.FogMax!.Value);
            Field(CellLighting.Inherit.ClipDistance, s.FogClipDistance is not null, () => lz.FogClipDistance = s.FogClipDistance!.Value);
            Field(CellLighting.Inherit.FogPower, s.FogPower is not null, () => lz.FogPower = s.FogPower!.Value);
            Field(CellLighting.Inherit.LightFadeDistances, s.LightFadeBegin is not null, () => lz.LightFadeBegin = s.LightFadeBegin!.Value);
            Field(CellLighting.Inherit.LightFadeDistances, s.LightFadeEnd is not null, () => lz.LightFadeEnd = s.LightFadeEnd!.Value);
            if (s.DirectionalAmbient is { } da) FillAmbientColors(lz.AmbientColors ??= new(), da);
        }

        // All CellLighting.Inherit flags OR'd — a cell with a template but no inline overrides
        // inherits everything (matches vanilla interior cells).
        private static CellLighting.Inherit AllInheritFlags()
        {
            CellLighting.Inherit all = 0;
            foreach (CellLighting.Inherit f in Enum.GetValues<CellLighting.Inherit>()) all |= f;
            return all;
        }
    }
}
