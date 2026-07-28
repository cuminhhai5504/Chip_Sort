using System;
using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Board board;
    [SerializeField] private ChipStack stackPrefab;
    [SerializeField] private Chip chipPrefab;

    [Header("Generation")]
    [SerializeField] private int minChipPerStack = 1;
    [SerializeField] private int maxChipPerStack = 4;

    private void Start()
    {
        GenerateBoard();
    }

    private void GenerateBoard()
    {
        foreach (Cell cell in board.Cells)
        {
            SpawnStack(cell);
        }
    }

    private void SpawnStack(Cell cell)
    {
        ChipStack stack = Instantiate(
            stackPrefab,
            cell.StackAnchor.position,
            Quaternion.identity);

        cell.SetStack(stack);

        int chipCount =
            UnityEngine.Random.Range(
                minChipPerStack,
                maxChipPerStack + 1);

        for (int i = 0; i < chipCount; i++)
        {
            Chip chip = Instantiate(chipPrefab);

            ChipColor randomColor =
                (ChipColor)UnityEngine.Random.Range(
                    0,
                    Enum.GetValues(typeof(ChipColor)).Length);

            chip.Setup(randomColor);

            stack.AddChip(chip);
        }
    }
}