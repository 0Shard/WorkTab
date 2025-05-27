// Constants.cs
// Copyright Karel Kroeze, 2017-2020

using System.Collections.Generic;
using UnityEngine;

namespace WorkTab;

public static class Constants {
    public const           int                        ExtraTopSpace           = 40;
    public const           int                        Margin                  = 4;
    public const           float                      MinTimeBarLabelSpacing  = 50f;
    public const           int                        TimeBarHeight           = 40;
    public const           int                        WorkGiverBoxSize        = 20;
    public const           int                        WorkGiverWidth          = 25;
    public const           int                        WorkGiverHeaderWidth    = 50;
    public const           int                        WorkTypeWidth           = 32;
    public const           int                        WorkTypeBoxSize         = 25;
    public static readonly Vector2                    PriorityLabelSize       = new( 160, 30 );
    public static readonly Dictionary<string, string> TruncationCache         = new();
}