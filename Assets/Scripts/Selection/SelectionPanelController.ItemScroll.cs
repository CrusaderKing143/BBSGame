using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class SelectionPanelController
{
    private const int VisibleItemRowCount = 5;
    private const string ItemScrollContentName = "ItemScrollContent";

    private readonly Dictionary<Transform, ItemScrollRuntime> itemScrollRuntimes =
        new Dictionary<Transform, ItemScrollRuntime>();

    private sealed class ItemScrollRuntime
    {
        public RectTransform Viewport;
        public RectTransform Content;
        public GridLayoutGroup Grid;
        public ScrollRect ScrollRect;
        public float ViewportHeight;
    }

    private Transform GetActiveItemViewParent()
    {
        Transform itemRoot = activeLayout?.ItemRoot;
        ItemScrollRuntime runtime = EnsureItemScrollRuntime(itemRoot);
        return runtime?.Content != null ? runtime.Content : itemRoot;
    }

    private ItemScrollRuntime EnsureItemScrollRuntime(Transform itemRoot)
    {
        if (itemRoot == null)
        {
            return null;
        }

        if (itemScrollRuntimes.TryGetValue(itemRoot, out ItemScrollRuntime existing)
            && existing?.Viewport != null
            && existing.Content != null
            && existing.Grid != null
            && existing.ScrollRect != null)
        {
            return existing;
        }

        if (!(itemRoot is RectTransform viewport))
        {
            WarnOnce($"ItemRoot '{itemRoot.name}' must use RectTransform to support scrolling.");
            return null;
        }

        GridLayoutGroup sourceGrid = itemRoot.GetComponent<GridLayoutGroup>();
        if (sourceGrid == null)
        {
            WarnOnce($"ItemRoot '{itemRoot.name}' has no GridLayoutGroup; scrolling was not created.");
            return null;
        }

        float viewportHeight = CalculateGridHeight(sourceGrid, VisibleItemRowCount);
        ResizeViewportKeepingTop(viewport, viewportHeight);

        if (itemRoot.GetComponent<RectMask2D>() == null)
        {
            itemRoot.gameObject.AddComponent<RectMask2D>();
        }

        Image raycastSurface = itemRoot.GetComponent<Image>();
        if (raycastSurface == null)
        {
            raycastSurface = itemRoot.gameObject.AddComponent<Image>();
            raycastSurface.color = Color.clear;
        }

        raycastSurface.raycastTarget = true;

        ScrollRect scrollRect = itemRoot.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = itemRoot.gameObject.AddComponent<ScrollRect>();
        }

        GameObject contentObject = new GameObject(
            ItemScrollContentName,
            typeof(RectTransform),
            typeof(GridLayoutGroup));
        contentObject.layer = itemRoot.gameObject.layer;

        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.SetParent(itemRoot, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, viewportHeight);

        GridLayoutGroup contentGrid = contentObject.GetComponent<GridLayoutGroup>();
        CopyGridConfiguration(sourceGrid, contentGrid);

        List<Transform> existingItemViews = new List<Transform>();
        for (int childIndex = 0; childIndex < itemRoot.childCount; childIndex++)
        {
            Transform child = itemRoot.GetChild(childIndex);
            if (child != content && child.GetComponent<SelectionItemView>() != null)
            {
                existingItemViews.Add(child);
            }
        }

        foreach (Transform itemView in existingItemViews)
        {
            itemView.SetParent(content, false);
        }

        sourceGrid.enabled = false;

        scrollRect.content = content;
        scrollRect.viewport = viewport;
        scrollRect.horizontal = false;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;
        scrollRect.scrollSensitivity = 40f;
        scrollRect.horizontalScrollbar = null;
        scrollRect.verticalScrollbar = null;

        ItemScrollRuntime runtime = new ItemScrollRuntime
        {
            Viewport = viewport,
            Content = content,
            Grid = contentGrid,
            ScrollRect = scrollRect,
            ViewportHeight = viewportHeight
        };
        itemScrollRuntimes[itemRoot] = runtime;
        ResetItemScrollToTop(runtime);
        return runtime;
    }

    private void RefreshActiveItemScroll(int visibleItemCount)
    {
        ItemScrollRuntime runtime = EnsureItemScrollRuntime(activeLayout?.ItemRoot);
        if (runtime == null)
        {
            return;
        }

        int columnCount = GetGridColumnCount(runtime.Grid, runtime.Viewport.rect.width);
        int rowCount = visibleItemCount > 0
            ? Mathf.CeilToInt(visibleItemCount / (float)columnCount)
            : 0;
        float contentHeight = Mathf.Max(
            runtime.ViewportHeight,
            CalculateGridHeight(runtime.Grid, rowCount));

        runtime.Content.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            contentHeight);
        LayoutRebuilder.ForceRebuildLayoutImmediate(runtime.Content);
        runtime.ScrollRect.vertical = contentHeight > runtime.ViewportHeight + 0.01f;
        ResetItemScrollToTop(runtime);
    }

    private static void ResetItemScrollToTop(ItemScrollRuntime runtime)
    {
        if (runtime?.ScrollRect == null || runtime.Content == null)
        {
            return;
        }

        runtime.ScrollRect.StopMovement();
        Canvas.ForceUpdateCanvases();
        runtime.ScrollRect.verticalNormalizedPosition = 1f;
        runtime.Content.anchoredPosition = new Vector2(
            runtime.Content.anchoredPosition.x,
            0f);
    }

    private static float CalculateGridHeight(GridLayoutGroup grid, int rowCount)
    {
        if (grid == null)
        {
            return 0f;
        }

        int safeRowCount = Mathf.Max(0, rowCount);
        float rowsHeight = safeRowCount * grid.cellSize.y;
        float spacingHeight = Mathf.Max(0, safeRowCount - 1) * grid.spacing.y;
        return grid.padding.top + rowsHeight + spacingHeight + grid.padding.bottom;
    }

    private static int GetGridColumnCount(GridLayoutGroup grid, float viewportWidth)
    {
        if (grid == null)
        {
            return 1;
        }

        if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
        {
            return Mathf.Max(1, grid.constraintCount);
        }

        float usableWidth = Mathf.Max(
            0f,
            viewportWidth - grid.padding.left - grid.padding.right);
        float cellStride = grid.cellSize.x + grid.spacing.x;
        if (cellStride <= 0f)
        {
            return 1;
        }

        return Mathf.Max(
            1,
            Mathf.FloorToInt((usableWidth + grid.spacing.x) / cellStride));
    }

    private static void ResizeViewportKeepingTop(
        RectTransform viewport,
        float targetHeight)
    {
        float currentHeight = viewport.rect.height;
        float topPosition = viewport.anchoredPosition.y
            + (1f - viewport.pivot.y) * currentHeight;

        viewport.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            targetHeight);

        Vector2 anchoredPosition = viewport.anchoredPosition;
        anchoredPosition.y = topPosition
            - (1f - viewport.pivot.y) * targetHeight;
        viewport.anchoredPosition = anchoredPosition;
    }

    private static void CopyGridConfiguration(
        GridLayoutGroup source,
        GridLayoutGroup destination)
    {
        destination.padding = new RectOffset(
            source.padding.left,
            source.padding.right,
            source.padding.top,
            source.padding.bottom);
        destination.childAlignment = source.childAlignment;
        destination.startCorner = source.startCorner;
        destination.startAxis = source.startAxis;
        destination.cellSize = source.cellSize;
        destination.spacing = source.spacing;
        destination.constraint = source.constraint;
        destination.constraintCount = source.constraintCount;
    }
}
