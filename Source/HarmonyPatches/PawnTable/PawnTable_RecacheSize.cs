// PawnTable_RecacheIfDirty.cs
// Copyright Karel Kroeze, 2020-2020

using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using WorkTab.HarmonyPatches.ThirdPart;

namespace WorkTab;

[HarmonyPatch(typeof(PawnTable), "RecacheSize")]
public static class PawnTable_RecacheSize
{
    private static int _maxTable = int.MaxValue;

    public static bool Prepare()
    {
        return !GroupedPawnsListsPatches.Prepare();
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr)
    {
        var screenWidth = AccessTools.Field(typeof(UI), nameof(UI.screenWidth));
        var screenHeight = AccessTools.Field(typeof(UI), nameof(UI.screenHeight));
        var maxTable = AccessTools.Field(typeof(PawnTable_RecacheSize), nameof(_maxTable));
        foreach (var codeInstruction in instr)
        {
            if (codeInstruction.LoadsField(screenWidth))
            {
                codeInstruction.operand = maxTable;
            }
            if (codeInstruction.LoadsField(screenHeight))
            {
                codeInstruction.operand = maxTable;
            }
            yield return codeInstruction;
        }
    }
}