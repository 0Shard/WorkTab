// Copyright Karel Kroeze, 2020-2021.
// WorkTab/WorkTab/PawnColumnWorker_WorkTabLabel.cs

using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using static WorkTab.InteractionUtilities;

namespace WorkTab {

    [Obsolete("PawnColumnWorker_WorkTabLabel is abandoned. See PawnColumnWorker_Label_DoCell.cs for a replacement.")]
    public class PawnColumnWorker_WorkTabLabel {
        public int Compare(Pawn a, Pawn b) {
            return string.Compare(a.Name.ToStringShort, b.Name.ToStringShort,
                                  StringComparison.CurrentCultureIgnoreCase);
        }

        public void DoCell(Rect rect, Pawn pawn, PawnTable table) {
            // intercept interactions before base has a chance to act on them
            if (Shift && Mouse.IsOver(rect)) {
                if (RightClicked(rect) || ScrolledUp(rect, true)) {
                    Increment(pawn);
                    return;
                }

                if (LeftClicked(rect) || ScrolledDown(rect, true)) {
                    Decrement(pawn);
                    return;
                }
            }

            // override tooltip
            TooltipHandler.ClearTooltipsFrom(rect);
            TooltipHandler.TipRegion(rect, GetTooltip(pawn));
        }

        public int GetMinWidth(PawnTable table) => 145;

        private static string GetTooltip(Pawn pawn) {
            return "WorkTab.LabelCellTip".Translate() + "\n\n" + pawn.GetTooltip().text;
        }

        public void Increment(Pawn pawn) {
            var actionTaken = false;
            // just go over all workgivers and increase their priority number by one, with a maximum of maxPriority
            foreach (var workgiver in DefDatabase<WorkGiverDef>.AllDefsListForReading.Where(pawn.AllowedToDo)) {
                // get current priority
                var priority = pawn.GetPriority(workgiver, MainTabWindow_WorkTab.VisibleHour);
                var curr = priority;
                // detailed mode
                if (PriorityManager.ShowPriorities) {
                    if (priority == Settings.maxPriority) {
                        priority = 0;
                        actionTaken = true;
                    } else if (priority != 0) {
                        priority++;
                        actionTaken = true;
                    }
                } else // simple mode
                {
                    if (priority != 0) {
                        actionTaken = true;
                    }

                    priority = 0;
                }

                if (curr != priority)
                {
                    // apply new priority
                    pawn.SetPriority(workgiver, priority, MainTabWindow_WorkTab.SelectedHours);
                }
            }

            if (actionTaken) {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
        }

        public void Decrement(Pawn pawn) {
            var actionTaken = false;
            // just go over all workgivers and lower their priority number by one, with a minimum of 1
            foreach (var workgiver in DefDatabase<WorkGiverDef>.AllDefsListForReading.Where(pawn.AllowedToDo)) {
                // get current priority
                var priority = pawn.GetPriority(workgiver, MainTabWindow_WorkTab.VisibleHour);
                var curr = priority;
                // detailed mode
                if (PriorityManager.ShowPriorities) {
                    if (priority == 0) {
                        priority = Settings.maxPriority;
                        actionTaken = true;
                    } else if (priority != 1) {
                        priority--;
                        actionTaken = true;
                    }
                } else // simple mode
                {
                    if (priority == 0) {
                        actionTaken = true;
                    }

                    priority = Settings.defaultPriority;
                }

                if (curr != priority)
                {
                    // apply new priority
                    pawn.SetPriority(workgiver, priority, MainTabWindow_WorkTab.SelectedHours);
                }
            }

            if (actionTaken) {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
        }
    }
}
