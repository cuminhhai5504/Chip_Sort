using UnityEngine;

public class Chip : MonoBehaviour
{
    [SerializeField] private SpriteRenderer chipRenderer;
    [SerializeField] private Rigidbody2D rb;
    private SpriteRenderer trayShadowRenderer;
    private Vector3 originalScale;
    public ChipColor ColorType { get; private set; }

    public bool IsReleased { get; private set; }
    public bool IsInsideMachine { get; private set; }

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void Setup(ChipColor colorType)
    {
        ColorType = colorType;
        chipRenderer.color = GetDisplayColor(colorType);
    }

    public static Color GetDisplayColor(ChipColor colorType)
    {
        return colorType switch
        {
            ChipColor.Green => Color.green,
            ChipColor.Red => Color.red,
            ChipColor.Blue => Color.blue,
            ChipColor.Black => Color.black,
            ChipColor.Purple => Color.magenta,
            ChipColor.Orange => new Color(1f, 0.5f, 0f),
            ChipColor.Yellow => Color.yellow,
            _ => Color.white
        };
    }

    public void SetSortingOrder(int order)
    {
        chipRenderer.sortingOrder = order;

        if (trayShadowRenderer != null)
            trayShadowRenderer.sortingOrder = order - 1;
    }
    #region Add Core Mechanic
    public void Release()
    {
        gameObject.layer =
            LayerMask.NameToLayer("ReleasedChip");

        transform.SetParent(null);

        rb.simulated = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        IsReleased = true;
        IsInsideMachine = false;

        rb.linearVelocity =
            new Vector2(
                Random.Range(-1f, 1f),
                Random.Range(2f, 4f));
    }

    public bool TryCollect()
    {
        if (!IsReleased || IsInsideMachine)
            return false;

        IsInsideMachine = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;
        gameObject.SetActive(false);

        return true;
    }

    public void Dispense(Vector3 position, Vector2 velocity)
    {
        transform.SetParent(null);
        transform.position = position;
        gameObject.SetActive(true);

        gameObject.layer = LayerMask.NameToLayer("ReleasedChip");
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.simulated = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.linearVelocity = velocity;
        rb.angularVelocity = Random.Range(-90f, 90f);

        IsReleased = true;
        IsInsideMachine = false;
    }

    public void PlaceInTray(Vector3 position, int sortingOrder)
    {
        transform.SetParent(null);
        transform.position = position;
        transform.rotation = Quaternion.identity;
        transform.localScale = originalScale;
        gameObject.SetActive(true);

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;

        IsReleased = false;
        IsInsideMachine = false;
        EnsureTrayShadow();
        SetSortingOrder(sortingOrder);
    }

    private void EnsureTrayShadow()
    {
        if (trayShadowRenderer != null)
        {
            trayShadowRenderer.enabled = true;
            return;
        }

        GameObject shadowObject = new GameObject("TrayShadow");
        shadowObject.transform.SetParent(transform, false);
        shadowObject.transform.localPosition = new Vector3(0f, -0.25f, 0f);
        shadowObject.transform.localScale = Vector3.one * 1.02f;

        trayShadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
        trayShadowRenderer.sprite = chipRenderer.sprite;
        trayShadowRenderer.sharedMaterial = chipRenderer.sharedMaterial;
        trayShadowRenderer.color = new Color(0f, 0f, 0f, 0.22f);
        trayShadowRenderer.sortingLayerID = chipRenderer.sortingLayerID;
    }
    #endregion
}
