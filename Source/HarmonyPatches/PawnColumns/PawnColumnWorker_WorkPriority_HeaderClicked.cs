using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkTab;

[HarmonyPatch(typeof(PawnColumnWorker_WorkPriority), "HeaderClicked")]
public static class PawnColumnWorker_WorkPriority_HeaderClicked
{

    private static int GetDefaultPriority()
    {
        return Settings.defaultPriority;
    }

    private static int GetMaxPriority()
    {
        return Settings.maxPriority;
    }

    private static bool Execute(PawnColumnDef def, PawnTable table)
    {
        if (def.Worker is not PawnColumnWorker_WorkType workerWorkType)
        {
            return false;
        }

        if (workerWorkType.HeaderExpand())
        {
            MainTabWindow_WorkTab.SetCurrentWorkTabDirty();
        }

        return true;
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> _instr)
    {
        var instructions = _instr.ToList();

        var method = AccessTools.PropertyGetter("UnityEngine.Event:current");
        var flag = true;
        var count = instructions.Count;
        for (var i = 0; i < count; i++)
        {
            var code = instructions[i];

            if (flag && code.Calls(method))
            {
                flag = false;
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PawnColumnWorker), "def"));
                yield return new CodeInstruction(OpCodes.Ldarg_2);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PawnColumnWorker_WorkPriority_HeaderClicked), nameof(Execute)));
                yield return new CodeInstruction(instructions[i + 2]).WithLabels(instructions[i + 2].labels); // brfalse => ret
            }

            if (code.opcode == OpCodes.Ldc_I4_4 && i < count - 1 &&
                (instructions[i + 1].opcode == OpCodes.Stloc_S || instructions[i + 1].opcode == OpCodes.Ble_S))
            {
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(PawnColumnWorker_WorkPriority_HeaderClicked), nameof(GetMaxPriority)));
                continue;
            }

            if (i < count - 1 && code.opcode == OpCodes.Ldc_I4_3 &&
                instructions[i + 1].opcode == OpCodes.Callvirt &&
                instructions[i + 1].operand is MethodInfo { Name: "SetPriority" }) {
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(PawnColumnWorker_WorkPriority_HeaderClicked), nameof(GetDefaultPriority)));
                continue;
            }

            yield return code;
        }
    }
}