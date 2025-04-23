using HarmonyLib;
using Verse;

namespace WorkTab.HarmonyPatches.ThirdPart;

public static class WorkManagerPatches
{
    public static bool IsActive()
    {
        return ModsConfig.IsActive("LordKuper.WorkManager");
    }

    public static void UnPatch(Harmony harmony)
    {
        if (!IsActive())
        {
            return;
        }
        
        LongEventHandler.QueueLongEvent(() =>
        {
            var original = AccessTools.Method(typeof(PawnColumnWorker_WorkType), "DoHeader");
            harmony.Unpatch(original, HarmonyPatchType.Prefix, "LordKuper.WorkManager");
            
            var original2 = AccessTools.Method(typeof(PawnColumnWorker_WorkType), "HandleInteractionsDetailed");
            harmony.Unpatch(original2, HarmonyPatchType.Prefix, "LordKuper.WorkManager");
            
            var original3 = AccessTools.Method(typeof(PawnColumnWorker_WorkType), "DrawWorkTypeBoxFor");
            harmony.Unpatch(original3, HarmonyPatchType.Postfix, "LordKuper.WorkManager");

            var original4 = AccessTools.Method(typeof(PawnColumnWorker_WorkType), "GetMinHeaderHeight");
            harmony.Unpatch(original4, HarmonyPatchType.Postfix, "LordKuper.WorkManager");

        }, "DelayedUnpatch", false, null);
    }
}