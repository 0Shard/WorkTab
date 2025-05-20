using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTab;

[HarmonyPatch(typeof(MainTabWindow_Work), "DoManualPrioritiesCheckbox")]
public static class MainTabWindow_Work_DoManualPrioritiesCheckbox
{
    private static TaggedString ReplaceMaxPriority(TaggedString text)
    {
        return text.Replace("4", Settings.MaxPriority.ToString());
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr)
    {
        var widgetsLabelMethod =
            AccessTools.Method(typeof(Widgets), nameof(Widgets.Label), [typeof(Rect), typeof(TaggedString)]);

        foreach (var code in instr)
        {
            if (code.Calls(widgetsLabelMethod))
            {
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
                                                     typeof(MainTabWindow_Work_DoManualPrioritiesCheckbox),
                                                     nameof(ReplaceMaxPriority)));
            }

            yield return code;
        }
    }
}