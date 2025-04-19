using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WorkTab;

[HarmonyPatch(typeof(PawnColumnWorker_WorkPriority), nameof(PawnColumnWorker_WorkPriority.DoHeader))]
public class PawnColumnWorker_WorkPriority_DoHeader
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> _instr,
                                                          ILGenerator generator)
    {
        var instructions = _instr.ToList();
        var doHeaderLabelMethodInfoLabel = generator.DefineLabel();
        
        var count = instructions.Count;
        for (var i = 0; i < count; i++)
        {
            var code = instructions[i];
            if (code.Calls(AccessTools.Method(typeof(Widgets), nameof(Widgets.Label), [typeof(Rect), typeof(string)])))
            {
                
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field("RimWorld.PawnColumnWorker:def"));
                yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field("RimWorld.PawnColumnDef:workerClass"));
                // IL_004c: call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
                // IL_0051: call         bool [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
                // IL_0056: brfalse.s    IL_0078
                yield return new CodeInstruction(OpCodes.Ldtoken, typeof(PawnColumnWorker_WorkType));
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(System.Type), nameof(System.Type.GetTypeFromHandle)));
                yield return new CodeInstruction(OpCodes.Ceq);
                yield return new CodeInstruction(OpCodes.Brfalse_S, doHeaderLabelMethodInfoLabel);
                
                // labelRect
                // labelShort
                // rect
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Call, 
                    AccessTools.Method(typeof(LabelUtilities), nameof(LabelUtilities.DoHeaderLabel)));;
                yield return new CodeInstruction(OpCodes.Ret);
                
                code.labels.Add(doHeaderLabelMethodInfoLabel);
            }
            
            yield return code;
        }
    }
}