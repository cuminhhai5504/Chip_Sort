using System.Collections.Generic;
using UnityEngine;

public static class StackMerger
{
    public static void Merge(
        ChipStack source,
        ChipStack target)
    {
        List<Chip> chips =
            source.GetAllChips();

        foreach (Chip chip in chips)
        {
            target.AddChip(chip);
        }

        source.Clear();

        Object.Destroy(source.gameObject);
        #region Add Core Mechanic
        if (target.IsFull())
        {
            target.BreakStack();
        }
        #endregion
    }
}