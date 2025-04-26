using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace WorkTab.HarmonyPatches.ThirdPart;

public static class CompactWorkTabPatches
{
    private static FieldInfo _headerOrientationFieldInfo;

    private static MethodInfo _labelDrawerDrawLabelMethodInfo;

    private static bool Prepare()
    {
        return ModsConfig.IsActive("Mlie.CompactWorkTab");
    }

    public static bool CanDrawLabel()
    {
        if (!Prepare())
        {
            return false;
        }
        if (_headerOrientationFieldInfo == null)
        {
            _headerOrientationFieldInfo = AccessTools.Field("CompactWorkTab.ModSettings:HeaderOrientation");
        }
        if (_labelDrawerDrawLabelMethodInfo == null)
        {
            _labelDrawerDrawLabelMethodInfo = AccessTools.Method("CompactWorkTab.LabelDrawer:DrawLabel");
        }
        var value = (int) _headerOrientationFieldInfo.GetValue(null);
        // Horizontal
        return value != 2 && _labelDrawerDrawLabelMethodInfo != null;
    }

    public static (Rect transformedRect, Matrix4x4 transformationMatrix) DrawLabel(Rect rect, string label)
    {
        return ((Rect transformedRect, Matrix4x4 transformationMatrix)) _labelDrawerDrawLabelMethodInfo.Invoke(null, [rect, label]);
    }

    [HarmonyPatch("CompactWorkTab.Cache", "Recache")]
    public static class CompactWorkTab_Cache_Recache
    {
        private static FieldInfo _maxPriorityFieldInfo;
        private static FieldInfo _defPriorityFieldInfo;
        private static FieldInfo _minHeaderHeightFieldInfo;

        public static bool Prepare()
        {
            return CompactWorkTabPatches.Prepare();
        }
        
        public static void Postfix()
        {
            if (_maxPriorityFieldInfo == null)
            {
                _maxPriorityFieldInfo = AccessTools.Field("CompactWorkTab.Cache:MaxPriority");
            }

            if (_defPriorityFieldInfo == null)
            {
                _defPriorityFieldInfo = AccessTools.Field("CompactWorkTab.Cache:DefPriority");
            }

            if (_minHeaderHeightFieldInfo == null)
            {
                _minHeaderHeightFieldInfo = AccessTools.Field("CompactWorkTab.Cache:MinHeaderHeight");
            }

            if (_maxPriorityFieldInfo == null || _defPriorityFieldInfo == null || _minHeaderHeightFieldInfo == null)
            {
                return;
            }
        
            _maxPriorityFieldInfo.SetValue(null, Settings.maxPriority);
            _defPriorityFieldInfo.SetValue(null, Settings.defaultPriority);
            _minHeaderHeightFieldInfo.SetValue(null, Constants.HorizontalHeaderHeight);
        }
    }

    [HarmonyPatch("CompactWorkTab.ModSettings", "DoSettingsWindowContents")]
    public static class CompactWorkTab_ModSettings_DoSettingsWindowContents
    {
        public static bool Prepare()
        {
            if (_labelDrawerDrawLabelMethodInfo == null)
            {
                _labelDrawerDrawLabelMethodInfo = AccessTools.Method("CompactWorkTab.LabelDrawer:DrawLabel");
            }
            // PR#2 is merged. see https://github.com/emipa606/CompactWorkTab/pull/2
            return CompactWorkTabPatches.Prepare() && _labelDrawerDrawLabelMethodInfo != null;
        }
        
        public static void DisabledCheckboxLabeledAndTip(Rect firstRow, string label, ref bool useScrollWheel, bool disabled = false,
                                                         Texture2D texChecked = null, Texture2D texUnchecked = null, bool placeCheckboxNearText = false,
                                                         bool paintable = false)
        {
            useScrollWheel = false;
            Widgets.CheckboxLabeled(firstRow, label, ref useScrollWheel, true);
            TooltipHandler.TipRegion(firstRow, "Fluffy.WorkTab.CompactWorkTabSettingDisabledTip".Translate("WorkTab".Translate()));
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var method = AccessTools.Method(typeof(Widgets), nameof(Widgets.CheckboxLabeled));
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(method))
                {
                    instruction.operand = AccessTools.Method(typeof(CompactWorkTab_ModSettings_DoSettingsWindowContents), nameof(DisabledCheckboxLabeledAndTip));
                }

                yield return instruction;
            }
        }
    }
}