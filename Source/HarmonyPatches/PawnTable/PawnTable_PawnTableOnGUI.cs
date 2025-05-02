// PawnTable_PawnTableOnGUI.cs
// Copyright Karel Kroeze, 2020-2020

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTab;

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

    private static readonly MethodInfo recache = AccessTools.Method(typeof(PawnTable), "RecacheIfDirty");

    // The above three FieldInfos can actually be injected into method parameters through ___field, but in order to maintain compatibility
    // Don't change this method param. 'Grouped Pawns Lists' mod need.
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

        var headerH = __instance.HeaderHeight;
        var heightNoScroll = __instance.HeightNoScrollbar;
        var labelW = widths[0];
        
        // Calculate vertical space, clamped to screen, considering bottom space for the timeline
        const float bottomMargin = MainButtonDef.ButtonHeight + Constants.TimeBarHeight + 42f;
        var maxScrollH = UI.screenHeight - position.y - bottomMargin;
        var outH = Mathf.Min(heightNoScroll - headerH, maxScrollH);
        var outW = InitialSize(__instance.Size) - labelW;

        // Adjust outH based on timeline height to ensure enough space for the timeline at the bottom
        outH = Mathf.Min(outH, maxScrollH - Constants.TimeBarHeight);

        // Define rects
        var headerOut = new Rect(position.x + labelW, position.y, outW, headerH);
        var labelOut = new Rect(position.x, position.y + headerH, labelW, outH);
        var tableOut = new Rect(position.x + labelW, position.y + headerH, outW, outH);

        // Calculate the content size
        var contentW = widths.Skip(1).Sum(); // Skipping the label width for content
        var contentH = heights.Sum();

        // Ensure the content area is scrollable and scrollbars appear if content overflows
        var needH = contentW > outW;
        var needV = contentH > outH;

        // Enable scrollbars if content is too large
        if (needH) { headerOut.height += 16f; tableOut.height += 16f; }
        if (needV) { labelOut.width += 16f; tableOut.width += 16f; }
        
        var headerView = new Rect(0f, 0f, contentW, headerH);
        var labelView  = new Rect(0f, 0f, labelW, contentH);
        var tableView = new Rect(0f, 0f, contentW, contentH);

        // Draw frozen label header
        columns[0].Worker.DoHeader(new Rect(position.x, position.y, labelW, headerH), __instance);

        // Horizontal header scroll
        var hPos = new Vector2(___scrollPosition.x, 0f);
        Widgets.BeginScrollView(headerOut, ref hPos, headerView, false);
        var curX = 0f;
        for (var i = 1; i < columns.Count; i++)
        {
            var w = (i == columns.Count - 1) ? contentW - curX : widths[i];
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
        var vPos = new Vector2(0f, ___scrollPosition.y);
        Widgets.BeginScrollView(labelOut, ref vPos, labelView, false);
        var curY = 0f;
        for (var j = 0; j < pawns.Count; j++)
        {
            // only draw if on screen
            if (tableView.height <= tableOut.height ||
                curY - ___scrollPosition.y + heights[j] >= 0f &&
                curY - ___scrollPosition.y <= tableOut.height)
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
            }

            curY += heights[j];
        }
        Widgets.EndScrollView();
        ___scrollPosition.y = vPos.y;
        _hoveredRowContent = -1;

        // Main table scroll
        Widgets.BeginScrollView(tableOut, ref ___scrollPosition, tableView);
        
        // Column content sync highlight
        curX = 0f;
        for (var i = 1; i < columns.Count; i++)
        {
            var w = (i == columns.Count - 1) ? contentW - curX : widths[i];
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
        var rowY = 0f;
        for (var j = 0; j < pawns.Count; j++)
        {
            if (tableView.height <= tableOut.height ||
                rowY - ___scrollPosition.y + heights[j] >= 0f &&
                rowY - ___scrollPosition.y <= tableOut.height)
            {
                GUI.color = borderColor;
                Widgets.DrawLineHorizontal(0f, rowY, tableView.width);
                GUI.color = Color.white;

                var rowRect = new Rect(0f, rowY, tableView.width, heights[j]);
                if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);
                    _hoveredRowContent = j;
                }

                if (_hoveredRowLabel == j) Widgets.DrawHighlight(rowRect);

                var cellX = 0f;
                for (var c = 1; c < columns.Count; c++)
                {
                    var w = (c == columns.Count - 1) ? contentW - cellX : widths[c];
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
            }

            rowY += heights[j];
        }

        Widgets.EndScrollView();
        return false;
    }
    
    private static float InitialSize(Vector2 size)
    {
        const float margin = 6f;
        var initialSize = size.x - margin * 2f;
        if (initialSize > UI.screenWidth)
            initialSize = UI.screenWidth - margin * 2f;
        return initialSize;
    }
}