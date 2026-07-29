using UnityEngine;

public class StackDrag : MonoBehaviour
{
    private Camera cam;

    private Vector3 startPosition;

    private ChipStack stack;

    private bool dragging;

    private void Awake()
    {
        cam = Camera.main;
        stack = GetComponent<ChipStack>();
    }
    private void OnMouseDown()
    {
        Debug.Log("Down");
        startPosition = transform.position;

        dragging = true;
    }

    private void OnMouseUp()
    {
        Debug.Log("Up");
        dragging = false;

        TryDrop();
    }

    private void Update()
    {
        if (!dragging)
            return;

        Vector3 mouse =
            cam.ScreenToWorldPoint(Input.mousePosition);

        mouse.z = 0;

        transform.position = mouse;

        stack.RefreshSortingOrder();
    }

    private void TryDrop()
    {
        Cell targetCell =
            Board.Instance.GetNearestCell(transform.position);

        if (targetCell == null)
        {
            ReturnToStart();
            return;
        }

        if (targetCell == stack.CurrentCell)
        {
            ReturnToStart();
            return;
        }

        if (targetCell.IsEmpty)
        {
            MoveToEmptyCell(targetCell);
        }
        else
        {
            MergeIntoCell(targetCell);
        }
    }

    private void MergeInto(ChipStack target)
    {
        StackMerger.Merge(stack, target);
    }
    private void MoveToEmptyCell(Cell targetCell)
    {
        stack.CurrentCell.ClearStack();

        targetCell.SetStack(stack);

        stack.SetCell(targetCell);

        transform.position =
            targetCell.StackAnchor.position;
    }
    private void MergeIntoCell(Cell targetCell)
    {
        ChipStack targetStack =
            targetCell.CurrentStack;

        StackMerger.Merge(stack, targetStack);

        stack.CurrentCell.ClearStack();
    }
    private void ReturnToStart()
    {
        transform.position = startPosition;

        stack.RefreshSortingOrder();
    }
}