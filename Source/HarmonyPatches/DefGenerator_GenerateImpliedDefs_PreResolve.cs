// Copyright Karel Kroeze, 2020-2021.
// WorkTab/WorkTab/DefGenerator_GenerateImpliedDefs_PreResolve.cs

using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkTab {
    [HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
    public class DefGenerator_GenerateImpliedDefs_PreResolve {
        private static void Postfix() {
            // replace worker on Work MainButtonDef
            MainButtonDefOf.Work.tabWindowClass = typeof(MainTabWindow_WorkTab);

            // get our table
            var workTable = PawnTableDefOf.Work;
            var workTableColumns = workTable.columns;

            // insert mood and job columns before first work column name
            var firstWorkIndex = workTableColumns.FindIndex(d => d.workerClass == typeof(PawnColumnWorker_WorkPriority));
            workTableColumns.Insert(firstWorkIndex, PawnColumnDefOf.Job);
            workTableColumns.Insert(firstWorkIndex + 1, PawnColumnDefOf.Mood);

            // go over PawnColumnDefs and replace all PawnColumnWorker_WorkPriority
            foreach (var column in DefDatabase<PawnColumnDef>.AllDefs) {
                if (column.workerClass == typeof(PawnColumnWorker_WorkPriority)) {
                    column.workerClass = typeof(PawnColumnWorker_WorkType);
                }
            }

            // add PawnColumnDefs for all workgivers
            foreach (var workgiver in DefDatabase<WorkGiverDef>.AllDefsListForReading) {
                // prepare the def, note that we're not assigning label or tip, we'll get those from the def later.
                // we're also not adding the def to the table, we'll do that dynamically when a worktype is expanded.
                var column = new PawnColumnDef_WorkGiver {
                    defName = "WorkGiver_" + workgiver.defName,
                    description = workgiver.description,
                    workType = workgiver.workType,
                    workgiver = workgiver,
                    workerClass = typeof(PawnColumnWorker_WorkGiver),
                    sortable = true
                };

                // finalize
                column.PostLoad();
                DefDatabase<PawnColumnDef>.Add(column);
            }

            // replace and move copy/paste to the right
            var copyPasteColumnIndex = workTableColumns.IndexOf(PawnColumnDefOf.CopyPasteWorkPriorities);
            workTableColumns.RemoveAt(copyPasteColumnIndex);

            var count = workTableColumns.Count;
            // Note; the far right column is a spacer to take all remaining available space, so index should be count - 2 (count - 1 before insert).
            workTableColumns.Insert(count - 1, PawnColumnDefOf.CopyPasteWorkPriorities);

            // add favourite column before copy paste
            workTableColumns.Insert(count - 2, PawnColumnDefOf.Favourite);

            // store this list of all columns
            Controller.allColumns = new List<PawnColumnDef>(workTableColumns);
        }
    }
}
