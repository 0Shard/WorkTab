// PawnTable_RecacheIfDirty.cs
// Copyright Karel Kroeze, 2020-2020

using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace WorkTab;

[HarmonyPatch(typeof(PawnTable), "RecacheSize")]
public static class PawnTable_RecacheSize
{
    public static bool Prefix(PawnTable __instance)
    {
        if (__instance.def != PawnTableDefOf.Work)
        {
            return true;
        }

        __instance.maxTableWidth = int.MaxValue;
        __instance.maxTableHeight = int.MaxValue;

        if (__instance.hasFixedSize)
        {
            __instance.cachedSize = __instance.fixedSize;
        }
        else
        {
            var columns = __instance.Columns;
            var num = columns.Where(c => !c.ignoreWhenCalculatingOptimalTableSize).Sum(__instance.GetOptimalWidth);
            var a1 = Mathf.Clamp(num + 16f, __instance.minTableWidth, __instance.maxTableWidth);
            var a2 = Mathf.Clamp(__instance.cachedHeightNoScrollbar, __instance.minTableHeight, __instance.maxTableHeight);
            __instance.cachedSize = new Vector2(a1, a2);
        }
        return false;
    }
}