using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum SelectionCategoryType
{
    Character = 0,
    Background = 1,
    Props = 2
}

[Serializable]
public sealed class SelectionItemDefinition
{
    [SerializeField] private string itemId;
    [SerializeField] private Sprite iconSprite;
    [SerializeField] private Sprite previewSprite;
    [SerializeField] private bool unlockedByDefault;
    [SerializeField] private float initialDisplayScale = 1f;

    public string ItemId => itemId;
    public Sprite IconSprite => iconSprite;
    public Sprite PreviewSprite => previewSprite;
    public bool UnlockedByDefault => unlockedByDefault;
    public float InitialDisplayScale => initialDisplayScale > 0f ? initialDisplayScale : 1f;
}

[Serializable]
public sealed class SelectionCategoryDefinition
{
    [SerializeField] private SelectionCategoryType categoryType;
    [SerializeField] private SelectionItemDefinition[] items;

    public SelectionCategoryType CategoryType => categoryType;
    public SelectionItemDefinition[] Items => items;
}

[Serializable]
public sealed class SelectionBackgroundLayoutDefinition
{
    [SerializeField] private GameObject layoutRoot;
    [SerializeField] private Button backButton;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button characterButton;
    [SerializeField] private Button backgroundButton;
    [SerializeField] private Button propsButton;
    [SerializeField] private Transform itemRoot;
    [SerializeField] private Image characterPreview;
    [SerializeField] private Image propsPreview;
    [SerializeField] private RectTransform captureRoot;
    [SerializeField] private GameObject selectionFrame;
    [SerializeField] private GameObject bottomNavigation;
    [SerializeField] private Sprite itemBackgroundSprite;

    public GameObject LayoutRoot => layoutRoot;
    public Button BackButton => backButton;
    public Button SubmitButton => submitButton;
    public Transform ItemRoot => itemRoot;
    public Image CharacterPreview => characterPreview;
    public Image PropsPreview => propsPreview;
    public RectTransform CharacterPlacementRoot => characterPreview?.rectTransform;
    public RectTransform PropsPlacementRoot => propsPreview?.rectTransform;
    public RectTransform CaptureRoot => captureRoot;
    public GameObject SelectionFrame => selectionFrame;
    public GameObject BottomNavigation => bottomNavigation;
    public Sprite ItemBackgroundSprite => itemBackgroundSprite;

    public Button GetCategoryButton(SelectionCategoryType categoryType)
    {
        switch (categoryType)
        {
            case SelectionCategoryType.Character:
                return characterButton;
            case SelectionCategoryType.Background:
                return backgroundButton;
            case SelectionCategoryType.Props:
                return propsButton;
            default:
                return null;
        }
    }
}

[Serializable]
public sealed class SelectionRoundBackgroundDefinition
{
    [SerializeField, Min(0)] private int roundIndex;
    [SerializeField] private SelectionItemDefinition[] items;
    [SerializeField] private SelectionBackgroundLayoutDefinition[] layouts;
    [SerializeField] private SelectionCategoryType[] requiredCategoriesOverride;
    [SerializeField] private string[] allowedCharacterItemIds;
    [SerializeField] private bool singleCharacterPlacement;
    [SerializeField] private string defaultCharacterItemId;

    public int RoundIndex => roundIndex;
    public SelectionItemDefinition[] Items => items;
    public SelectionBackgroundLayoutDefinition[] Layouts => layouts;
    public SelectionCategoryType[] RequiredCategoriesOverride => requiredCategoriesOverride;
    public string[] AllowedCharacterItemIds => allowedCharacterItemIds;
    public bool SingleCharacterPlacement => singleCharacterPlacement;
    public string DefaultCharacterItemId => defaultCharacterItemId;
}

public sealed partial class SelectionPanelController : MonoBehaviour
{
    private const int CategoryCount = 3;

    private void Start()
    {
        OpenPanel();
    }

    [Header("Items")]
    [SerializeField] private SelectionItemView itemViewPrefab;
    [SerializeField] private SelectionCategoryDefinition[] categories;

    [Header("Background Layouts")]
    [SerializeField] private SelectionBackgroundLayoutDefinition[] backgroundLayouts;
    [SerializeField] private SelectionRoundBackgroundDefinition[] roundBackgrounds;

    [Header("Selection Rules")]
    [SerializeField] private SelectionCategoryType[] requiredCategories =
    {
        SelectionCategoryType.Character,
        SelectionCategoryType.Background,
        SelectionCategoryType.Props
    };
    [SerializeField] private SelectionCategoryType initialCategory = SelectionCategoryType.Character;

    [Header("Events")]
    [SerializeField] private UnityEvent onSubmitted = new UnityEvent();

    private readonly Dictionary<SelectionCategoryType, SelectionCategoryDefinition> categoryLookup =
        new Dictionary<SelectionCategoryType, SelectionCategoryDefinition>();

    private readonly Dictionary<Button, UnityAction> buttonActions =
        new Dictionary<Button, UnityAction>();

    private readonly List<SelectionItemView> itemViews = new List<SelectionItemView>();
    private readonly List<int> visibleItemIndices = new List<int>();
    private readonly Dictionary<SelectionCategoryType, HashSet<string>> collectedItemIds =
        new Dictionary<SelectionCategoryType, HashSet<string>>();
    private readonly HashSet<string> loggedWarnings = new HashSet<string>();
    private readonly int[] committedSelections = { -1, -1, -1 };
    private readonly int[] draftSelections = { -1, -1, -1 };

    private SelectionCategoryType currentCategory = SelectionCategoryType.Character;
    private SelectionBackgroundLayoutDefinition activeLayout;
    private SelectionRoundBackgroundDefinition activeRoundBackground;
    private int configuredRoundIndex = -1;
    private bool configurationValid;

    public UnityEvent OnSubmitted => onSubmitted;

    public bool CollectItem(SelectionCategoryType categoryType, string itemId)
    {
        RebuildCategoryLookup(false);
        if (!IsCollectibleCategory(categoryType))
        {
            WarnOnce($"Only Character and Props items can be collected. Category: {categoryType}.");
            return false;
        }

        if (!TryFindItem(categoryType, itemId, out _, out _))
        {
            WarnOnce($"Collectible item '{itemId}' was not found in category {categoryType}.");
            return false;
        }

        if (!collectedItemIds.TryGetValue(categoryType, out HashSet<string> itemIds))
        {
            itemIds = new HashSet<string>(StringComparer.Ordinal);
            collectedItemIds.Add(categoryType, itemIds);
        }

        bool collected = itemIds.Add(itemId);
        if (collected && gameObject.activeInHierarchy && currentCategory == categoryType)
        {
            ShowCategory(currentCategory);
        }

        return collected;
    }

    public void ResetCollectedItems()
    {
        collectedItemIds.Clear();
        RebuildCategoryLookup(false);
        ClampSelections(committedSelections);
        ClampSelections(draftSelections);
        PruneUnavailablePlacementComposition();

        if (gameObject.activeInHierarchy)
        {
            ApplyAllPreviews();
            ShowCategory(currentCategory);
            RefreshSubmitButton();
        }
    }

    public bool IsItemCollected(SelectionCategoryType categoryType, string itemId)
    {
        return collectedItemIds.TryGetValue(categoryType, out HashSet<string> itemIds)
            && !string.IsNullOrEmpty(itemId)
            && itemIds.Contains(itemId);
    }

    public bool IsItemAvailable(SelectionCategoryType categoryType, string itemId)
    {
        RebuildCategoryLookup(false);
        return TryFindItem(categoryType, itemId, out _, out SelectionItemDefinition item)
            && IsItemAvailable(categoryType, item);
    }

    public bool TryGetItemIcon(
        SelectionCategoryType categoryType,
        string itemId,
        out Sprite iconSprite)
    {
        iconSprite = null;
        RebuildCategoryLookup(false);
        if (!TryFindItem(categoryType, itemId, out _, out SelectionItemDefinition item)
            || item.IconSprite == null)
        {
            return false;
        }

        iconSprite = item.IconSprite;
        return true;
    }

    public bool IsConfigurationValid()
    {
        configurationValid = RebuildCategoryLookup(false);
        return configurationValid;
    }

    public void ConfigureForRound(int roundIndex)
    {
        int safeRoundIndex = Mathf.Max(0, roundIndex);
        SelectionRoundBackgroundDefinition nextBackground = FindRoundBackground(safeRoundIndex);
        if (configuredRoundIndex == safeRoundIndex
            && ReferenceEquals(activeRoundBackground, nextBackground))
        {
            return;
        }

        CancelPendingSubmission();
        UnbindButtons();
        DeactivateEveryBackgroundLayout();

        configuredRoundIndex = safeRoundIndex;
        activeRoundBackground = nextBackground;
        FillSelections(committedSelections, -1);
        FillSelections(draftSelections, -1);
        ResetPlacementComposition();
        ClearCommittedSnapshot();
        currentCategory = initialCategory;
        configurationValid = RebuildCategoryLookup(false);
        PreparePlacementLayers();
        BindButtons();
        HideAllItemViews();
        RefreshSubmitButton();
    }

    private SelectionRoundBackgroundDefinition FindRoundBackground(int roundIndex)
    {
        if (roundBackgrounds == null)
        {
            return null;
        }

        SelectionRoundBackgroundDefinition result = null;
        foreach (SelectionRoundBackgroundDefinition definition in roundBackgrounds)
        {
            if (definition == null || definition.RoundIndex != roundIndex)
            {
                continue;
            }

            if (result != null)
            {
                WarnOnce($"Round {roundIndex} has more than one Background definition; the first one is used.");
                break;
            }

            result = definition;
        }

        return result;
    }

    private void DeactivateEveryBackgroundLayout()
    {
        HashSet<GameObject> roots = new HashSet<GameObject>();
        DeactivateBackgroundLayouts(backgroundLayouts, roots);

        if (roundBackgrounds == null)
        {
            return;
        }

        foreach (SelectionRoundBackgroundDefinition definition in roundBackgrounds)
        {
            DeactivateBackgroundLayouts(definition?.Layouts, roots);
        }
    }

    private static void DeactivateBackgroundLayouts(
        SelectionBackgroundLayoutDefinition[] layouts,
        HashSet<GameObject> roots)
    {
        if (layouts == null)
        {
            return;
        }

        foreach (SelectionBackgroundLayoutDefinition layout in layouts)
        {
            GameObject root = layout?.LayoutRoot;
            if (root != null && roots.Add(root))
            {
                root.SetActive(false);
            }
        }
    }

    public void OpenPanel()
    {
        gameObject.SetActive(true);
        PrepareRuntime(true);
        CopySelections(committedSelections, draftSelections);
        CopyCommittedPlacementsToDraft();
        SelectDefaultBackgroundIfNeeded();
        PlaceDefaultCharacterIfNeeded();
        currentCategory = categoryLookup.ContainsKey(initialCategory)
            ? initialCategory
            : SelectionCategoryType.Character;
        RestoreSelectedPlacementForCategory(currentCategory);
        ApplyAllPreviews();
        ShowCategory(currentCategory);
        RefreshSubmitButton();
    }

    public void CancelAndClose()
    {
        if (submissionInProgress)
        {
            return;
        }

        PrepareRuntime(false);
        CopySelections(committedSelections, draftSelections);
        RestoreCommittedPlacementsToDraft();
        ApplyAllPreviews();
        gameObject.SetActive(false);
    }

    public void ClosePanel()
    {
        CancelAndClose();
    }

    public void Submit()
    {
        if (submissionInProgress)
        {
            return;
        }

        PrepareRuntime(true);
        if (!CanSubmit())
        {
            WarnOnce("Submit blocked because every required category must contain one valid selection.");
            RefreshSubmitButton();
            return;
        }

        CopySelections(draftSelections, committedSelections);
        CommitDraftPlacements();
        BeginSnapshotSubmission(activeLayout?.CaptureRoot);
    }

    public int GetCommittedSelectionIndex(SelectionCategoryType categoryType)
    {
        if (!IsKnownCategory(categoryType))
        {
            return -1;
        }

        return committedSelections[ToIndex(categoryType)];
    }

    public string GetCommittedItemId(SelectionCategoryType categoryType)
    {
        int selectedIndex = GetCommittedSelectionIndex(categoryType);
        if (!TryGetItem(categoryType, selectedIndex, out SelectionItemDefinition item)
            || !IsItemAvailable(categoryType, item))
        {
            return string.Empty;
        }

        return item.ItemId ?? string.Empty;
    }

    public void ResetSelections()
    {
        CancelPendingSubmission();
        FillSelections(committedSelections, -1);
        FillSelections(draftSelections, -1);
        ResetPlacementComposition();
        ClearCommittedSnapshot();
        ApplyAllPreviews();
        ShowCategory(currentCategory);
        RefreshSubmitButton();
    }

    private void OnDestroy()
    {
        CancelPendingSubmission();
        UnbindButtons();

        foreach (SelectionItemView itemView in itemViews)
        {
            itemView?.Clear();
        }

        ClearPlacedItemViews();
        ClearCommittedSnapshot();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ValidateSerializedConfiguration();
        }
    }

    private void PrepareRuntime(bool logWarnings)
    {
        configurationValid = RebuildCategoryLookup(logWarnings);
        ClampSelections(committedSelections);
        ClampSelections(draftSelections);
        PruneInvalidPlacements(committedPlacements);
        PruneInvalidPlacements(draftPlacements);
        PreparePlacementLayers();
        BindButtons();
    }

    private SelectionBackgroundLayoutDefinition[] CurrentBackgroundLayouts =>
        activeRoundBackground?.Layouts ?? backgroundLayouts;

    private SelectionCategoryType[] CurrentRequiredCategories
    {
        get
        {
            SelectionCategoryType[] overrideCategories =
                activeRoundBackground?.RequiredCategoriesOverride;
            return overrideCategories != null && overrideCategories.Length > 0
                ? overrideCategories
                : requiredCategories;
        }
    }

    private SelectionItemDefinition[] GetCategoryItems(SelectionCategoryDefinition category)
    {
        if (category?.CategoryType == SelectionCategoryType.Background
            && activeRoundBackground != null)
        {
            return activeRoundBackground.Items;
        }

        return category?.Items;
    }

    private bool RebuildCategoryLookup(bool logWarnings)
    {
        categoryLookup.Clear();
        bool valid = true;

        valid &= RequireReference(itemViewPrefab, "Item view prefab is not assigned.", logWarnings);
        valid &= RequireReference(
            placedItemViewPrefab,
            "Placed item view prefab is not assigned.",
            logWarnings);

        if (!IsKnownCategory(initialCategory))
        {
            valid = false;
            WarnIfRequested($"Initial category {initialCategory} is invalid.", logWarnings);
        }

        SelectionCategoryType[] currentRequiredCategories = CurrentRequiredCategories;
        if (currentRequiredCategories == null || currentRequiredCategories.Length == 0)
        {
            valid = false;
            WarnIfRequested("At least one required category must be configured.", logWarnings);
        }
        else
        {
            HashSet<SelectionCategoryType> requiredCategoryTypes = new HashSet<SelectionCategoryType>();
            foreach (SelectionCategoryType requiredCategory in currentRequiredCategories)
            {
                if (!IsKnownCategory(requiredCategory) || !requiredCategoryTypes.Add(requiredCategory))
                {
                    valid = false;
                    WarnIfRequested($"Required category {requiredCategory} is invalid or duplicated.", logWarnings);
                }
            }
        }

        if (categories == null)
        {
            WarnIfRequested("Categories are not assigned.", logWarnings);
            return false;
        }

        foreach (SelectionCategoryDefinition category in categories)
        {
            if (category == null)
            {
                valid = false;
                WarnIfRequested("A category entry is null.", logWarnings);
                continue;
            }

            if (categoryLookup.ContainsKey(category.CategoryType))
            {
                valid = false;
                WarnIfRequested($"Category {category.CategoryType} is configured more than once.", logWarnings);
                continue;
            }

            categoryLookup.Add(category.CategoryType, category);

            SelectionItemDefinition[] items = GetCategoryItems(category);
            if (items == null || items.Length == 0)
            {
                valid = false;
                WarnIfRequested($"Category {category.CategoryType} has no items.", logWarnings);
                continue;
            }

            if (category.CategoryType == SelectionCategoryType.Background
                && (items.Length < 1 || items.Length > 2))
            {
                valid = false;
                WarnIfRequested("Background must contain one or two items.", logWarnings);
            }

            HashSet<string> itemIds = new HashSet<string>(StringComparer.Ordinal);
            for (int itemIndex = 0; itemIndex < items.Length; itemIndex++)
            {
                SelectionItemDefinition item = items[itemIndex];
                if (item == null)
                {
                    valid = false;
                    WarnIfRequested($"Category {category.CategoryType} item {itemIndex} is null.", logWarnings);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.ItemId))
                {
                    valid = false;
                    WarnIfRequested($"Category {category.CategoryType} item {itemIndex} has no stable ID.", logWarnings);
                }
                else if (!itemIds.Add(item.ItemId))
                {
                    valid = false;
                    WarnIfRequested($"Category {category.CategoryType} contains duplicate item ID '{item.ItemId}'.", logWarnings);
                }

                if (item.IconSprite == null)
                {
                    valid = false;
                    WarnIfRequested($"Category {category.CategoryType} item '{item.ItemId}' has no icon Sprite.", logWarnings);
                }

                if (IsPlacementCategory(category.CategoryType)
                    && item.PreviewSprite == null
                    && item.IconSprite == null)
                {
                    valid = false;
                    WarnIfRequested(
                        $"Category {category.CategoryType} item '{item.ItemId}' has no Sprite for placement.",
                        logWarnings);
                }
            }
        }

        for (int categoryIndex = 0; categoryIndex < CategoryCount; categoryIndex++)
        {
            SelectionCategoryType categoryType = (SelectionCategoryType)categoryIndex;
            if (!categoryLookup.ContainsKey(categoryType))
            {
                valid = false;
                WarnIfRequested($"Required category {categoryType} is missing.", logWarnings);
            }
        }

        valid &= ValidateCurrentRoundCharacterRules(logWarnings);
        valid &= ValidateBackgroundLayouts(logWarnings);

        return valid;
    }

    private bool ValidateBackgroundLayouts(bool logWarnings)
    {
        if (!categoryLookup.TryGetValue(
                SelectionCategoryType.Background,
                out SelectionCategoryDefinition backgroundCategory)
            || GetCategoryItems(backgroundCategory) == null)
        {
            return false;
        }

        SelectionBackgroundLayoutDefinition[] layouts = CurrentBackgroundLayouts;
        int backgroundCount = GetCategoryItems(backgroundCategory).Length;
        if (layouts == null || layouts.Length != backgroundCount)
        {
            WarnIfRequested(
                "Background layout count must match the Background item count.",
                logWarnings);
            return false;
        }

        bool valid = true;
        HashSet<GameObject> layoutRoots = new HashSet<GameObject>();
        HashSet<Transform> itemRoots = new HashSet<Transform>();
        HashSet<Button> buttons = new HashSet<Button>();
        HashSet<Image> placementLayers = new HashSet<Image>();

        for (int layoutIndex = 0; layoutIndex < layouts.Length; layoutIndex++)
        {
            SelectionBackgroundLayoutDefinition layout = layouts[layoutIndex];
            if (layout == null)
            {
                valid = false;
                WarnIfRequested($"Background layout {layoutIndex} is null.", logWarnings);
                continue;
            }

            valid &= ValidateLayoutRoot(layout, layoutIndex, layoutRoots, logWarnings);
            valid &= ValidateUniqueReference(
                layout.ItemRoot,
                itemRoots,
                $"Background layout {layoutIndex} ItemRoot is missing or duplicated.",
                logWarnings);
            valid &= ValidateUniqueReference(
                layout.CharacterPreview,
                placementLayers,
                $"Background layout {layoutIndex} Character preview is not assigned.",
                logWarnings);
            valid &= ValidateUniqueReference(
                layout.PropsPreview,
                placementLayers,
                $"Background layout {layoutIndex} Props preview is not assigned.",
                logWarnings);
            valid &= ValidateLayoutButton(
                layout.BackButton,
                buttons,
                layoutIndex,
                "Back",
                logWarnings);
            valid &= ValidateLayoutButton(
                layout.SubmitButton,
                buttons,
                layoutIndex,
                "Post",
                logWarnings);

            for (int categoryIndex = 0; categoryIndex < CategoryCount; categoryIndex++)
            {
                SelectionCategoryType categoryType = (SelectionCategoryType)categoryIndex;
                valid &= ValidateLayoutButton(
                    layout.GetCategoryButton(categoryType),
                    buttons,
                    layoutIndex,
                    categoryType.ToString(),
                    logWarnings);
            }
        }

        return valid;
    }

    private bool ValidateLayoutRoot(
        SelectionBackgroundLayoutDefinition layout,
        int layoutIndex,
        HashSet<GameObject> layoutRoots,
        bool logWarnings)
    {
        GameObject layoutRoot = layout.LayoutRoot;
        if (layoutRoot == null || !layoutRoots.Add(layoutRoot))
        {
            WarnIfRequested(
                $"Background layout {layoutIndex} root is missing or duplicated.",
                logWarnings);
            return false;
        }

        if (layoutRoot.transform.parent != transform)
        {
            WarnIfRequested(
                $"Background layout {layoutIndex} root must be a direct child of {name}.",
                logWarnings);
            return false;
        }

        return true;
    }

    private bool ValidateLayoutButton(
        Button button,
        HashSet<Button> buttons,
        int layoutIndex,
        string buttonName,
        bool logWarnings)
    {
        return ValidateUniqueReference(
            button,
            buttons,
            $"Background layout {layoutIndex} {buttonName} button is missing or duplicated.",
            logWarnings);
    }

    private bool ValidateUniqueReference<T>(
        T value,
        HashSet<T> values,
        string warning,
        bool logWarnings)
        where T : UnityEngine.Object
    {
        if (value != null && values.Add(value))
        {
            return true;
        }

        WarnIfRequested(warning, logWarnings);
        return false;
    }

    private void BindButtons()
    {
        UnbindButtons();

        SelectionBackgroundLayoutDefinition[] layouts = CurrentBackgroundLayouts;
        if (layouts == null)
        {
            return;
        }

        foreach (SelectionBackgroundLayoutDefinition layout in layouts)
        {
            if (layout == null)
            {
                continue;
            }

            BindButton(layout.BackButton, CancelAndClose);
            BindButton(layout.SubmitButton, Submit);

            for (int categoryIndex = 0; categoryIndex < CategoryCount; categoryIndex++)
            {
                SelectionCategoryType capturedCategory = (SelectionCategoryType)categoryIndex;
                BindButton(
                    layout.GetCategoryButton(capturedCategory),
                    () => ShowCategory(capturedCategory));
            }
        }
    }

    private void BindButton(Button button, UnityAction action)
    {
        if (button == null || action == null || buttonActions.ContainsKey(button))
        {
            return;
        }

        buttonActions.Add(button, action);
        button.onClick.AddListener(action);
    }

    private void UnbindButtons()
    {
        foreach (KeyValuePair<Button, UnityAction> pair in buttonActions)
        {
            pair.Key?.onClick.RemoveListener(pair.Value);
        }

        buttonActions.Clear();
    }

    private void ShowCategory(SelectionCategoryType categoryType)
    {
        if (submissionInProgress)
        {
            return;
        }

        currentCategory = categoryType;
        UpdateCategoryButtons();

        if (!categoryLookup.TryGetValue(categoryType, out SelectionCategoryDefinition category)
            || itemViewPrefab == null
            || activeLayout?.ItemRoot == null)
        {
            HideAllItemViews();
            RefreshSubmitButton();
            return;
        }

        SelectionItemDefinition[] items = GetCategoryItems(category);
        if (items == null)
        {
            HideAllItemViews();
            RefreshSubmitButton();
            return;
        }

        MoveItemViewsToActiveRoot();
        visibleItemIndices.Clear();
        for (int itemIndex = 0; itemIndex < items.Length; itemIndex++)
        {
            SelectionItemDefinition item = items[itemIndex];
            if (item != null && IsItemAvailable(categoryType, item))
            {
                visibleItemIndices.Add(itemIndex);
            }
        }

        EnsureItemViewCapacity(visibleItemIndices.Count);
        int selectedIndex = draftSelections[ToIndex(categoryType)];

        for (int viewIndex = 0; viewIndex < itemViews.Count; viewIndex++)
        {
            SelectionItemView itemView = itemViews[viewIndex];
            if (viewIndex >= visibleItemIndices.Count)
            {
                itemView.Clear();
                continue;
            }

            int itemIndex = visibleItemIndices[viewIndex];
            int capturedIndex = itemIndex;
            SelectionItemDefinition item = items[itemIndex];
            bool selected = categoryType == SelectionCategoryType.Background
                ? itemIndex == selectedIndex
                : IsSelectedPlacement(categoryType, item?.ItemId);
            itemView.Configure(
                item != null ? item.IconSprite : null,
                activeLayout.ItemBackgroundSprite,
                selected,
                () => SelectItem(categoryType, capturedIndex));
        }

        RefreshActiveItemScroll(visibleItemIndices.Count);
        RefreshSubmitButton();
    }

    private void SelectItem(SelectionCategoryType categoryType, int itemIndex)
    {
        if (submissionInProgress)
        {
            return;
        }

        if (!TryGetItem(categoryType, itemIndex, out SelectionItemDefinition item)
            || !IsItemAvailable(categoryType, item))
        {
            return;
        }

        if (IsPlacementCategory(categoryType))
        {
            PlaceOrSelectItem(categoryType, itemIndex, item);
            return;
        }

        draftSelections[ToIndex(categoryType)] = itemIndex;

        if (categoryType == SelectionCategoryType.Background)
        {
            ApplyAllPreviews();
            ShowCategory(currentCategory);
            return;
        }

        RefreshSubmitButton();
    }

    private void EnsureItemViewCapacity(int requiredCount)
    {
        Transform itemViewParent = GetActiveItemViewParent();
        if (itemViewParent == null)
        {
            return;
        }

        while (itemViews.Count < requiredCount)
        {
            SelectionItemView itemView = Instantiate(itemViewPrefab, itemViewParent);
            itemView.name = $"SelectionItem_{itemViews.Count:00}";
            itemViews.Add(itemView);
        }
    }

    private void MoveItemViewsToActiveRoot()
    {
        Transform itemViewParent = GetActiveItemViewParent();
        if (itemViewParent == null)
        {
            return;
        }

        foreach (SelectionItemView itemView in itemViews)
        {
            if (itemView != null && itemView.transform.parent != itemViewParent)
            {
                itemView.transform.SetParent(itemViewParent, false);
            }
        }
    }

    private void HideAllItemViews()
    {
        foreach (SelectionItemView itemView in itemViews)
        {
            itemView?.Clear();
        }
    }

    private void UpdateCategoryButtons()
    {
        SelectionBackgroundLayoutDefinition[] layouts = CurrentBackgroundLayouts;
        if (layouts == null)
        {
            return;
        }

        foreach (SelectionBackgroundLayoutDefinition layout in layouts)
        {
            if (layout == null)
            {
                continue;
            }

            for (int categoryIndex = 0; categoryIndex < CategoryCount; categoryIndex++)
            {
                SelectionCategoryType categoryType = (SelectionCategoryType)categoryIndex;
                Button button = layout.GetCategoryButton(categoryType);
                if (button != null)
                {
                    button.interactable = categoryType != currentCategory;
                }
            }
        }
    }

    private void ApplyAllPreviews()
    {
        ApplyPreview(SelectionCategoryType.Background);
        PreparePlacementLayers();
        RebuildPlacementViews();
    }

    private void ApplyPreview(SelectionCategoryType categoryType)
    {
        if (categoryType == SelectionCategoryType.Background)
        {
            ActivateSelectedBackgroundLayout();
            return;
        }

        RebuildPlacementViews();
    }

    private void ActivateSelectedBackgroundLayout()
    {
        activeLayout = null;
        SelectionBackgroundLayoutDefinition[] layouts = CurrentBackgroundLayouts;
        if (layouts == null)
        {
            return;
        }

        DeactivateEveryBackgroundLayout();

        int selectedIndex = draftSelections[ToIndex(SelectionCategoryType.Background)];
        bool hasValidSelection = TryGetItem(
            SelectionCategoryType.Background,
            selectedIndex,
            out _);

        for (int layoutIndex = 0; layoutIndex < layouts.Length; layoutIndex++)
        {
            SelectionBackgroundLayoutDefinition layout = layouts[layoutIndex];
            bool selected = hasValidSelection && layoutIndex == selectedIndex;
            if (layout?.LayoutRoot != null)
            {
                layout.LayoutRoot.SetActive(selected);
            }

            if (selected)
            {
                activeLayout = layout;
            }
        }

        MoveItemViewsToActiveRoot();
    }

    private void SelectDefaultBackgroundIfNeeded()
    {
        int backgroundIndex = ToIndex(SelectionCategoryType.Background);
        if (!TryGetItem(
                SelectionCategoryType.Background,
                draftSelections[backgroundIndex],
                out _)
            && TryGetItem(SelectionCategoryType.Background, 0, out _))
        {
            draftSelections[backgroundIndex] = 0;
        }
    }

    private void RefreshSubmitButton()
    {
        bool canSubmit = !submissionInProgress && CanSubmit();
        SelectionBackgroundLayoutDefinition[] layouts = CurrentBackgroundLayouts;
        if (layouts == null)
        {
            return;
        }

        foreach (SelectionBackgroundLayoutDefinition layout in layouts)
        {
            if (layout?.SubmitButton != null)
            {
                layout.SubmitButton.interactable = canSubmit;
            }
        }
    }

    private bool CanSubmit()
    {
        if (!configurationValid)
        {
            return false;
        }

        SelectionCategoryType[] currentRequiredCategories = CurrentRequiredCategories;
        if (currentRequiredCategories == null)
        {
            return false;
        }

        foreach (SelectionCategoryType categoryType in currentRequiredCategories)
        {
            if (!IsKnownCategory(categoryType))
            {
                return false;
            }

            if (IsPlacementCategory(categoryType))
            {
                if (!HasValidDraftPlacement(categoryType))
                {
                    return false;
                }

                continue;
            }

            if (!TryGetItem(
                    categoryType,
                    draftSelections[ToIndex(categoryType)],
                    out SelectionItemDefinition item)
                || !IsItemAvailable(categoryType, item))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryGetItem(
        SelectionCategoryType categoryType,
        int itemIndex,
        out SelectionItemDefinition item)
    {
        item = null;
        if (!categoryLookup.TryGetValue(categoryType, out SelectionCategoryDefinition category)
            || itemIndex < 0)
        {
            return false;
        }

        SelectionItemDefinition[] items = GetCategoryItems(category);
        if (items == null || itemIndex >= items.Length)
        {
            return false;
        }

        item = items[itemIndex];
        return item != null;
    }

    private bool TryFindItem(
        SelectionCategoryType categoryType,
        string itemId,
        out int itemIndex,
        out SelectionItemDefinition item)
    {
        itemIndex = -1;
        item = null;
        if (string.IsNullOrWhiteSpace(itemId)
            || !categoryLookup.TryGetValue(
                categoryType,
                out SelectionCategoryDefinition category))
        {
            return false;
        }

        SelectionItemDefinition[] items = GetCategoryItems(category);
        if (items == null)
        {
            return false;
        }

        for (int index = 0; index < items.Length; index++)
        {
            SelectionItemDefinition candidate = items[index];
            if (candidate != null
                && string.Equals(candidate.ItemId, itemId, StringComparison.Ordinal))
            {
                itemIndex = index;
                item = candidate;
                return true;
            }
        }

        return false;
    }

    private bool IsItemAvailable(
        SelectionCategoryType categoryType,
        SelectionItemDefinition item)
    {
        if (item == null)
        {
            return false;
        }

        if (!IsItemAllowedInCurrentRound(categoryType, item.ItemId))
        {
            return false;
        }

        if (categoryType == SelectionCategoryType.Background)
        {
            return true;
        }

        if (categoryType == SelectionCategoryType.Character
            && (item.UnlockedByDefault || IsCurrentRoundDefaultCharacter(item.ItemId)))
        {
            return true;
        }

        return IsCollectibleCategory(categoryType)
            && IsItemCollected(categoryType, item.ItemId);
    }

    private bool IsItemAllowedInCurrentRound(
        SelectionCategoryType categoryType,
        string itemId)
    {
        if (categoryType != SelectionCategoryType.Character)
        {
            return true;
        }

        string[] allowedItemIds = activeRoundBackground?.AllowedCharacterItemIds;
        if (allowedItemIds == null || allowedItemIds.Length == 0)
        {
            return true;
        }

        foreach (string allowedItemId in allowedItemIds)
        {
            if (string.Equals(allowedItemId, itemId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsCurrentRoundDefaultCharacter(string itemId)
    {
        return !string.IsNullOrWhiteSpace(itemId)
            && string.Equals(
                activeRoundBackground?.DefaultCharacterItemId,
                itemId,
                StringComparison.Ordinal);
    }

    private bool IsAllowedDefaultCharacter(string itemId)
    {
        string[] allowedItemIds = activeRoundBackground?.AllowedCharacterItemIds;
        if (string.IsNullOrWhiteSpace(itemId)
            || allowedItemIds == null
            || allowedItemIds.Length == 0)
        {
            return false;
        }

        foreach (string allowedItemId in allowedItemIds)
        {
            if (string.Equals(allowedItemId, itemId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private bool ValidateCurrentRoundCharacterRules(bool logWarnings)
    {
        string[] allowedItemIds = activeRoundBackground?.AllowedCharacterItemIds;
        string defaultCharacterItemId = activeRoundBackground?.DefaultCharacterItemId;
        if ((allowedItemIds == null || allowedItemIds.Length == 0)
            && string.IsNullOrWhiteSpace(defaultCharacterItemId))
        {
            return true;
        }

        bool valid = true;
        HashSet<string> uniqueItemIds = new HashSet<string>(StringComparer.Ordinal);
        if (allowedItemIds != null)
        {
            foreach (string itemId in allowedItemIds)
            {
                if (string.IsNullOrWhiteSpace(itemId)
                    || !uniqueItemIds.Add(itemId)
                    || !TryFindItem(
                        SelectionCategoryType.Character,
                        itemId,
                        out _,
                        out _))
                {
                    valid = false;
                    WarnIfRequested(
                        $"Round {configuredRoundIndex} allowed Character item ID '{itemId}' is missing or duplicated.",
                        logWarnings);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(defaultCharacterItemId)
            && (!TryFindItem(
                    SelectionCategoryType.Character,
                    defaultCharacterItemId,
                    out _,
                    out _)
                || !IsAllowedDefaultCharacter(defaultCharacterItemId)))
        {
            valid = false;
            WarnIfRequested(
                $"Round {configuredRoundIndex} default Character item ID '{defaultCharacterItemId}' must exist in Character items and the current Round whitelist.",
                logWarnings);
        }

        return valid;
    }

    private static bool IsCollectibleCategory(SelectionCategoryType categoryType)
    {
        return categoryType == SelectionCategoryType.Character
            || categoryType == SelectionCategoryType.Props;
    }

    private void ClampSelections(int[] selections)
    {
        for (int categoryIndex = 0; categoryIndex < CategoryCount; categoryIndex++)
        {
            SelectionCategoryType categoryType = (SelectionCategoryType)categoryIndex;
            if (!TryGetItem(
                    categoryType,
                    selections[categoryIndex],
                    out SelectionItemDefinition item)
                || !IsItemAvailable(categoryType, item))
            {
                selections[categoryIndex] = -1;
            }
        }
    }

    private bool RequireReference(UnityEngine.Object value, string warning, bool logWarnings)
    {
        if (value != null)
        {
            return true;
        }

        WarnIfRequested(warning, logWarnings);
        return false;
    }

    private void WarnIfRequested(string message, bool logWarnings)
    {
        if (logWarnings)
        {
            WarnOnce(message);
        }
    }

    private void WarnOnce(string message)
    {
        if (loggedWarnings.Add(message))
        {
            Debug.LogWarning($"[SelectionPanelController] {message}", this);
        }
    }

    private void ValidateSerializedConfiguration()
    {
        RebuildCategoryLookup(true);
    }

    private static int ToIndex(SelectionCategoryType categoryType)
    {
        return (int)categoryType;
    }

    private static bool IsKnownCategory(SelectionCategoryType categoryType)
    {
        int categoryIndex = (int)categoryType;
        return categoryIndex >= 0 && categoryIndex < CategoryCount;
    }

    private static void CopySelections(int[] source, int[] destination)
    {
        Array.Copy(source, destination, CategoryCount);
    }

    private static void FillSelections(int[] selections, int value)
    {
        for (int selectionIndex = 0; selectionIndex < selections.Length; selectionIndex++)
        {
            selections[selectionIndex] = value;
        }
    }
}
