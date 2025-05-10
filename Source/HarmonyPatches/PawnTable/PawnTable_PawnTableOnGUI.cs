// PawnTable_PawnTableOnGUI.cs
// Copyright Karel Kroeze, 2020-2020

using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTab;

[HarmonyPatch(typeof(PawnTable), nameof(PawnTable.PawnTableOnGUI))]
public static class PawnTable_PawnTableOnGUI
{
    private static readonly Color BorderColor = PawnTable.BorderColor;

    private static int _hoveredColumnContent = -1;
    private static int _hoveredColumnLabel = -1;
    private static int _hoveredRowContent = -1;
    private static int _hoveredRowLabel = -1;

    // HIGHLIGHT: Don't change this method param. 'Grouped Pawns Lists' mod need.
    public static bool Prefix(PawnTable __instance, Vector2 position, PawnTableDef ___def, ref Vector2 ___scrollPosition)
    {
        if (___def != PawnTableDefOf.Work)
            return true;
        if (Event.current.type == EventType.Layout)
            return false;

        // Refresh cache
        _hoveredRowLabel = -1;
        __instance.RecacheIfDirty();

        var columns = __instance.Columns;
        var pawns = __instance.PawnsListForReading;
        var widths = __instance.CachedColumnWidths();
        var heights = __instance.CachedRowHeights();

        var headerH = __instance.HeaderHeight;
        var heightNoScroll = __instance.HeightNoScrollbar;
        var labelW = widths[0];

        var initialSize = InitialSize(__instance.Size);
        var outH = Mathf.Min(heightNoScroll - headerH, initialSize.y - position.y);
        var outW = initialSize.x - labelW;

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
        if (needV) { tableOut.width += 16f; }

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
            var w = widths[i];
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
        var rowY = 0f;
        for (var j = 0; j < pawns.Count; j++)
        {
            var rowHeight = heights[j];
            var isVisible = tableView.height <= tableOut.height ||
                            rowY - ___scrollPosition.y + rowHeight >= 0f &&
                            rowY - ___scrollPosition.y <= tableOut.height;
            if (isVisible)
            {
                var cellRect = new Rect(0f, rowY, labelW, rowHeight);
                if (_hoveredRowContent == j) Widgets.DrawHighlight(cellRect);
                if (Mouse.IsOver(cellRect)) _hoveredRowLabel = j;

                GUI.color = BorderColor;
                Widgets.DrawLineHorizontal(0f, rowY, labelW);
                GUI.color = Color.white;
                columns[0].Worker.DoCell(cellRect, pawns[j], __instance);
                if (pawns[j].Downed)
                {
                    GUI.color = new Color(1f, 0f, 0f, 0.5f);
                    Widgets.DrawLineHorizontal(0f, cellRect.center.y, labelW);
                    GUI.color = Color.white;
                }
            }

            rowY += rowHeight;
        }
        Widgets.EndScrollView();
        ___scrollPosition.y = vPos.y;
        _hoveredRowContent = -1;

        // Main table scroll
        Widgets.BeginScrollView(tableOut, ref ___scrollPosition, tableView);

        // Column content sync highlight
        var newHoveredColContent = -1;
        curX = 0f;
        for (var i = 1; i < columns.Count; i++)
        {
            var w = widths[i];
            var rect = new Rect(curX, 0f, w, contentH);
            if (Mouse.IsOver(rect))
            {
                newHoveredColContent = i;
            }
            curX += w;
        }
        _hoveredColumnContent = newHoveredColContent;

        curX = 0f;
        for (var i = 1; i < columns.Count; i++)
        {
            var w = widths[i];
            if (_hoveredColumnContent == i || _hoveredColumnLabel == i)
            {
                var highlightDrawRect = new Rect(curX, ___scrollPosition.y, w, outH);
                Widgets.DrawHighlight(highlightDrawRect);
            }
            curX += w;
        }
        _hoveredColumnLabel = -1;

        // Draw rows with separators and cells
        rowY = 0f;
        for (var j = 0; j < pawns.Count; j++)
        {
            var rowHeight = heights[j];
            var isVisible = tableView.height <= tableOut.height ||
                            rowY - ___scrollPosition.y + rowHeight >= 0f &&
                            rowY - ___scrollPosition.y <= tableOut.height;
            if (isVisible)
            {
                var rowRect = new Rect(0f, rowY, contentW, rowHeight);
                if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);
                    _hoveredRowContent = j;
                }
                if (_hoveredRowLabel == j) Widgets.DrawHighlight(rowRect);

                GUI.color = BorderColor;
                Widgets.DrawLineHorizontal(0f, rowY, contentW);
                GUI.color = Color.white;
                var cellX = 0f;
                for (var c = 1; c < columns.Count; c++)
                {
                    var w = widths[c];
                    var cellR = new Rect(cellX, rowY, w, rowHeight);
                    columns[c].Worker.DoCell(cellR, pawns[j], __instance);
                    cellX += w;
                }

                if (pawns[j].Downed)
                {
                    GUI.color = new Color(1f, 0f, 0f, 0.5f);
                    Widgets.DrawLineHorizontal(0f, rowRect.center.y, contentW);
                    GUI.color = Color.white;
                }
            }

            rowY += rowHeight;
        }

        Widgets.EndScrollView();
        return false;
    }

    private static Vector2 InitialSize(Vector2 size)
    {
        const float margin = 8f;
        size.x -= margin * 2f;
        if (size.x > UI.screenWidth)
        {
            size.x = UI.screenWidth - 30f;
        }

        const float baseExtraBottomSpace = 53f;
        var extraBottomSpace = PriorityManager.ShowScheduler ? (baseExtraBottomSpace / 2f) + Constants.TimeBarHeight : baseExtraBottomSpace;
        size.y = UI.screenHeight - 35 - extraBottomSpace - Constants.ExtraTopSpace - margin * 2f;
        return size;
    }
}