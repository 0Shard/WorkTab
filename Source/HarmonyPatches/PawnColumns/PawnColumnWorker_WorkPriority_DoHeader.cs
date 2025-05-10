using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTab;

[HarmonyPatch(typeof(PawnColumnWorker_WorkPriority), "DoHeader")]
public static class PawnColumnWorker_WorkPriority_DoHeader
{

    private static Vector2 CalcSize(string label, PawnColumnDef def, TextAnchor defaultHeaderAlignment)
    {
        Text.WordWrap = false;
        Text.Anchor = defaultHeaderAlignment;
        if (def.workerClass == typeof(PawnColumnWorker_WorkGiver))
        {
            label = def.LabelCap;
        }
        var result = Text.CalcSize(label);
        if (def.workerClass == typeof(PawnColumnWorker_WorkGiver))
        {
            result.x = Mathf.Min(result.x, (Constants.WorkGiverWidth * 2) - Constants.Margin);
        }
        return result;
    }

    private static void DrawLabel(Rect labelRect, string label, PawnColumnDef def, Color defaultHeaderColor)
    {
        GUI.color = defaultHeaderColor;
        if (def.workerClass == typeof(PawnColumnWorker_WorkGiver))
        {
            label = def.LabelCap.Resolve().Truncate(labelRect.width);
        }
        Widgets.Label(labelRect, label);
        Text.Font = GameFont.Small;
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.WordWrap = true;
    }

    // base.DoHeader(rect, table); therefore, the Prefix method is not adopted
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> _instr)
    {
        var instructions = _instr.ToList();

        var defField = AccessTools.Field(typeof(PawnColumnWorker), "def");

        var getLabelRectMethod = AccessTools.Method("RimWorld.PawnColumnWorker_WorkPriority:GetLabelRect");
        var calcSizeMethod = AccessTools.Method("Verse.Text:CalcSize");
        var widgetsLabelMethod = AccessTools.Method("Verse.Widgets:Label", [typeof(Rect), typeof(string)]);

        var defaultHeaderColorGetter = AccessTools.PropertyGetter("RimWorld.PawnColumnWorker:DefaultHeaderColor");
        var defaultHeaderFontGetter = AccessTools.PropertyGetter("RimWorld.PawnColumnWorker:DefaultHeaderFont");
        var defaultHeaderAlignmentGetter = AccessTools.PropertyGetter("RimWorld.PawnColumnWorker:DefaultHeaderAlignment");

        var textFontSetter = AccessTools.PropertySetter("Verse.Text:Font");
        var textAnchorSetter = AccessTools.PropertySetter("Verse.Text:Anchor");

        var labelBefore = true;
        var count = instructions.Count;
        for (var i = 0; i < count; i++)
        {
            var code = instructions[i];

            if (labelBefore && i < count - 1 && code.opcode == OpCodes.Ldc_I4_1 &&
                instructions[i + 1].Calls(textFontSetter)) {

                yield return new CodeInstruction(OpCodes.Ldarg_0);
                code = new CodeInstruction(OpCodes.Callvirt, defaultHeaderFontGetter);
            }

            if (code.Calls(calcSizeMethod))
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, defField);
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Callvirt, defaultHeaderAlignmentGetter);
                code.operand = AccessTools.Method(typeof(PawnColumnWorker_WorkPriority_DoHeader), nameof(CalcSize));
            }

            if (code.Calls(getLabelRectMethod))
            {
                yield return new CodeInstruction(OpCodes.Ldarg_2);
                code.operand = AccessTools.Method("RimWorld.PawnColumnWorker_WorkPriority:GetInteractableHeaderRect");
            }

            if (labelBefore && i < count - 1 && code.opcode == OpCodes.Ldc_I4_4 &&
                instructions[i + 1].Calls(textAnchorSetter)) {

                yield return new CodeInstruction(OpCodes.Ldarg_0);
                code = new CodeInstruction(OpCodes.Callvirt, defaultHeaderAlignmentGetter);
            }

            if (code.Calls(widgetsLabelMethod))
            {
                labelBefore = false;
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, defField);
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Callvirt, defaultHeaderColorGetter);
                code.operand = AccessTools.Method(typeof(PawnColumnWorker_WorkPriority_DoHeader), nameof(DrawLabel));
            }

            yield return code;
        }
    }
}