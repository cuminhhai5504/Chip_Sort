using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public static Board Instance { get; private set; }

    private List<Cell> cells = new();

    public IReadOnlyList<Cell> Cells => cells;

    private void Awake()
    {
        Instance = this;

        foreach (Transform child in transform)
        {
            Cell cell = child.GetComponent<Cell>();

            if (cell != null)
            {
                cells.Add(cell);
            }
        }
    }

    public Cell GetNearestCell(
    Vector3 worldPos,
    float maxDistance = 1f)
    {
        Cell nearest = null;

        float minDistance = maxDistance;

        foreach (Cell cell in cells)
        {
            float distance =
                Vector2.Distance(
                    worldPos,
                    cell.transform.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = cell;
            }
        }

        return nearest;
    }
}