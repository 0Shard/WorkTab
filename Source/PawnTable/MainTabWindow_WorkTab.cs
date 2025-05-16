// MainTabWindow_WorkTab.cs
// Copyright Karel Kroeze, 2020-2020

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static WorkTab.Constants;
using static WorkTab.InteractionUtilities;

namespace WorkTab {
    public class MainTabWindow_WorkTab : MainTabWindow_PawnTable {
        private static          List<int> _selectedHours = TimeUtilities.WholeDay;
        private static          int       _visibleHour = -1;

        private bool _columnsChanged;

        private Rect _timeBarRect;

        public MainTabWindow_WorkTab() {
            Instance = this;
        }

        public static MainTabWindow_WorkTab Instance { get; private set; }

        public static List<int> SelectedHours => PriorityManager.ShowScheduler ? _selectedHours : TimeUtilities.WholeDay;

        public static int VisibleHour => PriorityManager.ShowScheduler ? _visibleHour : -1;

        private static List<PawnColumnDef> Columns => Instance.Table.columns;

        // HIGHLIGHT: 'Grouped Pawns Lists' mod need.
        public PawnTable Table => table;

        public Rect TimeBarRect {
            get {
                if (_timeBarRect == default) {
                    RecacheTimeBarRect();
                }

                return _timeBarRect;
            }
        }

        protected override float ExtraTopSpace => base.ExtraTopSpace + Constants.ExtraTopSpace;
        public override PawnTableDef PawnTableDef => PawnTableDefOf.Work;

        protected override IEnumerable<Pawn> Pawns => base.Pawns.Where(pawn => !pawn.DevelopmentalStage.Baby());

        // HIGHLIGHT: 'Grouped Pawns Lists' mod need.
        private bool AnyExpanded => AnyExpandedNew(Table);

        public static void AddSelectedHour(int hour, bool replace) {
            if (replace) {
                _selectedHours.Clear();
            }

            if (replace || !_selectedHours.Contains(hour)) {
                _selectedHours.Add(hour);
            }

            _visibleHour = hour;
        }

        public static void RemoveSelectedHour(int hour) {
            if (_selectedHours.Contains(hour)) {
                _selectedHours.Remove(hour);
            }

            if (_visibleHour == hour) {
                _visibleHour = -1;
            }
        }

        public static void SelectWholeDay() {
            _selectedHours = TimeUtilities.WholeDay;
            _visibleHour = -1;
        }

        public override void DoWindowContents(Rect rect) {
            if (_columnsChanged) {
                RebuildTable(false);
            }

            base.DoWindowContents(rect);
            if (Event.current.type == EventType.Layout) {
                return;
            }

            var canvas = new Rect(rect.x, rect.y, Mathf.Min(rect.width, UI.screenWidth), rect.height);
            DoToggleButtons(canvas);
            DoPriorityLabels(canvas);
            if (PriorityManager.ShowScheduler) {
                DoTimeBar(canvas);
            }
        }

        public void Notify_ColumnsChanged() {
            _columnsChanged = true;
        }

        public override void PostOpen() {
            base.PostOpen();
            RebuildTable();
        }

        protected override void SetInitialSizeAndPosition()
        {
            base.SetInitialSizeAndPosition();
            RecacheTimeBarRect();
        }

        // HIGHLIGHT: Do not modify the modifier, 'Grouped Pawns Lists' mod requires direct access
        public void RebuildTable(bool alreadyDirty = true) {
            if (!alreadyDirty)
            {
                // force recache of table sizes: set the table to be dirty, then get the size - which calls the private recache routine.
                SetDirty();
            }
            // force recache of timeBarRect
            _timeBarRect = default;

            // clear dirty flag
            _columnsChanged = false;
        }

        public void RecacheTimeBarRect() {
            if (table == null)
            {
                return;
            }
            var widths  = table.cachedColumnWidths;
            var   columns = Columns;
            float start   = 0;
            float width   = 0;

            // loop over columns, initially add any column that is not a workbox to the start, but not after we've seen a workbox.
            // Add widths for workboxes to width.
            // NOTE: This assumes a single contiguous block of workboxes!
            for (var i = 0; i < columns.Count; i++) {
                if (typeof(PawnColumnWorker_WorkPriority).IsAssignableFrom(columns[i].workerClass)) {
                    width += widths[i];
                } else if (width < 1) {
                    start += widths[i];
                }
            }

            // build the rect
            _timeBarRect = new Rect(
                start,
                // note that we're not subtracting the time bar height itself, as at this point the window's height has not yet been updated to include it.
                windowRect.height - base.ExtraBottomSpace,
                width,
                TimeBarHeight);

            // limit maximum size
            _timeBarRect.xMax = Mathf.Min(_timeBarRect.xMax, UI.screenWidth - StandardMargin);
        }

        private void DoPriorityLabels(Rect canvas) {
            Rect priorityLabelRect = new Rect( (canvas.width / 3f) - (PriorityLabelSize.x / 2f),
                                              canvas.yMin,
                                              PriorityLabelSize.x,
                                              PriorityLabelSize.y );

            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            Text.Anchor = TextAnchor.UpperCenter;
            Text.Font = GameFont.Tiny;

            Widgets.Label(priorityLabelRect, "<= " + "HigherPriority".Translate());

            priorityLabelRect.x += canvas.width / 3f;
            Widgets.Label(priorityLabelRect, "LowerPriority".Translate() + " =>");

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DoTimeBar(Rect rect) {
            // set up rects
            Rect bar = TimeBarRect;
            Rect buttons = new Rect( rect.xMin, bar.yMin + (bar.height / 3f), bar.xMin - rect.xMin,
                                    bar.height                       * 2 / 3f );
            Rect button = new Rect( buttons.xMax - (buttons.height * 2f), buttons.yMin, buttons.height,
                                   buttons.height );

            // split the available area into rects. bottom 2/3's are used for 'buttons', with text for times.
            float hourWidth         = bar.width      / GenDate.HoursPerDay;
            float barheight         = bar.height * 2 / 3f;
            float timeIndicatorSize = bar.height * 2 / 3f;
            float lastLabelPosition = 0f;
            Rect hourRect          = new Rect( bar.xMin, bar.yMax - barheight, hourWidth, barheight );

            // draw buttons
            TooltipHandler.TipRegion(button, "WorkTab.SelectWholeDayTip".Translate());
            if (Widgets.ButtonImage(button, Resources.PrioritiesWholeDay, Color.white, GenUI.MouseoverColor)) {
                SelectWholeDay();
            }

            button.x -= button.height + Margin;
            TooltipHandler.TipRegion(button, "WorkTab.SelectCurrentHourTip".Translate());
            if (Widgets.ButtonImage(button, Resources.Now, Color.white, GenUI.MouseoverColor)) {
                AddSelectedHour(GenLocalDate.HourOfDay(Find.CurrentMap), true);
            }

            // draw first tick
            GUI.color = Color.grey;
            Widgets.DrawLineVertical(hourRect.xMin, hourRect.yMin + (hourRect.height * 1 / 2f),
                                      hourRect.height * 1 / 2f);

            // draw horizontal line ( y - 1 because canvas gets clipped on bottom )
            Widgets.DrawLineHorizontal(bar.xMin, bar.yMax - 1, bar.width);
            GUI.color = Color.white;

            // label and rect
            string label;
            Rect   labelRect;

            for (int hour = 0; hour < GenDate.HoursPerDay; hour++) {
                bool selected = SelectedHours.Contains( hour );
                bool focused  = hour == VisibleHour;

                // print major tick
                GUI.color = Color.grey;
                Widgets.DrawLineVertical(hourRect.xMax, hourRect.yMin + (hourRect.height * 1 / 2f),
                                          hourRect.height * 1 / 2f);

                // print minor ticks
                Widgets.DrawLineVertical(hourRect.xMin + (hourRect.width * 1 / 4f),
                                          hourRect.yMin + (hourRect.height * 3 / 4f), hourRect.height * 1 / 4f);
                Widgets.DrawLineVertical(hourRect.xMin + (hourRect.width * 2 / 4f),
                                          hourRect.yMin + (hourRect.height * 3 / 4f), hourRect.height * 1 / 4f);
                Widgets.DrawLineVertical(hourRect.xMin + (hourRect.width * 3 / 4f),
                                          hourRect.yMin + (hourRect.height * 3 / 4f), hourRect.height * 1 / 4f);
                GUI.color = Color.white;

                // create and draw labelrect - but only if the last label isn't too close
                if (hourRect.xMin - lastLabelPosition > MinTimeBarLabelSpacing) {
                    label = hour.FormatHour();
                    labelRect = new Rect(0f, bar.yMin + (bar.height * 1 / 3f), label.NoWrapWidth(),
                                          bar.height * 2 / 3f);
                    labelRect.x = hourRect.xMin - (labelRect.width / 2f);
                    LabelUtilities.Label(labelRect, label, Color.grey, GameFont.Tiny, TextAnchor.UpperCenter);

                    lastLabelPosition = labelRect.xMax;
                }

                // draw hour rect with mouseover + interactions
                Widgets.DrawHighlightIfMouseover(hourRect);

                // set/remove focus (LMB and any other MB respectively)
                if (Mouse.IsOver(hourRect)) {
                    if (Input.GetMouseButton(0)) {
                        AddSelectedHour(hour, Shift);
                    }

                    if (Input.GetMouseButton(1)) {
                        RemoveSelectedHour(hour);
                    }

                    // handle tooltip
                    var selectedString = selected ? "WorkTab.Selected".Translate() : "WorkTab.NotSelected".Translate();
                    var interactionString = "";
                    if (selected) {
                        interactionString += "WorkTab.RightClickToDeselect".Translate().Colorize(ColoredText.SubtleGrayColor);
                        if (focused) {
                            interactionString += "\n" + "WorkTab.ClickToFocus".Translate().Colorize(ColoredText.SubtleGrayColor);
                        }
                    } else {
                        interactionString += "WorkTab.ClickToSelect".Translate().Colorize(ColoredText.SubtleGrayColor);
                    }

                    TooltipHandler.TipRegion(hourRect,
                                              "WorkTab.SchedulerHourTip".Translate(hour.FormatHour(),
                                                                                    (hour + (1 % GenDate.HoursPerDay)).FormatHour(),
                                                                                    selectedString,
                                                                                    interactionString));
                }

                // if this is currently the 'main' timeslot, and not the actual time, draw an eye
                if (focused && hour != GenLocalDate.HourOfDay(Find.CurrentMap)) {
                    Rect eyeRect = new Rect( hourRect.center.x                 - (timeIndicatorSize * 1 / 2f),
                                            hourRect.yMax - timeIndicatorSize - (hourRect.height   * 1 / 6f),
                                            timeIndicatorSize, timeIndicatorSize );
                    GUI.DrawTexture(eyeRect, Resources.PinEye);
                }

                // also highlight all selected timeslots
                if (selected) {
                    Widgets.DrawHighlightSelected(hourRect);
                }

                // advance rect
                hourRect.x += hourRect.width;
            }

            // draw final label
            label = 0.FormatHour();
            labelRect = new Rect(0f, bar.yMin + (bar.height * 1 / 3f), label.NoWrapWidth(), bar.height * 2 / 3f);
            labelRect.x = hourRect.xMin - (labelRect.width / 2f);
            LabelUtilities.Label(labelRect, label, Color.grey, GameFont.Tiny, TextAnchor.UpperCenter);

            // draw current time indicator
            float curTimeX = GenLocalDate.DayPercent( Find.CurrentMap ) * bar.width;
            Rect curTimeRect = new Rect( bar.xMin + curTimeX               - (timeIndicatorSize * 1 / 2f),
                                        hourRect.yMax - timeIndicatorSize - (hourRect.height   * 1 / 6f),
                                        timeIndicatorSize,
                                        timeIndicatorSize );
            GUI.DrawTexture(curTimeRect, Resources.PinClock);
        }

        private void DoToggleButtons(Rect canvas) {
            var button = new Rect( canvas.xMax - 30f - Margin, canvas.yMin, 30f, 30f );

            ButtonImageToggle(() => PriorityManager.ShowPriorities, val => PriorityManager.ShowPriorities = val,
                               button,
                               "WorkTab.PrioritiesDetailed".Translate(), Resources.PrioritiesDetailed,
                               "WorkTab.PrioritiesSimple".Translate(), Resources.PrioritiesSimple);
            button.x -= 30f + Margin;

            ButtonImageToggle(() => PriorityManager.ShowScheduler, val => PriorityManager.ShowScheduler = val, button,
                               "WorkTab.PrioritiesTimed".Translate(), Resources.PrioritiesTimed,
                               "WorkTab.PrioritiesWholeDay".Translate(), Resources.PrioritiesWholeDay);
            button.x -= 30f + Margin;

            ButtonImageToggle(() => AnyExpanded,
                              v => ExpandAll(Table, v),
                              button,
                               "WorkTab.ExpandAll".Translate(), Resources.Expand,
                               "WorkTab.CollapseAll".Translate(), Resources.Collapse);
        }

        public static bool AnyExpandedNew(PawnTable table) => table.columns
                                                                   .Select(c => c.Worker)
                                                                   .OfType<IExpandableColumn>()
                                                                   .Any(w => w.Expanded);

        public static void ExpandAll(PawnTable table, bool expand) {
            foreach (var expandableColumn in table.columns.Select(c => c.Worker).OfType<IExpandableColumn>()) {
                Expand(expandableColumn, expand);
            }
            SetCurrentWorkTabDirty();
        }

        public static void Expand(IExpandableColumn expandableColumn, bool expand) {
            if (expandableColumn.Expanded != expand && expandableColumn.CanExpand) {
                expandableColumn.Expanded = expand;
            }
        }

        public static void SetCurrentWorkTabDirty()
        {
            Instance?.Notify_ColumnsChanged();
            MainTabWindow_PawnTable_DoWindowContents.ColumnsChanged = true;
        }
    }
}
