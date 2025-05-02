using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkTab;

#pragma warning disable CS0618
public static class PawnColumnWorker_CopyPasteWorkPriorities_CopyFrom_PasteTo
{
    private static readonly PawnColumnWorker_CopyPasteDetailedWorkPriorities Instance = new();
    
    [HarmonyPatch]
    public static class PawnColumnWorker_CopyPasteWorkPriorities_AnythingInClipboard
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(PawnColumnWorker_CopyPasteWorkPriorities), "AnythingInClipboard");
        }
        
        public static bool Prefix(ref bool __result)
        {
            __result = PawnColumnWorker_CopyPasteDetailedWorkPriorities.clipboard != null;
            return false;
        }
    }

    [HarmonyPatch(typeof(PawnColumnWorker_CopyPasteWorkPriorities), "CopyFrom")]
    public static class PawnColumnWorker_CopyPasteWorkPriorities_CopyFrom
    {
        public static bool Prefix(Pawn p)
        {
            Instance.CopyFrom(p);
            return false;
        }
    }

    [HarmonyPatch(typeof(PawnColumnWorker_CopyPasteWorkPriorities), "PasteTo")]
    public static class PawnColumnWorker_CopyPasteWorkPriorities_PasteTo
    {
        public static bool Prefix(Pawn p)
        {
            Instance.PasteTo(p);
            return false;
        }
    }
}
#pragma warning restore CS0618
