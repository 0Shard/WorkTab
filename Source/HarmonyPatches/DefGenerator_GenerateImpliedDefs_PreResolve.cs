// Copyright Karel Kroeze, 2020-2021.
// WorkTab/WorkTab/DefGenerator_GenerateImpliedDefs_PreResolve.cs

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkTab;

[HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
public static class DefGenerator_GenerateImpliedDefs_PreResolve
{
    private const string PawnColumnDefWorkGiverDefNamePrefix = "WorkPriority_WorkGiver_";

    public static bool ExpandableColumnsInitialized = true;

    public static void Postfix()
    {
        // replace worker on Work MainButtonDef
        MainButtonDefOf.Work.tabWindowClass = typeof(MainTabWindow_WorkTab);

        // Current is in rimworld async thread.
        ReBuildWorkTabColumns(true, true);

        ExpandableColumnsInitialized = false;
    }

    public static void ReBuildWorkTabColumns(bool isBuildColumns = false, bool isInRimWorldAsyncThread = false)
    {
        // get our table
        var workTable = PawnTableDefOf.Work;
        var workTableColumns = workTable.columns;

        if (isBuildColumns)
        {
            // Insert mood and job columns before the first work column.
            InsertMoodAndJobColumns(workTableColumns);

            MoveCopyPasteToRemainingSpace(workTableColumns);
        }

        // Keep the LabelWithIcon column first and freeze. Fix 'WorkManager' mod insert order error.
        EnsureLabelWithIconIsFirst(workTableColumns);

        // go over PawnColumnDefs and replace all PawnColumnWorker_WorkPriority
        ReplacePawnColumnDefWorkerClass();

        // add PawnColumnDefs for all workgivers
        WrapperWorkGiverDefToPawnColumnDef();

        // Inserts the corresponding workGiverColumn after each workTypeColumn
        var columns = ProcessWorkGiverColumns(workTableColumns, isInRimWorldAsyncThread);

        PawnTableDefOf.Work.columns = columns;

        MainTabWindow_WorkTab.SetCurrentWorkTabDirty();
    }

    private static void InsertMoodAndJobColumns(List<PawnColumnDef> workTableColumns)
    {
        var firstWorkIndex = workTableColumns.FindIndex(d => d.workerClass == typeof(PawnColumnWorker_WorkPriority));
        workTableColumns.InsertRange(firstWorkIndex, [PawnColumnDefOf.Job, PawnColumnDefOf.Mood]);
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
        if (columns[0] == PawnColumnDefOf.LabelWithIcon)
        {
            return;
        }

        columns.Remove(PawnColumnDefOf.LabelWithIcon);
        columns.Insert(0, PawnColumnDefOf.LabelWithIcon);
    }

    private static void ReplacePawnColumnDefWorkerClass()
    {
        foreach (var column in DefDatabase<PawnColumnDef>.AllDefs)
        {
            if (column.workerClass == typeof(PawnColumnWorker_WorkPriority))
            {
                column.workerClass = typeof(PawnColumnWorker_WorkType);
            }
        }
    }

    private static void WrapperWorkGiverDefToPawnColumnDef()
    {
        foreach (var column in DefDatabase<WorkGiverDef>.AllDefsListForReading
                                                        .Where(workgiver => workgiver.workType != null)
                                                        .Select(workgiver => new PawnColumnDef_WorkGiver
                                                        {
                                                            defName = PawnColumnDefWorkGiverDefNamePrefix + workgiver.defName,
                                                            description = workgiver.description,
                                                            workType = workgiver.workType,
                                                            workgiver = workgiver,
                                                            workerClass = typeof(PawnColumnWorker_WorkGiver),
                                                            sortable = true,
                                                            modContentPack = Controller.Instance.Content
                                                        })
                                                        .Cast<PawnColumnDef>())
        {
            if (DefDatabase<PawnColumnDef>.GetNamedSilentFail(column.defName) == null)
            {
                DefGenerator.AddImpliedDef(column);
            }
        }
    }

    // Note: All column addition, deletion and modification operations must be called once.
    private static List<PawnColumnDef> ProcessWorkGiverColumns(List<PawnColumnDef> workTableColumns, bool isInRimWorldAsyncThread = false)
    {
        var pawnColumnDefWorkGivers = DefDatabase<PawnColumnDef>.AllDefsListForReading
                                                                .Where(c => c.workerClass == typeof(PawnColumnWorker_WorkGiver))
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

            if (column.workerClass != typeof(PawnColumnWorker_WorkType))
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

            if (isInRimWorldAsyncThread)
            {
                continue;
            }

            // If you are currently in an asynchronous thread, you cannot directly access the Worker instance,
            // because some custom Workers with mods directly define Texture2D fields.
            // The first direct access will trigger instantiation and will cause an error:
            // 'Tried to get a resource "xxx" from a different thread. All resources must be loaded in the main thread.'
            if (column.Worker is PawnColumnWorker_WorkType worker)
            {
                if (list.Count > 1)
                {
                    worker.CanExpand = true;
                }
                foreach (var pawnColumnWorkerWorkGiver in list.Select(d => d.Worker as PawnColumnWorker_WorkGiver))
                {
                    if (pawnColumnWorkerWorkGiver != null)
                    {
                        pawnColumnWorkerWorkGiver.ColumnWorkerWorkType = worker;
                    }
                }
            }

            pawnColumnDefWorkGivers = pawnColumnDefWorkGivers.Except(list).ToList();
        }

        return columns;
    }

    public static void InitializeExpandableColumns()
    {
        var workTableColumns = PawnTableDefOf.Work.columns;
        var count = workTableColumns.Count;
        for (var i = 0; i < count; i++)
        {
            if (workTableColumns[i].Worker is not PawnColumnWorker_WorkType workerWorkType)
            {
                continue;
            }

            var j = i + 1;
            var start = j;

            while (j < count && workTableColumns[j].Worker is PawnColumnWorker_WorkGiver)
            {
                j++;
            }

            var num = j - start;
            if (num > 1)
            {
                workerWorkType.CanExpand = true;
            }
            for (var k = start; k < j; k++)
            {
                var workerWorkGiver = (PawnColumnWorker_WorkGiver)workTableColumns[k].Worker;
                workerWorkGiver.ColumnWorkerWorkType = workerWorkType;
            }

            i = j - 1;
        }

        ExpandableColumnsInitialized = true;
    }
}