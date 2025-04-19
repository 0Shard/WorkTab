using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkTab
{
    [HarmonyPatch(typeof(WidgetsWork), "DrawWorkBoxFor")]
    public static class WidgetsWork_DrawWorkBoxFor
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
            
            var setPriorityMI = AccessTools.Method(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.SetPriority));
            var getDefaultPriorityMI = AccessTools.Method(typeof(GameInitData_PrepForMapGen), nameof(GetDefaultPriority));

            var count = instructions.Count;
            for (var i = 0; i < count; i++)
            {
                var code = instructions[i];
                if (code.opcode == OpCodes.Ldc_I4_4 && i < count - 1 &&
                    (instructions[i + 1].opcode == OpCodes.Stloc_S || instructions[i + 1].opcode == OpCodes.Ble_S))
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WidgetsWork_DrawWorkBoxFor), nameof(GetMaxPriority)));
                    continue;
                }
                
                if (i < count - 1 && code.opcode == OpCodes.Ldc_I4_3 && instructions[i + 1].Calls(setPriorityMI)) {
                    yield return new CodeInstruction(OpCodes.Call, getDefaultPriorityMI);
                    continue;
                }
                
                if (i > 0 &&
                    instructions[i - 1].opcode == OpCodes.Ldarg_3 &&
                    code.opcode == OpCodes.Callvirt &&
                    code.operand is MethodInfo { Name: "WorkTypeIsDisabled" } method2 &&
                    method2.GetParameters().Length == 1 &&
                    method2.GetParameters()[0].ParameterType == typeof(WorkTypeDef))
                {
                    code.opcode = OpCodes.Call;
                    code.operand = AccessTools.Method(typeof(Pawn_Extensions), 
                        nameof(Pawn_Extensions.NotAllowedToDo), [typeof(Pawn), typeof(WorkTypeDef)]);
                }

                yield return code;
            }
        }
    }
}