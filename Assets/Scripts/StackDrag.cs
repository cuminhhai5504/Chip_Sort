using UnityEngine;
using UnityEngine.EventSystems;

public class StackDrag : MonoBehaviour
{
    private static StackDrag activeDrag;

    private Camera cam;

    private Vector3 startPosition;

    private ChipStack stack;

    private bool dragging;

    private void Awake()
    {
        cam = Camera.main;
        stack = GetComponent<ChipStack>();
    }
    private void BeginDrag()
    {
        if (activeDrag != null
            || Board.Instance == null
            || !Board.Instance.IsPlaying)
            return;

        startPosition = transform.position;
        dragging = true;
        activeDrag = this;
    }

    private void EndDrag()
    {
        if (!dragging)
            return;

        dragging = false;
        activeDrag = null;

        TryDrop();
    }

    private void Update()
    {
        if (!dragging)
        {
            if (Input.GetMouseButtonDown(0)
                && (EventSystem.current == null
                    || !EventSystem.current.IsPointerOverGameObject())
                && IsPointerOverStack())
            {
                if (Board.Instance != null && Board.Instance.HasSelectedAbility)
                {
                    Board.Instance.TryUseSelectedAbility(stack);
                    return;
                }

                BeginDrag();
            }

            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            EndDrag();
            return;
        }

        Vector3 mouse =
            cam.ScreenToWorldPoint(Input.mousePosition);

        mouse.z = 0;

        transform.position = mouse;

        stack.RefreshSortingOrder();
    }

    private bool IsPointerOverStack()
    {
        if (cam == null)
            return false;

        Vector3 pointerPosition =
            cam.ScreenToWorldPoint(Input.mousePosition);

        // Query only this stack's layer. Released chips keep their colliders for
        // physics and machine collection, but can no longer consume drag input.
        int stackLayerMask = 1 << gameObject.layer;
        Collider2D hit = Physics2D.OverlapPoint(
            pointerPosition,
            stackLayerMask);

        return hit != null
            && hit.GetComponentInParent<StackDrag>() == this;
    }

    private void OnDisable()
    {
        if (activeDrag == this)
            activeDrag = null;
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
        if (Board.Instance.IsBoardFull)
        {
            Board.Instance.ShowBoardFull(targetCell.transform.position);
            ReturnToStart();
            return;
        }

        ChipStack targetStack =
            targetCell.CurrentStack;

        Cell sourceCell = stack.CurrentCell;
        Vector2 mergeDirection =
            targetCell.transform.position
            - sourceCell.transform.position;

        StackMerger.Merge(stack, targetStack, mergeDirection);

        sourceCell.ClearStack();
        Board.Instance.CheckLoseCondition();
    }
    private void ReturnToStart()
    {
        transform.position = startPosition;

        stack.RefreshSortingOrder();
    }
}
