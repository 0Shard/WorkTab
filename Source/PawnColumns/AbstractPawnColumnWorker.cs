using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTab;

public abstract class AbstractPawnColumnWorker : PawnColumnWorker_WorkPriority, IAlternatingColumn
{
    private string _cachedHeaderTip;

    public Vector2 LabelSize => cachedWorkLabelSize;

    public bool MoveDown
    {
        get => def.moveWorkTypeLabelDown;
        set => def.moveWorkTypeLabelDown = value;
    }

    protected virtual bool ShouldDrawCell(Pawn pawn)
    {
        return !pawn.NothingCanBeDone();
    }

    protected virtual bool IsDoCurrentJob(Pawn pawn) => pawn.CurJob?.workGiverDef?.workType?.defName == def.workType.defName;

    protected void HighlightCurrentJob(Rect rect, Pawn pawn)
    {
        if (!Settings.highlightActiveWorkCells)
        {
            return;
        }

        if (!IsDoCurrentJob(pawn))
        {
            return;
        }

        GUI.color = Color.magenta;
        Widgets.DrawBox(rect);
        GUI.color = Color.white;
    }

    public override void DoHeader(Rect rect, PawnTable table)
    {
        base.DoHeader(rect, table);
        var interactableHeaderRect = GetInteractableHeaderRect(rect, table);
        HeaderInteractions(interactableHeaderRect, table);
    }

    protected virtual string CreateHeaderTip(string tip) {
        if (!_cachedHeaderTip.NullOrEmpty())
        {
            return _cachedHeaderTip;
        }
        if (Find.PlaySettings.useWorkPriorities && !Settings.disableScrollwheel) {
            tip += "\n" + "WorkTab.DetailedColumnTip".Translate().Colorize(ColoredText.SubtleGrayColor);
        } else if (!Settings.disableScrollwheel) {
            tip += "\n" + "WorkTab.ToggleColumnTip".Translate().Colorize(ColoredText.SubtleGrayColor);
        }

        _cachedHeaderTip = tip;
        return _cachedHeaderTip;
    }

    public virtual void InvalidateCache()
    {
        _cachedHeaderTip = null;
    }

    protected virtual void HandleInteractions(Rect rect, Pawn pawn)
    {
        if (Find.PlaySettings.useWorkPriorities)
        {
            HandleInteractionsDetailed(rect, pawn);
        }
        else
        {
            HandleInteractionsToggle(rect, pawn);
        }
    }

    public virtual int GetOptimalHeaderWidth(PawnTable table)
    {
        return GetOptimalWidth(table);
    }

    protected abstract void HeaderInteractions(Rect interactableHeaderRect, PawnTable table, bool clicked = false);

    protected abstract void HandleInteractionsDetailed(Rect rect, Pawn pawn);

    protected abstract void HandleInteractionsToggle(Rect rect, Pawn pawn);
}