using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    private List<Cell> cells;

    public IReadOnlyList<Cell> Cells => cells;

    private void Awake()
    {
        cells = new List<Cell>();

        foreach (Transform child in transform)
        {
            Cell cell = child.GetComponent<Cell>();

            if (cell != null)
            {
                cells.Add(cell);
            }
        }
    }
}