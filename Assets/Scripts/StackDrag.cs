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
        if (Board.Instance == null || !Board.Instance.IsPlaying)
            return;

        startPosition = transform.position;

        dragging = true;
    }

    private void OnMouseUp()
    {
        if (!dragging)
            return;

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
            ReturnToStart();
            return;
        }

        MergeIntoCell(targetCell);
    }

    
    private void MergeIntoCell(Cell targetCell)
    {
        ChipStack targetStack =
            targetCell.CurrentStack;

        Cell sourceCell = stack.CurrentCell;

        StackMerger.Merge(stack, targetStack);

        sourceCell.ClearStack();
        Board.Instance.CheckLoseCondition();
    }
    private void ReturnToStart()
    {
        transform.position = startPosition;

        stack.RefreshSortingOrder();
    }
}
