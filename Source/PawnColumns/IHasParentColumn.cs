#nullable enable
using RimWorld;

namespace WorkTab;

public interface IHasParentColumn
{
    PawnColumnDef? ParentColumn { get; set; }
}
