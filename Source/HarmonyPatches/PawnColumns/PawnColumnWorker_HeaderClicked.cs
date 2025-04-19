using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace WorkTab;

[HarmonyPatch(typeof(PawnColumnWorker), "HeaderClicked")]
public static class PawnColumnWorker_HeaderClicked
{
    
    public static bool Prefix()
    {
        return !Event.current.control;
    }
}