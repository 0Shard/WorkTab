using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace WorkTab.HarmonyPatches.ThirdPart;

[HarmonyPatch("CompactWorkTab.Cache", "Recache")]
public static class CompactWorkTabPatches
{
    private static FieldInfo _maxPriorityFieldInfo;
    private static FieldInfo _defPriorityFieldInfo;
    private static FieldInfo _minHeaderHeightFieldInfo;
    
    private static FieldInfo _headerOrientationFieldInfo;
    
    public static MethodInfo _labelDrawerDrawLabelMethodInfo;
    
    public static bool Prepare()
    {
        return false;
        // return ModsConfig.IsActive("Mlie.CompactWorkTab");
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
        var value = (int) _headerOrientationFieldInfo.GetValue(null);
        return value != 2; // Horizontal
    }

    public static (Rect transformedRect, Matrix4x4 transformationMatrix) DrawLabel(Rect rect, string label)
    {
        if (_labelDrawerDrawLabelMethodInfo == null)
        {
            _labelDrawerDrawLabelMethodInfo = AccessTools.Method("CompactWorkTab.LabelDrawer:DrawLabel");
        }

        return ((Rect transformedRect, Matrix4x4 transformationMatrix)) _labelDrawerDrawLabelMethodInfo.Invoke(null, [rect, label]);
    }
}