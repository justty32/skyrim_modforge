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
                // Do NOT set VendorLocation here. A LocationTarget anchored at the chest with Radius 0 is a
                // DEGENERATE point — the engine's GetOffersServicesNow check then evaluates the player as
                // outside the (zero-size) shop area and returns 0, so the trade dialogue never opens.
                // Vanilla merchants (e.g. Belethor) leave VendorLocation EMPTY and gate "on shift" purely
                // via the merchant's scheduled sell package + JobMerchant faction. Mirror that: leave unset.
                linksWired++;
                if (LooksExternalRef(refStr)) extLinks++;
                // (The chest placement is forced Persistent in the placement loop via deferredAnchorEds.)
            }
        }

        // Editor ids of placements referenced as merchant chests (forced Persistent — they hold the
        // vendor's gold/stock, must persist). Read by the placement loop's deferredAnchorEds.
        public IEnumerable<string> MerchantContainerRefs()
            => deferredMerchantContainers.Select(w => w.Ref);
    }
}
