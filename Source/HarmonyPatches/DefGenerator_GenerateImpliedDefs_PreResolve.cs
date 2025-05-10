// Copyright Karel Kroeze, 2020-2021.
// WorkTab/WorkTab/DefGenerator_GenerateImpliedDefs_PreResolve.cs

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkTab;

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
        var pawnColumnDefWorkGiverCount = 0;
        foreach (var column in DefDatabase<WorkGiverDef>.AllDefsListForReading
                                                        .Where(workgiver => workgiver.workType != null)
                                                        .Select(workgiver => new PawnColumnDef_WorkGiver {
                                                            defName = "WorkPriority_WorkGiver_" + workgiver.defName,
                                                            description = workgiver.description,
                                                            workType = workgiver.workType,
                                                            workgiver = workgiver,
                                                            workerClass = typeof(PawnColumnWorker_WorkGiver),
                                                            sortable = true,
                                                            modContentPack = Controller.Instance.Content
                                                        }).Cast<PawnColumnDef>())
        {
            DefGenerator.AddImpliedDef(column);
            ++pawnColumnDefWorkGiverCount;
        }

        var count = workTableColumns.Count;
        var columns = new List<PawnColumnDef>(pawnColumnDefWorkGiverCount + count);
        for (var index = 0; index < count; index++)
        {
            var column = workTableColumns[index];

            if (!typeof(PawnColumnWorker_WorkPriority).IsAssignableFrom(column.workerClass))
            {
                columns.Add(column);
                continue;
            }

            var list = DefDatabase<PawnColumnDef>.AllDefsListForReading
                                                 .Where(c => c.workerClass == typeof(PawnColumnWorker_WorkGiver))
                                                 .Where(c => c.workType == column.workType)
                                                 .OrderByDescending(c => ((PawnColumnDef_WorkGiver)c).workgiver.priorityInType)
                                                 .ToList();

            columns.Add(column);
            if (list.NullOrEmpty())
            {
                continue;
            }
            columns.AddRange(list);
            if (column.Worker is PawnColumnWorker_WorkType worker)
            {
                worker.ChildColumns = list;
            }
            foreach (var pawnColumnWorkerWorkGiver in list.Select(d => d.Worker as PawnColumnWorker_WorkGiver))
            {
                if (pawnColumnWorkerWorkGiver != null)
                {
                    pawnColumnWorkerWorkGiver.ParentColumn = column;
                }
            }
        }

        // move copy/paste to the right
        var copyPasteColumnIndex = columns.IndexOf(PawnColumnDefOf.CopyPasteWorkPriorities);
        columns.RemoveAt(copyPasteColumnIndex);

        var remainingSpaceColumnIndex = columns.IndexOf(PawnColumnDefOf.RemainingSpace);
        columns.InsertRange(remainingSpaceColumnIndex, [PawnColumnDefOf.Favourite, PawnColumnDefOf.CopyPasteWorkPriorities]);

        // keep the LabelWithIcon column first and freeze. Fix 'WorkManager' mod insert order error.
        if (columns[0] != PawnColumnDefOf.LabelWithIcon)
        {
            columns.Remove(PawnColumnDefOf.LabelWithIcon);
            columns.Insert(0, PawnColumnDefOf.LabelWithIcon);
        }

        PawnTableDefOf.Work.columns = columns;
    }
}
