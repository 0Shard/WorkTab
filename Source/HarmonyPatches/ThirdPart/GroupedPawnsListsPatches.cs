using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTab.HarmonyPatches.ThirdPart;

// TODO: Once this PR https://gitlab.com/krypt_lynx/PawnTableGrouped/-/merge_requests/3 is merged, this patch can be deleted.
[HarmonyPatch("PawnTableGrouped.PawnTableGroupedImpl", "UpdateColumnWidths")]
public static class GroupedPawnsListsPatches
{
    public static bool Prepare()
    {
        return ModsConfig.IsActive("name.krypt.rimworld.pawntablegrouped");
    }

    public static bool Prefix(Verse.WeakReference<PawnTable> ___table, out float width, ref bool __result)
    {
        var table = ___table.Target;
        const float scrollBar = 16f;
        var cachedColumnWidths = table.CachedColumnWidths();
        var optimalWidth = table.Size.x - scrollBar;

        // if (model.AllowHScroll)
        if (true) // this mod always allow horizontal scroll bar.
        {
            float minWidth = 0;
            var columns = table.Columns;
            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                if (!column.ignoreWhenCalculatingOptimalTableSize)
                {
                    if (i != 0)
                    {
                        var minColumnWidth = table.GetMinWidth(column);
                        cachedColumnWidths[i] = Mathf.Max(cachedColumnWidths[i], minColumnWidth);
                        minWidth += minColumnWidth;
                    }
                    else
                    {
                        var optColumnWidth = table.GetOptimalWidth(column);
                        cachedColumnWidths[i] = Mathf.Max(cachedColumnWidths[i], optColumnWidth);
                        minWidth += optColumnWidth;
                    }
                }
            }

            if (minWidth > optimalWidth)
            {
                width = minWidth;
                __result = false;
            }
            else
            {
                width = optimalWidth;
                __result = true;
            }
        }
        // else
        // {
        //     width = optimalWidth;
        //     __result = true;
        // }
        return false;
    }
}