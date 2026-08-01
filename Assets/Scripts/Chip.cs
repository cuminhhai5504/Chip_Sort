using UnityEngine;

public class Chip : MonoBehaviour
{
    [SerializeField] private SpriteRenderer chipRenderer;
    [SerializeField] private Rigidbody2D rb;
    public ChipColor ColorType { get; private set; }

    public void Setup(ChipColor colorType)
    {
        ColorType = colorType;

        chipRenderer.color = colorType switch
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
    }
    #region Add Core Mechanic
    public void Release()
    {
        gameObject.layer =
            LayerMask.NameToLayer("ReleasedChip");

        transform.SetParent(null);

        rb.simulated = true;
        rb.bodyType = RigidbodyType2D.Dynamic;

        rb.linearVelocity =
            new Vector2(
                Random.Range(-1f, 1f),
                Random.Range(2f, 4f));
    }
    #endregion
}