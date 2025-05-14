// WorkType_Extensions.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace WorkTab {
    public static class WorkType_Extensions {

        [Obsolete("_workgiversByType field is abandoned.")]
        private static readonly Dictionary<WorkTypeDef, List<WorkGiverDef>> _workgiversByType = new();

        [Obsolete("workListCache field is abandoned.")]
        private static readonly Dictionary<WorkTypeDef, string> workListCache = new();

        public static void DecrementPriority(this WorkTypeDef worktype, Pawn pawn, int hour = -1,
                                              List<int> hours = null, bool playSound = true) {
            // get default hour if not specified
            if (hour < 0) {
                hour = GenLocalDate.HourOfDay(pawn);
            }

            // get and decrement priority
            int priority = pawn.GetPriority( worktype, hour );
            pawn.SetPriority(worktype, priority - 1, hours);

            // play sounds
            if (playSound && priority > 1) {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }

            if (playSound && priority == 1) {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
        }

        public static void DecrementPriority(this WorkTypeDef worktype, List<Pawn> pawns, int hour = -1,
                                              List<int> hours = null, bool playSound = true) {
            // bail out on bad input
            if (pawns.NullOrEmpty()) {
                return;
            }

            // get default hour if not specified, we're assuming all pawns are on the same map/tile
            if (hour < 0) {
                hour = GenLocalDate.HourOfDay(pawns.FirstOrDefault());
            }

            // play sounds
            if (playSound && pawns.Any(p => p.GetPriority(worktype, hour) != 1)) {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }

            // decrease priorities that are not 1 only (no wrapping around once we're at max priority)
            foreach (Pawn pawn in pawns.Where(p => p.GetPriority(worktype, hour) != 1).DistinctTrackers()) {
                DecrementPriority(worktype, pawn, hour, hours, false);
            }
        }

        public static void IncrementPriority(this WorkTypeDef worktype, Pawn pawn, int hour = -1,
                                              List<int> hours = null, bool playSound = true) {
            // get default hour if not specified
            if (hour < 0) {
                hour = GenLocalDate.HourOfDay(pawn);
            }

            // get and increment priority
            int priority = pawn.GetPriority( worktype, hour );
            pawn.SetPriority(worktype, priority + 1, hours);

            // play sounds
            if (playSound && priority == 0) {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }

            if (playSound && priority > 0) {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
        }

        public static void IncrementPriority(this WorkTypeDef worktype, List<Pawn> pawns, int hour = -1,
                                              List<int> hours = null, bool playSound = true) {
            // bail out on bad input
            if (pawns.NullOrEmpty()) {
                return;
            }

            // get default hour if not specified, we're assuming all pawns are on the same map/tile
            if (hour < 0) {
                hour = GenLocalDate.HourOfDay(pawns.FirstOrDefault());
            }

            // play sounds
            if (playSound && pawns.Any(p => p.GetPriority(worktype, hour) > 0)) {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }

            // increase priorities that are > 0 only (no wrapping around once we're at min priority
            foreach (Pawn pawn in pawns.Where(p => p.GetPriority(worktype, hour) > 0).DistinctTrackers()) {
                IncrementPriority(worktype, pawn, hour, hours, false);
            }
        }

        [Obsolete("Use WorkTypeDef.workGiversByPriority instead. This method is deprecated and will be removed in a future version.")]
        public static List<WorkGiverDef> WorkGivers(this WorkTypeDef worktype) {
            return worktype.workGiversByPriority;
        }
    }
}
