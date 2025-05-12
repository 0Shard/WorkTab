// TimeUtilities.cs
// Copyright Karel Kroeze, 2017-2020

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkTab;

public static class TimeUtilities
{
    private static List<int> _day;

    public static List<int> WholeDay
    {
        get
        {
            if (!_day.NullOrEmpty())
            {
                return [.._day];
            }

            _day = [];
            for (var hour = 0; hour < GenDate.HoursPerDay; hour++)
            {
                _day.Add(hour);
            }

            return [.._day];
        }
    }
}