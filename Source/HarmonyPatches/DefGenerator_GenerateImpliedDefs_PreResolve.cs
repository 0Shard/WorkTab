// Copyright Karel Kroeze, 2020-2021.
// WorkTab/WorkTab/DefGenerator_GenerateImpliedDefs_PreResolve.cs

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkTab;

[HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
public static class DefGenerator_GenerateImpliedDefs_PreResolve {

    public static void Postfix() {
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
        }

        // Insert the corresponding workGiverColumn after each workTypeColumn
        var columns = ProcessWorkGiverColumns(workTableColumns);

        MoveCopyPasteToRemainingSpace(columns);

        EnsureLabelWithIconIsFirst(columns);

        PawnTableDefOf.Work.columns = columns;
    }

    public static List<PawnColumnDef> ProcessWorkGiverColumns(List<PawnColumnDef> workTableColumns)
    {
        var pawnColumnDefWorkGivers = DefDatabase<PawnColumnDef>.AllDefsListForReading
                                                       .Where(c => c.Worker is PawnColumnWorker_WorkGiver)
                                                       .ToList();
        var count = workTableColumns.Count;
        var columns = new List<PawnColumnDef>(count + pawnColumnDefWorkGivers.Count);
        foreach (var column in workTableColumns)
        {
            if (!typeof(PawnColumnWorker_WorkPriority).IsAssignableFrom(column.workerClass))
            {
                columns.Add(column);
                continue;
            }

            if (column.Worker is not PawnColumnWorker_WorkType)
            {
                continue;
            }

            var list = pawnColumnDefWorkGivers.Where(c => ((PawnColumnDef_WorkGiver)c).workgiver.workType == column.workType)
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

            pawnColumnDefWorkGivers = pawnColumnDefWorkGivers.Except(list).ToList();
        }

        return columns;
    }

    private static void MoveCopyPasteToRemainingSpace(List<PawnColumnDef> columns)
    {
        // move copy/paste to the right
        var copyPasteColumnIndex = columns.IndexOf(PawnColumnDefOf.CopyPasteWorkPriorities);
        columns.RemoveAt(copyPasteColumnIndex);

        var remainingSpaceColumnIndex = columns.IndexOf(PawnColumnDefOf.RemainingSpace);
        columns.InsertRange(remainingSpaceColumnIndex, [PawnColumnDefOf.Favourite, PawnColumnDefOf.CopyPasteWorkPriorities]);
    }

    private static void EnsureLabelWithIconIsFirst(List<PawnColumnDef> columns)
    {
        // Keep the LabelWithIcon column first and freeze. Fix 'WorkManager' mod insert order error.
        if (columns[0] == PawnColumnDefOf.LabelWithIcon)
        {
            return;
        }
        columns.Remove(PawnColumnDefOf.LabelWithIcon);
        columns.Insert(0, PawnColumnDefOf.LabelWithIcon);
    }
}
