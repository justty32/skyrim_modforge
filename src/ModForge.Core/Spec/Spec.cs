namespace ModForge;

// --- ESP generator spec (the structured IR; deserialized case-insensitively) ---------
// The root document, and only the document-level knobs. Every `List<XSpec>` record family
// lives on a `partial class ModSpec` block in the SAME Spec.*.cs file that declares its DTO
// (so `weapons[]` sits next to WeaponSpec in Spec.Items.cs) — adding a record family no
// longer means editing this file. Property order is irrelevant: ModSpec is only ever
// DESERIALIZED, never serialized, and the unknown-field check (Program.Schema.cs) looks
// members up by name.
//
// "ref" fields throughout accept EITHER an in-spec editorId OR an external "<master>:0xFORMID"
// (e.g. "Skyrim.esm:0x013746" — find them with the `find` command). External refs auto-add
// the master on write (Mutagen MastersListContent=Iterate).
public sealed partial class ModSpec
{
    // Master -> the spec fields that named it, snapshotted by ExpandMacros BEFORE it expands anything
    // (Generator.Dependencies.cs). INTERNAL on purpose: not a spec field — it must not deserialize, must
    // not show up in the unknown-field check, and must not be re-walked as spec content.
    internal IReadOnlyDictionary<string, IReadOnlyList<string>>? AuthoredRefSources { get; set; }

    public string PluginName { get; set; } = "Generated.esp";
    public bool Esl { get; set; } = true;
    public PresetCatalogSpec Presets { get; set; } = new(); // non-emitting cookbook fragments for copy/paste recipes
    // External-resource pipeline (see docs/external_assets.md): a source directory whose
    // `Meshes/`, `Textures/`, `Sounds/` (and loose `.hkx`) sub-trees `package` copies next to
    // the .esp so the packaged mod is self-contained / MO2-ready. ModForge REFERENCES + BUNDLES
    // user assets — it does NOT author meshes/anims. A path is relative to the spec file (or
    // absolute); a `package --assets <dir>` CLI arg overrides this.
    public string Assets { get; set; } = "";
}

/// <summary>
/// Non-emitting preset/cookbook catalog. The builder intentionally ignores these fragments; they
/// exist so specs can carry named, schema-valid copy/paste recipes next to the concrete records that
/// use them. Values are arbitrary JSON objects because each category maps to an existing spec family.
/// </summary>
public sealed class PresetCatalogSpec
{
    public Dictionary<string, JsonElement> Lighting { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, JsonElement> Weather { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, JsonElement> Packages { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, JsonElement> Identities { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
