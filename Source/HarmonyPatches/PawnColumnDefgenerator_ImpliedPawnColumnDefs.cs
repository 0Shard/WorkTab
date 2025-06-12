using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;

namespace WorkTab
{
    [HarmonyPatch(typeof(PawnColumnDefGenerator), nameof(PawnColumnDefGenerator.ImpliedPawnColumnDefs))]
    public static class PawnColumnDefgenerator_ImpliedPawnColumnDefs
    {

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var target = typeof(PawnColumnWorker_WorkPriority);
            var replacement = typeof(PawnColumnWorker_WorkType);

            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldtoken && instruction.operand as Type == target)
                {
                    yield return new CodeInstruction(OpCodes.Ldtoken, replacement);
                    continue;
                }

                yield return instruction;
            }
        }
    }
}