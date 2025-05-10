// PawnTable_RecacheIfDirty.cs
// Copyright Karel Kroeze, 2020-2020

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;

namespace WorkTab;

[HarmonyPatch(typeof(PawnTable), "Columns", MethodType.Getter)]
public static class PawnTable_Columns
{
    public static void Postfix(PawnTable __instance, ref List<PawnColumnDef> ___columns)
    {
        if (__instance.def != PawnTableDefOf.Work)
        {
            return;
        }
        // update alternating vertical positions
        var moveWorkTypeLabelDown = false;
        foreach (var columnDef in ___columns.Where(columnDef => typeof(PawnColumnWorker_WorkPriority).IsAssignableFrom(columnDef.workerClass)))
        {
            moveWorkTypeLabelDown = !moveWorkTypeLabelDown;
            columnDef.moveWorkTypeLabelDown = moveWorkTypeLabelDown;
        }
        __instance.RecacheIfDirty();
    }
}