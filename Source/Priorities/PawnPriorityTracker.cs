// PawnPriorityTracker.cs
// Copyright Karel Kroeze, 2018-2020

using RimWorld;
using Verse;

namespace WorkTab;

public class PawnPriorityTracker : PriorityTracker
{
    private Pawn _pawn;

    public PawnPriorityTracker()
    {
        // Scribe
    }

    public PawnPriorityTracker(Pawn pawn)
    {
        _pawn = pawn;
        foreach (var workgiver in DefDatabase<WorkGiverDef>.AllDefsListForReading)
        {
            priorities.Add(workgiver, new WorkPriority(this, workgiver));
        }
    }

    public override Pawn Pawn => _pawn;

    public override void ExposeData()
    {
        Scribe_References.Look(ref _pawn, "Pawn");
        base.ExposeData();
    }
}