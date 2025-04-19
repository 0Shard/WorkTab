// Pawn_WorkSettings_GetPriority.cs
// Copyright Karel Kroeze, 2020-2020

using HarmonyLib;
using RimWorld;
using Verse;
using static WorkTab.MainTabWindow_WorkTab;

namespace WorkTab {
    [HarmonyPatch(typeof(Pawn_WorkSettings), "GetPriority")]
    public class Pawn_WorkSettings_GetPriority {
        private static void Postfix(WorkTypeDef w, Pawn_WorkSettings __instance, ref int __result) {
            var pawn = __instance.Pawn();
            if (!pawn.RaceProps.Humanlike)
            {
                return;
            }
            __result = pawn.GetPriority(w, VisibleHour);
        }
    }
}
