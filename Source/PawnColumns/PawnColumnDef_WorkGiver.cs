// PawnColumnDef_WorkGiver.cs
// Copyright Karel Kroeze, 2017-2020

using RimWorld;
using Verse;

namespace WorkTab;

public class PawnColumnDef_WorkGiver : PawnColumnDef
{
    public WorkGiverDef workgiver;

    public override TaggedString LabelCap => workgiver.label.NullOrEmpty() ? workgiver.gerund?.CapitalizeFirst() : workgiver.LabelCap.Resolve()?.CapitalizeFirst();
}