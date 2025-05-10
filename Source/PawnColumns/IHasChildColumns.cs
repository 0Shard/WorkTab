using System.Collections.Generic;
using RimWorld;

namespace WorkTab;

public interface IHasChildColumns
{
    List<PawnColumnDef> ChildColumns { get; set; }
}