using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkTab;

#pragma warning disable CS0618
public static class PawnColumnWorker_CopyPasteWorkPriorities_CopyFrom_PasteTo
{
    private static readonly PawnColumnWorker_CopyPasteDetailedWorkPriorities Instance = new();

    private static PawnTableDef _columnBelongTableDef;

    [HarmonyPatch]
    public static class PawnColumnWorker_CopyPasteWorkPriorities_AnythingInClipboard
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(PawnColumnWorker_CopyPasteWorkPriorities), "AnythingInClipboard");
        }

        public static bool Prefix(ref bool __result)
        {
            if (_columnBelongTableDef != PawnTableDefOf.Work)
            {
                return true;
            }
            __result = PawnColumnWorker_CopyPasteDetailedWorkPriorities.clipboard != null;
            return false;
        }
    }

    [HarmonyPatch(typeof(PawnColumnWorker_CopyPasteWorkPriorities), "DoCell")]
    public static class PawnColumnWorker_CopyPasteWorkPriorities_DoCell
    {
        public static void Prefix(PawnTable table)
        {
            _columnBelongTableDef = table.def;
        }
    }

    [HarmonyPatch(typeof(PawnColumnWorker_CopyPasteWorkPriorities), "CopyFrom")]
    public static class PawnColumnWorker_CopyPasteWorkPriorities_CopyFrom
    {
        public static bool Prefix(Pawn p)
        {
            if (_columnBelongTableDef != PawnTableDefOf.Work)
            {
                return true;
            }
            Instance.CopyFrom(p);
            return false;
        }
    }

    [HarmonyPatch(typeof(PawnColumnWorker_CopyPasteWorkPriorities), "PasteTo")]
    public static class PawnColumnWorker_CopyPasteWorkPriorities_PasteTo
    {
        public static bool Prefix(Pawn p)
        {
            if (_columnBelongTableDef != PawnTableDefOf.Work)
            {
                return true;
            }
            Instance.PasteTo(p);
            return false;
        }
    }
}
#pragma warning restore CS0618
