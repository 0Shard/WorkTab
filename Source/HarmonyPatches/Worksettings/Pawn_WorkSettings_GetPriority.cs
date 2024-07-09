// Pawn_WorkSettings_GetPriority.cs
// Copyright Karel Kroeze, 2020-2020

using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkTab {
    [HarmonyPatch(typeof(Pawn_WorkSettings), "GetPriority")]
    public class Pawn_WorkSettings_GetPriority {
        private static bool Prefix(WorkTypeDef w, Pawn_WorkSettings __instance, ref int __result) {
            Pawn pawn = __instance.Pawn();
            if (!pawn.RaceProps.Humanlike)
            {
                return true;
            }
            __result = pawn.GetPriority(w, -1);
            return false;
        }
    }
}
