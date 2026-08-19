using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BoardGenerator : MonoBehaviour
{
    private const string StackLevelFolder = "StackLevels";
    private const string CurrentLevelKey = "CurrentLevel";
    private const int BoardColumnCount = 5;

    [Header("References")]
    [SerializeField] private Board board;
    [SerializeField] private ChipStack stackPrefab;
    [SerializeField] private Chip chipPrefab;

    private void Start()
    {
        GenerateBoard();
    }

    private void GenerateBoard()
    {
        if (!TryLoadStackColors(out List<List<ChipColor>> stackColors))
            return;

        for (int i = 0; i < board.Cells.Count; i++)
        {
            if (stackColors[i].Count > 0)
                SpawnStack(board.Cells[i], stackColors[i]);
        }

        board.CheckLoseCondition();
    }

    private void SpawnStack(Cell cell, List<ChipColor> colors)
    {
        ChipStack stack = Instantiate(
            stackPrefab,
            cell.StackAnchor.position,
            Quaternion.identity);

        cell.SetStack(stack);
        stack.SetCell(cell);

        // Colors are listed bottom-to-top inside each CSV cell.
        for (int i = 0; i < colors.Count; i++)
        {
            Chip chip = Instantiate(chipPrefab);
            chip.Setup(colors[i]);
            stack.AddChip(chip);
        }
    }

    private bool TryLoadStackColors(
        out List<List<ChipColor>> stackColors)
    {
        stackColors = new List<List<ChipColor>>(board.Cells.Count);
        for (int i = 0; i < board.Cells.Count; i++)
            stackColors.Add(new List<ChipColor>());

        int defaultLevel = SceneManager.GetActiveScene().buildIndex + 1;
        int level = Mathf.Max(1, PlayerPrefs.GetInt(
            CurrentLevelKey,
            defaultLevel));
        string resourcePath = $"{StackLevelFolder}/Level_{level}";
        TextAsset csv = Resources.Load<TextAsset>(resourcePath);

        if (csv == null)
        {
            Debug.LogError(
                $"Missing stack CSV at Resources/{resourcePath}.csv. " +
                $"Use {BoardColumnCount} stacks separated by ';' per row " +
                "and write each stack bottom-to-top, for example " +
                "1,3,2,5;2,4;7;1,6,3;5,2,4.",
                this);
            return false;
        }

        string[] rows = csv.text.Split(new[] { '\r', '\n' });
        int boardRow = 0;

        for (int sourceRow = 0; sourceRow < rows.Length; sourceRow++)
        {
            string rowText = rows[sourceRow].Trim().TrimStart('\uFEFF');
            if (string.IsNullOrWhiteSpace(rowText)
                || rowText.StartsWith("#")
                || rowText.StartsWith("sep="))
            {
                continue;
            }

            int firstCellIndex = boardRow * BoardColumnCount;
            if (firstCellIndex >= board.Cells.Count)
            {
                Debug.LogError(
                    $"{resourcePath}.csv has more board rows than the " +
                    $"scene's {board.Cells.Count} cells can hold.",
                    this);
                return false;
            }

            string[] cells = rowText.Split(';');
            if (cells.Length > BoardColumnCount)
            {
                Debug.LogError(
                    $"{resourcePath}.csv row {boardRow + 1} has " +
                    $"{cells.Length} columns. Only {BoardColumnCount} " +
                    "columns are allowed.",
                    this);
                return false;
            }

            for (int column = 0; column < BoardColumnCount; column++)
            {
                int cellIndex = firstCellIndex + column;
                if (cellIndex >= board.Cells.Count)
                    break;

                string stackText = column < cells.Length
                    ? cells[column].Trim()
                    : string.Empty;
                if (string.IsNullOrEmpty(stackText))
                {
                    Debug.LogError(
                        $"Empty stack at {resourcePath}.csv row " +
                        $"{boardRow + 1}, column {column + 1}. " +
                        "Every stack must contain 1 to 4 chips.",
                        this);
                    return false;
                }

                string[] chipValues = stackText.Split(',');
                if (chipValues.Length < 1 || chipValues.Length > 4)
                {
                    Debug.LogError(
                        $"Stack at {resourcePath}.csv row " +
                        $"{boardRow + 1}, column {column + 1} contains " +
                        $"{chipValues.Length} chips. Each stack must " +
                        "contain 1 to 4 chips.",
                        this);
                    return false;
                }

                for (int height = 0; height < chipValues.Length; height++)
                {
                    string value = chipValues[height].Trim();
                    if (!int.TryParse(value, out int colorNumber)
                        || !TryGetColor(colorNumber, out ChipColor color))
                    {
                        Debug.LogError(
                            $"Invalid chip color '{value}' at " +
                            $"{resourcePath}.csv row {boardRow + 1}, " +
                            $"column {column + 1}, height {height + 1}. " +
                            "Use a number from 1 to 7.",
                            this);
                        return false;
                    }

                    stackColors[cellIndex].Add(color);
                }
            }

            boardRow++;
        }

        int requiredRows = Mathf.CeilToInt(
            board.Cells.Count / (float)BoardColumnCount);
        if (boardRow != requiredRows)
        {
            Debug.LogError(
                $"{resourcePath}.csv has {boardRow} board rows, but the " +
                $"scene needs {requiredRows} rows for {board.Cells.Count} " +
                "cells.",
                this);
            return false;
        }

        return true;
    }

    private static bool TryGetColor(int colorNumber, out ChipColor color)
    {
        switch (colorNumber)
        {
            case 1: color = ChipColor.Green; return true;
            case 2: color = ChipColor.Red; return true;
            case 3: color = ChipColor.Blue; return true;
            case 4: color = ChipColor.Black; return true;
            case 5: color = ChipColor.Purple; return true;
            case 6: color = ChipColor.Orange; return true;
            case 7: color = ChipColor.Yellow; return true;
            default:
                color = default;
                return false;
        }
    }
}
