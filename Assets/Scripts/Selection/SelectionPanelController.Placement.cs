using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed partial class SelectionPanelController
{
    [Header("Runtime Placement")]
    [SerializeField] private SelectionPlacedItemView placedItemViewPrefab;

    private readonly List<SelectionPlacedItemData> committedPlacements =
        new List<SelectionPlacedItemData>();
    private readonly List<SelectionPlacedItemData> draftPlacements =
        new List<SelectionPlacedItemData>();
    private readonly Dictionary<string, SelectionPlacedItemView> placedItemViews =
        new Dictionary<string, SelectionPlacedItemView>(StringComparer.Ordinal);

    private SelectionCategoryType selectedPlacementCategory;
    private string selectedPlacementItemId;
    private SelectionPlacementToolMode placementToolMode = SelectionPlacementToolMode.Move;
    private Vector2 dragPointerOffset;
    private float dragStartPointerAngle;
    private float dragStartRotation;
    private float dragStartPointerDistance;
    private float dragStartScale;

    public SelectionPlacementToolMode CurrentPlacementToolMode => placementToolMode;

    public SelectionCompositionData GetCommittedComposition()
    {
        return new SelectionCompositionData(
            GetCommittedItemId(SelectionCategoryType.Background),
            committedPlacements);
    }

    public SelectionPlacedItemData[] GetCommittedPlacements()
    {
        return CopyPlacementArray(committedPlacements);
    }

    public bool TryGetCommittedPlacement(
        SelectionCategoryType categoryType,
        string itemId,
        out SelectionPlacedItemData data)
    {
        SelectionPlacedItemData placement = FindPlacement(
            committedPlacements,
            categoryType,
            itemId);
        data = placement?.DeepCopy();
        return data != null;
    }

    public void SetPlacementToolMode(SelectionPlacementToolMode mode)
    {
        placementToolMode = mode;
    }

    public bool DeleteSelectedDraftItem()
    {
        if (submissionInProgress || !HasSelectedPlacement())
        {
            return false;
        }

        SelectionPlacedItemData placement = FindPlacement(
            draftPlacements,
            selectedPlacementCategory,
            selectedPlacementItemId);
        if (placement == null)
        {
            ClearSelectedPlacement();
            return false;
        }

        draftPlacements.Remove(placement);
        int categoryIndex = ToIndex(placement.CategoryType);
        if (TryGetItem(
                placement.CategoryType,
                draftSelections[categoryIndex],
                out SelectionItemDefinition selectedItem)
            && string.Equals(selectedItem.ItemId, placement.ItemId, StringComparison.Ordinal))
        {
            draftSelections[categoryIndex] = -1;
        }

        RemovePlacedItemView(placement.CategoryType, placement.ItemId);
        ClearSelectedPlacement();
        RefreshPlacementSelectionMarks();
        RefreshSubmitButton();
        return true;
    }

    internal void HandlePlacedItemPointerDown(
        SelectionPlacedItemView view,
        PointerEventData eventData)
    {
        if (!IsUsablePlacedView(view))
        {
            return;
        }

        SelectDraftPlacement(view.CategoryType, view.ItemId);
        BringPlacementToFront(view.CategoryType, view.ItemId);
        eventData?.Use();
    }

    internal void HandlePlacedItemBeginDrag(
        SelectionPlacedItemView view,
        PointerEventData eventData)
    {
        if (!IsUsablePlacedView(view) || eventData == null)
        {
            return;
        }

        SelectDraftPlacement(view.CategoryType, view.ItemId);
        BringPlacementToFront(view.CategoryType, view.ItemId);
        RectTransform placementLayer = GetPlacementLayer(view.CategoryType);
        if (placementLayer == null)
        {
            return;
        }

        if (placementToolMode == SelectionPlacementToolMode.Move)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    placementLayer,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 pointerLocal))
            {
                Vector3 localPosition = view.RectTransform.localPosition;
                dragPointerOffset = new Vector2(localPosition.x, localPosition.y) - pointerLocal;
            }
        }
        else if (placementToolMode == SelectionPlacementToolMode.Rotate)
        {
            dragStartPointerAngle = GetPointerAngle(view, eventData);
            SelectionPlacedItemData placement = FindPlacement(
                draftPlacements,
                view.CategoryType,
                view.ItemId);
            dragStartRotation = placement?.RotationDegrees ?? 0f;
        }
        else
        {
            dragStartPointerDistance = Mathf.Max(
                1f,
                GetPointerDistance(view, eventData));
            SelectionPlacedItemData placement = FindPlacement(
                draftPlacements,
                view.CategoryType,
                view.ItemId);
            dragStartScale = placement?.Scale ?? 1f;
        }

        eventData.Use();
    }

    internal void HandlePlacedItemDrag(
        SelectionPlacedItemView view,
        PointerEventData eventData)
    {
        if (!IsUsablePlacedView(view) || eventData == null)
        {
            return;
        }

        SelectionPlacedItemData placement = FindPlacement(
            draftPlacements,
            view.CategoryType,
            view.ItemId);
        if (placement == null)
        {
            return;
        }

        if (placementToolMode == SelectionPlacementToolMode.Move)
        {
            MovePlacementFromPointer(view, placement, eventData);
        }
        else if (placementToolMode == SelectionPlacementToolMode.Rotate)
        {
            float currentPointerAngle = GetPointerAngle(view, eventData);
            float angleDelta = Mathf.DeltaAngle(dragStartPointerAngle, currentPointerAngle);
            placement.SetRotationDegrees(dragStartRotation + angleDelta);
            ApplyPlacementTransform(view, placement);
        }
        else
        {
            float currentPointerDistance = GetPointerDistance(view, eventData);
            placement.SetScale(
                dragStartScale * currentPointerDistance / dragStartPointerDistance);
            ApplyPlacementTransform(view, placement);
        }

        eventData.Use();
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy || submissionInProgress)
        {
            return;
        }

        bool movePressed = false;
        bool rotatePressed = false;
        bool scalePressed = false;
        bool deletePressed = false;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            movePressed = keyboard.wKey.wasPressedThisFrame;
            rotatePressed = keyboard.eKey.wasPressedThisFrame;
            scalePressed = keyboard.rKey.wasPressedThisFrame;
            deletePressed = keyboard.deleteKey.wasPressedThisFrame;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        movePressed = Input.GetKeyDown(KeyCode.W);
        rotatePressed = Input.GetKeyDown(KeyCode.E);
        scalePressed = Input.GetKeyDown(KeyCode.R);
        deletePressed = Input.GetKeyDown(KeyCode.Delete);
#endif

        if (movePressed)
        {
            placementToolMode = SelectionPlacementToolMode.Move;
        }
        else if (rotatePressed)
        {
            placementToolMode = SelectionPlacementToolMode.Rotate;
        }
        else if (scalePressed)
        {
            placementToolMode = SelectionPlacementToolMode.Scale;
        }

        if (deletePressed)
        {
            DeleteSelectedDraftItem();
        }
    }

    private bool PlaceOrSelectItem(
        SelectionCategoryType categoryType,
        int itemIndex,
        SelectionItemDefinition item)
    {
        if (!IsPlacementCategory(categoryType)
            || item == null
            || string.IsNullOrWhiteSpace(item.ItemId))
        {
            return false;
        }

        Sprite sprite = GetPlacementSprite(item);
        if (sprite == null)
        {
            WarnOnce(
                $"Cannot place {categoryType} item '{item.ItemId}' because it has no Preview or Icon Sprite.");
            return false;
        }

        if (placedItemViewPrefab == null)
        {
            WarnOnce("Cannot place an item because the placed item view prefab is not assigned.");
            return false;
        }

        RectTransform placementLayer = GetPlacementLayer(categoryType);
        if (placementLayer == null)
        {
            WarnOnce(
                $"Cannot place {categoryType} item '{item.ItemId}' because the active layout has no placement layer.");
            return false;
        }

        SelectionPlacedItemData placement = FindPlacement(
            draftPlacements,
            categoryType,
            item.ItemId);
        if (placement == null)
        {
            if (categoryType == SelectionCategoryType.Character
                && activeRoundBackground?.SingleCharacterPlacement == true)
            {
                RemoveDraftPlacementsForCategory(SelectionCategoryType.Character);
            }

            placement = new SelectionPlacedItemData(
                categoryType,
                item.ItemId,
                new Vector2(0.5f, 0.5f),
                0f,
                item.InitialDisplayScale,
                GetNextDisplayOrder(categoryType));
            draftPlacements.Add(placement);
        }

        draftSelections[ToIndex(categoryType)] = itemIndex;
        placementToolMode = SelectionPlacementToolMode.Move;
        SelectDraftPlacement(categoryType, item.ItemId);
        BringPlacementToFront(categoryType, item.ItemId);
        RebuildPlacementViews();
        RefreshPlacementSelectionMarks();
        RefreshSubmitButton();
        return true;
    }

    private void RemoveDraftPlacementsForCategory(SelectionCategoryType categoryType)
    {
        for (int index = draftPlacements.Count - 1; index >= 0; index--)
        {
            SelectionPlacedItemData placement = draftPlacements[index];
            if (placement == null || placement.CategoryType != categoryType)
            {
                continue;
            }

            RemovePlacedItemView(placement.CategoryType, placement.ItemId);
            draftPlacements.RemoveAt(index);
        }

        draftSelections[ToIndex(categoryType)] = -1;
        if (selectedPlacementCategory == categoryType)
        {
            ClearSelectedPlacement();
        }
    }

    private void PlaceDefaultCharacterIfNeeded()
    {
        string defaultItemId = activeRoundBackground?.DefaultCharacterItemId;
        if (string.IsNullOrWhiteSpace(defaultItemId)
            || HasValidDraftPlacement(SelectionCategoryType.Character)
            || !IsAllowedDefaultCharacter(defaultItemId)
            || !TryFindItem(
                SelectionCategoryType.Character,
                defaultItemId,
                out int itemIndex,
                out SelectionItemDefinition item)
            || !IsItemAvailable(SelectionCategoryType.Character, item)
            || GetPlacementSprite(item) == null)
        {
            return;
        }

        draftPlacements.Add(new SelectionPlacedItemData(
            SelectionCategoryType.Character,
            defaultItemId,
            new Vector2(0.5f, 0.5f),
            0f,
            item.InitialDisplayScale,
            GetNextDisplayOrder(SelectionCategoryType.Character)));
        draftSelections[ToIndex(SelectionCategoryType.Character)] = itemIndex;
        placementToolMode = SelectionPlacementToolMode.Move;
        SelectDraftPlacement(SelectionCategoryType.Character, defaultItemId);
    }

    private void CopyCommittedPlacementsToDraft()
    {
        CopyPlacements(committedPlacements, draftPlacements);
        RestoreSelectedPlacementFromDraftSelection();
        RebuildPlacementViews();
    }

    private void CommitDraftPlacements()
    {
        CopyPlacements(draftPlacements, committedPlacements);
    }

    private void ResetPlacementComposition()
    {
        committedPlacements.Clear();
        draftPlacements.Clear();
        ClearSelectedPlacement();
        ClearPlacedItemViews();
    }

    private void RestoreCommittedPlacementsToDraft()
    {
        CopyPlacements(committedPlacements, draftPlacements);
        RestoreSelectedPlacementFromDraftSelection();
        RebuildPlacementViews();
    }

    private void PreparePlacementLayers()
    {
        SelectionBackgroundLayoutDefinition[] layouts = CurrentBackgroundLayouts;
        if (layouts == null)
        {
            return;
        }

        foreach (SelectionBackgroundLayoutDefinition layout in layouts)
        {
            DisablePreviewImage(layout?.CharacterPreview);
            DisablePreviewImage(layout?.PropsPreview);
        }
    }

    private static void DisablePreviewImage(Image image)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = null;
        image.raycastTarget = false;
        image.enabled = false;
    }

    private void RebuildPlacementViews()
    {
        PruneInvalidPlacements(draftPlacements);

        List<string> obsoleteKeys = new List<string>();
        foreach (KeyValuePair<string, SelectionPlacedItemView> pair in placedItemViews)
        {
            SelectionPlacedItemView view = pair.Value;
            if (view == null
                || FindPlacement(draftPlacements, view.CategoryType, view.ItemId) == null)
            {
                obsoleteKeys.Add(pair.Key);
            }
        }

        foreach (string key in obsoleteKeys)
        {
            if (placedItemViews.TryGetValue(key, out SelectionPlacedItemView view)
                && view != null)
            {
                view.gameObject.SetActive(false);
                Destroy(view.gameObject);
            }

            placedItemViews.Remove(key);
        }

        if (activeLayout == null || placedItemViewPrefab == null)
        {
            return;
        }

        draftPlacements.Sort(ComparePlacements);
        foreach (SelectionPlacedItemData placement in draftPlacements)
        {
            if (!TryFindItem(
                    placement.CategoryType,
                    placement.ItemId,
                    out _,
                    out SelectionItemDefinition item))
            {
                continue;
            }

            RectTransform placementLayer = GetPlacementLayer(placement.CategoryType);
            Sprite sprite = GetPlacementSprite(item);
            if (placementLayer == null || sprite == null)
            {
                continue;
            }

            string key = GetPlacementKey(placement.CategoryType, placement.ItemId);
            if (!placedItemViews.TryGetValue(key, out SelectionPlacedItemView view)
                || view == null)
            {
                view = Instantiate(placedItemViewPrefab, placementLayer);
                view.name = $"Placed_{placement.CategoryType}_{placement.ItemId}";
                placedItemViews[key] = view;
            }
            else if (view.transform.parent != placementLayer)
            {
                view.transform.SetParent(placementLayer, false);
            }

            view.Configure(
                this,
                placement.CategoryType,
                placement.ItemId,
                sprite,
                item.InitialDisplayScale,
                IsSelectedPlacement(placement.CategoryType, placement.ItemId));
            ApplyPlacementTransform(view, placement);
        }

        ApplyPlacementSiblingOrder(SelectionCategoryType.Character);
        ApplyPlacementSiblingOrder(SelectionCategoryType.Props);
    }

    private void ApplyPlacementSiblingOrder(SelectionCategoryType categoryType)
    {
        List<SelectionPlacedItemData> categoryPlacements = draftPlacements.FindAll(
            placement => placement != null && placement.CategoryType == categoryType);
        categoryPlacements.Sort((left, right) => left.DisplayOrder.CompareTo(right.DisplayOrder));
        for (int index = 0; index < categoryPlacements.Count; index++)
        {
            SelectionPlacedItemData placement = categoryPlacements[index];
            if (placedItemViews.TryGetValue(
                    GetPlacementKey(categoryType, placement.ItemId),
                    out SelectionPlacedItemView view)
                && view != null)
            {
                view.transform.SetSiblingIndex(index);
            }
        }
    }

    private void BringPlacementToFront(
        SelectionCategoryType categoryType,
        string itemId)
    {
        SelectionPlacedItemData selectedPlacement = FindPlacement(
            draftPlacements,
            categoryType,
            itemId);
        if (selectedPlacement == null)
        {
            return;
        }

        List<SelectionPlacedItemData> categoryPlacements = draftPlacements.FindAll(
            placement => placement != null && placement.CategoryType == categoryType);
        categoryPlacements.Sort((left, right) => left.DisplayOrder.CompareTo(right.DisplayOrder));
        categoryPlacements.Remove(selectedPlacement);
        categoryPlacements.Add(selectedPlacement);

        for (int index = 0; index < categoryPlacements.Count; index++)
        {
            categoryPlacements[index].SetDisplayOrder(index);
        }

        ApplyPlacementSiblingOrder(categoryType);
    }

    private void ApplyPlacementTransform(
        SelectionPlacedItemView view,
        SelectionPlacedItemData placement)
    {
        if (view == null || placement == null)
        {
            return;
        }

        RectTransform placementLayer = GetPlacementLayer(placement.CategoryType);
        RectTransform viewRect = view.RectTransform;
        if (placementLayer == null || viewRect == null)
        {
            return;
        }

        Rect layerRect = placementLayer.rect;
        Vector2 normalized = placement.NormalizedPosition;
        Vector2 localPosition = new Vector2(
            Mathf.Lerp(layerRect.xMin, layerRect.xMax, normalized.x),
            Mathf.Lerp(layerRect.yMin, layerRect.yMax, normalized.y));
        viewRect.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
        viewRect.localRotation = Quaternion.Euler(0f, 0f, placement.RotationDegrees);
        viewRect.localScale = Vector3.one * placement.Scale;
    }

    private void MovePlacementFromPointer(
        SelectionPlacedItemView view,
        SelectionPlacedItemData placement,
        PointerEventData eventData)
    {
        RectTransform placementLayer = GetPlacementLayer(placement.CategoryType);
        if (placementLayer == null
            || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                placementLayer,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 pointerLocal))
        {
            return;
        }

        Rect layerRect = placementLayer.rect;
        Vector2 localPosition = pointerLocal + dragPointerOffset;
        localPosition.x = Mathf.Clamp(localPosition.x, layerRect.xMin, layerRect.xMax);
        localPosition.y = Mathf.Clamp(localPosition.y, layerRect.yMin, layerRect.yMax);

        placement.SetNormalizedPosition(new Vector2(
            Mathf.InverseLerp(layerRect.xMin, layerRect.xMax, localPosition.x),
            Mathf.InverseLerp(layerRect.yMin, layerRect.yMax, localPosition.y)));
        ApplyPlacementTransform(view, placement);
    }

    private static float GetPointerAngle(
        SelectionPlacedItemView view,
        PointerEventData eventData)
    {
        Camera camera = eventData.pressEventCamera;
        Vector2 center = RectTransformUtility.WorldToScreenPoint(
            camera,
            view.RectTransform.position);
        Vector2 direction = eventData.position - center;
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    private static float GetPointerDistance(
        SelectionPlacedItemView view,
        PointerEventData eventData)
    {
        Camera camera = eventData.pressEventCamera;
        Vector2 center = RectTransformUtility.WorldToScreenPoint(
            camera,
            view.RectTransform.position);
        return Vector2.Distance(eventData.position, center);
    }

    private RectTransform GetPlacementLayer(SelectionCategoryType categoryType)
    {
        if (activeLayout == null)
        {
            return null;
        }

        switch (categoryType)
        {
            case SelectionCategoryType.Character:
                return activeLayout.CharacterPlacementRoot;
            case SelectionCategoryType.Props:
                return activeLayout.PropsPlacementRoot;
            default:
                return null;
        }
    }

    private void SelectDraftPlacement(
        SelectionCategoryType categoryType,
        string itemId)
    {
        SelectionPlacedItemData placement = FindPlacement(
            draftPlacements,
            categoryType,
            itemId);
        if (placement == null)
        {
            return;
        }

        selectedPlacementCategory = categoryType;
        selectedPlacementItemId = itemId;

        if (TryFindItem(categoryType, itemId, out int itemIndex, out _))
        {
            draftSelections[ToIndex(categoryType)] = itemIndex;
        }

        RefreshPlacementSelectionMarks();
    }

    private void RestoreSelectedPlacementFromDraftSelection()
    {
        ClearSelectedPlacement();
        foreach (SelectionCategoryType categoryType in new[]
                 {
                     SelectionCategoryType.Character,
                     SelectionCategoryType.Props
                 })
        {
            if (TryGetItem(
                    categoryType,
                    draftSelections[ToIndex(categoryType)],
                    out SelectionItemDefinition item)
                && FindPlacement(draftPlacements, categoryType, item.ItemId) != null)
            {
                selectedPlacementCategory = categoryType;
                selectedPlacementItemId = item.ItemId;
            }
        }
    }

    private void RestoreSelectedPlacementForCategory(SelectionCategoryType categoryType)
    {
        if (!IsPlacementCategory(categoryType)
            || !TryGetItem(
                categoryType,
                draftSelections[ToIndex(categoryType)],
                out SelectionItemDefinition item)
            || FindPlacement(draftPlacements, categoryType, item.ItemId) == null)
        {
            ClearSelectedPlacement();
            return;
        }

        selectedPlacementCategory = categoryType;
        selectedPlacementItemId = item.ItemId;
    }

    private void RefreshPlacementSelectionMarks()
    {
        foreach (SelectionPlacedItemView view in placedItemViews.Values)
        {
            if (view != null)
            {
                view.SetSelected(IsSelectedPlacement(view.CategoryType, view.ItemId));
            }
        }

        if (!IsPlacementCategory(currentCategory))
        {
            return;
        }

        for (int viewIndex = 0; viewIndex < itemViews.Count; viewIndex++)
        {
            SelectionItemView itemView = itemViews[viewIndex];
            bool selected = false;
            if (itemView != null
                && itemView.gameObject.activeSelf
                && viewIndex < visibleItemIndices.Count
                && TryGetItem(
                    currentCategory,
                    visibleItemIndices[viewIndex],
                    out SelectionItemDefinition item))
            {
                selected = IsSelectedPlacement(currentCategory, item.ItemId);
            }

            itemView?.SetSelected(selected);
        }
    }

    private bool HasValidDraftPlacement(SelectionCategoryType categoryType)
    {
        foreach (SelectionPlacedItemData placement in draftPlacements)
        {
            if (placement != null
                && placement.CategoryType == categoryType
                && TryFindItem(categoryType, placement.ItemId, out _, out SelectionItemDefinition item)
                && IsItemAvailable(categoryType, item)
                && GetPlacementSprite(item) != null)
            {
                return true;
            }
        }

        return false;
    }

    private void PruneInvalidPlacements(List<SelectionPlacedItemData> placements)
    {
        if (placements == null)
        {
            return;
        }

        for (int index = placements.Count - 1; index >= 0; index--)
        {
            SelectionPlacedItemData placement = placements[index];
            if (placement == null
                || !IsPlacementCategory(placement.CategoryType)
                || !TryFindItem(
                    placement.CategoryType,
                    placement.ItemId,
                    out _,
                    out SelectionItemDefinition item)
                || !IsItemAvailable(placement.CategoryType, item)
                || GetPlacementSprite(item) == null)
            {
                placements.RemoveAt(index);
            }
        }

        if (HasSelectedPlacement()
            && FindPlacement(
                placements,
                selectedPlacementCategory,
                selectedPlacementItemId) == null)
        {
            ClearSelectedPlacement();
        }
    }

    private void PruneUnavailablePlacementComposition()
    {
        PruneInvalidPlacements(committedPlacements);
        PruneInvalidPlacements(draftPlacements);
        RebuildPlacementViews();
    }

    private bool IsUsablePlacedView(SelectionPlacedItemView view)
    {
        return view != null
            && gameObject.activeInHierarchy
            && !submissionInProgress
            && FindPlacement(draftPlacements, view.CategoryType, view.ItemId) != null;
    }

    private bool IsSelectedPlacement(SelectionCategoryType categoryType, string itemId)
    {
        return HasSelectedPlacement()
            && selectedPlacementCategory == categoryType
            && string.Equals(selectedPlacementItemId, itemId, StringComparison.Ordinal);
    }

    private bool HasSelectedPlacement()
    {
        return IsPlacementCategory(selectedPlacementCategory)
            && !string.IsNullOrEmpty(selectedPlacementItemId);
    }

    private void ClearSelectedPlacement()
    {
        selectedPlacementCategory = SelectionCategoryType.Background;
        selectedPlacementItemId = null;
    }

    private int GetNextDisplayOrder(SelectionCategoryType categoryType)
    {
        int nextOrder = 0;
        foreach (SelectionPlacedItemData placement in draftPlacements)
        {
            if (placement != null && placement.CategoryType == categoryType)
            {
                nextOrder = Mathf.Max(nextOrder, placement.DisplayOrder + 1);
            }
        }

        return nextOrder;
    }

    private void RemovePlacedItemView(SelectionCategoryType categoryType, string itemId)
    {
        string key = GetPlacementKey(categoryType, itemId);
        if (!placedItemViews.TryGetValue(key, out SelectionPlacedItemView view))
        {
            return;
        }

        placedItemViews.Remove(key);
        if (view != null)
        {
            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
        }
    }

    private void ClearPlacedItemViews()
    {
        foreach (SelectionPlacedItemView view in placedItemViews.Values)
        {
            if (view != null)
            {
                view.gameObject.SetActive(false);
                Destroy(view.gameObject);
            }
        }

        placedItemViews.Clear();
    }

    private static SelectionPlacedItemData FindPlacement(
        List<SelectionPlacedItemData> placements,
        SelectionCategoryType categoryType,
        string itemId)
    {
        if (placements == null || string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        return placements.Find(placement =>
            placement != null
            && placement.CategoryType == categoryType
            && string.Equals(placement.ItemId, itemId, StringComparison.Ordinal));
    }

    private static void CopyPlacements(
        List<SelectionPlacedItemData> source,
        List<SelectionPlacedItemData> destination)
    {
        destination.Clear();
        if (source == null)
        {
            return;
        }

        foreach (SelectionPlacedItemData placement in source)
        {
            if (placement != null)
            {
                destination.Add(placement.DeepCopy());
            }
        }
    }

    private static SelectionPlacedItemData[] CopyPlacementArray(
        List<SelectionPlacedItemData> source)
    {
        SelectionPlacedItemData[] result = new SelectionPlacedItemData[source.Count];
        for (int index = 0; index < source.Count; index++)
        {
            result[index] = source[index]?.DeepCopy();
        }

        return result;
    }

    private static int ComparePlacements(
        SelectionPlacedItemData left,
        SelectionPlacedItemData right)
    {
        int categoryComparison = left.CategoryType.CompareTo(right.CategoryType);
        return categoryComparison != 0
            ? categoryComparison
            : left.DisplayOrder.CompareTo(right.DisplayOrder);
    }

    private static string GetPlacementKey(
        SelectionCategoryType categoryType,
        string itemId)
    {
        return $"{(int)categoryType}:{itemId}";
    }

    private static Sprite GetPlacementSprite(SelectionItemDefinition item)
    {
        return item?.PreviewSprite != null ? item.PreviewSprite : item?.IconSprite;
    }

    private static bool IsPlacementCategory(SelectionCategoryType categoryType)
    {
        return categoryType == SelectionCategoryType.Character
            || categoryType == SelectionCategoryType.Props;
    }
}
