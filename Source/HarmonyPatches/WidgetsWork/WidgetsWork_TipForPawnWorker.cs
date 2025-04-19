using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTab
{
    [HarmonyPatch(typeof(WidgetsWork), "TipForPawnWorker")]
    public static class WidgetsWork_TipForPawnWorker
    {
        private static string PriorityLabel(string str2, WorkTypeDef wDef, int priority)
        {
            var gerundLabel = wDef.gerundLabel.CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor);
            return $"{gerundLabel}: {DrawUtilities.PriorityLabel(priority)}";
        }
        
        private static void TipForPawnWorker(Pawn pawn, WorkTypeDef workType, StringBuilder tip)
        {
            var tracker = PriorityManager.Get[pawn];
            if (!tracker.TimeScheduled(workType))
            {
                return;
            }
                
            tip.AppendLine();
            tip.Append("WorkTab.XIsAssignedToY".Translate(pawn.NameShortColored, workType.LabelCap));
            tip.Append(tracker.TimeScheduledTip(workType));
        }
        
        private static Color TipColor(int priority)
        {
            return new Color(1f, 0.9f, 0.5f);
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> _instr,
                                                              ILGenerator generator)
        {
            var originalCodes = _instr.ToList();
            var originalLastPop = originalCodes.FindLastIndex(code => code.opcode == OpCodes.Pop);
            
            var newCodes = new List<CodeInstruction>();
            
            var tipForPawnWorkerMethodLabel = generator.DefineLabel();
            var lastBrfalse_S = originalCodes.FindLast(code => code.opcode == OpCodes.Brfalse_S);
            lastBrfalse_S.operand = tipForPawnWorkerMethodLabel;
            
            var patched = false;
            for (var index = 0; index < originalCodes.Count; index++)
            {
                var code = originalCodes[index];

                if (!patched &&
                    code.opcode == OpCodes.Callvirt &&
                    code.operand is MethodInfo { Name: "AppendLine" } method &&
                    method.GetParameters().Length == 1 &&
                    method.GetParameters()[0].ParameterType == typeof(string))
                {
                    newCodes.Add(new CodeInstruction(OpCodes.Ldarg_1)); // WorkTypeDef wDef
                    newCodes.Add(new CodeInstruction(OpCodes.Ldloc_2)); // priority
                    newCodes.Add(new CodeInstruction(OpCodes.Call, 
                        AccessTools.Method(typeof(WidgetsWork_TipForPawnWorker), nameof(PriorityLabel))));
                    patched = true;
                }
                
                if (index > 0 &&
                    originalCodes[index - 1].opcode == OpCodes.Ldarg_1 &&
                    code.opcode == OpCodes.Callvirt &&
                    code.operand is MethodInfo { Name: "WorkTypeIsDisabled" } method2 &&
                    method2.GetParameters().Length == 1 &&
                    method2.GetParameters()[0].ParameterType == typeof(WorkTypeDef))
                {
                    code.opcode = OpCodes.Call;
                    code.operand = AccessTools.Method(typeof(Pawn_Extensions), 
                        nameof(Pawn_Extensions.NotAllowedToDo), [typeof(Pawn), typeof(WorkTypeDef)]);
                }
                
                if (index > 0 &&
                    originalCodes[index - 1].opcode == OpCodes.Ldc_I4_2 &&
                    code.Calls(AccessTools.Method(typeof(WidgetsWork), nameof(WidgetsWork.ColorOfPriority))))
                {
                    code.operand = AccessTools.Method(typeof(WidgetsWork_TipForPawnWorker), nameof(TipColor));
                }

                newCodes.Add(code);

                if (index != originalLastPop)
                {
                    continue;
                }
                
                var ldPawn = new CodeInstruction(OpCodes.Ldarg_0); // Pawn p
                ldPawn.labels.Add(tipForPawnWorkerMethodLabel);
                newCodes.Add(ldPawn);
                newCodes.Add(new CodeInstruction(OpCodes.Ldarg_1)); // WorkTypeDef wDef
                newCodes.Add(new CodeInstruction(OpCodes.Ldloc_0)); // stringBuilder
                newCodes.Add(new CodeInstruction(OpCodes.Call, 
                    AccessTools.Method(typeof(WidgetsWork_TipForPawnWorker), nameof(TipForPawnWorker))));
            }

            return newCodes;
        }
    }
}