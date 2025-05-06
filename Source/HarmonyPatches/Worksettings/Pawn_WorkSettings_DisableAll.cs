// Pawn_WorkSettings_DisableAll.cs
// Copyright Karel Kroeze, 2020-2020

using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkTab {
    [HarmonyPatch(typeof(Pawn_WorkSettings), "DisableAll")]
    public static class Pawn_WorkSettings_DisableAll {
        public static void Prefix(Pawn_WorkSettings __instance) {
            var pawn = __instance.pawn;
            if (!pawn.Humanlike())
            {
                return;
            }

            pawn.DisableAll();
        }
    }

    [HarmonyPatch(typeof(Pawn_WorkSettings), "Disable")]
    public static class Pawn_WorkSettings_Disable {
        public static void Prefix(Pawn_WorkSettings __instance, WorkTypeDef w) {
            var pawn = __instance.pawn;
            if (!pawn.Humanlike())
            {
                return;
            }

            pawn.Disable(w);
        }
    }
}
