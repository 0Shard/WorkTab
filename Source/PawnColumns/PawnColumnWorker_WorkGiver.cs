// PawnColumnWorker_WorkGiver.cs
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
using static WorkTab.Resources;

namespace WorkTab {
    public class PawnColumnWorker_WorkGiver: PawnColumnWorker, IAlternatingColumn {
        
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

        public PawnColumnDef_WorkGiver Def => def as PawnColumnDef_WorkGiver;

        public string Label => WorkGiver.label.NullOrEmpty() ? WorkGiver.gerund?.CapitalizeFirst() : WorkGiver.LabelCap.Resolve()?.CapitalizeFirst();

        public WorkGiverDef WorkGiver => Def?.workgiver;

        public Vector2 LabelSize {
            get {
                if (cachedLabelSize == Vector2.zero) {
                    cachedLabelSize = Text.CalcSize(Label);
                    cachedLabelSize.x = Mathf.Min(cachedLabelSize.x, (WorkGiverWidth * 2) - Margin);
                }

                return cachedLabelSize;
            }
        }

        public bool MoveDown { get; set; }

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table) {
            if (pawn.NothingCanBeDone()) {
                return;
            }
            
            if (!ShouldDrawCell(pawn)) {
                return;
            }
            
            // draw the workbox
            Text.Font = GameFont.Tiny;
            DrawWorkGiverBoxFor(rect, pawn);
            
            // handle interactions
            HandleInteractions(rect, pawn);
        }

        public override void DoHeader(Rect rect, PawnTable table) {
            // make sure we're at the correct font size
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = new Color(1f, 1f, 1f, .8f);
            var label = Label.Truncate(LabelSize.x, TruncationCache);
            if (CompactWorkTabPatches.CanDrawLabel())
            {
                var originalMatrix = GUI.matrix;
                var (transformedRect, transformationMatrix) = CompactWorkTabPatches.DrawLabel(rect, label);
                GUI.matrix = transformationMatrix;
                var mouseIsOver = transformedRect.Contains(Event.current.mousePosition);
                GUI.matrix = originalMatrix;
                if (!mouseIsOver)
                {
                    return;
                }

                var rect1 = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);
                TooltipHandler.TipRegion(rect1, GetHeaderTip(table));
                // handle interactions (click and scroll)
                HeaderInteractions(rect1, table);
            }
            else
            {
                // get offset rect
                var labelRect = GetLabelRect(rect);
                // draw label, slightly greyed out to contrast with work types
                Widgets.Label(labelRect, label);

                // handle interactions (click and scroll)
                HeaderInteractions(labelRect, table);
                // mouseover stuff
                if (Mouse.IsOver(labelRect))
                {
                    Widgets.DrawHighlight(labelRect);
                    TooltipHandler.TipRegion(labelRect, GetHeaderTip(table));
                }
                
                // get bottom center of label, offset left to align with boxes
                var start  = new Vector2( labelRect.center.x - 2f, labelRect.yMax );
                var length = rect.yMax - start.y;

                // make sure we're not at a whole pixel
                if (start.x - (int) start.x < 1e-4) {
                    start.x += .5f;
                }

                // draw the lines - two separate lines give a clearer edge than one 2px line...
                GUI.color = new Color(1f, 1f, 1f, 0.3f);
                Widgets.DrawLineVertical(Mathf.FloorToInt(start.x), start.y, length);
                Widgets.DrawLineVertical(Mathf.CeilToInt(start.x), start.y, length);
            }
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            
            if (table.SortingBy != def)
            {
                return;
            }
            // sort icon
            var sortIcon = !table.SortingDescending ? SortingIcon : SortingDescendingIcon;
            var bottomRight = new Rect(rect.xMax - sortIcon.width - 1f,
                rect.yMax - sortIcon.height - 1f, 
                sortIcon.width,
                sortIcon.height);
            GUI.DrawTexture(bottomRight, sortIcon);
        }
        
        public override int GetMinHeaderHeight(PawnTable table) {
            return HorizontalHeaderHeight;
        }

        public override int GetMinWidth(PawnTable table) {
            return WorkGiverWidth;
        }

        public void HeaderInteractions(Rect headerRect, PawnTable table) {
            if (!Mouse.IsOver(headerRect) || Instance == null) {
                return;
            }

            // handle interactions
            if (Shift) {
                // deal with clicks and scrolls
                if (Find.PlaySettings.useWorkPriorities) {
                    if (ScrolledUp(headerRect, true) || RightClicked(headerRect)) {
                        WorkGiver.IncrementPriority(CapablePawns, VisibleHour, SelectedHours);
                    }

                    if (ScrolledDown(headerRect, true) || LeftClicked(headerRect)) {
                        WorkGiver.DecrementPriority(CapablePawns, VisibleHour, SelectedHours);
                    }
                } else {
                    // this gets slightly more complicated
                    List<Pawn> pawns = CapablePawns;
                    if (ScrolledUp(headerRect, true) || RightClicked(headerRect)) {
                        if (pawns.Any(p => p.GetPriority(WorkGiver, VisibleHour) != 0)) {
                            SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();

                            foreach (Pawn pawn in pawns) {
                                pawn.SetPriority(WorkGiver, 0, SelectedHours);
                            }
                        }
                    }

                    if (ScrolledDown(headerRect, true) || LeftClicked(headerRect)) {
                        if (pawns.Any(p => p.GetPriority(WorkGiver, VisibleHour) == 0)) {
                            SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();

                            foreach (Pawn pawn in pawns) {
                                pawn.SetPriority(WorkGiver, Settings.defaultPriority, SelectedHours);
                            }
                        }
                    }
                }
            } else if (def.sortable) {
                if (LeftClicked(headerRect)) {
                    Sort(table);
                }

                if (RightClicked(headerRect)) {
                    Sort(table, false);
                }
            }
        }

        private bool ShouldDrawCell(Pawn pawn) {
            return pawn.AllowedToDo(WorkGiver) && !pawn.NothingCanBeDone();
        }

        protected virtual void DrawWorkGiverBoxFor(Rect rect, Pawn pawn) {
            // is the pawn currently capable of doing this job?
            var incapable = !pawn.CapableOf(WorkGiver);
            
            // create rect in centre of cell, slightly offsetting left to give the appearance of aligning to worktype.
            var pos = rect.center - (new Vector2( WorkGiverBoxSize, WorkGiverBoxSize ) / 2f);
            var box = new Rect( pos.x - 2f, pos.y, WorkGiverBoxSize, WorkGiverBoxSize );
            
            // draw background
            GUI.color = incapable ? new Color(1f, .3f, .3f) : Color.white;
            DrawUtilities.DrawWorkBoxBackground(box, pawn, WorkGiver.workType);
            GUI.color = Color.white;
            
            HighlightActiveWorkGiver(pawn, box);
            
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
                    pawn.thingIDNumber ^ WorkGiver.workType.GetHashCode());
            }
        }

        private void HighlightActiveWorkGiver(Pawn pawn, Rect box)
        {
            if (!Settings.highlightActiveWorkCells) return;
            var doingNow = pawn.CurJob?.workGiverDef?.defName == WorkGiver.defName;
            if (!doingNow) return;
            GUI.color = Color.magenta;
            Widgets.DrawBox(box);
            GUI.color = Color.white;
        }

        protected override string GetHeaderTip(PawnTable table) {
            return CreateHeaderTip();
        }
        
        public override int Compare(Pawn a, Pawn b) {
            return GetValueToCompare(a).CompareTo(GetValueToCompare(b));
        }
        
        protected virtual void Sort(PawnTable table, bool ascending = true) {
            if (ascending) {
                if (table.SortingBy != def) {
                    table.SortBy(def, true);
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                } else if (table.SortingDescending) {
                    table.SortBy(def, false);
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                } else {
                    table.SortBy(null, false);
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
            } else {
                if (table.SortingBy != def) {
                    table.SortBy(def, false);
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                } else if (table.SortingDescending) {
                    table.SortBy(null, false);
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                } else {
                    table.SortBy(def, true);
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                }
            }
        }
        
        private string CreateHeaderTip()
        {
            var tip = "";
            tip += Label.Colorize(ColoredText.TipSectionTitleColor) + "\n";
            if (!WorkGiver.description.NullOrEmpty()) {
                tip += "\n" + WorkGiver.description + "\n";
            }

            tip += "\n" + "ClickToSortByThisColumn".Translate().Colorize(ColoredText.SubtleGrayColor);
            if (Find.PlaySettings.useWorkPriorities && !Settings.disableScrollwheel) {
                tip += "\n" + "WorkTab.DetailedColumnTip".Translate().Colorize(ColoredText.SubtleGrayColor);
            } else if (!Settings.disableScrollwheel) {
                tip += "\n" + "WorkTab.ToggleColumnTip".Translate().Colorize(ColoredText.SubtleGrayColor);
            }

            return tip;
        }

        private Rect GetLabelRect(Rect headerRect) {
            float x = headerRect.center.x;
            // move down slightly since we're using a smaller font here.
            Rect result = new Rect( x - (LabelSize.x / 2f) - 2f, headerRect.y + 3f, LabelSize.x, LabelSize.y );
            if (MoveDown) {
                result.y += 20f;
            }

            return result;
        }

        private float GetValueToCompare(Pawn pawn) {
            if (pawn.NothingCanBeDone()) {
                return -2f;
            }

            if (pawn.NotAllowedToDo(WorkGiver.workType)) {
                return -1f;
            }

            return pawn.skills.AverageOfRelevantSkillsFor(WorkGiver.workType);
        }

        protected virtual void HandleInteractions(Rect rect, Pawn pawn) {
            if (Find.PlaySettings.useWorkPriorities) {
                HandleInteractionsDetailed(rect, pawn);
            } else {
                HandleInteractionsToggle(rect, pawn);
            }
        }
        
        private void HandleInteractionsDetailed(Rect rect, Pawn pawn) {
            if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.ScrollWheel)
              && Mouse.IsOver(rect)) {
                // track priority so we can play appropriate sounds
                int oldpriority = pawn.GetPriority( WorkGiver, VisibleHour );

                // deal with clicks and scrolls
                if (ScrolledUp(rect, true) || RightClicked(rect)) {
                    WorkGiver.IncrementPriority(pawn, VisibleHour, SelectedHours);
                }

                if (ScrolledDown(rect, true) || LeftClicked(rect)) {
                    WorkGiver.DecrementPriority(pawn, VisibleHour, SelectedHours);
                }

                // get new priority, play crunch if it wasn't pretty
                var newPriority = pawn.GetPriority( WorkGiver, VisibleHour );
                if (oldpriority == 0 && newPriority > 0 && pawn.skills.AverageOfRelevantSkillsFor(WorkGiver.workType) <= 2f) {
                    SoundDefOf.Crunch.PlayOneShotOnCamera();
                }

                // update tutorials
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.WorkTab,
                                                               KnowledgeAmount.SpecificInteraction);
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.ManualWorkPriorities,
                                                               KnowledgeAmount.SmallInteraction);
            }
        }

        private void HandleInteractionsToggle(Rect rect, Pawn pawn) {
            if ((Event.current.type == EventType.MouseDown ||
                   Event.current.type == EventType.ScrollWheel && !Settings.disableScrollwheel)
              && Mouse.IsOver(rect)) {
                // track priority so we can play appropriate sounds
                bool active = pawn.GetPriority( WorkGiver, VisibleHour ) > 0;

                if (active) {
                    pawn.SetPriority(WorkGiver, 0, SelectedHours);
                    SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
                } else {
                    pawn.SetPriority(WorkGiver, Settings.defaultPriority, SelectedHours);
                    SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();

                    if (pawn.skills.AverageOfRelevantSkillsFor(WorkGiver.workType) <= 2f) {
                        SoundDefOf.Crunch.PlayOneShotOnCamera();
                    }
                }

                // stop event propagation, update tutorials
                Event.current.Use();
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.WorkTab, KnowledgeAmount.SpecificInteraction);
            }
        }
    }
}
