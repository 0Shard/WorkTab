// PawnColumnWorker_Job.cs
// Copyright Karel Kroeze, 2017-2020

using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTab;

public class PawnColumnWorker_Job : PawnColumnWorker_Icon
{
    protected override Texture2D GetIconFor(Pawn pawn)
    {
        return pawn?.CurJob?.def.StatusIcon();
    }

    protected override Vector2 GetIconSize(Pawn pawn)
    {
        return def.HeaderIconSize;
    }

    protected override string GetIconTip(Pawn pawn)
    {
        return pawn.jobs?.curDriver?.GetReport() ?? "";
    }
}