namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        void CheckEnum<TEnum>(string val, string what) where TEnum : struct, Enum
        { if (!string.IsNullOrWhiteSpace(val) && !Enum.TryParse<TEnum>(val, ignoreCase: true, out _)) Problems.Add($"{what} '{val}' invalid"); }

        void CheckEffects(string ed, List<EffectSpec> effects, string kind)
        {
            foreach (var e in effects)
                if (string.IsNullOrWhiteSpace(e.MagicEffect)) Problems.Add($"{kind} '{ed}' has an effect with empty magicEffect ref");
                else CheckRef(e.MagicEffect, $"{kind} '{ed}' effect magicEffect");
        }

        bool ValidComparison(string cmp) => string.IsNullOrEmpty(cmp)
            || cmp is "==" or "=" or "!=" or ">" or ">=" or "<" or "<="
            || Enum.TryParse<CompareOperator>(cmp, true, out _);

        void CheckCondition(ConditionSpec cs, string what)
        {
            if (string.IsNullOrWhiteSpace(cs.Function)) { Problems.Add($"{what}: condition has empty function"); return; }
            if (!SupportedConditionFunctions.Contains(cs.Function))
                Problems.Add($"{what}: unsupported condition function '{cs.Function}'");
            if (!ValidComparison(cs.Comparison))
                Problems.Add($"{what}: invalid comparison '{cs.Comparison}' (== != > >= < <= or the CompareOperator enum names)");
            if (!string.IsNullOrEmpty(cs.RunOn) && !Enum.TryParse<Condition.RunOnType>(cs.RunOn, true, out _))
                Problems.Add($"{what}: invalid runOn '{cs.RunOn}' (Subject|Target|Reference|CombatTarget|...)");
            switch (cs.Function.ToLowerInvariant())
            {
                case "getbaseactorvalue":
                case "getactorvalue":
                case "getactorvaluepercent":
                    if (string.IsNullOrWhiteSpace(cs.ActorValue)) Problems.Add($"{what}: {cs.Function} needs an actorValue");
                    else CheckEnum<ActorValue>(cs.ActorValue, $"{what} actorValue");
                    break;
                case "haskeyword": case "wornhaskeyword": case "hasperk": case "getisid":
                case "getisrace": case "getitemcount": case "isspelltarget": case "getinfaction":
                case "getglobalvalue": case "getstage": case "getrelationshiprank":
                case "getquestrunning": case "getincell": case "getinworldspace":
                case "getequipped": case "getdeadcount":
                    if (string.IsNullOrWhiteSpace(cs.Param)) Problems.Add($"{what}: {cs.Function} needs a param ref");
                    else CheckRef(cs.Param, $"{what} param");
                    break;
                case "getequippeditemtype":
                    if (!string.IsNullOrEmpty(cs.ItemType)) CheckEnum<CastSource>(cs.ItemType, $"{what} itemType");
                    break;
                case "getisaliasref":
                    if (string.IsNullOrWhiteSpace(cs.Alias)) Problems.Add($"{what}: GetIsAliasRef needs an alias (the quest alias name)");
                    break;
                case "getstagedone":
                    if (string.IsNullOrWhiteSpace(cs.Param)) Problems.Add($"{what}: GetStageDone needs a param ref (the quest)");
                    else CheckRef(cs.Param, $"{what} param");
                    if (cs.Stage < 0) Problems.Add($"{what}: GetStageDone needs a stage index");
                    break;
            }
        }

        void CheckModelPath(string model, string what)
        {
            if (string.IsNullOrWhiteSpace(model)) return;
            var p = model.Replace('/', '\\');
            if (!p.EndsWith(".nif", StringComparison.OrdinalIgnoreCase))
                Problems.Add($"{what} model '{model}' must be a .nif path (Data-relative, rooted at Meshes\\, e.g. Weapons\\Iron\\LongSword.nif)");
            if (p.StartsWith("Meshes\\", StringComparison.OrdinalIgnoreCase))
                Problems.Add($"{what} model '{model}' must NOT start with 'Meshes\\' — the engine roots model paths at Meshes\\ already (drop the prefix)");
            if (System.IO.Path.IsPathRooted(model) || p.StartsWith('\\') || p.Contains(':'))
                Problems.Add($"{what} model '{model}' must be a relative path, not absolute/drive-qualified");
        }

        void CheckSoundFile(string file, string what)
        {
            if (string.IsNullOrWhiteSpace(file)) return;
            var p = file.Replace('/', '\\');
            if (!(p.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".xwm", StringComparison.OrdinalIgnoreCase)))
                Problems.Add($"{what} file '{file}' must be a .wav or .xwm path (Data-relative under Sound\\, e.g. Sound\\fx\\mymod\\bell.wav)");
            if (System.IO.Path.IsPathRooted(file) || p.StartsWith('\\') || p.Contains(':'))
                Problems.Add($"{what} file '{file}' must be a relative path, not absolute/drive-qualified");
        }

        void CheckTexPath(string path, string what)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            var raw = path.Trim();
            var p = raw.Replace('/', '\\');
            bool rooted = raw.StartsWith('/') || raw.StartsWith('\\') || Path.IsPathRooted(raw)
                || (raw.Length >= 2 && char.IsLetter(raw[0]) && raw[1] == ':');
            if (rooted)
                Problems.Add($"{what} path '{path}' must be RELATIVE to Data\\Textures (e.g. mymod\\sword_d.dds), not absolute");
            if (p.StartsWith("Textures\\", StringComparison.OrdinalIgnoreCase))
                Problems.Add($"{what} path '{path}' must NOT start with 'Textures\\' — TXST slots are already relative to Data\\Textures (use e.g. mymod\\sword_d.dds)");
            if (!p.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                Problems.Add($"{what} path '{path}' must be a .dds texture");
        }

        void CheckAltTextures(string ed, string model, List<AlternateTextureSpec> alts, string kind)
        {
            if (alts.Count == 0) return;
            if (string.IsNullOrWhiteSpace(model))
                Problems.Add($"{kind} '{ed}' has alternateTextures but no `model` — nothing to retexture");
            foreach (var alt in alts)
            {
                if (string.IsNullOrWhiteSpace(alt.Name))
                    Problems.Add($"{kind} '{ed}' alternateTexture has empty `name` (must match a material/sub-mesh in the .nif)");
                if (string.IsNullOrWhiteSpace(alt.TextureSet))
                    Problems.Add($"{kind} '{ed}' alternateTexture '{alt.Name}' has empty `textureSet` ref");
                else CheckRef(alt.TextureSet, $"{kind} '{ed}' alternateTexture '{alt.Name}' textureSet");
                if (alt.Index < 0) Problems.Add($"{kind} '{ed}' alternateTexture '{alt.Name}' index must be >= 0");
            }
        }
    }
}
