using System.Reflection;
using HarmonyLib;
using Verse;

namespace WorkTab.HarmonyPatches.ThirdPart;

public static class HardworkingKzPatches
{
    public static bool Prepare()
    {
        return ModsConfig.IsActive("Moo.Hardworking.Kz");
    }

    [HarmonyPatch(typeof(Pawn_Extensions), nameof(Pawn_Extensions.Humanlike))]
    public static class HardworkingKzPatches_Pawn_Extensions_Humanlike
    {
        private static MethodInfo _isHardworkingPawnMethod;
        
        public static bool Prepare()
        {
            return HardworkingKzPatches.Prepare();
        }
        
        public static void Postfix(Pawn pawn, ref bool __result)
        {
            __result = __result || IsHardworkingPawnMethod(pawn);
        }

        private static bool IsHardworkingPawnMethod(Pawn pawn)
        {
            if (_isHardworkingPawnMethod == null)
            {
                _isHardworkingPawnMethod = AccessTools.Method("Kz.HardworkingUtility:IsHardworkingPawn", [typeof(Pawn)]);
            }
            return (bool)_isHardworkingPawnMethod.Invoke(null, [pawn]);
        }
    }

}