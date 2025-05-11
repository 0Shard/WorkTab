// PawnColumnWorker_WorkType.cs
// Copyright Karel Kroeze, 2020-2020

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using static WorkTab.Constants;
using static WorkTab.InteractionUtilities;
using static WorkTab.MainTabWindow_WorkTab;

namespace WorkTab {
    public class PawnColumnWorker_WorkType : AbstractPawnColumnWorker, IExpandableColumn
    {
        private bool _expanded;

        public bool CanExpand => ChildColumns?.Count > 1;

        public bool NeedExpand { get; private set; }

        public bool NeedCollapse { get; private set; }

        public bool Expanded {
            get => _expanded;
            set {
                _expanded = value;
                NeedExpand = false;
                NeedCollapse = false;
            }
        }

        public List<PawnColumnDef> ChildColumns { get; set; }

        protected override Color DefaultHeaderColor => Color.white;

        protected override GameFont DefaultHeaderFont => GameFont.Small;

        protected override TextAnchor DefaultHeaderAlignment => TextAnchor.MiddleCenter;

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table) {

            base.DoCell(rect, pawn, table);

            if (!ShouldDrawCell(pawn)) {
                return;
            }
            var x = rect.x + (float) ((rect.width - 25.0) / 2.0);
            var y = rect.y + 2.5f;
            var highlight = new Rect(x, y, WorkTypeBoxSize, WorkTypeBoxSize);
            HighlightCurrentJob(highlight, pawn);

            HandleInteractions(rect, pawn);

            // create rect in centre of cell
            var pos = rect.center - (new Vector2( WorkTypeBoxSize, WorkTypeBoxSize ) / 2f);
            var box = new Rect( pos.x, pos.y, WorkTypeBoxSize, WorkTypeBoxSize );
            DrawWorkTypeBoxFor(box, pawn, def.workType);
        }

        public bool HeaderExpand()
        {
            if (!Ctrl || !CanExpand)
            {
                return false;
            }
            // Note: The column cannot be updated immediately within OnGUI and expect the view to adapt immediately.
            // Unity's UI lifecycle does not allow you to change the layout structure during drawing.
            // So don't do this: Expanded = !Expanded;
            // Instead, we need to wait until the next frame.
            if (Expanded)
            {
                NeedCollapse = true;
            }
            else
            {
                NeedExpand = true;
            }
            return true;
        }

        protected override bool ShouldDrawCell(Pawn pawn) {
            return base.ShouldDrawCell(pawn) && pawn.AllowedToDo(def.workType);
        }

        // HIGHLIGHT: WorkManager mod need the box param. Don't rename it.
        protected virtual void DrawWorkTypeBoxFor(Rect box, Pawn pawn, WorkTypeDef worktype) {
            var tracker = PriorityManager.Get[pawn];
            if (tracker.TimeScheduled(worktype)) {
                DrawUtilities.DrawTimeScheduled(box);
            }
            if (tracker.PartScheduled(worktype)) {
                DrawUtilities.DrawPartScheduled(box);
            }
        }

        protected override string GetHeaderTip(PawnTable table) {
            return CreateHeaderTip(base.GetHeaderTip(table));
        }

        // HIGHLIGHT: WorkManager mod needed. Don't remove it.
        public override int GetMinHeaderHeight(PawnTable table) {
            return base.GetMinHeaderHeight(table);
        }

        // HIGHLIGHT: WorkManager mod needed. Don't remove it.
        public override void DoHeader(Rect rect, PawnTable table)
        {
            base.DoHeader(rect, table);
        }

        protected override string CreateHeaderTip(string tip) {
            if (_expanded)
            {
                tip += "\n" + "WorkTab.CollapseWorkgiversColumnTip".Translate().Colorize(ColoredText.SubtleGrayColor);
            }
            if (CanExpand && !_expanded)
            {
                tip += "\n" + "WorkTab.ExpandWorkgiversColumnTip".Translate().Colorize(ColoredText.SubtleGrayColor);
            }
            return base.CreateHeaderTip(tip);
        }

        // HIGHLIGHT: Numbers mod need the table param. Don't remove it.
        protected override void HeaderInteractions(Rect headerRect, PawnTable table, bool clicked = false) {
            if (!Mouse.IsOver(headerRect) || !Shift) {
                return;
            }

            var pawns = table.PawnsListForReading.Where(ShouldDrawCell).ToList();
            if (Find.PlaySettings.useWorkPriorities) {
                if (ScrolledUp(headerRect, true)) {
                    def.workType.IncrementPriority(pawns, VisibleHour, SelectedHours);
                }
                if (ScrolledDown(headerRect, true)) {
                    def.workType.DecrementPriority(pawns, VisibleHour, SelectedHours);
                }
            } else {
                if (ScrolledUp(headerRect, true) &&
                    pawns.Any(p => p.GetPriority(def.workType, VisibleHour) != 0)) {

                    SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
                    foreach (var pawn in pawns) {
                        pawn.SetPriority(def.workType, 0, SelectedHours);
                    }
                }
                if (ScrolledDown(headerRect, true) &&
                    pawns.Any(p => p.GetPriority(def.workType, VisibleHour) == 0)) {

                    SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
                    foreach (var pawn in pawns) {
                        pawn.SetPriority(def.workType, Settings.defaultPriority, SelectedHours);
                    }
                }
            }
        }

        protected override void HandleInteractionsDetailed(Rect rect, Pawn pawn)
        {
            if (!Mouse.IsOver(rect) || Event.current.type != EventType.ScrollWheel || Settings.disableScrollwheel)
            {
                return;
            }

            var num = pawn.workSettings.WorkIsActive(def.workType) ? 1 : 0;

            // deal with clicks and scrolls
            if (ScrolledUp(rect, true)) {
                def.workType.IncrementPriority(pawn, VisibleHour, SelectedHours);
            }

            if (ScrolledDown(rect, true)) {
                def.workType.DecrementPriority(pawn, VisibleHour, SelectedHours);
            }

            if (num == 0 && pawn.workSettings.WorkIsActive(def.workType) &&
                def.workType.relevantSkills.Any() && pawn.skills.AverageOfRelevantSkillsFor(def.workType) <= 2.0)
                SoundDefOf.Crunch.PlayOneShotOnCamera();
            if (num == 0 && pawn.workSettings.WorkIsActive(def.workType) &&
                pawn.Ideo != null && pawn.Ideo.IsWorkTypeConsideredDangerous(def.workType))
            {
                Messages.Message((string) "MessageIdeoOpposedWorkTypeSelected".Translate(pawn, def.workType.gerundLabel),
                    pawn, MessageTypeDefOf.CautionInput, false);
                SoundDefOf.DislikedWorkTypeActivated.PlayOneShotOnCamera();
            }
            Event.current.Use();
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.WorkTab, KnowledgeAmount.SpecificInteraction);
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.ManualWorkPriorities, KnowledgeAmount.SmallInteraction);
        }

        protected override void HandleInteractionsToggle(Rect rect, Pawn pawn)
        {
            if (!Mouse.IsOver(rect) || Event.current.type != EventType.ScrollWheel || Settings.disableScrollwheel)
            {
                return;
            }
            // track priority so we can play appropriate sounds
            var active = pawn.GetPriority( def.workType, VisibleHour ) > 0;

            if (active) {
                pawn.SetPriority(def.workType, 0, SelectedHours);
                SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
            } else {
                pawn.SetPriority(def.workType, Settings.defaultPriority, SelectedHours);
                SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();

                if (def.workType.relevantSkills.Any() && pawn.skills.AverageOfRelevantSkillsFor(def.workType) <= 2.0)
                    SoundDefOf.Crunch.PlayOneShotOnCamera();
                if (pawn.Ideo != null && pawn.Ideo.IsWorkTypeConsideredDangerous(def.workType))
                {
                    Messages.Message("MessageIdeoOpposedWorkTypeSelected".Translate(pawn,
                        def.workType.gerundLabel), pawn, MessageTypeDefOf.CautionInput, false);
                    SoundDefOf.DislikedWorkTypeActivated.PlayOneShotOnCamera();
                }
            }

            // stop event propagation, update tutorials
            Event.current.Use();
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.WorkTab, KnowledgeAmount.SpecificInteraction);
        }
    }
}
