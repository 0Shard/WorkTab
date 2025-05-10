// Listing_Extensions.cs
// Copyright Karel Kroeze, 2017-2020

using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WorkTab;

public static class Listing_Extensions
{
    public static void DropdownLabeled<T>(this Listing_Standard listing, string label, string buttonLabel, T selected,
                                          Func<T, string> labelGetter, IEnumerable<T> options, Action<T> onSelect, string tooltip = null)
        where T : struct
    {
        const float textfieldWidthFraction = 1 / 3f;
        var rect = listing.GetRect(Text.LineHeight);
        var fieldRect = rect;
        var labelRect = rect;
        fieldRect.xMin = rect.xMax - (rect.width * textfieldWidthFraction);
        labelRect.xMax = fieldRect.xMin;
        if (!tooltip.NullOrEmpty())
        {
            TooltipHandler.TipRegion(rect, tooltip);
        }

        Widgets.DrawHighlightIfMouseover(rect);
        Widgets.Label(labelRect, label);
        Widgets.Dropdown(fieldRect, selected, labelGetter,
                                    t => options.Select(opt => new Widgets.DropdownMenuElement<string>()
                                    {
                                        option = new FloatMenuOption(labelGetter(opt), () => onSelect(opt)),
                                        payload = labelGetter(opt)
                                    }),
                                    buttonLabel);

        listing.Gap(listing.verticalSpacing);
    }

    public static void TextFieldNumericLabeled<T>(this Listing_Standard listing, string label, ref T value,
                                                  ref string buffer, float min = 0, float max = float.MaxValue,
                                                  string tooltip = null, float textfieldWidthFraction = 1 / 3f)
        where T : struct
    {
        var rect = listing.GetRect(Text.LineHeight);
        var fieldRect = rect;
        var labelRect = rect;
        fieldRect.xMin = rect.xMax - (rect.width * textfieldWidthFraction);
        labelRect.xMax = fieldRect.xMin;
        if (!tooltip.NullOrEmpty())
        {
            TooltipHandler.TipRegion(rect, tooltip);
        }

        Widgets.DrawHighlightIfMouseover(rect);
        Widgets.Label(labelRect, label);
        Widgets.TextFieldNumeric(fieldRect, ref value, ref buffer, min, max);
        listing.Gap(listing.verticalSpacing);
    }
}