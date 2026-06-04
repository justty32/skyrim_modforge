namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- pass 2: vendor (merchant) faction FormLinks ---
        // SellBuyList (a FormList of VendorItem keywords) resolves now. The MerchantContainer points at
        // a PLACEMENT (the placed merchant chest), which doesn't exist until the placement loop runs —
        // so collect those into deferredMerchantContainers and wire them after placements (in the World
        // partial, alongside the deferred package targets).
        public void WireVendors()
        {
            foreach (var f in spec.Factions)
            {
                if (f.Vendor is not { } v) continue;
                if (!recordsByEd.TryGetValue(f.EditorId, out var rec) || rec is not IFaction fact) continue;
                Resolve($"faction '{f.EditorId}' vendor.sellBuyList", v.SellBuyList,
                    fk => fact.VendorBuySellList.SetTo(fk));
                if (!string.IsNullOrWhiteSpace(v.MerchantContainer))
                    deferredMerchantContainers.Add((fact, f.EditorId, v.MerchantContainer));
            }
        }

        // Resolve the deferred vendor MerchantContainer refs (a faction's merchant chest is a
        // PLACEMENT created in the placement loop). Called after placement editorIds are registered.
        // Also anchors the VendorLocation at the chest so trading is allowed wherever the chest sits.
        public void WireDeferredMerchantContainers()
        {
            foreach (var (fact, factEd, refStr) in deferredMerchantContainers)
            {
                if (!TryResolveRef(refStr, formKeyByEd, out var fk))
                { Warn($"  ! faction '{factEd}' vendor.merchantContainer '{refStr}' unresolved — vendor has no merchant chest (no gold/stock to trade)"); continue; }
                fact.MerchantContainer.SetTo(fk);
                // VendorLocation = the merchant's CELL (a LocationCell, binary locType 1), NOT a reference
                // anchored at the chest. Verified against vanilla Belethor (ServicesWhiterunBelethorsGoods
                // PLVD = LocationCell -> WhiterunBelethorsGeneralGoods cell, radius 0): the engine's
                // GetOffersServicesNow check asks "is the player in the vendor's sell area?", and for a
                // generated NPC — which has NO CK editor location to fall back on — that area must be stated
                // explicitly or services are never offered (trade dialogue never opens). The earlier bug
                // was using a chest REFERENCE target with radius 0 (a degenerate point); removing it
                // entirely was also wrong. An InCell location needs no radius. Resolve the cell from the
                // merchant-container placement's `cell` (in-spec editorId or vanilla <master>:0xFORMID).
                var chestPlacement = spec.Placements.FirstOrDefault(
                    p => string.Equals(p.EditorId, refStr, StringComparison.OrdinalIgnoreCase));
                if (chestPlacement is { } cp && !string.IsNullOrWhiteSpace(cp.Cell)
                    && TryResolveRef(cp.Cell, formKeyByEd, out var cellFk))
                {
                    var loc = new LocationCell();
                    loc.Link.SetTo(cellFk);
                    fact.VendorLocation = new LocationTargetRadius { Target = loc, Radius = 0 };
                }
                else
                {
                    Warn($"  ! faction '{factEd}' vendor: could not resolve the merchant cell for VendorLocation " +
                         $"(chest placement '{refStr}' has no resolvable cell) — GetOffersServicesNow may stay 0 and trade won't open");
                }
                linksWired++;
                if (LooksExternalRef(refStr)) extLinks++;
                // (The chest placement is forced Persistent in the placement loop via deferredAnchorEds.)
            }
        }

        // Editor ids of placements referenced as merchant chests (forced Persistent — they hold the
        // vendor's gold/stock, must persist). Read by the placement loop's deferredAnchorEds.
        public IEnumerable<string> MerchantContainerRefs()
            => deferredMerchantContainers.Select(w => w.Ref);

        // --- pass 1: Faction (FACT) — incl. inline vendor data; sellBuyList/container wired in pass 2 ---
        public void BuildFactions()
        {
            foreach (var f in spec.Factions)
            {
                var r = mod.Factions.AddNew();
                r.EditorID = f.EditorId; r.Name = f.Name;
                if (f.Vendor is { } v)
                {
                    // Vendor flag = "this faction's members are merchants". CanBeOwner mirrors vanilla
                    // merchant factions (they own their shop cell/chest). VendorValues carries the hours,
                    // sell radius, buy-stolen flag, and whether the buy/sell list is a NOT-sell list.
                    r.Flags |= Faction.FactionFlag.Vendor | Faction.FactionFlag.CanBeOwner;
                    r.VendorValues = new VendorValues
                    {
                        StartHour = (ushort)Math.Clamp((int)v.StartHour, 0, 24),
                        EndHour = (ushort)Math.Clamp((int)v.EndHour, 0, 24),
                        Radius = v.Radius,
                        OnlyBuysStolenItems = v.BuysStolen,
                        NotSellBuy = v.NotSellBuyList,
                    };
                    if (!string.IsNullOrEmpty(f.EditorId)) vendorFactionEds.Add(f.EditorId);
                    // SellBuyList (FormList) + MerchantContainer (a placed ref) are FormLinks resolved
                    // in pass 2 (WireVendors) — the container placement is created in the placement loop.
                }
            }
        }
    }
}
