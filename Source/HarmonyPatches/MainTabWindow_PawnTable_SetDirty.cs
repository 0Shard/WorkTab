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
        foreach (var expandableColumn in __instance.table.Columns.Select(c => c.Worker).OfType<IExpandableColumn>())
        {
            if (expandableColumn.NeedExpand)
            {
                MainTabWindow_WorkTab.Expand(expandableColumn, expandableColumn.NeedExpand);
            }
            if (expandableColumn.NeedCollapse)
            {
                MainTabWindow_WorkTab.Expand(expandableColumn, !expandableColumn.NeedCollapse);
            }
        }
        __instance.windowRect.width = Mathf.Min(__instance.table.Size.x + __instance.Margin * 2f, UI.screenWidth);
    }
}