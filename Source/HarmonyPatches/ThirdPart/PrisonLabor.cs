using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace WorkTab.HarmonyPatches.ThirdPart;

[HarmonyPatch("PrisonLabor.Core.MainButton_Window.MainTabWindow_Labor", "DoManualPrioritiesCheckbox")]
public static class MainTabWindow_Labor_DoManualPrioritiesCheckbox
{
    public static bool Prepare()
    {
        return ModsConfig.IsActive("avius.prisonlabor");
    }

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
                                                     typeof(MainTabWindow_Labor_DoManualPrioritiesCheckbox),
                                                     nameof(ReplaceMaxPriority)));
            }

            yield return code;
        }
    }
}
