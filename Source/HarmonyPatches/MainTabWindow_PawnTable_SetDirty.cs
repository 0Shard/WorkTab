using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTab;

[HarmonyPatch(typeof(MainTabWindow_PawnTable), "SetDirty")]
public static class MainTabWindow_PawnTable_SetDirty
{
    public static void Prefix(MainTabWindow_PawnTable __instance)
    {
        if (__instance.PawnTableDef != PawnTableDefOf.Work)
        {
            return;
        }
        foreach (var columnWorker in __instance.table.def.columns.Select(c => c.Worker))
        {
            switch (columnWorker)
            {
                case IExpandableColumn expandableColumn:
                {
                    if (expandableColumn.NeedExpand)
                    {
                        MainTabWindow_WorkTab.Expand(expandableColumn, expandableColumn.NeedExpand);
                    }
                    if (expandableColumn.NeedCollapse)
                    {
                        MainTabWindow_WorkTab.Expand(expandableColumn, !expandableColumn.NeedCollapse);
                    }
                    break;
                }
                case PawnColumnWorker_WorkGiver workerWorkGiver:
                    workerWorkGiver.InvalidateVisibility();
                    break;
            }
        }
        __instance.windowRect.width = Mathf.Min(__instance.table.Size.x + __instance.Margin * 2f, UI.screenWidth - 16f);
    }
}