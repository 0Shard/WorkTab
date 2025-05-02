// PawnColumnWorker_CopyPasteDetailedWorkPriorities.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WorkTab
{
    [Obsolete("PawnColumnWorker_CopyPasteDetailedWorkPriorities is abandoned. See PawnColumnWorker_CopyPasteWorkPriorities_CopyFrom_PasteTo.cs for replacement.")]
    public class PawnColumnWorker_CopyPasteDetailedWorkPriorities
    {
        // Don't rename it. Multiplayer Compatibility mod need.
        public static Dictionary<WorkGiverDef, int[]> clipboard;
        
        public void CopyFrom(Pawn pawn)
        {
            clipboard ??= new Dictionary<WorkGiverDef, int[]>();
            foreach (var workgiver in DefDatabase<WorkGiverDef>.AllDefsListForReading)
            {
                clipboard[workgiver] = pawn.GetPriorities(workgiver);
            }
        }

        public void PasteTo(Pawn pawn)
        {
            foreach (var workTypeDef in DefDatabase<WorkTypeDef>.AllDefsListForReading
                         .Where(workTypeDef => !pawn.WorkTypeIsDisabled(workTypeDef)))
            {
                // apply all workgivers for this type
                foreach (var workgiver in workTypeDef.WorkGivers())
                {
                    for (var hour = 0; hour < GenDate.HoursPerDay; hour++)
                    {
                        // recache the type (bubbles down to workgivers). InvalidateCache will be called internally.
                        pawn.SetPriority(workgiver, clipboard[workgiver][hour], hour);
                    }
                }
            }
        }
    }
}