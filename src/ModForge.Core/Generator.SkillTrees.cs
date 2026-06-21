namespace ModForge;

public static partial class Generator
{
    // --- In-world skill tree macro-expansion (Idea #20) ----------------------------------------
    // A skillTree is sugar: it EXPANDS into the low-level records the validated, IN-GAME-CONFIRMED
    // hand-authored tree used, so every battle-tested build pass (globals, activators, placements,
    // script-attach) does the real work. Called once at the very top of Build() (before pass 1).
    //
    // For each tree, for each node[i] (stacked bottom→top at origin + i*spacing in +Z):
    //   * a rank GLOB ("<tree>_<node>_Rank", short 0)
    //   * a node Activator (the floating star) + its placement at the stacked position
    //   * an MFSkillNode script attach (ability/rank/points/name + prereq+downLine for i>0)
    //   * for i>0: a connector-line Activator + placement at the midpoint, oriented vertically
    // Plus one shared points GLOB per tree (auto "<tree>_Points" unless `pointsGlobal` is given).
    //
    // MFSkillNode.pex (node behaviour) is shipped by `package` when any skillTree exists.
    public const string SkillNodeScript = "MFSkillNode";
    public const string DefaultSkillNodeModel = @"campfire\_camp_intperkstars01.nif";
    public const string DefaultSkillLineModel = @"campfire\_camp_intperkline01.nif";
    // The default line mesh fits between nodes 65 units apart at scale 1.0 (IN-GAME-CONFIRMED). Other
    // spacings uniformly scale the line ref (Skyrim XSCL is a single float — length and thickness scale
    // together). Vertical lines use Frostfall's proven rotation.
    private const float SkillLineNativeSpacing = 65f;

    public static void ExpandSkillTrees(ModSpec spec)
    {
        if (spec.SkillTreesExpanded) return;
        spec.SkillTreesExpanded = true;
        if (spec.SkillTrees.Count == 0) return;

        foreach (var st in spec.SkillTrees)
        {
            var nodeModel = string.IsNullOrWhiteSpace(st.NodeModel) ? DefaultSkillNodeModel : st.NodeModel;
            var lineModel = string.IsNullOrWhiteSpace(st.LineModel) ? DefaultSkillLineModel : st.LineModel;

            // Points pool: reference an existing global, or auto-create one seeded with startingPoints.
            string pointsGlobal = st.PointsGlobal;
            if (string.IsNullOrWhiteSpace(pointsGlobal))
            {
                pointsGlobal = $"{st.EditorId}_Points";
                spec.Globals.Add(new GlobalSpec { EditorId = pointsGlobal, Type = "short", Value = st.StartingPoints });
            }

            string PrevRank(int i) => $"{st.EditorId}_{st.Nodes[i].EditorId}_Rank";

            for (int i = 0; i < st.Nodes.Count; i++)
            {
                var n = st.Nodes[i];
                var rankGlobal = $"{st.EditorId}_{n.EditorId}_Rank";
                var nodeEd = $"{st.EditorId}_{n.EditorId}";
                var nodeRef = nodeEd + "Ref";
                float z = st.Origin.Z + i * st.Spacing;

                spec.Globals.Add(new GlobalSpec { EditorId = rankGlobal, Type = "short", Value = 0 });
                spec.Activators.Add(new ActivatorSpec { EditorId = nodeEd, Name = n.Name, Model = nodeModel });
                // NB: NOT persistent — the IN-GAME-CONFIRMED hand-authored tree placed these as temporary
                // refs (the only state that must persist is the rank GLOBs, which are; the lit visual is
                // re-applied by MFSkillNode.OnLoad). Persistent scripted refs were the one structural diff
                // vs the confirmed-good build, so keep parity.
                spec.Placements.Add(new PlacementSpec
                {
                    Base = nodeEd, EditorId = nodeRef, Cell = st.Cell,
                    Position = new Vec3 { X = st.Origin.X, Y = st.Origin.Y, Z = z },
                });

                string downLineRef = "";
                if (i > 0)
                {
                    // Connector line from the node below to this one — placed at the midpoint, stood
                    // vertical (Frostfall's proven rot), scaled to the spacing.
                    var lineEd = $"{st.EditorId}_Line{i}";
                    downLineRef = lineEd + "Ref";
                    float midZ = st.Origin.Z + (i - 0.5f) * st.Spacing;
                    spec.Activators.Add(new ActivatorSpec { EditorId = lineEd, Model = lineModel });
                    spec.Placements.Add(new PlacementSpec
                    {
                        Base = lineEd, EditorId = downLineRef, Cell = st.Cell,
                        Position = new Vec3 { X = st.Origin.X, Y = st.Origin.Y, Z = midZ },
                        Rotation = new Vec3 { X = 90f, Y = 0f, Z = 180f },
                        Scale = st.Spacing / SkillLineNativeSpacing,
                    });
                }

                var props = new List<PropertySpec>
                {
                    new() { Name = "nodeAbility",  Type = "object", ObjectEditorId = n.Ability },
                    new() { Name = "rankGlobal",   Type = "object", ObjectEditorId = rankGlobal },
                    new() { Name = "pointsGlobal", Type = "object", ObjectEditorId = pointsGlobal },
                    new() { Name = "nodeName",     Type = "string", Str = n.Name },
                };
                if (i > 0)
                {
                    props.Add(new() { Name = "prereqGlobal", Type = "object", ObjectEditorId = PrevRank(i - 1) });
                    props.Add(new() { Name = "downLine",     Type = "object", ObjectEditorId = downLineRef });
                }
                spec.Scripts.Add(new ScriptAttachSpec { TargetEditorId = nodeEd, ScriptName = SkillNodeScript, Properties = props });
            }
        }
    }
}
