using HarmonyLib;
using Verse;

namespace WorkTab.HarmonyPatches.ThirdPart;

[StaticConstructorOnStartup]
public static class TDEnhancementPackPatches
{
    static TDEnhancementPackPatches()
    {
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (!ModsConfig.IsActive("Uuugggg.TDPack"))
            {
                return;
            }

            if (AccessTools.Field("TD_Enhancement_Pack.UrgentRefill:active")?.GetValue(null) is not true)
            {
                return;
            }

            DefGenerator_GenerateImpliedDefs_PreResolve.ReBuildWorkTabColumns();
        });
    }
}