// PawnColumnWorker_CopyPasteDetailedWorkPriorities.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTab
{
    public class PawnColumnWorker_CopyPasteDetailedWorkPriorities : PawnColumnWorker_CopyPasteWorkPriorities
    {
        private Dictionary<WorkGiverDef, int[]> _clipboard;

        private Pawn _copiedPawn;

        private bool CanPaste(Pawn pawn) => _clipboard != null && pawn != _copiedPawn;

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if (pawn.Dead || pawn.workSettings == null || !pawn.workSettings.EverWork)
            {
                return;
            }
            
            Action pasteAction = null;
            if (CanPaste(pawn))
            {
                pasteAction = () => PasteTo(pawn);
            }
            CopyPasteUI.DoCopyPasteButtons(new Rect(rect.x, rect.y, 36f, 30f), () => CopyFrom(pawn), pasteAction);
        }

        protected override void CopyFrom(Pawn pawn)
        {
            _clipboard ??= new Dictionary<WorkGiverDef, int[]>();
            _copiedPawn = pawn;

            foreach (var workgiver in DefDatabase<WorkGiverDef>.AllDefsListForReading)
            {
                _clipboard[workgiver] = pawn.GetPriorities(workgiver);
            }
        }

        protected override void PasteTo(Pawn pawn)
        {
            if (pawn == _copiedPawn)
            {
                return;
            }
            
            foreach (var workTypeDef in DefDatabase<WorkTypeDef>.AllDefsListForReading
                         .Where(workTypeDef => !pawn.WorkTypeIsDisabled(workTypeDef)))
            {
                // apply all workgivers for this type
                foreach (var workgiver in workTypeDef.WorkGivers())
                {
                    for (var hour = 0; hour < GenDate.HoursPerDay; hour++)
                    {
                        // recache the type (bubbles down to workgivers). InvalidateCache will be called internally.
                        pawn.SetPriority(workgiver, _clipboard[workgiver][hour], hour);
                    }
                }
            }
        }
    }
}