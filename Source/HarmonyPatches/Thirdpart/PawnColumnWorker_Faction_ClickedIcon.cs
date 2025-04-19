using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkTab.RelationTab
{
    [HarmonyPatch(typeof(PawnColumnWorker_Faction), "ClickedIcon")]
    public static class PawnColumnWorker_Faction_ClickedIcon
    {
        public static bool Prepare()
        {
            return ModsConfig.IsActive("Fluffy.FluffyRelations");
        }
        
        public static bool Prefix(Pawn pawn)
        {
            var homeFaction = pawn.HomeFaction;
            if (homeFaction == null)
            {
                return false;
            }
            
            var mainButtonDef = RimWorld.MainButtonDefOf.Factions;
            var instance = mainButtonDef.TabWindow;
                
            var prop = AccessTools.Property(mainButtonDef.tabWindowClass, "CurrentPage");
            var enumValue = Enum.ToObject(prop.PropertyType, 1);
            prop.SetValue(instance, enumValue);
            
            Find.MainTabsRoot.SetCurrentTab(mainButtonDef);
            AccessTools.Property(mainButtonDef.tabWindowClass, "SelectedFaction").SetValue(instance, homeFaction);
            return false;
        }
    }
}