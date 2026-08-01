using System.Collections.Generic;
using UnityEngine;

public class ChipStack : MonoBehaviour
{
    [SerializeField] private float chipOffset = 0.15f;

    private readonly List<Chip> chips = new();

    public IReadOnlyList<Chip> Chips => chips;

    public void AddChip(Chip chip)
    {
        chips.Add(chip);

        chip.transform.SetParent(transform);

        chip.transform.localPosition =
            new Vector3(0, (chips.Count - 1) * chipOffset, 0);

        RefreshSortingOrder();
    }

    public void RefreshSortingOrder()
    {
        int baseOrder =
            Mathf.RoundToInt(-transform.position.y * 100);

        for (int i = 0; i < chips.Count; i++)
        {
            chips[i].SetSortingOrder(baseOrder + i);
        }
    }

    public Chip GetTopChip()
    {
        if (chips.Count == 0)
            return null;

        return chips[chips.Count - 1];
    }
    public List<Chip> GetAllChips()
    {
        return new List<Chip>(chips);
    }

    public int Count => chips.Count;

    public void Clear()
    {
        chips.Clear();
    }
    public Cell CurrentCell { get; private set; }

    public void SetCell(Cell cell)
    {
        CurrentCell = cell;
    }
    #region Add Core Mechanic
    public bool IsFull()
    {
        return chips.Count >= 5;
    }
    public void BreakStack()
    {
        foreach (Chip chip in chips)
        {
            chip.Release();
        }

        chips.Clear();

        if (CurrentCell != null)
        {
            CurrentCell.ClearStack();
        }

        Destroy(gameObject);
    }
    #endregion
}