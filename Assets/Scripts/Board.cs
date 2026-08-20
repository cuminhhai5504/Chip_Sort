using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private TMP_Text boardFullText;
    private Coroutine boardFullRoutine;
    private readonly List<Button> abilityButtons = new();
    private int selectedAbility;

    [Header("Board capacity")]
    [SerializeField, Min(0)] private int maxReleasedChipsForMerge = 15;
    [SerializeField, Min(0.1f)] private float boardFullTextDuration = 1f;
    [SerializeField] private Vector3 boardFullTextOffset =
        new Vector3(0f, 0.65f, 0f);

    public IReadOnlyList<Cell> Cells => cells;
    public GameResult Result { get; private set; } = GameResult.Playing;
    public bool IsPlaying => Result == GameResult.Playing;
    public bool HasSelectedAbility => selectedAbility != 0;
    public bool IsBoardFull => ReleasedChipCount > maxReleasedChipsForMerge;
    public int ReleasedChipCount
    {
        get
        {
            Chip[] chips = FindObjectsByType<Chip>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            int count = 0;

            for (int i = 0; i < chips.Length; i++)
            {
                if (chips[i].IsReleased && !chips[i].IsInsideMachine)
                    count++;
            }

            return count;
        }
    }
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

            if (cell != null && child.gameObject.activeInHierarchy)
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
        CreateAbilityButtons();
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

    public void ShowBoardFull(Vector3 mergePosition)
    {
        if (boardFullText == null)
            CreateBoardFullText();

        if (boardFullRoutine != null)
            StopCoroutine(boardFullRoutine);

        boardFullText.transform.position = mergePosition + boardFullTextOffset;
        boardFullText.gameObject.SetActive(true);
        boardFullRoutine = StartCoroutine(HideBoardFullText());
    }

    public bool TryUseSelectedAbility(ChipStack stack)
    {
        if (!IsPlaying || selectedAbility != 1 || stack == null)
            return false;

        selectedAbility = 0;
        RefreshAbilityButtons();
        stack.DropStack();
        CheckLoseCondition();
        return true;
    }

    public bool IsAbilitySelected(int abilityNumber)
    {
        return selectedAbility == abilityNumber;
    }

    public bool TryConsumeSelectedAbility(int abilityNumber)
    {
        if (selectedAbility != abilityNumber)
            return false;

        selectedAbility = 0;
        RefreshAbilityButtons();
        return true;
    }

    private void CreateAbilityButtons()
    {
        GameObject canvasObject = new GameObject(
            "AbilityCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
        }

        const float buttonWidth = 190f;
        const float buttonSpacing = 20f;
        const int buttonCount = 3;
        float totalWidth = buttonWidth * buttonCount
            + buttonSpacing * (buttonCount - 1);
        float firstX = -(totalWidth - buttonWidth) * 0.5f;

        for (int i = 0; i < buttonCount; i++)
        {
            int abilityNumber = i + 1;
            GameObject buttonObject = new GameObject(
                $"AbilityButton_{abilityNumber}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(canvasObject.transform, false);

            RectTransform buttonRect =
                buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 1f);
            buttonRect.anchorMax = new Vector2(0.5f, 1f);
            buttonRect.pivot = new Vector2(0.5f, 1f);
            buttonRect.sizeDelta = new Vector2(buttonWidth, 90f);
            buttonRect.anchoredPosition = new Vector2(
                firstX + i * (buttonWidth + buttonSpacing),
                -35f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.interactable = abilityNumber <= 3;
            button.onClick.AddListener(() => SelectAbility(abilityNumber));
            abilityButtons.Add(button);

            GameObject labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label =
                labelObject.GetComponent<TextMeshProUGUI>();
            label.text = $"Button {abilityNumber}";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 30f;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        RefreshAbilityButtons();
    }

    private void SelectAbility(int abilityNumber)
    {
        if (abilityNumber < 1 || abilityNumber > 3 || !IsPlaying)
            return;

        if (abilityNumber == 3)
        {
            ChipMachineSystem machine =
                FindFirstObjectByType<ChipMachineSystem>();
            if (machine != null)
                machine.ScatterReleasedChips();

            selectedAbility = 0;
            RefreshAbilityButtons();
            return;
        }

        selectedAbility = selectedAbility == abilityNumber
            ? 0
            : abilityNumber;
        RefreshAbilityButtons();
    }

    private void RefreshAbilityButtons()
    {
        for (int i = 0; i < abilityButtons.Count; i++)
        {
            Image image = abilityButtons[i].GetComponent<Image>();
            if (image == null)
                continue;

            image.color = selectedAbility == i + 1
                ? new Color(0.2f, 0.75f, 0.3f, 1f)
                : new Color(0.2f, 0.2f, 0.2f, 0.95f);
        }
    }

    private void CreateBoardFullText()
    {
        GameObject textObject = new GameObject(
            "BoardFullText",
            typeof(RectTransform),
            typeof(TextMeshPro));
        textObject.transform.SetParent(transform, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(4f, 1f);

        boardFullText = textObject.GetComponent<TextMeshPro>();
        boardFullText.text = "Board full";
        boardFullText.alignment = TextAlignmentOptions.Center;
        boardFullText.fontSize = 4f;
        boardFullText.fontStyle = FontStyles.Bold;
        boardFullText.color = new Color(1f, 0.2f, 0.2f);
        boardFullText.outlineColor = Color.white;
        boardFullText.outlineWidth = 0.2f;

        MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sortingOrder = 2000;

        textObject.SetActive(false);
    }

    private IEnumerator HideBoardFullText()
    {
        yield return new WaitForSeconds(boardFullTextDuration);

        if (boardFullText != null)
            boardFullText.gameObject.SetActive(false);

        boardFullRoutine = null;
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
