// PawnColumnWorker_WorkGiver.cs
// Copyright Karel Kroeze, 2020-2020

using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.Steam;
using static WorkTab.Constants;
using static WorkTab.InteractionUtilities;
using static WorkTab.MainTabWindow_WorkTab;

namespace WorkTab {
    public class PawnColumnWorker_WorkGiver : AbstractPawnColumnWorker, IHasParentColumn {
        private PawnColumnDef_WorkGiver Def => def as PawnColumnDef_WorkGiver;

        private string Label => def.LabelCap.Resolve();

        // HIGHLIGHT: Do not modify the modifier, Grouped Pawns Lists mod requires direct access
        public WorkGiverDef WorkGiver => Def.workgiver;

        public override bool VisibleCurrently => base.VisibleCurrently && VisibleNow();

        public PawnColumnDef ParentColumn { get; set; }

        private bool VisibleNow()
        {
            return ParentColumn != null && ((PawnColumnWorker_WorkType)ParentColumn.Worker).Expanded;
        }

        protected override Color DefaultHeaderColor => new(1f, 1f, 1f, .8f);

        protected override GameFont DefaultHeaderFont => GameFont.Tiny;

        protected override TextAnchor DefaultHeaderAlignment => TextAnchor.MiddleLeft;

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table) {
            if (pawn.NothingCanBeDone()) {
                return;
            }

            if (!ShouldDrawCell(pawn)) {
                return;
            }

            // create rect in centre of cell, slightly offsetting left to give the appearance of aligning to worktype.
            var pos = rect.center - (new Vector2( WorkGiverBoxSize, WorkGiverBoxSize ) / 2f);
            var box = new Rect( pos.x, pos.y, WorkGiverBoxSize, WorkGiverBoxSize );

            // draw the workbox
            Text.Font = GameFont.Tiny;
            DrawWorkGiverBoxFor(box, pawn);
            Text.Font = GameFont.Small;

            HighlightCurrentJob(box, pawn);
            // handle interactions
            HandleInteractions(rect, pawn);
        }

        private void DrawWorkGiverBoxFor(Rect box, Pawn pawn) {
            // is the pawn currently capable of doing this job?
            var incapable = !pawn.CapableOf(WorkGiver);

            // draw background
            GUI.color = incapable ? new Color(1f, .3f, .3f) : Color.white;
            WidgetsWork.DrawWorkBoxBackground(box, pawn, WorkGiver.workType);
            GUI.color = Color.white;

            // draw extras
            var tracker = PriorityManager.Get[pawn];
            if (tracker.TimeScheduled(WorkGiver)) {
                DrawUtilities.DrawTimeScheduled(box);
            }

            // draw priorities / checks
            DrawUtilities.DrawPriority(box, pawn.GetPriority(WorkGiver, VisibleHour), true);

            if (Mouse.IsOver(box)) {
                // plop in the tooltip
                TooltipHandler.TipRegion(box,
                                         () => DrawUtilities.TipForPawnWorker(pawn, WorkGiver, incapable),
                                         pawn.thingIDNumber ^ WorkGiver.GetHashCode());
            }
        }

        public override int GetMinWidth(PawnTable table) {
            return WorkGiverWidth;
        }

        protected override bool ShouldDrawCell(Pawn pawn) {
            return base.ShouldDrawCell(pawn) && pawn.AllowedToDo(WorkGiver);
        }

        protected override bool IsDoCurrentJob(Pawn pawn) => pawn.CurJob?.workGiverDef?.defName == WorkGiver.defName;

        protected override string GetHeaderTip(PawnTable table) {
            return CreateHeaderTip("");
        }

        protected override string CreateHeaderTip(string tip)
        {
            tip += Label.Colorize(ColoredText.TipSectionTitleColor) + "\n";
            if (!WorkGiver.description.NullOrEmpty()) {
                tip += "\n" + WorkGiver.description + "\n";
            }
            if (def.sortable)
                tip += "\n" + "ClickToSortByThisColumn".Translate().Colorize(ColoredText.SubtleGrayColor);
            if (SteamDeck.IsSteamDeckInNonKeyboardMode)
            {
                return base.CreateHeaderTip(tip);
            }
            if (Find.PlaySettings.useWorkPriorities)
            {
                tip += "\n" + "WorkPriorityShiftClickTip".Translate().Colorize(ColoredText.SubtleGrayColor);
            }
            else
            {
                tip += "\n" + "WorkPriorityShiftClickEnableDisableTip".Translate().Colorize(ColoredText.SubtleGrayColor);
            }
            return base.CreateHeaderTip(tip);
        }

        protected override void HeaderClicked(Rect headerRect, PawnTable table)
        {
            base.HeaderClicked(headerRect, table);
            HeaderInteractions(headerRect, table, true);
        }

        protected override void HeaderInteractions(Rect headerRect, PawnTable table, bool clicked = false)
        {
            if (!Mouse.IsOver(headerRect) || !Shift)
            {
                return;
            }
            var pawns = table.PawnsListForReading.Where(ShouldDrawCell).ToList();
            // deal with clicks and scrolls
            if (Find.PlaySettings.useWorkPriorities)
            {
                if (clicked ? (ScrolledUp() || RightClicked()) : (ScrolledUp(headerRect, true) || RightClicked(headerRect)))
                {
                    WorkGiver.IncrementPriority(pawns, VisibleHour, SelectedHours);
                }

                if (clicked ? (ScrolledDown() || LeftClicked()) : (ScrolledDown(headerRect, true) || LeftClicked(headerRect)))
                {
                    WorkGiver.DecrementPriority(pawns, VisibleHour, SelectedHours);
                }
            }
            else
            {
                if (clicked ? (ScrolledUp() || RightClicked()) : (ScrolledUp(headerRect, true) || RightClicked(headerRect)))
                {
                    if (pawns.Any(p => p.GetPriority(WorkGiver, VisibleHour) != 0))
                    {
                        SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();

                        foreach (var pawn in pawns)
                        {
                            pawn.SetPriority(WorkGiver, 0, SelectedHours);
                        }
                    }
                }

                if (clicked ? (ScrolledDown() || LeftClicked()) : (ScrolledDown(headerRect, true) || LeftClicked(headerRect)))
                {
                    if (pawns.Any(p => p.GetPriority(WorkGiver, VisibleHour) == 0))
                    {
                        SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();

                        foreach (var pawn in pawns)
                        {
                            pawn.SetPriority(WorkGiver, Settings.defaultPriority, SelectedHours);
                        }
                    }
                }
            }
        }

        protected override void HandleInteractionsDetailed(Rect rect, Pawn pawn)
        {
            if ((Event.current.type != EventType.MouseDown && Event.current.type != EventType.ScrollWheel) || !Mouse.IsOver(rect))
            {
                return;
            }
            // track priority so we can play appropriate sounds
            var priority = pawn.GetPriority(WorkGiver, VisibleHour);

            // deal with clicks and scrolls
            if (ScrolledUp(rect, true) || RightClicked(rect))
            {
                WorkGiver.IncrementPriority(pawn, VisibleHour, SelectedHours);
            }

            if (ScrolledDown(rect, true) || LeftClicked(rect))
            {
                WorkGiver.DecrementPriority(pawn, VisibleHour, SelectedHours);
            }

            // get new priority, play crunch if it wasn't pretty
            var newPriority = pawn.GetPriority(WorkGiver, VisibleHour);
            if (priority == 0 && newPriority > 0 &&
                pawn.skills.AverageOfRelevantSkillsFor(WorkGiver.workType) <= 2f)
            {
                SoundDefOf.Crunch.PlayOneShotOnCamera();
            }

            // update tutorials
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.WorkTab, KnowledgeAmount.SpecificInteraction);
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.ManualWorkPriorities, KnowledgeAmount.SmallInteraction);
        }

        protected override void HandleInteractionsToggle(Rect rect, Pawn pawn)
        {
            if ((Event.current.type == EventType.MouseDown ||
                 Event.current.type == EventType.ScrollWheel && !Settings.disableScrollwheel) && Mouse.IsOver(rect))
            {
                // track priority so we can play appropriate sounds
                var active = pawn.GetPriority(WorkGiver, VisibleHour) > 0;

                if (active)
                {
                    pawn.SetPriority(WorkGiver, 0, SelectedHours);
                    SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
                }
                else
                {
                    pawn.SetPriority(WorkGiver, Settings.defaultPriority, SelectedHours);
                    SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();

                    if (WorkGiver.workType.relevantSkills.Any() && pawn.skills.AverageOfRelevantSkillsFor(WorkGiver.workType) <= 2.0)
                        SoundDefOf.Crunch.PlayOneShotOnCamera();
                    if (pawn.Ideo != null && pawn.Ideo.IsWorkTypeConsideredDangerous(WorkGiver.workType))
                    {
                        Messages.Message((string) "MessageIdeoOpposedWorkTypeSelected".Translate(pawn, WorkGiver.workType.gerundLabel), pawn, MessageTypeDefOf.CautionInput, false);
                        SoundDefOf.DislikedWorkTypeActivated.PlayOneShotOnCamera();
                    }
                }
            }
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.WorkTab, KnowledgeAmount.SpecificInteraction);
        }
    }
}
