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
    [SerializeField] private GameObject previewObject;

    public string ItemId => itemId;
    public Sprite IconSprite => iconSprite;
    public Sprite PreviewSprite => previewSprite;
    public GameObject PreviewObject => previewObject;
}

[Serializable]
public sealed class SelectionCategoryDefinition
{
    [SerializeField] private SelectionCategoryType categoryType;
    [SerializeField] private Button categoryButton;
    [SerializeField] private SelectionItemDefinition[] items;

    public SelectionCategoryType CategoryType => categoryType;
    public Button CategoryButton => categoryButton;
    public SelectionItemDefinition[] Items => items;
}

public sealed class SelectionPanelController : MonoBehaviour
{
    private const int CategoryCount = 3;

    [Header("Navigation")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button submitButton;

    [Header("Items")]
    [SerializeField] private Transform itemRoot;
    [SerializeField] private SelectionItemView itemViewPrefab;
    [SerializeField] private SelectionCategoryDefinition[] categories;

    [Header("Selection Rules")]
    [SerializeField] private SelectionCategoryType[] requiredCategories =
    {
        SelectionCategoryType.Character,
        SelectionCategoryType.Background,
        SelectionCategoryType.Props
    };
    [SerializeField] private SelectionCategoryType initialCategory = SelectionCategoryType.Character;

    [Header("Preview Layers")]
    [SerializeField] private Image characterPreview;
    [SerializeField] private Image propsPreview;

    [Header("Events")]
    [SerializeField] private UnityEvent onSubmitted = new UnityEvent();

    private readonly Dictionary<SelectionCategoryType, SelectionCategoryDefinition> categoryLookup =
        new Dictionary<SelectionCategoryType, SelectionCategoryDefinition>();

    private readonly Dictionary<Button, UnityAction> categoryButtonActions =
        new Dictionary<Button, UnityAction>();

    private readonly List<SelectionItemView> itemViews = new List<SelectionItemView>();
    private readonly HashSet<string> loggedWarnings = new HashSet<string>();
    private readonly int[] committedSelections = { -1, -1, -1 };
    private readonly int[] draftSelections = { -1, -1, -1 };

    private SelectionCategoryType currentCategory = SelectionCategoryType.Character;
    private bool configurationValid;

    public UnityEvent OnSubmitted => onSubmitted;

    public bool IsConfigurationValid()
    {
        configurationValid = RebuildCategoryLookup(false);
        return configurationValid;
    }

    public void OpenPanel()
    {
        gameObject.SetActive(true);
        PrepareRuntime(true);
        CopySelections(committedSelections, draftSelections);
        currentCategory = categoryLookup.ContainsKey(initialCategory)
            ? initialCategory
            : SelectionCategoryType.Character;
        ApplyAllPreviews();
        ShowCategory(currentCategory);
        RefreshSubmitButton();
    }

    public void CancelAndClose()
    {
        PrepareRuntime(false);
        CopySelections(committedSelections, draftSelections);
        ApplyAllPreviews();
        gameObject.SetActive(false);
    }

    public void ClosePanel()
    {
        CancelAndClose();
    }

    public void Submit()
    {
        PrepareRuntime(true);
        if (!CanSubmit())
        {
            WarnOnce("Submit blocked because every required category must contain one valid selection.");
            RefreshSubmitButton();
            return;
        }

        CopySelections(draftSelections, committedSelections);
        onSubmitted?.Invoke();
        gameObject.SetActive(false);
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
        if (!TryGetItem(categoryType, selectedIndex, out SelectionItemDefinition item))
        {
            return string.Empty;
        }

        return item.ItemId ?? string.Empty;
    }

    public void ResetSelections()
    {
        FillSelections(committedSelections, -1);
        FillSelections(draftSelections, -1);
        ApplyAllPreviews();
        ShowCategory(currentCategory);
        RefreshSubmitButton();
    }

    private void OnDestroy()
    {
        UnbindButtons();

        foreach (SelectionItemView itemView in itemViews)
        {
            itemView?.Clear();
        }
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
        BindButtons();
    }

    private bool RebuildCategoryLookup(bool logWarnings)
    {
        categoryLookup.Clear();
        bool valid = true;

        valid &= RequireReference(backButton, "Back button is not assigned.", logWarnings);
        valid &= RequireReference(submitButton, "Post button is not assigned.", logWarnings);
        valid &= RequireReference(itemRoot, "Item root is not assigned.", logWarnings);
        valid &= RequireReference(itemViewPrefab, "Item view prefab is not assigned.", logWarnings);
        valid &= RequireReference(characterPreview, "Character preview Image is not assigned.", logWarnings);
        valid &= RequireReference(propsPreview, "Props preview Image is not assigned.", logWarnings);

        if (!IsKnownCategory(initialCategory))
        {
            valid = false;
            WarnIfRequested($"Initial category {initialCategory} is invalid.", logWarnings);
        }

        if (requiredCategories == null || requiredCategories.Length == 0)
        {
            valid = false;
            WarnIfRequested("At least one required category must be configured.", logWarnings);
        }
        else
        {
            HashSet<SelectionCategoryType> requiredCategoryTypes = new HashSet<SelectionCategoryType>();
            foreach (SelectionCategoryType requiredCategory in requiredCategories)
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

        HashSet<Button> categoryButtons = new HashSet<Button>();
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

            if (category.CategoryButton == null)
            {
                valid = false;
                WarnIfRequested($"Category {category.CategoryType} has no button.", logWarnings);
            }
            else if (!categoryButtons.Add(category.CategoryButton))
            {
                valid = false;
                WarnIfRequested("The same category button is assigned to multiple categories.", logWarnings);
            }

            SelectionItemDefinition[] items = category.Items;
            if (items == null || items.Length == 0)
            {
                valid = false;
                WarnIfRequested($"Category {category.CategoryType} has no items.", logWarnings);
                continue;
            }

            if (category.CategoryType == SelectionCategoryType.Background && items.Length != 2)
            {
                valid = false;
                WarnIfRequested("Background must contain exactly two items.", logWarnings);
            }

            HashSet<string> itemIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<GameObject> previewObjects = new HashSet<GameObject>();
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

                if (category.CategoryType == SelectionCategoryType.Background)
                {
                    if (item.PreviewObject == null)
                    {
                        valid = false;
                        WarnIfRequested($"Background item '{item.ItemId}' has no preview object.", logWarnings);
                    }
                    else if (!previewObjects.Add(item.PreviewObject))
                    {
                        valid = false;
                        WarnIfRequested("Background items must use different preview objects.", logWarnings);
                    }
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

        return valid;
    }

    private void BindButtons()
    {
        UnbindButtons();

        backButton?.onClick.AddListener(CancelAndClose);
        submitButton?.onClick.AddListener(Submit);

        foreach (KeyValuePair<SelectionCategoryType, SelectionCategoryDefinition> pair in categoryLookup)
        {
            Button button = pair.Value.CategoryButton;
            if (button == null || categoryButtonActions.ContainsKey(button))
            {
                continue;
            }

            SelectionCategoryType capturedCategory = pair.Key;
            UnityAction action = () => ShowCategory(capturedCategory);
            categoryButtonActions.Add(button, action);
            button.onClick.AddListener(action);
        }
    }

    private void UnbindButtons()
    {
        backButton?.onClick.RemoveListener(CancelAndClose);
        submitButton?.onClick.RemoveListener(Submit);

        foreach (KeyValuePair<Button, UnityAction> pair in categoryButtonActions)
        {
            pair.Key?.onClick.RemoveListener(pair.Value);
        }

        categoryButtonActions.Clear();
    }

    private void ShowCategory(SelectionCategoryType categoryType)
    {
        currentCategory = categoryType;
        UpdateCategoryButtons();

        if (!categoryLookup.TryGetValue(categoryType, out SelectionCategoryDefinition category)
            || category.Items == null
            || itemViewPrefab == null
            || itemRoot == null)
        {
            HideAllItemViews();
            RefreshSubmitButton();
            return;
        }

        EnsureItemViewCapacity(category.Items.Length);
        int selectedIndex = draftSelections[ToIndex(categoryType)];

        for (int itemIndex = 0; itemIndex < itemViews.Count; itemIndex++)
        {
            SelectionItemView itemView = itemViews[itemIndex];
            if (itemIndex >= category.Items.Length)
            {
                itemView.Clear();
                continue;
            }

            int capturedIndex = itemIndex;
            SelectionItemDefinition item = category.Items[itemIndex];
            itemView.Configure(
                item != null ? item.IconSprite : null,
                itemIndex == selectedIndex,
                () => SelectItem(categoryType, capturedIndex));
        }

        RefreshSubmitButton();
    }

    private void SelectItem(SelectionCategoryType categoryType, int itemIndex)
    {
        if (!TryGetItem(categoryType, itemIndex, out _))
        {
            return;
        }

        draftSelections[ToIndex(categoryType)] = itemIndex;
        ApplyPreview(categoryType);

        if (categoryType == currentCategory)
        {
            for (int viewIndex = 0; viewIndex < itemViews.Count; viewIndex++)
            {
                if (itemViews[viewIndex] != null && itemViews[viewIndex].gameObject.activeSelf)
                {
                    itemViews[viewIndex].SetSelected(viewIndex == itemIndex);
                }
            }
        }

        RefreshSubmitButton();
    }

    private void EnsureItemViewCapacity(int requiredCount)
    {
        while (itemViews.Count < requiredCount)
        {
            SelectionItemView itemView = Instantiate(itemViewPrefab, itemRoot);
            itemView.name = $"SelectionItem_{itemViews.Count:00}";
            itemViews.Add(itemView);
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
        foreach (KeyValuePair<SelectionCategoryType, SelectionCategoryDefinition> pair in categoryLookup)
        {
            if (pair.Value.CategoryButton != null)
            {
                pair.Value.CategoryButton.interactable = pair.Key != currentCategory;
            }
        }
    }

    private void ApplyAllPreviews()
    {
        ApplyPreview(SelectionCategoryType.Background);
        ApplyPreview(SelectionCategoryType.Character);
        ApplyPreview(SelectionCategoryType.Props);
    }

    private void ApplyPreview(SelectionCategoryType categoryType)
    {
        if (categoryType == SelectionCategoryType.Background)
        {
            ApplyBackgroundPreview();
            return;
        }

        Image target = GetPreviewTarget(categoryType);
        int selectedIndex = draftSelections[ToIndex(categoryType)];
        Sprite sprite = null;

        if (TryGetItem(categoryType, selectedIndex, out SelectionItemDefinition item))
        {
            sprite = item.PreviewSprite != null ? item.PreviewSprite : item.IconSprite;
        }

        if (target == null)
        {
            return;
        }

        target.sprite = sprite;
        target.enabled = sprite != null;
        target.preserveAspect = true;
    }

    private void ApplyBackgroundPreview()
    {
        if (!categoryLookup.TryGetValue(
                SelectionCategoryType.Background,
                out SelectionCategoryDefinition category)
            || category.Items == null)
        {
            return;
        }

        int selectedIndex = draftSelections[ToIndex(SelectionCategoryType.Background)];
        for (int itemIndex = 0; itemIndex < category.Items.Length; itemIndex++)
        {
            SelectionItemDefinition item = category.Items[itemIndex];
            if (item?.PreviewObject != null)
            {
                item.PreviewObject.SetActive(itemIndex == selectedIndex);
            }
        }
    }

    private Image GetPreviewTarget(SelectionCategoryType categoryType)
    {
        switch (categoryType)
        {
            case SelectionCategoryType.Character:
                return characterPreview;
            case SelectionCategoryType.Props:
                return propsPreview;
            default:
                return null;
        }
    }

    private void RefreshSubmitButton()
    {
        if (submitButton != null)
        {
            submitButton.interactable = CanSubmit();
        }
    }

    private bool CanSubmit()
    {
        if (!configurationValid)
        {
            return false;
        }

        if (requiredCategories == null)
        {
            return false;
        }

        foreach (SelectionCategoryType categoryType in requiredCategories)
        {
            if (!IsKnownCategory(categoryType)
                || !TryGetItem(categoryType, draftSelections[ToIndex(categoryType)], out _))
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
            || category.Items == null
            || itemIndex < 0
            || itemIndex >= category.Items.Length)
        {
            return false;
        }

        item = category.Items[itemIndex];
        return item != null;
    }

    private void ClampSelections(int[] selections)
    {
        for (int categoryIndex = 0; categoryIndex < CategoryCount; categoryIndex++)
        {
            SelectionCategoryType categoryType = (SelectionCategoryType)categoryIndex;
            if (!TryGetItem(categoryType, selections[categoryIndex], out _))
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
        if (categories == null || categories.Length != CategoryCount)
        {
            Debug.LogWarning(
                $"[SelectionPanelController] Configure exactly {CategoryCount} categories.",
                this);
            return;
        }

        if (requiredCategories == null || requiredCategories.Length == 0)
        {
            Debug.LogWarning(
                "[SelectionPanelController] Configure at least one required category.",
                this);
        }

        foreach (SelectionCategoryDefinition category in categories)
        {
            if (category != null
                && category.CategoryType == SelectionCategoryType.Background
                && (category.Items == null || category.Items.Length != 2))
            {
                Debug.LogWarning(
                    "[SelectionPanelController] Background must contain exactly two items.",
                    this);
            }
        }
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
