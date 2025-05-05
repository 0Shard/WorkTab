// IAlternatingColumn.cs
// Copyright Karel Kroeze, 2017-2020

using System;
using UnityEngine;

namespace WorkTab;

public interface IAlternatingColumn
{
    [Obsolete( "LabelSize is abandoned.")]
    Vector2 LabelSize { get; }

    bool MoveDown { get; set; }
}