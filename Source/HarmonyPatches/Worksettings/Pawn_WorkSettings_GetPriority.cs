// Pawn_WorkSettings_GetPriority.cs
// Copyright Karel Kroeze, 2020-2020

using HarmonyLib;
using RimWorld;
using Verse;
using static WorkTab.MainTabWindow_WorkTab;

namespace WorkTab;

[HarmonyPatch(typeof(Pawn_WorkSettings), "GetPriority")]
public class Pawn_WorkSettings_GetPriority
{
    // HIGHLIGHT: 'Force Do Job' mod need. Don't change to Postfix.
    private static bool Prefix(WorkTypeDef w, Pawn_WorkSettings __instance, ref int __result)
    {
        var pawn = __instance.pawn;
        if (!pawn.Humanlike())
        {
            return true;
        }

        __result = pawn.GetPriority(w, VisibleHour);
        return false;
    }
}