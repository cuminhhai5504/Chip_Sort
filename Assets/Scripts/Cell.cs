using UnityEngine;

public class Cell : MonoBehaviour
{
    [SerializeField] private Transform stackAnchor;

    public Transform StackAnchor => stackAnchor;

    public ChipStack CurrentStack { get; private set; }

    public bool IsEmpty => CurrentStack == null;

    public void SetStack(ChipStack stack)
    {
        CurrentStack = stack;
    }

    public void ClearStack()
    {
        CurrentStack = null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one);
    }
}