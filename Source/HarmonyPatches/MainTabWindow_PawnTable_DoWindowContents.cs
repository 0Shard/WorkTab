using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTab;

[HarmonyPatch(typeof(MainTabWindow_PawnTable), "DoWindowContents")]
public static class MainTabWindow_PawnTable_DoWindowContents
{
    public static bool ColumnsChanged;

    public static void Prefix(MainTabWindow_PawnTable __instance)
    {
        if (__instance.PawnTableDef != PawnTableDefOf.Work || __instance is MainTabWindow_WorkTab)
        {
            ColumnsChanged = false;
            return;
        }

        if (ColumnsChanged)
        {
            __instance.SetDirty();
            __instance.table.RecacheIfDirty();
        }

        ColumnsChanged = false;
    }

    public static void Postfix(MainTabWindow_PawnTable __instance, Rect rect)
    {
        if (__instance.PawnTableDef != PawnTableDefOf.Work || __instance is MainTabWindow_WorkTab)
        {
            return;
        }

        var canvas = new Rect(rect.x, rect.y, Mathf.Min(rect.width, UI.screenWidth), rect.height);
        var button = new Rect(canvas.xMax - 30f - __instance.Margin, canvas.yMin, 30f, 30f);
        InteractionUtilities.ButtonImageToggle(() => MainTabWindow_WorkTab.AnyExpandedNew(__instance.table),
                                               v => MainTabWindow_WorkTab.ExpandAll(__instance.table, v),
                                               button,
                                               "WorkTab.ExpandAll".Translate(), Resources.Expand,
                                               "WorkTab.CollapseAll".Translate(), Resources.Collapse);
    }
}