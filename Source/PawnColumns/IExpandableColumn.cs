// IExpandableColumn.cs
// Copyright Karel Kroeze, 2017-2020

namespace WorkTab;

public interface IExpandableColumn
{
    bool CanExpand { get; }

    bool NeedExpand { get; }

    bool NeedCollapse { get; }

    bool Expanded { get; set; }
}