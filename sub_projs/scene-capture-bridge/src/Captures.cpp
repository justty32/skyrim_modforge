#include "Captures.h"

#include "Aim.h"
#include "Markers.h"
#include "SceneExporter.h"
#include "log.h"

#include <algorithm>

namespace {
    constexpr float kRadToDeg = 57.2957795f;

    std::vector<Captures::Entry> g_entries;
    std::uint32_t g_nextSeq = 1;

    // A MagicItem's effect list — shared by ENCH (weapon/armour enchant), ALCH
    // (potion) and INGR (ingredient). A runtime-only MGEF can't be named in an
    // esp, so it is dropped from the list (rare; kept quiet — the entry still
    // carries the effects that DO resolve).
    std::vector<Captures::Effect> ReadEffects(const RE::MagicItem* magic) {
        std::vector<Captures::Effect> out;
        if (!magic) return out;
        for (const auto* eff : magic->effects) {
            if (!eff || !eff->baseEffect) continue;
            auto id = SceneExporter::ResolveDurableId(eff->baseEffect);
            if (!id) continue;
            Captures::Effect e;
            e.magicEffect = *id;
            e.magnitude = eff->effectItem.magnitude;
            e.area = static_cast<std::int32_t>(eff->effectItem.area);
            e.duration = static_cast<std::int32_t>(eff->effectItem.duration);
            out.push_back(std::move(e));
        }
        return out;
    }

    // The enchantment ACTUALLY on this instance: a player-applied enchant lives
    // on the ref's ExtraEnchantment (base stays the vanilla weapon); a
    // pre-enchanted base carries formEnchanting. Prefer the instance's.
    void CaptureEnchant(Captures::Entry& e, RE::TESObjectREFR* ref, RE::TESEnchantableForm* form) {
        RE::EnchantmentItem* ench = nullptr;
        std::uint16_t charge = 0;
        if (ref) {
            if (auto* x = ref->extraList.GetByType<RE::ExtraEnchantment>(); x && x->enchantment) {
                ench = x->enchantment;
                charge = x->charge;
            }
        }
        if (!ench && form) {
            ench = form->formEnchanting;
            charge = form->amountofEnchantment;
        }
        if (!ench) return;
        e.effects = ReadEffects(ench);
        e.enchantAmount = charge;
        if (auto id = SceneExporter::ResolveDurableId(ench)) e.enchantBase = *id;
    }

    // Read a captured actor's TESNPC appearance/identity into the entry. Unique
    // check is the caller's — this just harvests. (See header caveat: whether the
    // TESNPC reflects a live-override tool like PROTEUS is IN-GAME TBD.)
    bool ReadNpc(Captures::Entry& e, RE::TESObjectREFR* ref) {
        auto* actor = ref->As<RE::Actor>();
        auto* npc = actor ? actor->GetActorBase() : nullptr;
        if (!npc) return false;
        auto& n = e.npc;

        if (auto* race = npc->GetRace()) {
            if (auto id = SceneExporter::ResolveDurableId(race)) n.race = *id;
        }
        n.female = npc->IsFemale();
        n.unique = npc->IsUnique();
        n.essential = npc->IsEssential();
        n.dead = actor->IsDead();
        n.protectedActor = actor->IsProtected();
        n.weight = npc->weight;
        n.height = npc->height;
        n.bodyR = npc->bodyTintColor.red;
        n.bodyG = npc->bodyTintColor.green;
        n.bodyB = npc->bodyTintColor.blue;

        if (auto* hrd = npc->headRelatedData) {
            if (auto* hc = hrd->hairColor) {
                if (auto id = SceneExporter::ResolveDurableId(hc)) n.hairColor = *id;
                n.hairR = hc->color.red;
                n.hairG = hc->color.green;
                n.hairB = hc->color.blue;
            }
            if (auto* ft = hrd->faceDetails) {
                if (auto id = SceneExporter::ResolveDurableId(ft)) n.faceTexture = *id;
            }
        }
        if (auto* outfit = npc->defaultOutfit) {
            if (auto id = SceneExporter::ResolveDurableId(outfit)) n.defaultOutfit = *id;
        }

        if (npc->headParts && npc->numHeadParts > 0) {
            for (std::int8_t i = 0; i < npc->numHeadParts; ++i) {
                auto* hp = npc->headParts[i];
                if (!hp) continue;
                if (auto id = SceneExporter::ResolveDurableId(hp)) n.headParts.push_back(*id);
            }
        }
        if (npc->tintLayers) {
            for (auto* layer : *npc->tintLayers) {
                if (!layer) continue;
                Captures::TintLayer t;
                t.index = layer->tintIndex;
                t.preset = layer->preset;
                t.value = layer->interpolationValue;
                t.r = layer->tintColor.red;
                t.g = layer->tintColor.green;
                t.b = layer->tintColor.blue;
                t.a = layer->tintColor.alpha;
                n.tints.push_back(t);
            }
        }
        if (npc->faceData) {
            for (int i = 0; i < RE::TESNPC::FaceData::Morphs::kUnk; ++i) n.morphs.push_back(npc->faceData->morphs[i]);
            for (std::int32_t p : npc->faceData->parts) n.parts.push_back(p);
        }

        // Perks (base BGSPerkRankArray): durable perk id + rank.
        if (npc->perks && npc->perkCount > 0) {
            for (std::uint32_t i = 0; i < npc->perkCount; ++i) {
                auto& pr = npc->perks[i];
                if (!pr.perk) continue;
                if (auto id = SceneExporter::ResolveDurableId(pr.perk))
                    n.perks.push_back({*id, pr.currentRank});
            }
        }

        // Current buffs — live active-effect snapshot (source spell + base MGEF).
        if (auto* mt = actor->GetMagicTarget()) {
            if (auto* list = mt->GetActiveEffectList()) {
                for (auto* ae : *list) {
                    if (!ae) continue;
                    auto* mgef = ae->GetBaseObject();
                    if (!mgef) continue;
                    Captures::ActiveEffect a;
                    if (auto id = SceneExporter::ResolveDurableId(mgef)) a.magicEffect = *id;
                    else continue;  // runtime MGEF — can't name it
                    if (ae->spell) {
                        if (auto id = SceneExporter::ResolveDurableId(ae->spell)) a.source = *id;
                    }
                    a.magnitude = ae->magnitude;
                    a.duration = ae->duration;
                    a.elapsed = ae->elapsedSeconds;
                    n.activeEffects.push_back(std::move(a));
                }
            }
        }

        n.position = ref->GetPosition();
        const RE::NiPoint3& ang = ref->data.angle;
        n.angleDeg = {ang.x * kRadToDeg, ang.y * kRadToDeg, ang.z * kRadToDeg};
        const auto anchor = SceneExporter::AnchorOf(ref);
        n.cellOrWs = anchor.id;
        n.isInterior = anchor.interior;
        return true;
    }

    Captures::Result CaptureRef(RE::NiPointer<RE::TESObjectREFR> ref, const char* how) {
        if (!ref) {
            SKSE::log::info("Captures: {} has no target", how);
            return Captures::Result::kNothing;
        }
        if (Markers::IsProxy(ref.get())) {
            SKSE::log::info("Captures: {} target is a marker gem — nothing to capture", how);
            return Captures::Result::kMarkerProxy;
        }
        // NPC capture (increment ②): harvest the actor's appearance/identity.
        // Unique NPCs are captured too (user-decided) — the `unique` flag rides
        // along for ModForge to act on.
        if (ref->GetFormType() == RE::FormType::ActorCharacter) {
            Captures::Entry e;
            const char* dn = ref->GetDisplayFullName();
            e.name = (dn && *dn) ? dn : "";
            if (auto* b = ref->GetBaseObject()) {
                if (auto id = SceneExporter::ResolveDurableId(b)) e.base = *id;
            }
            e.kind = Captures::Kind::kNpc;
            if (!ReadNpc(e, ref.get())) {
                SKSE::log::info("Captures: {} npc has no actor base — nothing captured", how);
                return Captures::Result::kNothing;
            }
            e.seq = g_nextSeq++;
            SKSE::log::info("Captures: captured NPC '{}' ({}) race={} {}{} — {} headpart(s), "
                "{} tint(s), {} perk(s), {} buff(s), face morphs {}", e.name,
                e.base.empty() ? "runtime base" : e.base,
                e.npc.race.empty() ? "?" : e.npc.race, e.npc.female ? "female" : "male",
                e.npc.unique ? " UNIQUE" : "", e.npc.headParts.size(), e.npc.tints.size(),
                e.npc.perks.size(), e.npc.activeEffects.size(),
                e.npc.morphs.empty() ? "none" : "captured");
            g_entries.push_back(std::move(e));
            return Captures::Result::kCaptured;
        }
        auto* base = ref->GetBaseObject();
        if (!base) {
            SKSE::log::info("Captures: {} target has no base", how);
            return Captures::Result::kNothing;
        }

        Captures::Entry e;
        const char* dn = ref->GetDisplayFullName();
        e.name = (dn && *dn) ? dn : "";
        if (auto id = SceneExporter::ResolveDurableId(base)) e.base = *id;

        switch (base->GetFormType()) {
        case RE::FormType::Weapon:
            e.kind = Captures::Kind::kWeapon;
            CaptureEnchant(e, ref.get(), static_cast<RE::TESObjectWEAP*>(base));
            break;
        case RE::FormType::Armor:
            e.kind = Captures::Kind::kArmor;
            CaptureEnchant(e, ref.get(), static_cast<RE::TESObjectARMO*>(base));
            break;
        case RE::FormType::AlchemyItem:
            e.kind = Captures::Kind::kPotion;
            e.effects = ReadEffects(static_cast<RE::AlchemyItem*>(base));
            break;
        case RE::FormType::Ingredient:
            e.kind = Captures::Kind::kIngredient;
            e.effects = ReadEffects(static_cast<RE::IngredientItem*>(base));
            break;
        default:
            SKSE::log::info("Captures: {} target '{}' is not a capturable item "
                "(weapon/armour/potion/ingredient)", how, e.name);
            return Captures::Result::kNotItem;
        }

        // Nothing to mint: a plain (unenchanted) weapon/armour or an empty potion
        // has no definition worth an authored record — ModForge can reference the
        // base directly. Reject loudly so the player knows why nothing landed.
        if (e.effects.empty()) {
            SKSE::log::info("Captures: '{}' has no enchantment / effects to capture", e.name);
            return Captures::Result::kNotItem;
        }

        e.seq = g_nextSeq++;
        SKSE::log::info("Captures: captured '{}' [{}] ({}) — {} effect(s){}", e.name,
            Captures::KindName(e.kind), e.base.empty() ? "runtime base" : e.base,
            e.effects.size(), e.enchantBase.empty() ? "" : " (+authored ENCH)");
        g_entries.push_back(std::move(e));
        return Captures::Result::kCaptured;
    }
}

namespace Captures {

    Result CaptureCrosshair() { return CaptureRef(Aim::CrosshairRef(), "crosshair"); }
    Result CaptureByRay() { return CaptureRef(Aim::RayRef(), "ray"); }

    std::vector<Entry>& All() { return g_entries; }

    const char* KindName(Kind k) {
        switch (k) {
        case Kind::kWeapon: return "weapon";
        case Kind::kArmor: return "armor";
        case Kind::kPotion: return "potion";
        case Kind::kIngredient: return "ingredient";
        case Kind::kNpc: return "npc";
        default: return "item";
        }
    }

    bool Undo() {
        if (g_entries.empty()) return false;
        g_entries.pop_back();
        return true;
    }

    bool UndoEntry(std::uint32_t seq) {
        auto it = std::find_if(g_entries.begin(), g_entries.end(),
            [seq](const Entry& e) { return e.seq == seq; });
        if (it == g_entries.end()) return false;
        g_entries.erase(it);
        return true;
    }

    void Clear() { g_entries.clear(); }

    void DropAll() { g_entries.clear(); }

    void OnRegistryRestored() {
        // Reseed the counter past the highest loaded seq so new captures don't
        // collide with restored ones (same pattern as Markers).
        std::uint32_t hi = 0;
        for (const auto& e : g_entries) hi = std::max(hi, e.seq);
        g_nextSeq = hi + 1;
    }

}  // namespace Captures
