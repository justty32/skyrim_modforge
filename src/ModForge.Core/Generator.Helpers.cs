namespace ModForge;

public static partial class Generator
{
    // Armor class string -> enum. Accepts shorthand (light/heavy/clothing) or the enum names;
    // anything unrecognised (incl. empty) falls back to Clothing.
    private static ArmorType ParseArmorType(string s) => s.Trim().ToLowerInvariant() switch
    {
        "light" or "lightarmor" => ArmorType.LightArmor,
        "heavy" or "heavyarmor" => ArmorType.HeavyArmor,
        _ => ArmorType.Clothing,
    };

    // Skyrim stores placement rotation in radians; specs author it in (friendlier) degrees.
    private static float Deg2Rad(float deg) => deg * (float)Math.PI / 180f;

    // Enchantment (ENCH/ObjectEffect) family -> Mutagen EnchantType + the vanilla-default cast/target
    // for that family (VERIFIED against Skyrim.esm: EnchWeaponFrostDamageBase = Enchantment/
    // FireAndForget/Touch, EnchArmorFortifyStaminaBase = Enchantment/ConstantEffect/Self,
    // StaffEnchIcySpear = StaffEnchantment/FireAndForget/Aimed). Note Mutagen's EnchantTypeEnum has
    // only {Enchantment, StaffEnchantment}; a constant-effect APPAREL enchant is still EnchantType=
    // Enchantment — it's the ConstantEffect *CastType* that makes it always-on. `apparel`/`armor`
    // and `staff` are accepted aliases; anything else falls back to the weapon family.
    private static readonly HashSet<string> EnchantTypes =
        new(StringComparer.OrdinalIgnoreCase) { "weapon", "apparel", "armor", "staff" };
    private static (ObjectEffect.EnchantTypeEnum type, CastType cast, TargetType target) EnchantFamily(string s)
        => s.Trim().ToLowerInvariant() switch
        {
            "apparel" or "armor" => (ObjectEffect.EnchantTypeEnum.Enchantment, CastType.ConstantEffect, TargetType.Self),
            "staff"              => (ObjectEffect.EnchantTypeEnum.StaffEnchantment, CastType.FireAndForget, TargetType.Aimed),
            _                    => (ObjectEffect.EnchantTypeEnum.Enchantment, CastType.FireAndForget, TargetType.Touch),
        };

    // Exterior worldspace cells are 4096 units square. A world position maps to cell grid
    // coords by floor(pos/4096); those map to the WRLD group nesting by floor(grid/8) (sub-block)
    // and floor(grid/32) (block) — VERIFIED against Tamriel (cell (7,-41) -> block (0,-2),
    // sub-block (0,-6)). NOTE: this must be FLOOR division (toward -inf), not C#'s truncating `/`
    // ((-41)/8 == -5, but floor is -6) — negative coordinates would land in the wrong group.
    private const int CellSize = 4096;
    private static int FloorDiv(int a, int b) => (int)Math.Floor((double)a / b);
    private static int PosToGrid(float pos) => (int)Math.Floor(pos / CellSize);

    // OR together a list of flag names (case-insensitive) into one enum value; unknown names
    // are ignored (validate is responsible for reporting them).
    private static T ParseFlags<T>(List<string> names) where T : struct, Enum
    {
        long acc = 0;
        foreach (var n in names)
            if (Enum.TryParse<T>(n, ignoreCase: true, out var v)) acc |= Convert.ToInt64(v);
        return (T)Enum.ToObject(typeof(T), acc);
    }

    // -------------------------------------------------------------------------------
    //  Reference resolver (It.7b). A "ref" string is EITHER an in-spec editorId, OR an
    //  external vanilla/master form "<master>:0xFORMID" (e.g. "Skyrim.esm:0x013746").
    //  External refs become a FormKey on the named master directly; Mutagen adds the
    //  master to the output's masters list on write (MastersListContent = Iterate).
    //  Discover external FormIDs with the `find` command.
    // -------------------------------------------------------------------------------
    private static bool LooksExternalRef(string s)
    {
        int i = s.IndexOf(':');
        if (i <= 0) return false;
        var master = s[..i].Trim();
        return master.EndsWith(".esm", StringComparison.OrdinalIgnoreCase)
            || master.EndsWith(".esp", StringComparison.OrdinalIgnoreCase)
            || master.EndsWith(".esl", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryExternalRef(string s, out FormKey fk)
    {
        fk = default;
        int i = s.IndexOf(':');
        if (i <= 0) return false;
        var master = s[..i].Trim();
        if (!LooksExternalRef(s)) return false;
        var idPart = s[(i + 1)..].Trim();
        if (idPart.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) idPart = idPart[2..];
        if (!uint.TryParse(idPart, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id)) return false;
        fk = new FormKey(ModKey.FromNameAndExtension(master), id & 0x00FFFFFF);  // mask off the master-index byte
        return true;
    }

    // Resolve a ref to a FormKey: external "<master>:0xID" first, else in-spec editorId.
    private static bool TryResolveRef(string s, Dictionary<string, FormKey> formKeyByEd, out FormKey fk)
    {
        if (string.IsNullOrWhiteSpace(s)) { fk = default; return false; }
        if (TryExternalRef(s, out fk)) return true;
        return formKeyByEd.TryGetValue(s, out fk);
    }
}
