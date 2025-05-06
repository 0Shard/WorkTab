// VanillaWorkSettings.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using System.Reflection;
using Verse;

namespace WorkTab;

public static class VanillaWorkSettings
{
    public static int GetVanillaPriority(this Pawn pawn, WorkTypeDef worktype)
    {
        if (worktype == null || pawn.NothingCanBeDone())
        {
            return 0;
        }

        var priorities = pawn.workSettings.priorities;

        int priority;
        try
        {
            priority = priorities[worktype];
        }
        catch (ArgumentOutOfRangeException)
        {
            priority = 0;
            Logger.Message($"Priority requested for a workgiver that did not yet exist for {pawn.Name.ToStringShort}. Did you add mods in an existing game?");
        }
        catch (TargetException)
        {
            priority = 0;
            Logger.Message($"Priority requested for a pawn that did not have worksettings ({pawn.Name.ToStringShort})");
        }

        return priority;
    }
}