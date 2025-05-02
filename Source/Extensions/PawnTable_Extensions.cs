using System.Reflection;
using HarmonyLib;
using RimWorld;

namespace WorkTab;

public static class PawnTable_Extensions
{
    private static FieldInfo _fieldInfo;
    
    public static PawnTableDef Def(this PawnTable table)
    {
        if (_fieldInfo == null)
        {
            _fieldInfo = AccessTools.Field(typeof(PawnTable), "def");
        }
        return _fieldInfo.GetValue(table) as PawnTableDef;
    }
}