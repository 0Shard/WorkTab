// PawnColumnWorker_WorkType.cs
// Copyright Karel Kroeze, 2020-2020

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using WorkTab.HarmonyPatches.ThirdPart;
using static WorkTab.Constants;
using static WorkTab.InteractionUtilities;
using static WorkTab.MainTabWindow_WorkTab;

namespace WorkTab {
    public class PawnColumnWorker_WorkType : PawnColumnWorker_WorkPriority, IAlternatingColumn, IExpandableColumn {
        private bool _expanded;
        private Vector2 cachedLabelSize = Vector2.zero;
        
        private List<Pawn> _cachedCapablePawns;
        private int _cachedCapablePawnsFrame = -1;
        private List<Pawn> CapablePawns {
            get {
                if (_cachedCapablePawnsFrame == Time.frameCount) return _cachedCapablePawns;
                _cachedCapablePawns = Instance
                                      .Table
                                      .PawnsListForReading
                                      .Where(ShouldDrawCell)
                                      .ToList();
                _cachedCapablePawnsFrame = Time.frameCount;
                return _cachedCapablePawns;
            }
        }

        public Vector2 LabelSize {
            get {
                if (cachedLabelSize == Vector2.zero) {
                    cachedLabelSize = Text.CalcSize(def.workType.labelShort.CapitalizeFirst());
                }
                return cachedLabelSize;
            }
        }

        public bool MoveDown { get; set; }
        
        public bool CanExpand => ChildColumns.Count > 1;
        
        private List<PawnColumnDef> _cachedChildColumns;
        private int _cachedChildColumnsFrame = -1;

        public List<PawnColumnDef> ChildColumns {
            get {
                if (_cachedChildColumnsFrame == Time.frameCount) return _cachedChildColumns;
                _cachedChildColumns = DefDatabase<PawnColumnDef>.AllDefsListForReading
                    .OfType<PawnColumnDef_WorkGiver>()
                    .Where(c => def.workType.workGiversByPriority.Contains(c.workgiver))
                    .OrderByDescending(c => c.workgiver.priorityInType)
                    .Cast<PawnColumnDef>()
                    .ToList();;
                _cachedChildColumnsFrame = Time.frameCount;
                return _cachedChildColumns;
            }
        }

        public bool Expanded {
            get => _expanded;
            set {
                _expanded = value;
                Instance.Notify_ColumnsChanged();
            }
        }

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table) {
            HighlightCurrentJob(rect, pawn);
            
            base.DoCell(rect, pawn, table);

            if (!ShouldDrawCell(pawn)) {
                return;
            }
            HandleInteractions(rect, pawn);
            
            // create rect in centre of cell
            var pos = rect.center - (new Vector2( WorkTypeBoxSize, WorkTypeBoxSize ) / 2f);
            var box = new Rect( pos.x, pos.y, WorkTypeBoxSize, WorkTypeBoxSize );
            DrawWorkTypeBoxFor(box, pawn, def.workType);
        }

        public override void DoHeader(Rect rect, PawnTable table)
        {
            base.DoHeader(rect, table);
            var interactableHeaderRect = GetInteractableHeaderRect(rect, table);
            HeaderInteractions(interactableHeaderRect, table);
        }

        protected override void HeaderClicked(Rect headerRect, PawnTable table)
        {
            HeaderExpand();
            base.HeaderClicked(headerRect, table);
        }

        // WorkManager mod needed. Don't remove it.
        public override int GetMinHeaderHeight(PawnTable table) {
            return base.GetMinHeaderHeight(table);
        }

        public override int GetMinWidth(PawnTable table) {
            return WorkTypeWidth;
        }

        private void HeaderExpand()
        {
            if (!Ctrl || !CanExpand || Instance == null)
            {
                return;
            }
            Expanded = !Expanded;
        }
        
        // Numbers mod need the table param. Don't remove it.
        private void HeaderInteractions(Rect headerRect, PawnTable table) {
            if (!Mouse.IsOver(headerRect) || !Shift || Instance == null) {
                return;
            }
            
            if (Find.PlaySettings.useWorkPriorities) {
                if (ScrolledUp(headerRect, true)) {
                    def.workType.IncrementPriority(CapablePawns, VisibleHour, SelectedHours);
                }
                if (ScrolledDown(headerRect, true)) {
                    def.workType.DecrementPriority(CapablePawns, VisibleHour, SelectedHours);
                }
            } else {
                var pawns = CapablePawns;
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

        private bool ShouldDrawCell(Pawn pawn) {
            return pawn.AllowedToDo(def.workType) && !pawn.NothingCanBeDone();
        }
        
        private void HighlightCurrentJob(Rect rect, Pawn pawn)
        {
            if (!Settings.highlightActiveWorkCells) return;
            var doingNow = pawn.CurJob?.workGiverDef?.workType?.defName == def.workType.defName;
            if (!doingNow) return;
            GUI.color = Color.magenta;
            Widgets.DrawBox(rect);
            GUI.color = Color.white;
        }
        
        // WorkManager mod need the box param. Don't rename it.
        protected virtual void DrawWorkTypeBoxFor(Rect box, Pawn pawn, WorkTypeDef worktype) {
            // 缓存 PriorityTracker 避免多次查找
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

        private string CreateHeaderTip(string tip) {
            if (_expanded)
            {
                tip += "\n" + "WorkTab.CollapseWorkgiversColumnTip".Translate().Colorize(ColoredText.SubtleGrayColor);
            } 
            if (CanExpand && !_expanded) 
            {
                tip += "\n" + "WorkTab.ExpandWorkgiversColumnTip".Translate().Colorize(ColoredText.SubtleGrayColor);
            }
            if (Find.PlaySettings.useWorkPriorities && !Settings.disableScrollwheel) {
                tip += "\n" + "WorkTab.DetailedColumnTip".Translate().Colorize(ColoredText.SubtleGrayColor);
            } else if (!Settings.disableScrollwheel) {
                tip += "\n" + "WorkTab.ToggleColumnTip".Translate().Colorize(ColoredText.SubtleGrayColor);
            }
            return tip;
        }

        protected virtual void HandleInteractions(Rect rect, Pawn pawn) {
            if (Find.PlaySettings.useWorkPriorities) {
                HandleInteractionsDetailed(rect, pawn);
            } 
            else 
            {
                HandleInteractionsToggle(rect, pawn);
            }
        }
        
        private void HandleInteractionsDetailed(Rect rect, Pawn pawn)
        {
            if (Event.current.type != EventType.ScrollWheel || Settings.disableScrollwheel || !Mouse.IsOver(rect))
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

        private void HandleInteractionsToggle(Rect rect, Pawn pawn)
        {
            if (Event.current.type != EventType.ScrollWheel || Settings.disableScrollwheel || !Mouse.IsOver(rect))
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
