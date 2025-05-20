// Settings.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using UnityEngine;
using Verse;

namespace WorkTab {
    public class Settings : ModSettings {
        // ReSharper disable InconsistentNaming
        public static int  maxPriority             = 9;
        public static int  defaultPriority         = 3;
        public static bool TwentyFourHourMode      = true;
        public static bool disableScrollwheel      = false;
        public static bool highlightActiveWorkCells = true;

        // Buffers will be initialized with current settings as soon as
        // DoWindowContents() → Listing_Standard.TextFieldNumericLabeled() → Widgets.TextFieldNumeric() will be called.
        private static string maxPriorityBuffer = null;
        private static string defaultPriorityBuffer = null;

        #region Obsolete field
        // Never used. But keep to maintain compatibility.
        [Obsolete("playSounds setting is abandoned.")]
        public static bool playSounds              = true;  // HIGHLIGHT: Grouped Pawns Lists mod needed.
        [Obsolete("playCrunch setting is abandoned.")]
        public static bool playCrunch              = true;
        [Obsolete("jobTextMode setting is abandoned.")]
        public static bool jobTextMode             = false;
        [Obsolete("verticalLabels setting is abandoned.")]
        public static bool verticalLabels          = false;
#pragma warning disable CS0414
        [Obsolete("_fontFix setting is abandoned.")]
        private static bool _fontFix = true;
#pragma warning restore CS0414
        #endregion
        // ReSharper restore InconsistentNaming

        public static int MaxPriority => maxPriority;

        public static int DefaultPriority => defaultPriority;

        public static void DoWindowContents(Rect rect) {
            var options = new Listing_Standard();
            options.Begin(rect);

            options.TextFieldNumericLabeled("WorkTab.MaxPriority".Translate(), ref maxPriority, ref maxPriorityBuffer,
                                             4, 9, "WorkTab.MaxPriorityTip".Translate(), 1 / 8f);

            options.TextFieldNumericLabeled("WorkTab.DefaultPriority".Translate(), ref defaultPriority, ref defaultPriorityBuffer,
                                            1, maxPriority, "WorkTab.DefaultPriorityTip".Translate(), 1 / 8f);

            options.CheckboxLabeled("WorkTab.24HourMode".Translate(), ref TwentyFourHourMode,
                                     "WorkTab.24HourModeTip".Translate());

            options.CheckboxLabeled("WorkTab.DisableScrollwheel".Translate(), ref disableScrollwheel,
                                     "WorkTab.DisableScrollwheelTip".Translate());

            options.CheckboxLabeled("WorkTab.HighlightActiveWorkCells".Translate(), ref highlightActiveWorkCells,
                                     "WorkTab.HighlightActiveWorkCellsTip".Translate());

            options.End();
        }

        public override void ExposeData() {
            Scribe_Values.Look(ref maxPriority, "MaxPriority", 9);
            Scribe_Values.Look(ref defaultPriority, "DefaultPriority", 3);
            Scribe_Values.Look(ref TwentyFourHourMode, "TwentyFourHourMode", true);
            Scribe_Values.Look(ref disableScrollwheel, "DisableScrollwheel");
            Scribe_Values.Look(ref highlightActiveWorkCells, "HighlightActiveWorkCells");
        }
    }
}
