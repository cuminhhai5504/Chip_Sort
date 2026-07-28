using UnityEngine;

public class Chip : MonoBehaviour
{
    [SerializeField] private SpriteRenderer chipRenderer;

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
}