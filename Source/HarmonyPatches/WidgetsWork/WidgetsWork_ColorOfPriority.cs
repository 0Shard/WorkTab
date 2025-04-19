using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace WorkTab
{
    [HarmonyPatch(typeof(WidgetsWork), "ColorOfPriority")]
    public static class WidgetsWork_ColorOfPriority
    {
        private static bool Prefix(int prio, ref Color __result)
        {
            __result = DrawUtilities.ColorOfPriority(prio);
            return false;
        }
    }
}