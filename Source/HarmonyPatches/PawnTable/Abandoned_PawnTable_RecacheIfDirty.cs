// PawnTable_RecacheIfDirty.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using HarmonyLib;
using RimWorld;

namespace WorkTab;

[Obsolete("PawnTable_RecacheIfDirty is abandoned. No more need.")]
[HarmonyPatch(typeof(PawnTable), "RecacheIfDirty")]
public class PawnTable_RecacheIfDirty
{
    // HIGHLIGHT: Don't change this method. 'Grouped Pawns Lists' mod need.
    private static void Postfix(PawnTable __instance, bool __state)
    {
    }

    // HIGHLIGHT: Don't change this method. 'Grouped Pawns Lists' mod need.
    private static void Prefix(PawnTable __instance, ref bool __state, PawnTableDef ___def)
    {
    }
}