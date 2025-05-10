// PawnTable_RecacheIfDirty.cs
// Copyright Karel Kroeze, 2020-2020

using HarmonyLib;
using RimWorld;
using Verse;
using WorkTab.HarmonyPatches.ThirdPart;

namespace WorkTab;

[HarmonyPatch(typeof(PawnTable), "SetMinMaxSize")]
public static class PawnTable_SetMinMaxSize
{
    public static bool Prepare()
    {
        return !GroupedPawnsListsPatches.Prepare();
    }

    public static void Prefix(ref int maxTableWidth, ref int maxTableHeight)
    {
        maxTableWidth = int.MaxValue;
        maxTableHeight = int.MaxValue;
    }
}