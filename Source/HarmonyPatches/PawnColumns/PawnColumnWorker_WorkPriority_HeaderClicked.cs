using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
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
    
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> _instr)
    {
        var instructions = _instr.ToList();

        var count = instructions.Count;
        for (var i = 0; i < count; i++)
        {
            var code = instructions[i];
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