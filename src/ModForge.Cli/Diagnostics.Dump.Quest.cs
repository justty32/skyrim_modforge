internal static partial class Program
{
    // Weather/climate/quest/scene portion of the dump per-record detail chain (extracted from
    // DumpRecordMagicAiAndText). Covers the IWeatherGetter, IClimateGetter, IQuestGetter and
    // ISceneGetter blocks — everything from the weather block to the end of the method.
    private static void DumpRecordQuestAndScene(IMajorRecordGetter r, Func<FormKey, string> Ref)
    {
        if (r is IWeatherGetter wthr)
        {
            int tex = wthr.CloudTextures.Count(t => t is not null);
            static string C(IWeatherColorGetter? c) => c is null ? "-"
                : $"sr={Rgb(c.Sunrise)} day={Rgb(c.Day)} ss={Rgb(c.Sunset)} ni={Rgb(c.Night)}";
            Console.WriteLine($"      weather: flags={wthr.Flags} wind(speed={wthr.WindSpeed} dir={wthr.WindDirection * 360f:0.#}deg range={wthr.WindDirectionRange * 360f:0.#}deg)"
                + $" {tex} cloud texture(s)"
                + (wthr.Precipitation.FormKeyNullable is { } pk && !pk.IsNull ? $" precip={Ref(pk)}" : ""));
            Console.WriteLine($"        skyUpper: {C(wthr.SkyUpperColor)}");
            Console.WriteLine($"        fogNear:  {C(wthr.FogNearColor)}");
            Console.WriteLine($"        sun:      {C(wthr.SunColor)}");
            Console.WriteLine($"        fogDist: day(near={wthr.FogDistanceDayNear} far={wthr.FogDistanceDayFar}) night(near={wthr.FogDistanceNightNear} far={wthr.FogDistanceNightFar})");
        }

        if (r is IClimateGetter clim)
        {
            Console.WriteLine($"      climate: sunrise({clim.SunriseBegin:HH:mm}-{clim.SunriseEnd:HH:mm}) sunset({clim.SunsetBegin:HH:mm}-{clim.SunsetEnd:HH:mm})"
                + $" moons={clim.Moons} phaseLen={clim.PhaseLength} volatility={clim.Volatility}"
                + $" sun={clim.SunTexture?.GivenPath ?? "-"} glare={clim.SunGlareTexture?.GivenPath ?? "-"}");
            foreach (var wt in clim.WeatherTypes ?? Enumerable.Empty<IWeatherTypeGetter>())
                Console.WriteLine($"        weather -> {Ref(wt.Weather.FormKey)} (chance {wt.Chance})");
        }

        if (r is IQuestGetter q)
        {
            Console.WriteLine($"      quest: flags={q.Flags}  priority={q.Priority}");
            foreach (var s in q.Stages)
            {
                Console.WriteLine($"      stage[{s.Index}] flags={s.Flags}");
                foreach (var le in s.LogEntries)
                {
                    var flagStr = le.Flags == default ? "" : $" [{le.Flags}]";
                    Console.WriteLine($"        log{flagStr}: \"{le.Entry?.String}\"" + (le.Conditions.Count > 0 ? $"  ({le.Conditions.Count} cond)" : ""));
                }
            }
            foreach (var o in q.Objectives)
                Console.WriteLine($"      objective[{o.Index}]: \"{o.DisplayText?.String}\"");
            // Scene actor aliases live on the host quest — surface their NPC binding (UniqueActor).
            foreach (var al in q.Aliases.OfType<IQuestAliasGetter>())
                if (!al.UniqueActor.FormKey.IsNull)
                    Console.WriteLine($"      alias[{al.ID}] \"{al.Name}\" -> uniqueActor {Ref(al.UniqueActor.FormKey)}");
        }

        if (r is ISceneGetter sc)
        {
            Console.WriteLine($"      scene: quest={Ref(sc.Quest.FormKey)}  flags={sc.Flags}  "
                + $"{sc.Actors.Count} actor(s), {sc.Phases.Count} phase(s), {sc.Actions.Count} action(s)");
            foreach (var a in sc.Actors)
                Console.WriteLine($"        actor alias #{a.ID}  behavior={a.BehaviorFlags}");
            foreach (var act in sc.Actions)
                Console.WriteLine($"        action: {act.Type} alias #{act.ActorID} phase {act.StartPhase}"
                    + (act.Topic.FormKey.IsNull ? "" : $" -> topic {Ref(act.Topic.FormKey)}")
                    + (act.Type == SceneAction.TypeEnum.Dialog ? $" ({act.Emotion})" : ""));
        }
    }
}
