using UnityEngine;

public class ChipSpawner : MonoBehaviour
{
    [SerializeField] private Chip chipPrefab;

    private void Start()
    {
        SpawnStack(
            new Vector3(-2, 0, 0),
            ChipColor.Green,
            5);

        SpawnStack(
            new Vector3(0, 0, 0),
            ChipColor.Red,
            4);

        SpawnStack(
            new Vector3(2, 0, 0),
            ChipColor.Blue,
            6);
    }

    private void SpawnStack(
        Vector3 position,
        ChipColor colorType,
        int count)
    {
        for (int i = 0; i < count; i++)
        {
            Chip chip = Instantiate(
                chipPrefab,
                position + new Vector3(0, i * 0.15f, 0),
                Quaternion.identity);

            chip.Setup(colorType);
        }
    }
}