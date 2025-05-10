using System.Collections.Generic;
using RimWorld;

namespace WorkTab;

public static class PawnTable_Extensions
{
    public static List<float> CachedColumnWidths(this PawnTable table)
    {
        table.RecacheIfDirty();
        if (table.cachedColumnWidths.Count != table.Columns.Count)
        {
            table.RecacheColumnWidths();
        }
        return table.cachedColumnWidths;
    }

    public static List<float> CachedRowHeights(this PawnTable table)
    {
        table.RecacheIfDirty();
        if (table.cachedRowHeights.Count != table.PawnsListForReading.Count)
        {
            table.RecacheRowHeights();
        }
        return table.cachedRowHeights;
    }
}