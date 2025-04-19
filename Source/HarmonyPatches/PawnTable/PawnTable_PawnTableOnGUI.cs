// PawnTable_PawnTableOnGUI.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTab
{
    [HarmonyPatch(typeof(PawnTable), nameof(PawnTable.PawnTableOnGUI))]
    public static class PawnTable_PawnTableOnGUI
    {
        private static int _hoveredColumnContent = -1;
        private static int _hoveredColumnLabel = -1;
        private static int _hoveredRowContent = -1;
        private static int _hoveredRowLabel = -1;

        private static readonly FieldInfo widthsField = AccessTools.Field(typeof(PawnTable), "cachedColumnWidths");
        private static readonly FieldInfo heightsField = AccessTools.Field(typeof(PawnTable), "cachedRowHeights");
        private static readonly FieldInfo borderField = AccessTools.Field(typeof(PawnTable), "BorderColor");
        private static readonly FieldInfo fixedField = AccessTools.Field(typeof(PawnTable), "fixedSize");
        private static readonly MethodInfo recache = AccessTools.Method(typeof(PawnTable), "RecacheIfDirty");

        public static bool Prefix(PawnTable __instance, Vector2 position, PawnTableDef ___def, ref Vector2 ___scrollPosition)
        {
            if (___def != PawnTableDefOf.Work)
                return true;
            if (Event.current.type == EventType.Layout)
                return false;

            // Refresh cache
            _hoveredRowLabel = -1;
            recache.Invoke(__instance, null);

            var columns = __instance.Columns;
            var pawns = __instance.PawnsListForReading;
            var widths = (List<float>)widthsField.GetValue(__instance);
            var heights = (List<float>)heightsField.GetValue(__instance);
            var borderColor = (Color)borderField.GetValue(__instance);
            var fixedSize = (Vector2)fixedField.GetValue(__instance);

            float headerH = __instance.HeaderHeight;
            float heightNoScroll = __instance.HeightNoScrollbar;
            float labelW = widths[0];
            
            // Calculate vertical space, clamped to screen, considering bottom space for the timeline
            float bottomMargin = MainButtonDef.ButtonHeight + Constants.TimeBarHeight + 42f;
            float maxScrollH = UI.screenHeight - position.y - bottomMargin;
            float outH = Mathf.Min(heightNoScroll - headerH, maxScrollH);
            float outW = fixedSize.x - labelW;

            // Adjust outH based on timeline height to ensure enough space for the timeline at the bottom
            outH = Mathf.Min(outH, maxScrollH - Constants.TimeBarHeight);

            // Define rects
            Rect headerOut = new Rect(position.x + labelW, position.y, outW, headerH);
            Rect labelOut = new Rect(position.x, position.y + headerH, labelW, outH);
            Rect tableOut = new Rect(position.x + labelW, position.y + headerH, outW, outH);

            // Calculate the content size
            float contentW = 0f;
            for (int i = 1; i < widths.Count; i++) contentW += widths[i];
            float contentH = 0f;
            foreach (var h in heights) contentH += h;
            
            // Ensure the content area is scrollable and scrollbars appear if content overflows
            bool needH = contentW > outW;
            bool needV = contentH > outH;

            // Enable scrollbars if content is too large
            if (needH) { headerOut.height += 16f; tableOut.height += 16f; }
            if (needV) { labelOut.width += 16f; tableOut.width += 16f; }
            
            Rect headerView = new Rect(0f, 0f, contentW, headerH);
            Rect labelView  = new Rect(0f, 0f, labelW, contentH);
            Rect tableView = new Rect(0f, 0f, contentW, contentH);

            // Draw frozen label header
            columns[0].Worker.DoHeader(new Rect(position.x, position.y, labelW, headerH), __instance);

            // Horizontal header scroll
            Vector2 hPos = new Vector2(___scrollPosition.x, 0f);
            Widgets.BeginScrollView(headerOut, ref hPos, headerView, false);
            float curX = 0f;
            for (int i = 1; i < columns.Count; i++)
            {
                float w = (i == columns.Count - 1) ? contentW - curX : widths[i];
                var rect = new Rect(curX, 0f, w, headerH);
                if (Mouse.IsOver(rect)) _hoveredColumnLabel = i;
                if (_hoveredColumnContent == i) Widgets.DrawHighlight(rect);
                columns[i].Worker.DoHeader(rect, __instance);
                curX += w;
            }
            _hoveredColumnContent = -1;
            Widgets.EndScrollView();
            ___scrollPosition.x = hPos.x;
            
            // scrollview for label column - VERTICAL ONLY
            if (_hoveredColumnLabel == 0) Widgets.DrawHighlight(labelOut);

            // Vertical label scroll with separators
            Vector2 vPos = new Vector2(0f, ___scrollPosition.y);
            Widgets.BeginScrollView(labelOut, ref vPos, labelView, false);
            float curY = 0f;
            for (int j = 0; j < pawns.Count; j++)
            {
                GUI.color = borderColor;
                Widgets.DrawLineHorizontal(0f, curY, labelW);
                GUI.color = Color.white;

                var cellRect = new Rect(0f, curY, labelW, heights[j]);
                columns[0].Worker.DoCell(cellRect, pawns[j], __instance);
                if (_hoveredRowContent == j) Widgets.DrawHighlight(cellRect);
                if (Mouse.IsOver(cellRect)) _hoveredRowLabel = j;
                if (pawns[j].Downed)
                {
                    GUI.color = new Color(1f, 0f, 0f, 0.5f);
                    Widgets.DrawLineHorizontal(0f, cellRect.center.y, labelW);
                    GUI.color = Color.white;
                }
                curY += heights[j];
            }
            Widgets.EndScrollView();
            ___scrollPosition.y = vPos.y;
            _hoveredRowContent = -1;

            // Main table scroll
            Widgets.BeginScrollView(tableOut, ref ___scrollPosition, tableView, true);
            
            // Column content sync highlight
            curX = 0f;
            for (int i = 1; i < columns.Count; i++)
            {
                float w = (i == columns.Count - 1) ? contentW - curX : widths[i];
                var rect = new Rect(curX, 0f, w, tableOut.height);
                if (Mouse.IsOver(rect)) 
                {
                    Widgets.DrawHighlight(rect);
                    _hoveredColumnContent = i;
                }
                if (_hoveredColumnLabel == i) Widgets.DrawHighlight(rect);
                curX += w;
            }
            _hoveredColumnLabel = -1;
            
            // Draw rows with separators and cells
            float rowY = 0f;
            for (int j = 0; j < pawns.Count; j++)
            {
                GUI.color = borderColor;
                Widgets.DrawLineHorizontal(0f, rowY, tableView.width);
                GUI.color = Color.white;

                Rect rowRect = new Rect(0f, rowY, tableView.width, heights[j]);
                if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);
                    _hoveredRowContent = j;
                }
                if (_hoveredRowLabel == j) Widgets.DrawHighlight(rowRect);

                float cellX = 0f;
                for (int c = 1; c < columns.Count; c++)
                {
                    float w = (c == columns.Count - 1) ? contentW - cellX : widths[c];
                    var cellR = new Rect(cellX, rowY, w, heights[j]);
                    columns[c].Worker.DoCell(cellR, pawns[j], __instance);
                    cellX += w;
                }

                if (pawns[j].Downed)
                {
                    GUI.color = new Color(1f, 0f, 0f, 0.5f);
                    Widgets.DrawLineHorizontal(0f, rowRect.center.y, tableView.width);
                    GUI.color = Color.white;
                }
                rowY += heights[j];
            }

            Widgets.EndScrollView();
            return false;
        }
    }
    
    [HarmonyPatch(typeof(PawnTable), "RecacheSize")]
    public static class Patch_RecacheSize_SkipFixedSize
    {
        // Prefix 中将 hasFixedSize 设置为 false，从而让 if (this.hasFixedSize) 跳过
        public static void Prefix(PawnTable __instance, PawnTableDef ___def)
        {
            if (___def != PawnTableDefOf.Work)
            {
                return;
            }
            
            var fieldHasFixedSize = AccessTools.Field(typeof(PawnTable), "hasFixedSize");
            fieldHasFixedSize.SetValue(__instance, false);
        }
    }
}