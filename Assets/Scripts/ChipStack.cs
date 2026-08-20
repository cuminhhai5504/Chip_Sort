using System.Collections.Generic;
using UnityEngine;

public class ChipStack : MonoBehaviour
{
    [SerializeField] private float chipOffset = 0.15f;
    [SerializeField, Min(0f)] private float burstOriginRadius = 0.08f;
    [SerializeField, Range(0f, 45f)] private float burstAngleJitter = 10f;

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
    public void BreakStack(Vector2 releaseDirection)
    {
        if (chips.Count == 0)
            return;

        Vector3 burstCenter = Vector3.zero;
        for (int i = 0; i < chips.Count; i++)
            burstCenter += chips[i].transform.position;

        burstCenter /= chips.Count;

        Vector2 baseDirection = releaseDirection.sqrMagnitude > 0.0001f
            ? releaseDirection.normalized
            : Vector2.up;
        float baseAngle = Mathf.Atan2(
            baseDirection.y,
            baseDirection.x) * Mathf.Rad2Deg;
        float angleStep = 360f / chips.Count;

        for (int i = 0; i < chips.Count; i++)
        {
            float angle = baseAngle
                + angleStep * i
                + Random.Range(-burstAngleJitter, burstAngleJitter);
            float angleInRadians = angle * Mathf.Deg2Rad;
            Vector2 burstDirection = new Vector2(
                Mathf.Cos(angleInRadians),
                Mathf.Sin(angleInRadians));
            Vector3 startPosition = burstCenter
                + (Vector3)(burstDirection * burstOriginRadius);

            chips[i].Release(burstDirection, startPosition);
        }

        chips.Clear();

        if (CurrentCell != null)
        {
            CurrentCell.ClearStack();
        }

        Destroy(gameObject);
    }

    public void DropStack()
    {
        for (int i = 0; i < chips.Count; i++)
        {
            Chip chip = chips[i];
            chip.Release(Vector2.down, chip.transform.position);
        }

        chips.Clear();

        if (CurrentCell != null)
            CurrentCell.ClearStack();

        Destroy(gameObject);
    }
    #endregion
}
