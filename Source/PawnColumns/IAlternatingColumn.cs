// IAlternatingColumn.cs
// Copyright Karel Kroeze, 2017-2020

using System;
using UnityEngine;

namespace WorkTab;

public interface IAlternatingColumn
{
    [Obsolete( "LabelSize is abandoned. Use PawnColumnWorker_WorkPriority.cachedWorkLabelSize instead.")]
    Vector2 LabelSize { get; }

    [Obsolete( "MoveDown is abandoned. Use PawnColumnDef.moveWorkTypeLabelDown instead.")]
    bool MoveDown { get; set; }
}