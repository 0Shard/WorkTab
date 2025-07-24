using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTab.HarmonyPatches.ThirdPart;

public static class GroupedPawnsListsPatches
{
    delegate bool PawnTable_RecacheSize_Prefix_Delegate(PawnTable arg1);

    public static bool Prepare()
    {
        return ModsConfig.IsActive("name.krypt.rimworld.pawntablegrouped");
    }

    // TODO: Once this PR https://gitlab.com/krypt_lynx/PawnTableGrouped/-/merge_requests/3 is merged, this patch can be deleted.
    [HarmonyPatch("PawnTableGrouped.PawnTableGroupedImpl", "UpdateColumnWidths")]
    public static class PawnTableGroupedImpl_UpdateColumnWidths_Prefix
    {
        public static bool Prepare()
        {
            return GroupedPawnsListsPatches.Prepare();
        }

        public static bool Prefix(Verse.WeakReference<PawnTable> ___table, out float width, ref bool __result)
        {
            var table = ___table.Target;
            const float scrollBar = 16f;
            var cachedColumnWidths = table.cachedColumnWidths;
            var optimalWidth = table.Size.x - scrollBar;

            // if (model.AllowHScroll)
            if (true) // this mod always allow horizontal scroll bar.
            {
                float minWidth = 0;
                var columns = table.columns;
                for (int i = 0; i < columns.Count; i++)
                {
                    var column = columns[i];
                    if (!column.ignoreWhenCalculatingOptimalTableSize)
                    {
                        if (i != 0)
                        {
                            var minColumnWidth = table.GetMinWidth(column);
                            cachedColumnWidths[i] = Mathf.Max(cachedColumnWidths[i], minColumnWidth);
                            minWidth += minColumnWidth;
                        }
                        else
                        {
                            var optColumnWidth = table.GetOptimalWidth(column);
                            cachedColumnWidths[i] = Mathf.Max(cachedColumnWidths[i], optColumnWidth);
                            minWidth += optColumnWidth;
                        }
                    }
                }

                if (minWidth > optimalWidth)
                {
                    width = minWidth;
                    __result = false;
                }
                else
                {
                    width = optimalWidth;
                    __result = true;
                }
            }
            // else
            // {
            //     width = optimalWidth;
            //     __result = true;
            // }
            return false;
        }
    }

    [HarmonyPatch("PawnTableGrouped.WorkTabBridge", "ResolvePatches")]
    public static class WorkTabBridge_ResolvePatches
    {
        public static bool Prepare()
        {
            return GroupedPawnsListsPatches.Prepare();
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr)
        {
            var codeInstructions = instr.ToList();
            var added = false;
            for (var i = 0; i < codeInstructions.Count; i++)
            {
                var code = codeInstructions[i];
                if (i == 1)
                {
                    code.opcode = OpCodes.Ldc_I4_5; // change arr size. 4 -> 5
                }
                if (!added && code.opcode == OpCodes.Ldarg_1)
                {
                    added = true;
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_4);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(codeInstructions[6]);
                    yield return new CodeInstruction(codeInstructions[7]);
                    yield return new CodeInstruction(OpCodes.Ldstr, "RecacheSize");
                    yield return new CodeInstruction(codeInstructions[9]);
                    yield return new CodeInstruction(codeInstructions[10]);
                    yield return new CodeInstruction(codeInstructions[11]);
                    yield return new CodeInstruction(codeInstructions[12]);
                    yield return new CodeInstruction(OpCodes.Ldftn, AccessTools.Method(typeof(PawnTable_RecacheSize), nameof(PawnTable_RecacheSize.Prefix)));;
                    yield return new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(PawnTable_RecacheSize_Prefix_Delegate), [typeof(object), typeof(IntPtr)]));
                    yield return new CodeInstruction(codeInstructions[15]);
                    yield return new CodeInstruction(codeInstructions[16]);
                    yield return new CodeInstruction(codeInstructions[17]);
                    yield return new CodeInstruction(codeInstructions[18]);
                    yield return new CodeInstruction(codeInstructions[19]);
                    yield return new CodeInstruction(codeInstructions[20]);
                    yield return new CodeInstruction(codeInstructions[21]);
                }
                yield return code;
            }
        }
    }

    [HarmonyPatch("PawnTableGrouped.PawnTableGroupedModel", "SetGUIDirty")]
    public static class PawnTableGroupedModel_SetGUIDirty
    {
        public static bool Prepare()
        {
            return GroupedPawnsListsPatches.Prepare();
        }

        public static bool Prefix()
        {
            MainTabWindowUtility.NotifyAllPawnTables_PawnsChanged();
            return false;
        }

    }
}