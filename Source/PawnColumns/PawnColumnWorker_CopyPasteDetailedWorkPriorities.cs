// PawnColumnWorker_CopyPasteDetailedWorkPriorities.cs
// Copyright Karel Kroeze, 2020-2020

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WorkTab
{
    public class PawnColumnWorker_CopyPasteDetailedWorkPriorities : PawnColumnWorker_CopyPasteWorkPriorities
    {
        // Don't rename it. Multiplayer Compatibility mod need.
        private static Dictionary<WorkGiverDef, int[]> clipboard;
        
        protected override bool AnythingInClipboard => clipboard != null;
        
        protected override void CopyFrom(Pawn pawn)
        {
            clipboard ??= new Dictionary<WorkGiverDef, int[]>();
            foreach (var workgiver in DefDatabase<WorkGiverDef>.AllDefsListForReading)
            {
                clipboard[workgiver] = pawn.GetPriorities(workgiver);
            }
        }

        protected override void PasteTo(Pawn pawn)
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