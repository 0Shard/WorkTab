// Pawn_Extensions.cs
// Copyright Karel Kroeze, 2020-2020

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkTab {
    public static class Pawn_Extensions {

        public static bool AllowedToDo(this Pawn pawn, WorkGiverDef workgiver)
        {
            return AllowedToDo(pawn, workgiver.workType);
        }

        public static bool AllowedToDo(this Pawn pawn, WorkTypeDef workType)
        {
            if (workType == null)
            {
                return false;
            }
            return !pawn.WorkTypeIsDisabled(workType);
        }

        public static bool NotAllowedToDo(this Pawn pawn, WorkTypeDef workType)
        {
            return !AllowedToDo(pawn, workType);
        }

        public static bool NothingCanBeDone(this Pawn pawn)
        {
            return pawn.Dead || pawn.workSettings == null || !pawn.workSettings.EverWork;
        }

        public static bool Humanlike(this Pawn pawn)
        {
            return pawn != null && pawn.RaceProps.Humanlike;
        }

        public static bool CapableOf(this Pawn pawn, WorkGiverDef workgiver) {
            return workgiver != null && !workgiver.requiredCapacities.Any(c => !pawn.health.capacities.CapableOf(c));
        }

        public static void Disable(this Pawn pawn, WorkTypeDef worktype) {
            pawn.SetPriority(worktype, 0, null);
        }

        public static void DisableAll(this Pawn pawn) {
            foreach (var worktype in DefDatabase<WorkTypeDef>.AllDefsListForReading) {
                Disable(pawn, worktype);
            }
        }

        public static int[] GetPriorities(this Pawn pawn, WorkTypeDef worktype) {
            return PriorityManager.Get[pawn].GetPriorities(worktype);
        }

        public static int[] GetPriorities(this Pawn pawn, WorkGiverDef workgiver) {
            return PriorityManager.Get[pawn].GetPriorities(workgiver);
        }

        public static int GetPriority(this Pawn pawn, WorkTypeDef worktype, int hour) {
            if (hour < 0) {
                hour = GenLocalDate.HourOfDay(pawn);
            }

            return PriorityManager.Get[pawn].GetPriority(worktype, hour);
        }

        public static int GetPriority(this Pawn pawn, WorkGiverDef workgiver, int hour) {
            if (hour < 0) {
                hour = GenLocalDate.HourOfDay(pawn);
            }

            return PriorityManager.Get[pawn].GetPriority(workgiver, hour);
        }

        public static void SetPriority(this Pawn pawn, WorkTypeDef worktype, int priority, List<int> hours) {
            PriorityManager.Get[pawn].SetPriority(worktype, priority, hours);
        }

        public static void SetPriority(this Pawn pawn, WorkTypeDef worktype, int priority, int hour, bool recache = true) {
            if (hour < 0) {
                hour = GenLocalDate.HourOfDay(pawn);
            }

            PriorityManager.Get[pawn].SetPriority(worktype, priority, hour, recache);
        }

        public static void SetPriority(this Pawn pawn, WorkGiverDef workgiver, int priority, List<int> hours) {
            PriorityManager.Get[pawn].SetPriority(workgiver, priority, hours);
        }

        public static void SetPriority(this Pawn pawn, WorkGiverDef workgiver, int priority, int hour, bool recache = true) {
            if (hour < 0) {
                hour = GenLocalDate.HourOfDay(pawn);
            }

            PriorityManager.Get[pawn].SetPriority(workgiver, priority, hour, recache);
        }
    }
}
