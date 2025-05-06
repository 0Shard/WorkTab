// Pawn_WorkSettings_CacheWorkGiversInOrder.cs
// Copyright Karel Kroeze, 2020-2020

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkTab;

[HarmonyPatch(typeof(Pawn_WorkSettings), "CacheWorkGiversInOrder")]
public class Pawn_WorkSettings_CacheWorkGiversInOrder {

    public static bool Prefix(Pawn_WorkSettings __instance,
                              ref bool ___workGiversDirty,
                              ref List<WorkGiver> ___workGiversInOrderEmerg,
                              ref List<WorkGiver> ___workGiversInOrderNormal) {
        var pawn = __instance.pawn;
        if (!pawn.Humanlike())
        {
            return true;
        }
        var allWorkgivers = DefDatabase<WorkGiverDef>.AllDefsListForReading
                                                     .Select( wgd => wgd.Worker )
                                                     .Where(wg => wg is { def: not null })
                                                     .Where(wg => pawn.GetPriority(wg.def, MainTabWindow_WorkTab.VisibleHour) > 0)
                                                     .ToList();

        var normalWorkgivers    = new List<WorkGiver>();
        var emergencyWorkgivers = new List<WorkGiver>();

        // sort by player set workgiver priorities => worktype order => workgiver order
        if (allWorkgivers.Any()) {
            allWorkgivers = allWorkgivers.OrderBy(wg => pawn.GetPriority(wg.def, MainTabWindow_WorkTab.VisibleHour))
                                         .ThenByDescending(wg => wg.def?.workType?.naturalPriority)
                                         .ThenByDescending(wg => wg.def?.priorityInType)
                                         .ToList();

            // lowest priority non-emergency job
            var maxEmergPrio = allWorkgivers.Where( wg => !wg.def.emergency )
                                            .Select( wg => pawn.GetPriority( wg.def, MainTabWindow_WorkTab.VisibleHour))
                                            .DefaultIfEmpty(int.MaxValue)
                                            .Min();

            // create lists of workgivers
            normalWorkgivers = allWorkgivers.Where(wg => !wg.def.emergency || pawn.GetPriority(wg.def, MainTabWindow_WorkTab.VisibleHour) > maxEmergPrio)
                                            .ToList();
            emergencyWorkgivers = allWorkgivers.Where(wg => wg.def.emergency && pawn.GetPriority(wg.def, MainTabWindow_WorkTab.VisibleHour) <= maxEmergPrio)
                                               .ToList();
        }

        Logger.Debug($"Updating priorities for {pawn.LabelShort};\n\t" +
                      $"{string.Join("\n\t", emergencyWorkgivers.Select(wg => wg.def.label).ToArray())}" +
                      $"{string.Join("\n\t", normalWorkgivers.Select(wg => wg.def.label).ToArray())}");

        // update cached lists of workgivers
        ___workGiversDirty = false;
        ___workGiversInOrderNormal = normalWorkgivers;
        ___workGiversInOrderEmerg = emergencyWorkgivers;

        // stop vanilla execution
        return false;
    }
}

