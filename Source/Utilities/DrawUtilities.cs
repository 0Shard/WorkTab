// DrawUtilities.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTab;

public static class DrawUtilities {

    public static void DrawPartScheduled(Rect box) {
        // draw clock icon in top left
        Color colour = GUI.color;
        GUI.color = Color.grey;
        GUI.DrawTexture(new Rect(box.xMin, box.yMin, box.height / 2f, box.width / 2f).ContractedBy(2f),
                         Resources.Half);
        GUI.color = colour;
    }

    public static void DrawPriority(Rect box, int priority, bool small = false) {
        // bail out if priority is 0
        if (priority == 0) {
            return;
        }

        GameFont font = small ? GameFont.Small : GameFont.Medium;

        // detailed mode
        if (Find.PlaySettings.useWorkPriorities) {
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = font;
            GUI.color = ColorOfPriority(priority);
            Widgets.Label(box.ContractedBy(-3f), priority.ToStringCached());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        // toggle mode
        else {
            GUI.DrawTexture(box, WidgetsWork.WorkBoxCheckTex);
        }
    }

    public static void DrawTimeScheduled(Rect box) {
        // draw clock icon in bottom left
        Color colour = GUI.color;
        GUI.color = Color.grey;
        GUI.DrawTexture(
            new Rect(box.xMin, box.yMin + (box.height / 2f), box.height / 2f, box.width / 2f).ContractedBy(2f),
            Resources.Clock);
        GUI.color = colour;
    }

    // HIGHLIGHT: Don't rename param worktype. 'Grouped Pawns Lists' mod need.
    [Obsolete("DrawWorkBoxBackground is abandoned. See WidgetsWork.DrawWorkBoxBackground instead.")]
    public static void DrawWorkBoxBackground(Rect box, Pawn pawn, WorkTypeDef worktype) {

    }

    #region PriorityRange Dictionary
    private struct PriorityRange
    {
        public int? MinInclusive;
        public int? MaxInclusive;
        public string Label;

        public bool Matches(int value)
        {
            if (MinInclusive.HasValue && value < MinInclusive.Value)
                return false;
            if (MaxInclusive.HasValue && value > MaxInclusive.Value)
                return false;
            return true;
        }
    }

    /**
     *                  9   8   7   6   5   4
     * top              1   1   1   1   1   1
     * very high        2   2   2   2
     * high             3   3   3   3   2   2
     * above normal     4   4
     * normal           5       4       3   3
     * below normal     6   5
     * low              7   6   5   4   4   4
     * very low         8   7   6   5
     * lowest           9   8   7   6   5
     *
     * KILL ME NOW.
     */
    private static readonly Dictionary<int, List<PriorityRange>> PriorityLabelMap = new()
    {
        [2] =
        [
            new PriorityRange { MinInclusive = 6, Label = "VeryHigh" },
            new PriorityRange { MaxInclusive = 5, Label = "High" }
        ],
        [3] =
        [
            new PriorityRange { MinInclusive = 6, Label = "High" },
            new PriorityRange { MaxInclusive = 5, Label = "Normal" }
        ],
        [4] =
        [
            new PriorityRange { MinInclusive = 8, Label = "AboveNormal" },
            new PriorityRange { MinInclusive = 7, MaxInclusive = 7, Label = "Normal" },
            new PriorityRange { MaxInclusive = 6, Label = "Low" }
        ],
        [5] =
        [
            new PriorityRange { MinInclusive = 9, MaxInclusive = 9, Label = "Normal" },
            new PriorityRange { MinInclusive = 8, MaxInclusive = 8, Label = "BelowNormal" },
            new PriorityRange { MinInclusive = 7, MaxInclusive = 7, Label = "Low" },
            new PriorityRange { MinInclusive = 6, MaxInclusive = 6, Label = "VeryLow" },
            new PriorityRange { MaxInclusive = 5, Label = "Lowest" }
        ],
        [6] =
        [
            new PriorityRange { MinInclusive = 9, MaxInclusive = 9, Label = "BelowNormal" },
            new PriorityRange { MinInclusive = 8, MaxInclusive = 8, Label = "Low" },
            new PriorityRange { MinInclusive = 7, MaxInclusive = 7, Label = "VeryLow" },
            new PriorityRange { MaxInclusive = 6, Label = "Lowest" }
        ],
        [7] =
        [
            new PriorityRange { MinInclusive = 9, MaxInclusive = 9, Label = "Low" },
            new PriorityRange { MinInclusive = 8, MaxInclusive = 8, Label = "VeryLow" },
            new PriorityRange { MaxInclusive = 7, Label = "Lowest" }
        ],
        [8] =
        [
            new PriorityRange { MinInclusive = 9, MaxInclusive = 9, Label = "VeryLow" },
            new PriorityRange { MaxInclusive = 8, Label = "Lowest" }
        ],
        [9] = [new PriorityRange { Label = "Lowest" }]  // catch-all
    };

    private static bool TryMatchMaxPriority(List<PriorityRange> ranges, int max, out string label)
    {
        foreach (var range in ranges.Where(range => range.Matches(max)))
        {
            label = range.Label;
            return true;
        }

        label = null;
        return false;
    }
    #endregion

    public static string PriorityLabel(int priority)
    {
        if (priority == 0)
            return "Fluffy.WorkTab.Priority.None".Translate().Colorize(ColorOfPriority(priority));
        if (priority == 1)
            return "Fluffy.WorkTab.Priority.Top".Translate().Colorize(ColorOfPriority(priority));
        if (priority == Settings.defaultPriority && !Find.PlaySettings.useWorkPriorities)
            return "Fluffy.WorkTab.Priority.Normal".Translate().Colorize(ColorOfPriority(priority));

        if (!PriorityLabelMap.TryGetValue(priority, out var ranges) ||
            !TryMatchMaxPriority(ranges, Settings.maxPriority, out var labelKey))
        {
            return "Fluffy.WorkTab.Priority.Unknown".Translate().Colorize(ColorOfPriority(priority));
        }
        return $"Fluffy.WorkTab.Priority.{labelKey}".Translate().Colorize(ColorOfPriority(priority));
    }

    public static string TimeScheduledTip(int[] priorities, string label) {
        var tip = "";
        var start = -1;
        var priority = -1;

        for (var hour = 0; hour < GenDate.HoursPerDay; hour++) {
            var curpriority = priorities[hour];

            // stop condition
            if (curpriority != priority && start >= 0) {
                tip += "\n   ";
                tip += start.FormatHour() + " - " + hour.FormatHour();
                if (Find.PlaySettings.useWorkPriorities) {
                    tip += " (" + priority + ")";
                }

                // reset start & priority
                start = -1;
                priority = -1;
            }

            // start condition
            if (curpriority > 0 && curpriority != priority && start < 0) {
                priority = curpriority;
                start = hour;
            }
        }

        // final check for x till midnight
        if (start > 0) {
            tip += "\n   ";
            tip += start.FormatHour() + " - " + 0.FormatHour();
            if (Find.PlaySettings.useWorkPriorities) {
                tip += " (" + priority + ")";
            }
        }

        return tip;
    }

    public static string TipForPawnWorker(Pawn pawn, WorkGiverDef workgiver, bool incapableBecauseOfCapacities) {
        var tip = new StringBuilder();
        tip.Append(workgiver.LabelCap.CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor));
        tip.Append(": " + PriorityLabel(pawn.GetPriority(workgiver, -1)));
        tip.AppendLine();

        if (!pawn.WorkGiverAllowedToDo(workgiver)) {
            string str = "CannotDoThisWork".Translate( pawn.LabelShort, pawn);
            var disabledWorkType = pawn.GetReasonsForDisabledWorkType(workgiver.workType);
            if (!disabledWorkType.NullOrEmpty())
            {
                var s = "\n\n" + string.Join(". ", disabledWorkType);
                if (disabledWorkType.Count == 1)
                    s += ".";
                str += s.Colorize(ColorLibrary.RedReadable);
            }
            tip.Append(str);
        } else {
            var num = pawn.skills.AverageOfRelevantSkillsFor(workgiver.workType);
            if (workgiver.workType.relevantSkills.Any()) {
                var str = workgiver.workType.relevantSkills
                                   .Aggregate("", (current, relevantSkill) => $"{current}{relevantSkill.skillLabel.CapitalizeFirst()}, ");
                tip.AppendLine("RelevantSkills".Translate(str.Substring(0, str.Length - 2), num.ToString("0.#"), 20));
            }

            if (pawn.Ideo != null && pawn.Ideo.IsWorkTypeConsideredDangerous(workgiver.workType))
            {
                tip.AppendLine();
                tip.AppendLine(((string) "SelectedWorkTypeOpposedByIdeo".Translate(pawn)).Colorize(WidgetsWork.ColorOfPriority(2)));
            }

            if (workgiver.workType.relevantSkills.Any() && num <= 2.0 && pawn.workSettings.WorkIsActive(workgiver.workType))
            {
                tip.AppendLine();
                tip.AppendLine(((string) "SelectedWorkTypeWithVeryBadSkill".Translate()).Colorize(WidgetsWork.ColorOfPriority(2)));
            }

            if (!workgiver.description.NullOrEmpty()) {
                tip.AppendLine();
                tip.Append(workgiver.description);
            }

            if (incapableBecauseOfCapacities) {
                tip.AppendLine();
                tip.AppendLine();
                tip.Append("IncapableOfWorkTypeBecauseOfCapacities".Translate());
            }

            var tracker = PriorityManager.Get[pawn];
            if (!tracker.TimeScheduled(workgiver))
            {
                return tip.ToString();
            }
            tip.AppendLine();
            tip.Append("WorkTab.XIsAssignedToY".Translate(pawn.NameShortColored, workgiver.LabelCap));
            tip.Append(tracker.TimeScheduledTip(workgiver));
        }

        return tip.ToString();
    }

    public static Color ColorOfPriority(int priority) {
        if (priority == 0) {
            return Color.grey;
        }

        float halfway = Settings.maxPriority / 2f;

        if (priority <= halfway) {
            return Color.Lerp(Color.green, Color.white, Mathf.InverseLerp(1, halfway, priority));
        }

        return Color.Lerp(Color.white, Color.grey, Mathf.InverseLerp(halfway, Settings.maxPriority, priority));
    }
}
