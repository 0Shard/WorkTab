using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTab;

[HarmonyPatch(typeof(MainTabWindow_Work), "DoWindowContents")]
public static class MainTabWindow_Work_DoWindowContents
{
    public static void Postfix(MainTabWindow_Work __instance, Rect rect)
    {
        var canvas = new Rect(rect.x, rect.y, Mathf.Min(rect.width, UI.screenWidth), rect.height);
        var button = new Rect(canvas.xMax - 30f - __instance.Margin, canvas.yMin, 30f, 30f);
        InteractionUtilities.ButtonImageToggle(() => MainTabWindow_WorkTab.AnyExpandedNew(__instance.table),
                                               v => MainTabWindow_WorkTab.ExpandAll(__instance.table, v),
                                               button,
                                               "WorkTab.ExpandAll".Translate(), Resources.Expand,
                                               "WorkTab.CollapseAll".Translate(), Resources.Collapse);
    }
}