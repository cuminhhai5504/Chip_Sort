using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum GameResult
{
    Playing,
    Won,
    Lost
}

public class Board : MonoBehaviour
{
    public static Board Instance { get; private set; }

    private List<Cell> cells = new();
    private TMP_Text resultText;

    public IReadOnlyList<Cell> Cells => cells;
    public GameResult Result { get; private set; } = GameResult.Playing;
    public bool IsPlaying => Result == GameResult.Playing;
    public int ActiveStackCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < cells.Count; i++)
            {
                if (!cells[i].IsEmpty)
                    count++;
            }

            return count;
        }
    }

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

        // CSV rows run top-to-bottom and columns run left-to-right.
        cells.Sort((left, right) =>
        {
            int verticalOrder = right.transform.position.y.CompareTo(
                left.transform.position.y);
            if (verticalOrder != 0)
                return verticalOrder;

            return left.transform.position.x.CompareTo(
                right.transform.position.x);
        });

        CreateResultDisplay();
    }

    public void CheckLoseCondition()
    {
        if (IsPlaying && ActiveStackCount == 1)
            FinishGame(GameResult.Lost);
    }

    public void DeclareWin()
    {
        if (IsPlaying)
            FinishGame(GameResult.Won);
    }

    private void FinishGame(GameResult result)
    {
        Result = result;

        if (resultText != null)
        {
            resultText.text = result == GameResult.Won
                ? "YOU WIN!"
                : "YOU LOSE!";
            resultText.color = result == GameResult.Won
                ? new Color(0.2f, 1f, 0.35f)
                : new Color(1f, 0.2f, 0.2f);
            resultText.gameObject.SetActive(true);
        }

        Debug.Log(result == GameResult.Won
            ? "All trays released. You win!"
            : "Only one stack remains. You lose!",
            this);
    }

    private void CreateResultDisplay()
    {
        GameObject canvasObject = new GameObject(
            "GameResultCanvas",
            typeof(Canvas),
            typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject textObject = new GameObject(
            "GameResultText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        resultText = textObject.GetComponent<TextMeshProUGUI>();
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.fontSize = 84f;
        resultText.fontStyle = FontStyles.Bold;
        resultText.raycastTarget = false;
        textObject.SetActive(false);
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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
