using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using static WorkTab.InteractionUtilities;

namespace WorkTab;

#pragma warning disable CS0618
[HarmonyPatch(typeof(PawnColumnWorker_Label), "DoCell")]
public static class PawnColumnWorker_Label_DoCell
{
    private static readonly PawnColumnWorker_WorkTabLabel Instance = new();

    public static bool Prefix(Rect rect, Pawn pawn, PawnTable table)
    {
        var pawnTableDef = table.def;
        if (pawnTableDef == null || pawnTableDef != PawnTableDefOf.Work)
        {
            return true;
        }

        // intercept interactions before base has a chance to act on them
        if (!Shift || !Mouse.IsOver(rect))
        {
            return true;
        }

        if (RightClicked(rect) || ScrolledUp(rect, true)) {
            Instance.Increment(pawn);
            return false;
        }

        if (LeftClicked(rect) || ScrolledDown(rect, true)) {
            Instance.Decrement(pawn);
            return false;
        }
        return true;
    }
}
#pragma warning restore CS0618