using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class ChipMachineSystem : MonoBehaviour
{
    private const string CollectMachineName = "CollectMachine";
    private const string FillMachineName = "FillMachine";
    private const string TrayResourceName = "Tray_Origin_2";
    private const string TrayLevelFolder = "TrayLevels";
    private const string LevelSettingsFolder = "LevelSettings";
    private const string CurrentLevelKey = "CurrentLevel";
    private const int TrayCount = 5;
    private const int TrayCapacity = 5;
    private const int DisplaySlotCount = 6;
    private const int DisplaySlotCapacity = 4;

    [Header("Output timing")]
    [SerializeField, Min(0f)] private float firstDispenseDelay = 0.5f;
    [SerializeField, Min(0.05f)] private float dispenseInterval = 0.15f;

    [Header("Collection capacity")]
    [SerializeField, Min(1)] private int defaultMaxWaitingChips = 24;
    [SerializeField, Min(0.02f)] private float collectionGateHeight = 0.1f;

    [Header("Buffer display")]
    [SerializeField, Min(0.1f)] private float displayCellHeight = 0.5f;
    [SerializeField, Min(0f)] private float displayCellSpacing = 0.04f;
    [SerializeField, Min(0f)] private float displayHorizontalMargin;
    [SerializeField] private Vector2 displayPositionOffset =
        new Vector2(0f, 0.25f);
    [SerializeField, Min(1f)] private float displayCountFontSize = 4f;
    [SerializeField, Range(0f, 1f)] private float displayCountOutlineWidth =
        0.25f;

    [Header("Output motion")]
    [SerializeField] private float outputOffset = 0.2f;
    [SerializeField, Min(0.05f)] private float fillMoveDuration = 0.25f;

    [Header("Button 2 respawn")]
    [SerializeField, Min(0f)] private float respawnTopMargin = 0.6f;
    [SerializeField, Min(0f)] private float respawnHorizontalSpacing = 0.35f;
    [SerializeField, Min(0f)] private float respawnFallSpeed = 0.5f;

    [Header("Button 3 scatter")]
    [SerializeField, Min(0f)] private float scatterHorizontalSpeed = 5f;
    [SerializeField, Min(0f)] private float scatterUpwardSpeedMin = 14f;
    [SerializeField, Min(0f)] private float scatterUpwardSpeedMax = 18f;
    [SerializeField, Min(0f)] private float scatterAngularSpeed = 240f;

    [Header("Tray layout")]
    [SerializeField] private float trayHeight = 1.25f;
    [SerializeField, Min(1f)] private float trayWidthMultiplier = 1.5f;
    [SerializeField] private float traySpacing = 0.95f;
    [SerializeField] private float trayBottomMargin = 0.05f;
    [SerializeField] private float trayChipSpacing = 0.15f;
    [SerializeField] private float trayChipVerticalOffset = 0.1f;

    [Header("Next tray indicator")]
    [SerializeField, Min(0.05f)] private float nextTrayIndicatorDiameter = 0.2f;
    [SerializeField, Min(0f)] private float nextTrayIndicatorGap = 0.04f;
    [SerializeField, Min(0f)] private float nextTrayIndicatorOutline = 0.025f;

    [Header("Hidden trays")]
    [SerializeField, Min(0.05f)] private float trayPushUpDuration = 0.3f;
    [SerializeField, Min(0f)] private float fullTrayDisappearDelay = 0.15f;

    private readonly List<Chip> storedChips = new();
    private readonly List<Chip> waitingChips = new();
    private readonly List<TrayColumn> trayColumns = new();
    private readonly List<BufferDisplaySlot> displaySlots = new();

    private Transform fillMachine;
    private GameObject trayContainer;
    private GameObject displayContainer;
    private Texture2D displayTexture;
    private Sprite displaySprite;
    private Texture2D nextTrayIndicatorTexture;
    private Sprite nextTrayIndicatorSprite;
    private BoxCollider2D collectionGate;
    private Coroutine dispenseRoutine;
    private bool initialized;
    private int maxWaitingChips;

    public int StoredCount => storedChips.Count;
    public int MaxWaitingChips => maxWaitingChips;

    public void ScatterReleasedChips()
    {
        Chip[] chips = FindObjectsByType<Chip>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        float minUpwardSpeed = Mathf.Min(
            scatterUpwardSpeedMin,
            scatterUpwardSpeedMax);
        float maxUpwardSpeed = Mathf.Max(
            scatterUpwardSpeedMin,
            scatterUpwardSpeedMax);

        for (int i = 0; i < chips.Length; i++)
        {
            Chip chip = chips[i];
            if (!chip.IsReleased || chip.IsInsideMachine)
                continue;

            Vector2 velocity = new Vector2(
                Random.Range(
                    -scatterHorizontalSpeed,
                    scatterHorizontalSpeed),
                Random.Range(minUpwardSpeed, maxUpwardSpeed));
            chip.Scatter(
                velocity,
                Random.Range(-scatterAngularSpeed, scatterAngularSpeed));
            waitingChips.Remove(chip);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<ChipMachineSystem>() != null)
            return;

        GameObject collectMachine = GameObject.Find(CollectMachineName);
        GameObject fillMachine = GameObject.Find(FillMachineName);

        if (collectMachine == null || fillMachine == null)
        {
            Debug.LogWarning(
                $"Chip machines need active GameObjects named '{CollectMachineName}' " +
                $"and '{FillMachineName}'.");
            return;
        }

        collectMachine.AddComponent<ChipMachineSystem>();
    }

    private void Awake()
    {
        if (initialized)
            return;

        GameObject fillObject = GameObject.Find(FillMachineName);
        if (fillObject == null)
        {
            Debug.LogWarning($"Could not find an active '{FillMachineName}' GameObject.", this);
            return;
        }

        Initialize(gameObject, fillObject);
    }

    private void Update()
    {
        HandleClearBufferInput();
    }

    private void Initialize(GameObject collectObject, GameObject fillObject)
    {
        if (initialized)
            return;

        initialized = true;
        fillMachine = fillObject.transform;
        maxWaitingChips = Mathf.Min(
            LoadMaxWaitingChips(),
            DisplaySlotCount * DisplaySlotCapacity);

        BoxCollider2D trigger = collectObject.GetComponent<BoxCollider2D>();
        if (trigger == null)
            trigger = collectObject.AddComponent<BoxCollider2D>();

        trigger.isTrigger = true;
        trigger.size = Vector2.one;
        CreateCollectionGate(collectObject, trigger);

        CreateTrays();
        CreateBufferDisplay();
        RefreshBufferDisplay();
        RefreshCollectionGate();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCollectOrQueue(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryCollectOrQueue(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Chip chip = other.GetComponentInParent<Chip>();
        if (chip != null)
            waitingChips.Remove(chip);
    }

    private void TryCollectOrQueue(Collider2D other)
    {
        Chip chip = other.GetComponentInParent<Chip>();
        if (chip == null || chip.IsInsideMachine)
            return;

        if (StoredCount < maxWaitingChips && CollectChip(chip))
        {
            waitingChips.Remove(chip);
            return;
        }

        if (!waitingChips.Contains(chip))
            waitingChips.Add(chip);
    }

    private bool CollectChip(Chip chip)
    {
        BufferDisplaySlot displaySlot = FindDisplaySlot(chip.ColorType);
        if (displaySlot == null)
            return false;

        if (!chip.TryCollect())
            return false;

        storedChips.Add(chip);
        displaySlot.Add(chip);
        RefreshBufferDisplay();
        RefreshCollectionGate();

        if (dispenseRoutine == null)
            dispenseRoutine = StartCoroutine(DispenseChips());

        return true;
    }

    private void RetryWaitingChips()
    {
        for (int i = waitingChips.Count - 1; i >= 0; i--)
        {
            Chip chip = waitingChips[i];
            if (chip == null || chip.IsInsideMachine)
            {
                waitingChips.RemoveAt(i);
                continue;
            }

            if (StoredCount >= maxWaitingChips)
                continue;

            if (CollectChip(chip))
                waitingChips.RemoveAt(i);
        }
    }

    private void CreateCollectionGate(
        GameObject collectObject,
        BoxCollider2D trigger)
    {
        GameObject gateObject = new GameObject("CollectionCapacityGate");
        gateObject.layer = collectObject.layer;
        gateObject.transform.SetParent(collectObject.transform, false);

        collectionGate = gateObject.AddComponent<BoxCollider2D>();
        collectionGate.isTrigger = false;
        collectionGate.size = new Vector2(
            trigger.size.x,
            collectionGateHeight);
        // Keep a trigger area above the solid gate. This lets the machine
        // inspect the chip before the gate either accepts or blocks it.
        collectionGate.offset = new Vector2(
            trigger.offset.x,
            trigger.offset.y
                + trigger.size.y * 0.5f
                - collectionGateHeight * 1.5f);
        collectionGate.enabled = true;
    }

    private void RefreshCollectionGate()
    {
        if (collectionGate != null)
            collectionGate.enabled = true;
    }

    private int LoadMaxWaitingChips()
    {
        int defaultLevel = SceneManager.GetActiveScene().buildIndex + 1;
        int level = Mathf.Max(1, PlayerPrefs.GetInt(
            CurrentLevelKey,
            defaultLevel));
        string resourcePath = $"{LevelSettingsFolder}/Level_{level}";
        TextAsset csv = Resources.Load<TextAsset>(resourcePath);

        if (csv == null)
        {
            Debug.LogWarning(
                $"Missing Resources/{resourcePath}.csv. Using default " +
                $"MaxWaitingChips = {defaultMaxWaitingChips}.",
                this);
            return defaultMaxWaitingChips;
        }

        string[] rows = csv.text.Split(
            new[] { '\r', '\n' },
            System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < rows.Length; i++)
        {
            string row = rows[i].Trim().TrimStart('\uFEFF');
            if (string.IsNullOrWhiteSpace(row)
                || row.StartsWith("#")
                || row.StartsWith("sep="))
            {
                continue;
            }

            char separator = row.Contains(";") ? ';' : ',';
            string[] cells = row.Split(separator);
            if (cells.Length < 2
                || !cells[0].Trim().Equals(
                    "MaxWaitingChips",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (int.TryParse(cells[1].Trim(), out int capacity)
                && capacity > 0)
            {
                return capacity;
            }

            Debug.LogError(
                $"Invalid MaxWaitingChips in Resources/{resourcePath}.csv. " +
                "The value must be an integer greater than 0.",
                this);
            return defaultMaxWaitingChips;
        }

        Debug.LogError(
            $"Resources/{resourcePath}.csv does not contain a " +
            "MaxWaitingChips row. Using the default value " +
            $"{defaultMaxWaitingChips}.",
            this);
        return defaultMaxWaitingChips;
    }

    private IEnumerator DispenseChips()
    {
        yield return new WaitForSeconds(firstDispenseDelay);

        while (TryTakeChipForExistingTray(
            out Chip chip,
            out TrayInfo tray,
            out int slotIndex))
        {
            Vector3 outputPosition = new Vector3(
                tray.Position.x,
                fillMachine.position.y - outputOffset,
                0f);

            Vector3 slotPosition = tray.GetSlotPosition(
                slotIndex,
                trayChipSpacing);
            slotPosition += Vector3.up * trayChipVerticalOffset;
            chip.PlaceInTray(outputPosition, 100 + slotIndex * 2);

            yield return MoveChipToSlot(chip, outputPosition, slotPosition);

            tray.AddChip(chip);

            if (tray.IsFull)
                yield return ReplaceFullTray(tray);

            yield return new WaitForSeconds(dispenseInterval);
        }

        dispenseRoutine = null;
    }

    private bool TryTakeChipForExistingTray(
        out Chip chip,
        out TrayInfo tray,
        out int slotIndex)
    {
        for (int i = 0; i < storedChips.Count; i++)
        {
            Chip candidate = storedChips[i];

            tray = FindAvailableTray(candidate.ColorType);
            if (tray == null)
                continue;

            storedChips.RemoveAt(i);
            RemoveChipFromDisplay(candidate);
            RefreshBufferDisplay();
            RefreshCollectionGate();
            RetryWaitingChips();
            chip = candidate;
            slotIndex = tray.ReserveSlot();
            return true;
        }

        chip = null;
        tray = null;
        slotIndex = -1;
        return false;
    }

    private TrayInfo FindAvailableTray(ChipColor color)
    {
        for (int i = 0; i < trayColumns.Count; i++)
        {
            TrayInfo tray = trayColumns[i].ActiveTray;
            if (tray != null && tray.Color == color && !tray.IsFull)
                return tray;
        }

        return null;
    }

    private IEnumerator ReplaceFullTray(TrayInfo fullTray)
    {
        if (fullTrayDisappearDelay > 0f)
            yield return new WaitForSeconds(fullTrayDisappearDelay);

        TrayColumn column = fullTray.Column;
        column.RemoveActiveTray();

        if (AreAllTraysReleased())
        {
            if (Board.Instance != null)
                Board.Instance.DeclareWin();

            yield break;
        }

        TrayInfo nextTray = column.ActiveTray;
        if (nextTray == null)
            yield break;

        Vector3 startPosition = nextTray.TrayObject.transform.position;
        Vector3 targetPosition = nextTray.GetTransformPositionAt(
            column.ActivePosition);
        nextTray.TrayObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < trayPushUpDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / trayPushUpDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            nextTray.TrayObject.transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                easedProgress);
            yield return null;
        }

        nextTray.TrayObject.transform.position = targetPosition;
        nextTray.CommitPosition(column.ActivePosition);
        column.MoveHiddenTraysOneLevelUp();
    }

    private bool AreAllTraysReleased()
    {
        for (int i = 0; i < trayColumns.Count; i++)
        {
            if (trayColumns[i].ActiveTray != null)
                return false;
        }

        return true;
    }

    private IEnumerator MoveChipToSlot(
        Chip chip,
        Vector3 startPosition,
        Vector3 targetPosition)
    {
        float elapsed = 0f;

        while (elapsed < fillMoveDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / fillMoveDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            if (chip != null)
            {
                chip.transform.position = Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    easedProgress);
                chip.transform.rotation = Quaternion.identity;
            }

            yield return null;
        }

        if (chip != null)
        {
            chip.transform.position = targetPosition;
            chip.transform.rotation = Quaternion.identity;
        }
    }

    private void CreateTrays()
    {
        if (!TryLoadTrayColors(out List<List<ChipColor>> colorsByColumn))
            return;

        Sprite traySprite = Resources.Load<Sprite>(TrayResourceName);
        if (traySprite == null)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(TrayResourceName);
            if (sprites.Length > 0)
                traySprite = sprites[0];
        }

        if (traySprite == null)
        {
            Debug.LogError(
                $"Could not load Resources/{TrayResourceName}.png as a Sprite.",
                this);
            return;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("The tray system needs a camera tagged MainCamera.", this);
            return;
        }

        trayContainer = new GameObject("ChipTrays");

        float screenBottom = camera.transform.position.y - camera.orthographicSize;
        float trayY = screenBottom + trayBottomMargin + trayHeight * 0.5f;
        float firstX = -(TrayCount - 1) * traySpacing * 0.5f;
        float spriteScale = trayHeight / traySprite.bounds.size.y;

        for (int i = 0; i < TrayCount; i++)
        {
            Vector3 position = new Vector3(firstX + i * traySpacing, trayY, 0f);
            trayColumns.Add(new TrayColumn(position, trayHeight));
        }

        for (int columnIndex = 0; columnIndex < TrayCount; columnIndex++)
        {
            TrayColumn column = trayColumns[columnIndex];
            List<ChipColor> columnColors = colorsByColumn[columnIndex];

            for (int row = 0; row < columnColors.Count; row++)
            {
                ChipColor trayColor = columnColors[row];
                Vector3 position = column.ActivePosition
                    + Vector3.down * (trayHeight * row);
                GameObject trayObject = CreateTrayVisual(
                    traySprite,
                    trayColor,
                    position,
                    spriteScale,
                    columnIndex,
                    row);

                TrayInfo tray = new TrayInfo(
                    trayColor,
                    position,
                    trayObject,
                    column);
                column.AddTray(tray);

                if (row > 0)
                    trayObject.SetActive(false);
            }
        }

        CreateNextTrayIndicators();
    }

    private bool TryLoadTrayColors(
        out List<List<ChipColor>> colorsByColumn)
    {
        colorsByColumn = new List<List<ChipColor>>(TrayCount);
        for (int i = 0; i < TrayCount; i++)
            colorsByColumn.Add(new List<ChipColor>());

        int defaultLevel = SceneManager.GetActiveScene().buildIndex + 1;
        int level = Mathf.Max(1, PlayerPrefs.GetInt(
            CurrentLevelKey,
            defaultLevel));
        string resourcePath = $"{TrayLevelFolder}/Level_{level}";
        TextAsset csv = Resources.Load<TextAsset>(resourcePath);

        if (csv == null)
        {
            Debug.LogError(
                $"Missing tray CSV at Resources/{resourcePath}.csv. " +
                $"Create a CSV with {TrayCount} columns; the first row is " +
                "visible and following rows are hidden trays.",
                this);
            return false;
        }

        string[] rows = csv.text.Split(
            new[] { '\r', '\n' },
            System.StringSplitOptions.RemoveEmptyEntries);
        int csvRow = 0;

        for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            string rowText = rows[rowIndex].Trim().TrimStart('\uFEFF');
            if (string.IsNullOrWhiteSpace(rowText)
                || rowText.StartsWith("#")
                || rowText.StartsWith("sep="))
            {
                continue;
            }

            csvRow++;
            char separator = rowText.Contains(";") ? ';' : ',';
            string[] cells = rowText.Split(separator);

            if (cells.Length > TrayCount)
            {
                Debug.LogError(
                    $"{resourcePath}.csv row {csvRow} has {cells.Length} " +
                    $"columns. Only {TrayCount} columns are allowed.",
                    this);
                return false;
            }

            for (int column = 0; column < cells.Length; column++)
            {
                string value = cells[column].Trim();
                if (string.IsNullOrEmpty(value))
                    continue;

                if (!int.TryParse(value, out int colorNumber)
                    || !TryGetColor(colorNumber, out ChipColor color))
                {
                    Debug.LogError(
                        $"Invalid tray color '{value}' at " +
                        $"{resourcePath}.csv row {csvRow}, " +
                        $"column {column + 1}. Use a number from 1 to 7.",
                        this);
                    return false;
                }

                colorsByColumn[column].Add(color);
            }
        }

        for (int column = 0; column < TrayCount; column++)
        {
            if (colorsByColumn[column].Count > 0)
                continue;

            Debug.LogError(
                $"{resourcePath}.csv column {column + 1} has no trays. " +
                "Every column needs at least one color number.",
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

    private GameObject CreateTrayVisual(
        Sprite traySprite,
        ChipColor trayColor,
        Vector3 position,
        float spriteScale,
        int columnIndex,
        int row)
    {
        GameObject trayObject = new GameObject(
            $"Tray_{columnIndex + 1}_{row + 1}_{trayColor}");
        trayObject.transform.SetParent(trayContainer.transform);
        trayObject.transform.position = position;
        trayObject.transform.localScale = new Vector3(
            spriteScale * trayWidthMultiplier,
            spriteScale,
            1f);

        SpriteRenderer renderer = trayObject.AddComponent<SpriteRenderer>();
        renderer.sprite = traySprite;
        renderer.color = Chip.GetDisplayColor(trayColor);
        renderer.sortingOrder = -10;

        // Keep the tray centered even if the imported sprite uses a custom pivot.
        trayObject.transform.position += position - renderer.bounds.center;
        return trayObject;
    }

    private void CreateNextTrayIndicators()
    {
        nextTrayIndicatorTexture = CreateCircleTexture(64);
        nextTrayIndicatorSprite = Sprite.Create(
            nextTrayIndicatorTexture,
            new Rect(
                0f,
                0f,
                nextTrayIndicatorTexture.width,
                nextTrayIndicatorTexture.height),
            new Vector2(0.5f, 0.5f),
            nextTrayIndicatorTexture.width);
        nextTrayIndicatorSprite.name = "NextTrayIndicatorSprite";

        for (int i = 0; i < trayColumns.Count; i++)
        {
            TrayColumn column = trayColumns[i];
            TrayInfo activeTray = column.ActiveTray;
            if (activeTray == null)
                continue;

            SpriteRenderer trayRenderer =
                activeTray.TrayObject.GetComponent<SpriteRenderer>();
            if (trayRenderer == null)
                continue;

            float radius = nextTrayIndicatorDiameter * 0.5f;
            Vector3 position = new Vector3(
                trayRenderer.bounds.min.x - nextTrayIndicatorGap - radius + 0.2f,
                trayRenderer.bounds.min.y + radius + 0.05f,
                0f);

            GameObject outlineObject = new GameObject(
                $"NextTrayIndicator_{i + 1}");
            outlineObject.transform.SetParent(trayContainer.transform, false);
            outlineObject.transform.position = position;
            outlineObject.transform.localScale = Vector3.one
                * (nextTrayIndicatorDiameter
                    + nextTrayIndicatorOutline * 2f);

            SpriteRenderer outlineRenderer =
                outlineObject.AddComponent<SpriteRenderer>();
            outlineRenderer.sprite = nextTrayIndicatorSprite;
            outlineRenderer.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            outlineRenderer.sortingOrder = 200;

            GameObject colorObject = new GameObject("Color");
            colorObject.transform.SetParent(outlineObject.transform, false);
            float innerScale = nextTrayIndicatorDiameter
                / (nextTrayIndicatorDiameter
                    + nextTrayIndicatorOutline * 2f);
            colorObject.transform.localScale = Vector3.one * innerScale;

            SpriteRenderer colorRenderer =
                colorObject.AddComponent<SpriteRenderer>();
            colorRenderer.sprite = nextTrayIndicatorSprite;
            colorRenderer.sortingOrder = 201;

            column.SetNextTrayIndicator(colorRenderer);
        }
    }

    private static Texture2D CreateCircleTexture(int size)
    {
        Texture2D texture = new Texture2D(
            size,
            size,
            TextureFormat.RGBA32,
            false)
        {
            name = "NextTrayIndicatorTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[size * size];
        float center = (size - 1) * 0.5f;
        float radius = size * 0.5f - 1f;
        float edgeStart = radius - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(
                    new Vector2(x, y),
                    new Vector2(center, center));
                float alpha = 1f - Mathf.Clamp01(distance - edgeStart);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private BufferDisplaySlot FindDisplaySlot(ChipColor color)
    {
        for (int i = 0; i < displaySlots.Count; i++)
        {
            BufferDisplaySlot slot = displaySlots[i];
            if (slot.CanAccept(color))
                return slot;
        }

        for (int i = 0; i < displaySlots.Count; i++)
        {
            if (displaySlots[i].IsEmpty)
                return displaySlots[i];
        }

        return null;
    }

    private void RemoveChipFromDisplay(Chip chip)
    {
        for (int i = 0; i < displaySlots.Count; i++)
        {
            if (displaySlots[i].Remove(chip))
                return;
        }
    }

    private void CreateBufferDisplay()
    {
        displayTexture = new Texture2D(
            1,
            1,
            TextureFormat.RGBA32,
            false)
        {
            name = "ChipBufferDisplayTexture",
            filterMode = FilterMode.Point
        };
        displayTexture.SetPixel(0, 0, Color.white);
        displayTexture.Apply();

        displaySprite = Sprite.Create(
            displayTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        displaySprite.name = "ChipBufferDisplaySprite";

        displayContainer = new GameObject("ChipBufferDisplay");
        Camera camera = Camera.main;
        Vector3 machineMidpoint =
            (transform.position + fillMachine.position) * 0.5f;
        float screenWidth = camera != null && camera.orthographic
            ? camera.orthographicSize * 2f * camera.aspect
            : Mathf.Abs(transform.lossyScale.x);
        float displayCenterX = camera != null
            ? camera.transform.position.x
            : machineMidpoint.x;

        displayContainer.transform.position = new Vector3(
            displayCenterX + displayPositionOffset.x,
            machineMidpoint.y + displayPositionOffset.y,
            0f);

        float usableWidth = Mathf.Max(
            0.1f,
            screenWidth - displayHorizontalMargin * 2f);
        float totalSpacing =
            (DisplaySlotCount - 1) * displayCellSpacing;
        float cellWidth = Mathf.Max(
            0.05f,
            (usableWidth - totalSpacing) / DisplaySlotCount);

        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(
            displayContainer.transform,
            false);
        backgroundObject.transform.localScale = new Vector3(
            screenWidth,
            displayCellHeight + displayCellSpacing * 2f,
            1f);

        SpriteRenderer backgroundRenderer =
            backgroundObject.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = displaySprite;
        backgroundRenderer.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        backgroundRenderer.sortingOrder = 1000;

        float firstX = -(usableWidth - cellWidth) * 0.5f;
        for (int i = 0; i < DisplaySlotCount; i++)
        {
            GameObject cellObject = new GameObject($"BufferCell_{i + 1}");
            cellObject.transform.SetParent(displayContainer.transform, false);
            cellObject.transform.localPosition = new Vector3(
                firstX + i * (cellWidth + displayCellSpacing),
                0f,
                0f);
            cellObject.transform.localScale = new Vector3(
                cellWidth,
                displayCellHeight,
                1f);

            SpriteRenderer cellRenderer =
                cellObject.AddComponent<SpriteRenderer>();
            cellRenderer.sprite = displaySprite;
            cellRenderer.color = Color.white;
            cellRenderer.sortingOrder = 1001;

            GameObject fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(cellObject.transform, false);

            SpriteRenderer fillRenderer =
                fillObject.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = displaySprite;
            fillRenderer.sortingOrder = 1002;
            fillRenderer.enabled = false;

            GameObject textObject = new GameObject(
                "Count",
                typeof(RectTransform),
                typeof(TextMeshPro));
            textObject.transform.SetParent(cellObject.transform, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1f, 1f);
            rect.localScale = new Vector3(
                1f / cellWidth,
                1f / displayCellHeight,
                1f);

            TMP_Text countText = textObject.GetComponent<TextMeshPro>();
            countText.alignment = TextAlignmentOptions.Center;
            countText.fontSize = displayCountFontSize;
            countText.fontStyle = FontStyles.Bold;
            countText.enableAutoSizing = false;
            countText.color = Color.white;
            countText.outlineColor = Color.black;
            countText.outlineWidth = displayCountOutlineWidth;

            MeshRenderer textRenderer =
                textObject.GetComponent<MeshRenderer>();
            if (textRenderer != null)
                textRenderer.sortingOrder = 1003;

            displaySlots.Add(new BufferDisplaySlot(
                cellRenderer,
                fillRenderer,
                countText));
        }
    }

    private void RefreshBufferDisplay()
    {
        for (int i = 0; i < displaySlots.Count; i++)
            displaySlots[i].Refresh();
    }

    private void HandleClearBufferInput()
    {
        if (!Input.GetMouseButtonDown(0)
            || Board.Instance == null
            || !Board.Instance.IsAbilitySelected(2)
            || (EventSystem.current != null
                && EventSystem.current.IsPointerOverGameObject()))
        {
            return;
        }

        Camera camera = Camera.main;
        if (camera == null)
            return;

        Vector3 pointerPosition = camera.ScreenToWorldPoint(Input.mousePosition);
        pointerPosition.z = 0f;

        for (int i = 0; i < displaySlots.Count; i++)
        {
            BufferDisplaySlot slot = displaySlots[i];
            if (!slot.Contains(pointerPosition) || slot.IsEmpty)
                continue;

            RespawnBufferChips(slot.TakeAllChips(), slot.CenterX);
            RefreshBufferDisplay();
            RefreshCollectionGate();
            RetryWaitingChips();
            Board.Instance.TryConsumeSelectedAbility(2);
            return;
        }
    }

    private void RespawnBufferChips(List<Chip> chips, float centerX)
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        float screenHalfWidth = camera.orthographic
            ? camera.orthographicSize * camera.aspect
            : 2.5f;
        float spawnY = camera.transform.position.y
            + camera.orthographicSize
            - respawnTopMargin;
        float totalWidth = (chips.Count - 1) * respawnHorizontalSpacing;

        for (int i = 0; i < chips.Count; i++)
        {
            Chip chip = chips[i];
            if (chip == null)
                continue;

            storedChips.Remove(chip);
            float spawnX = centerX
                - totalWidth * 0.5f
                + i * respawnHorizontalSpacing;
            spawnX = Mathf.Clamp(
                spawnX,
                camera.transform.position.x - screenHalfWidth + 0.5f,
                camera.transform.position.x + screenHalfWidth - 0.5f);

            chip.Dispense(
                new Vector3(spawnX, spawnY, 0f),
                Vector2.down * respawnFallSpeed);
        }
    }

    private void OnDestroy()
    {
        if (displayContainer != null)
            Destroy(displayContainer);

        if (displaySprite != null)
            Destroy(displaySprite);

        if (displayTexture != null)
            Destroy(displayTexture);

        if (nextTrayIndicatorSprite != null)
            Destroy(nextTrayIndicatorSprite);

        if (nextTrayIndicatorTexture != null)
            Destroy(nextTrayIndicatorTexture);

        if (trayContainer != null)
            Destroy(trayContainer);
    }

    private sealed class BufferDisplaySlot
    {
        private readonly List<Chip> chips = new();
        private readonly SpriteRenderer cellRenderer;
        private readonly SpriteRenderer fillRenderer;
        private readonly TMP_Text countText;
        private ChipColor color;

        public bool IsEmpty => chips.Count == 0;
        public float CenterX => cellRenderer.bounds.center.x;

        public BufferDisplaySlot(
            SpriteRenderer cellRenderer,
            SpriteRenderer fillRenderer,
            TMP_Text countText)
        {
            this.cellRenderer = cellRenderer;
            this.fillRenderer = fillRenderer;
            this.countText = countText;
        }

        public bool CanAccept(ChipColor chipColor)
        {
            return !IsEmpty
                && color == chipColor
                && chips.Count < DisplaySlotCapacity;
        }

        public void Add(Chip chip)
        {
            if (IsEmpty)
                color = chip.ColorType;

            chips.Add(chip);
        }

        public bool Remove(Chip chip)
        {
            return chips.Remove(chip);
        }

        public bool Contains(Vector3 worldPosition)
        {
            return cellRenderer.bounds.Contains(worldPosition);
        }

        public List<Chip> TakeAllChips()
        {
            List<Chip> removedChips = new List<Chip>(chips);
            chips.Clear();
            return removedChips;
        }

        public void Refresh()
        {
            if (IsEmpty)
            {
                fillRenderer.enabled = false;
                countText.text = string.Empty;
                return;
            }

            Color displayColor = Chip.GetDisplayColor(color);
            float fillAmount = Mathf.Clamp01(
                chips.Count / (float)DisplaySlotCapacity);

            fillRenderer.enabled = true;
            fillRenderer.color = displayColor;
            fillRenderer.transform.localScale = new Vector3(
                1f,
                fillAmount,
                1f);
            fillRenderer.transform.localPosition = new Vector3(
                0f,
                (fillAmount - 1f) * 0.5f,
                0f);

            countText.text = chips.Count.ToString();
            bool numberIsOverWhiteArea = chips.Count <= 2;
            countText.color = numberIsOverWhiteArea
                ? Color.black
                : Color.white;
            countText.outlineColor = numberIsOverWhiteArea
                ? Color.white
                : Color.black;
        }
    }

    private sealed class TrayInfo
    {
        public ChipColor Color { get; }
        public Vector3 Position { get; private set; }
        public GameObject TrayObject { get; }
        public TrayColumn Column { get; }
        public bool IsFull => filledSlots >= TrayCapacity;

        private readonly List<Chip> chips = new();
        private int filledSlots;

        public TrayInfo(
            ChipColor color,
            Vector3 position,
            GameObject trayObject,
            TrayColumn column)
        {
            Color = color;
            Position = position;
            TrayObject = trayObject;
            Column = column;
        }

        public int ReserveSlot()
        {
            int slotIndex = filledSlots;
            filledSlots++;
            return slotIndex;
        }

        public void AddChip(Chip chip)
        {
            if (chip == null)
                return;

            chip.transform.SetParent(TrayObject.transform, true);
            chips.Add(chip);
        }

        public Vector3 GetTransformPositionAt(Vector3 position)
        {
            return TrayObject.transform.position + (position - Position);
        }

        public void CommitPosition(Vector3 position)
        {
            Position = position;
        }

        public void ShiftPosition(Vector3 offset)
        {
            TrayObject.transform.position += offset;
            Position += offset;
        }

        public void DestroyContents()
        {
            for (int i = 0; i < chips.Count; i++)
            {
                if (chips[i] != null)
                    Object.Destroy(chips[i].gameObject);
            }

            Object.Destroy(TrayObject);
        }

        public Vector3 GetSlotPosition(int slotIndex, float stackOffset)
        {
            float stackHeight = (TrayCapacity - 1) * stackOffset;
            Vector3 backSlot = Position + Vector3.up * (stackHeight * 0.5f);
            return backSlot + Vector3.down * (slotIndex * stackOffset);
        }
    }

    private sealed class TrayColumn
    {
        private readonly List<TrayInfo> trays = new();
        private readonly float hiddenTraySpacing;
        private SpriteRenderer nextTrayIndicator;

        public Vector3 ActivePosition { get; }
        public TrayInfo ActiveTray => trays.Count > 0 ? trays[0] : null;

        public TrayColumn(Vector3 activePosition, float hiddenTraySpacing)
        {
            ActivePosition = activePosition;
            this.hiddenTraySpacing = hiddenTraySpacing;
        }

        public void AddTray(TrayInfo tray)
        {
            trays.Add(tray);
        }

        public void SetNextTrayIndicator(SpriteRenderer indicator)
        {
            nextTrayIndicator = indicator;
            RefreshNextTrayIndicator();
        }

        public void RemoveActiveTray()
        {
            if (trays.Count == 0)
                return;

            TrayInfo completedTray = trays[0];
            trays.RemoveAt(0);
            completedTray.DestroyContents();
            RefreshNextTrayIndicator();
        }

        private void RefreshNextTrayIndicator()
        {
            if (nextTrayIndicator == null)
                return;

            nextTrayIndicator.color = trays.Count > 1
                ? Chip.GetDisplayColor(trays[1].Color)
                : Color.white;
        }

        public void MoveHiddenTraysOneLevelUp()
        {
            for (int i = 1; i < trays.Count; i++)
            {
                TrayInfo tray = trays[i];
                tray.ShiftPosition(Vector3.up * hiddenTraySpacing);
            }
        }
    }
}
