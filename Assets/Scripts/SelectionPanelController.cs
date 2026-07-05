using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SelectionPanelController : MonoBehaviour
{
    [Serializable]
    public class SelectionCategory
    {
        public string name;
        public Button button;
        public GameObject optionRoot;
        public Button optionButtonPrefab;
        public Image[] previewImages;
        public SelectionOption[] options;

        [NonSerialized] public int selectedIndex = -1;
    }

    [Serializable]
    public class SelectionOption
    {
        public Button button;
        public Sprite[] sprites;
        public GameObject selectedMark;
        public GameObject previewObject;
    }

    [Header("Main Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button submitButton;

    [Header("Categories")]
    [SerializeField] private SelectionCategory[] categories;
    [SerializeField] private int defaultCategoryIndex;

    [Header("Option Button Size")]
    [SerializeField] private Vector2 optionButtonSize = new Vector2(120f, 120f);

    [Header("Option Grid")]
    [SerializeField] private bool useGridLayout = true;
    [SerializeField] private int gridColumnCount = 3;
    [SerializeField] private Vector2 gridSpacing = new Vector2(12f, 12f);
    [SerializeField] private int gridPaddingLeft;
    [SerializeField] private int gridPaddingRight;
    [SerializeField] private int gridPaddingTop;
    [SerializeField] private int gridPaddingBottom;

    [Header("Submit")]
    [SerializeField] private bool requireEveryCategorySelected = true;
    [SerializeField] private bool closePanelOnSubmit = true;
    [SerializeField] private UnityEvent onSubmit;

    private int currentCategoryIndex = -1;

    private void Start()
    {
        BuildOptionButtons();
        ApplyAllOptionRootLayouts();
        BindButtons();
        ApplyAllOptionButtonSizes();
        OpenCategory(defaultCategoryIndex);
        RefreshSubmitButton();
    }

    public void OpenPanel()
    {
        gameObject.SetActive(true);
        OpenCategory(defaultCategoryIndex);
        RefreshSubmitButton();
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    public void Submit()
    {
        if (requireEveryCategorySelected && !HasSelectedEveryCategory())
        {
            Debug.Log("[SelectionPanelController] Submit blocked: not every category has been selected.", this);
            return;
        }

        onSubmit?.Invoke();

        if (closePanelOnSubmit)
        {
            ClosePanel();
        }
    }

    public int GetSelectedIndex(int categoryIndex)
    {
        if (!IsValidCategoryIndex(categoryIndex))
        {
            return -1;
        }

        return categories[categoryIndex].selectedIndex;
    }

    public Sprite[] GetSelectedSprites(int categoryIndex)
    {
        if (!IsValidCategoryIndex(categoryIndex))
        {
            return Array.Empty<Sprite>();
        }

        SelectionCategory category = categories[categoryIndex];
        int selectedIndex = category.selectedIndex;
        if (category.options == null || selectedIndex < 0 || selectedIndex >= category.options.Length)
        {
            return Array.Empty<Sprite>();
        }

        return category.options[selectedIndex]?.sprites ?? Array.Empty<Sprite>();
    }

    public void ResetSelections()
    {
        if (categories == null)
        {
            return;
        }

        foreach (SelectionCategory category in categories)
        {
            if (category != null)
            {
                category.selectedIndex = -1;
            }
        }

        RefreshAllOptions();
        RefreshSubmitButton();
    }

    private void BindButtons()
    {
        AddClick(backButton, ClosePanel);
        AddClick(submitButton, Submit);

        if (categories == null)
        {
            return;
        }

        for (int categoryIndex = 0; categoryIndex < categories.Length; categoryIndex++)
        {
            int capturedCategoryIndex = categoryIndex;
            AddClick(categories[categoryIndex]?.button, () => OpenCategory(capturedCategoryIndex));

            SelectionOption[] options = categories[categoryIndex]?.options;
            if (options == null)
            {
                continue;
            }

            for (int optionIndex = 0; optionIndex < options.Length; optionIndex++)
            {
                int capturedOptionIndex = optionIndex;
                AddClick(options[optionIndex]?.button, () => SelectOption(capturedCategoryIndex, capturedOptionIndex));
            }
        }
    }

    private void OpenCategory(int categoryIndex)
    {
        if (!IsValidCategoryIndex(categoryIndex))
        {
            return;
        }

        currentCategoryIndex = categoryIndex;

        for (int i = 0; i < categories.Length; i++)
        {
            SelectionCategory category = categories[i];
            if (category == null)
            {
                continue;
            }

            bool active = i == currentCategoryIndex;
            SetActive(category.optionRoot, active);

            if (category.button != null)
            {
                category.button.interactable = !active;
            }
        }

        RefreshAllOptions();
    }

    private void SelectOption(int categoryIndex, int optionIndex)
    {
        if (!IsValidCategoryIndex(categoryIndex))
        {
            return;
        }

        SelectionOption[] options = categories[categoryIndex].options;
        if (options == null || optionIndex < 0 || optionIndex >= options.Length)
        {
            return;
        }

        categories[categoryIndex].selectedIndex = optionIndex;
        RefreshCategoryOptions(categoryIndex);
        RefreshSubmitButton();
    }

    private void RefreshAllOptions()
    {
        if (categories == null)
        {
            return;
        }

        for (int categoryIndex = 0; categoryIndex < categories.Length; categoryIndex++)
        {
            RefreshCategoryOptions(categoryIndex);
        }
    }

    private void RefreshCategoryOptions(int categoryIndex)
    {
        if (!IsValidCategoryIndex(categoryIndex))
        {
            return;
        }

        SelectionCategory category = categories[categoryIndex];
        SelectionOption[] options = category.options;
        if (options == null)
        {
            return;
        }

        for (int optionIndex = 0; optionIndex < options.Length; optionIndex++)
        {
            SelectionOption option = options[optionIndex];
            if (option == null)
            {
                continue;
            }

            bool selected = optionIndex == category.selectedIndex;
            SetActive(option.selectedMark, selected);
            SetActive(option.previewObject, selected);
        }

        RefreshPreviewImages(category.previewImages, GetSelectedSprites(categoryIndex));
    }

    private void RefreshSubmitButton()
    {
        if (submitButton != null)
        {
            submitButton.interactable = !requireEveryCategorySelected || HasSelectedEveryCategory();
        }
    }

    private bool HasSelectedEveryCategory()
    {
        if (categories == null || categories.Length == 0)
        {
            return false;
        }

        foreach (SelectionCategory category in categories)
        {
            if (category == null || category.selectedIndex < 0)
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyAllOptionButtonSizes()
    {
        if (categories == null)
        {
            return;
        }

        foreach (SelectionCategory category in categories)
        {
            SelectionOption[] options = category?.options;
            if (options == null)
            {
                continue;
            }

            foreach (SelectionOption option in options)
            {
                ApplyOptionButtonSize(option?.button);
            }
        }
    }

    private void BuildOptionButtons()
    {
        if (categories == null)
        {
            return;
        }

        foreach (SelectionCategory category in categories)
        {
            BuildOptionButtons(category);
        }
    }

    private void BuildOptionButtons(SelectionCategory category)
    {
        if (category == null)
        {
            return;
        }

        BuildMissingOptionButtons(category);
        ApplyOptionSpritesToButtons(category);
    }

    private void ApplyAllOptionRootLayouts()
    {
        if (!useGridLayout || categories == null)
        {
            return;
        }

        foreach (SelectionCategory category in categories)
        {
            ApplyOptionRootLayout(category);
        }
    }

    private void ApplyOptionRootLayout(SelectionCategory category)
    {
        if (category == null || category.optionRoot == null)
        {
            return;
        }

        foreach (LayoutGroup layoutGroup in category.optionRoot.GetComponents<LayoutGroup>())
        {
            if (!(layoutGroup is GridLayoutGroup))
            {
                layoutGroup.enabled = false;
            }
        }

        GridLayoutGroup gridLayout = category.optionRoot.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = category.optionRoot.AddComponent<GridLayoutGroup>();
        }

        gridLayout.enabled = true;
        gridLayout.cellSize = optionButtonSize;
        gridLayout.spacing = gridSpacing;
        gridLayout.padding = new RectOffset(gridPaddingLeft, gridPaddingRight, gridPaddingTop, gridPaddingBottom);
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = Mathf.Max(1, gridColumnCount);
    }

    private void BuildMissingOptionButtons(SelectionCategory category)
    {
        if (category.optionRoot == null || category.optionButtonPrefab == null || category.options == null)
        {
            return;
        }

        category.optionButtonPrefab.gameObject.SetActive(false);

        for (int optionIndex = 0; optionIndex < category.options.Length; optionIndex++)
        {
            SelectionOption option = category.options[optionIndex];
            if (option == null)
            {
                option = new SelectionOption();
                category.options[optionIndex] = option;
            }

            if (option.button == null)
            {
                Sprite iconSprite = GetFirstSprite(option);
                string spriteName = iconSprite != null ? iconSprite.name : "Empty";
                Button button = Instantiate(category.optionButtonPrefab, category.optionRoot.transform);
                button.name = $"Option_{optionIndex}_{spriteName}";
                button.gameObject.SetActive(true);

                option.button = button;
            }

            if (option.selectedMark == null)
            {
                Transform selectedMark = FindChild(option.button.transform, "SelectedMark");
                option.selectedMark = selectedMark != null ? selectedMark.gameObject : null;
            }

            SetActive(option.selectedMark, false);
        }
    }

    private void ApplyOptionSpritesToButtons(SelectionCategory category)
    {
        if (category.options == null)
        {
            return;
        }

        foreach (SelectionOption option in category.options)
        {
            Sprite iconSprite = GetFirstSprite(option);
            if (option == null || option.button == null || iconSprite == null)
            {
                continue;
            }

            Image optionImage = FindOptionImage(option.button);
            if (optionImage != null)
            {
                optionImage.sprite = iconSprite;
                optionImage.preserveAspect = true;
            }
        }
    }

    private Sprite GetFirstSprite(SelectionOption option)
    {
        if (option == null || option.sprites == null || option.sprites.Length == 0)
        {
            return null;
        }

        return option.sprites[0];
    }

    private void RefreshPreviewImages(Image[] previewImages, Sprite[] sprites)
    {
        if (previewImages == null)
        {
            return;
        }

        for (int i = 0; i < previewImages.Length; i++)
        {
            Image previewImage = previewImages[i];
            if (previewImage == null)
            {
                continue;
            }

            bool hasSprite = sprites != null && i < sprites.Length && sprites[i] != null;
            previewImage.enabled = hasSprite;
            previewImage.sprite = hasSprite ? sprites[i] : null;
            previewImage.preserveAspect = true;
            previewImage.gameObject.SetActive(hasSprite);
        }
    }

    private Image FindOptionImage(Button button)
    {
        Transform icon = FindChild(button.transform, "Icon");
        if (icon != null && icon.TryGetComponent(out Image iconImage))
        {
            return iconImage;
        }

        return button.GetComponent<Image>();
    }

    private Transform FindChild(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform nestedChild = FindChild(child, childName);
            if (nestedChild != null)
            {
                return nestedChild;
            }
        }

        return null;
    }

    private void ApplyOptionButtonSize(Button button)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = optionButtonSize;
        }

        LayoutElement layoutElement = button.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = button.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = optionButtonSize.x;
        layoutElement.minHeight = optionButtonSize.y;
        layoutElement.preferredWidth = optionButtonSize.x;
        layoutElement.preferredHeight = optionButtonSize.y;
    }

    private bool IsValidCategoryIndex(int categoryIndex)
    {
        return categories != null && categoryIndex >= 0 && categoryIndex < categories.Length && categories[categoryIndex] != null;
    }

    private void AddClick(Button button, UnityAction action)
    {
        if (button != null)
        {
            button.onClick.AddListener(action);
        }
    }

    private void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
