using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkTab
{
    [HarmonyPatch(typeof(GameInitData), nameof(GameInitData.PrepForMapGen))]
    public static class GameInitData_PrepForMapGen
    {

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> _instr)
        {
            var instructions = _instr.ToList();
            var setPriorityMI = AccessTools.Method(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.SetPriority));
            var getDefaultPriorityMI = AccessTools.PropertyGetter(typeof(Settings), nameof(Settings.DefaultPriority));

            var count = instructions.Count;
            for (var i = 0; i < count; i++)
            {
                if (i < count - 1 &&
                    instructions[i].opcode == OpCodes.Ldc_I4_3 &&
                    instructions[i + 1].Calls(setPriorityMI))
                {
                    yield return new CodeInstruction(OpCodes.Call, getDefaultPriorityMI);
                    continue;
                }

                yield return instructions[i];
            }
        }
    }
}