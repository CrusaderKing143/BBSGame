using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class SelectionPanelControllerTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (Object createdObject in createdObjects)
        {
            if (createdObject != null)
            {
                Object.Destroy(createdObject);
            }
        }

        createdObjects.Clear();
        yield return null;
    }

    [UnityTest]
    public IEnumerator CategoriesKeepOneDraftSelectionAndSubmitCommittedState()
    {
        TestPanel panel = CreatePanel();
        int submittedCount = 0;
        panel.Controller.OnSubmitted.AddListener(() => submittedCount++);

        panel.Controller.OpenPanel();
        yield return null;

        Assert.That(GetActiveViews(panel.ItemRoot), Has.Length.EqualTo(2));
        Assert.That(panel.SubmitButton.interactable, Is.False);

        GetActiveViews(panel.ItemRoot)[1].Button.onClick.Invoke();
        Assert.That(GetActiveViews(panel.ItemRoot).Count(view => view.IsSelected), Is.EqualTo(1));

        panel.BackgroundButton.onClick.Invoke();
        Assert.That(GetActiveViews(panel.ItemRoot), Has.Length.EqualTo(2));
        GetActiveViews(panel.ItemRoot)[0].Button.onClick.Invoke();

        panel.PropsButton.onClick.Invoke();
        Assert.That(GetActiveViews(panel.ItemRoot), Has.Length.EqualTo(5));
        GetActiveViews(panel.ItemRoot)[4].Button.onClick.Invoke();

        panel.CharacterButton.onClick.Invoke();
        SelectionItemView[] characterViews = GetActiveViews(panel.ItemRoot);
        Assert.That(characterViews[1].IsSelected, Is.True);
        Assert.That(panel.SubmitButton.interactable, Is.True);

        panel.SubmitButton.onClick.Invoke();
        Assert.That(panel.PanelObject.activeSelf, Is.False);
        Assert.That(submittedCount, Is.EqualTo(1));
        Assert.That(panel.Controller.GetCommittedSelectionIndex(SelectionCategoryType.Character), Is.EqualTo(1));
        Assert.That(panel.Controller.GetCommittedSelectionIndex(SelectionCategoryType.Background), Is.EqualTo(0));
        Assert.That(panel.Controller.GetCommittedSelectionIndex(SelectionCategoryType.Props), Is.EqualTo(4));
        Assert.That(panel.Controller.GetCommittedSelectionIndex((SelectionCategoryType)99), Is.EqualTo(-1));
        Assert.That(panel.Controller.GetCommittedItemId((SelectionCategoryType)99), Is.Empty);

        panel.Controller.OpenPanel();
        yield return null;

        Assert.That(GetActiveViews(panel.ItemRoot)[1].IsSelected, Is.True);
        Assert.That(panel.SubmitButton.interactable, Is.True);

        panel.Controller.OpenPanel();
        yield return null;

        Assert.That(GetActiveViews(panel.ItemRoot), Has.Length.EqualTo(2));
        Assert.That(panel.ItemRoot.GetComponentsInChildren<SelectionItemView>(true), Has.Length.EqualTo(5));
    }

    [UnityTest]
    public IEnumerator BackRestoresCommittedSelectionAndPreview()
    {
        TestPanel panel = CreatePanel();
        panel.Controller.OpenPanel();
        yield return null;

        SelectFirstItemInEveryCategory(panel);
        Sprite committedCharacter = panel.CharacterPreview.sprite;
        panel.SubmitButton.onClick.Invoke();

        panel.Controller.OpenPanel();
        yield return null;

        GetActiveViews(panel.ItemRoot)[1].Button.onClick.Invoke();
        Sprite draftCharacter = panel.CharacterPreview.sprite;
        Assert.That(draftCharacter, Is.Not.SameAs(committedCharacter));

        panel.BackButton.onClick.Invoke();
        Assert.That(panel.PanelObject.activeSelf, Is.False);
        Assert.That(panel.Controller.GetCommittedSelectionIndex(SelectionCategoryType.Character), Is.EqualTo(0));
        Assert.That(panel.CharacterPreview.sprite, Is.SameAs(committedCharacter));

        panel.Controller.OpenPanel();
        yield return null;
        Assert.That(GetActiveViews(panel.ItemRoot)[0].IsSelected, Is.True);
    }

    [UnityTest]
    public IEnumerator PartialSelectionIsDiscardedAndInvalidConfigurationCannotSubmit()
    {
        TestPanel panel = CreatePanel();
        panel.Controller.OpenPanel();
        yield return null;

        GetActiveViews(panel.ItemRoot)[0].Button.onClick.Invoke();
        Assert.That(panel.CharacterPreview.enabled, Is.True);
        panel.BackButton.onClick.Invoke();

        panel.Controller.OpenPanel();
        yield return null;

        Assert.That(panel.Controller.GetCommittedSelectionIndex(SelectionCategoryType.Character), Is.EqualTo(-1));
        Assert.That(panel.CharacterPreview.enabled, Is.False);
        Assert.That(panel.SubmitButton.interactable, Is.False);

        SelectionCategoryDefinition[] invalidCategories =
        {
            CreateCategory(SelectionCategoryType.Character, panel.CharacterButton, 2),
            CreateCategory(SelectionCategoryType.Background, panel.BackgroundButton, 0),
            CreateCategory(SelectionCategoryType.Props, panel.PropsButton, 5)
        };
        SetField(panel.Controller, "categories", invalidCategories);

        panel.Controller.OpenPanel();
        yield return null;

        Assert.That(panel.SubmitButton.interactable, Is.False);
        Assert.That(GetActiveViews(panel.ItemRoot), Has.Length.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator BackgroundOnlyRequirementUsesMutuallyExclusivePreviewObjects()
    {
        TestPanel panel = CreatePanel(true);
        panel.Controller.OpenPanel();
        yield return null;

        Assert.That(GetActiveViews(panel.ItemRoot), Has.Length.EqualTo(2));
        Assert.That(panel.SubmitButton.interactable, Is.False);
        Assert.That(panel.BackgroundPreviews.All(preview => !preview.activeSelf), Is.True);

        GetActiveViews(panel.ItemRoot)[0].Button.onClick.Invoke();
        Assert.That(panel.BackgroundPreviews[0].activeSelf, Is.True);
        Assert.That(panel.BackgroundPreviews[1].activeSelf, Is.False);
        Assert.That(panel.SubmitButton.interactable, Is.True);

        panel.CharacterButton.onClick.Invoke();
        Assert.That(GetActiveViews(panel.ItemRoot), Has.Length.EqualTo(2));
        Assert.That(panel.SubmitButton.interactable, Is.True);

        panel.SubmitButton.onClick.Invoke();
        Assert.That(panel.Controller.GetCommittedSelectionIndex(SelectionCategoryType.Background), Is.EqualTo(0));

        panel.Controller.OpenPanel();
        yield return null;

        GetActiveViews(panel.ItemRoot)[1].Button.onClick.Invoke();
        Assert.That(panel.BackgroundPreviews[0].activeSelf, Is.False);
        Assert.That(panel.BackgroundPreviews[1].activeSelf, Is.True);

        panel.BackButton.onClick.Invoke();
        Assert.That(panel.BackgroundPreviews[0].activeSelf, Is.True);
        Assert.That(panel.BackgroundPreviews[1].activeSelf, Is.False);
        Assert.That(panel.Controller.GetCommittedSelectionIndex(SelectionCategoryType.Background), Is.EqualTo(0));
    }

    private TestPanel CreatePanel(bool backgroundOnly = false)
    {
        GameObject root = Track(new GameObject("Selection Test Root"));
        GameObject panelObject = new GameObject("BBSChoose", typeof(RectTransform));
        panelObject.transform.SetParent(root.transform);
        panelObject.SetActive(false);

        SelectionPanelController controller = panelObject.AddComponent<SelectionPanelController>();
        Button backButton = CreateButton(panelObject.transform, "Back");
        Button submitButton = CreateButton(panelObject.transform, "Post");
        Button characterButton = CreateButton(panelObject.transform, "Character");
        Button backgroundButton = CreateButton(panelObject.transform, "Background");
        Button propsButton = CreateButton(panelObject.transform, "Props");
        Transform itemRoot = CreateChild(panelObject.transform, "ItemRoot", typeof(RectTransform), typeof(GridLayoutGroup)).transform;
        Image characterPreview = CreateImage(panelObject.transform, "CharacterPreview");
        Image propsPreview = CreateImage(panelObject.transform, "PropsPreview");
        SelectionItemView itemViewPrefab = CreateItemViewPrefab(root.transform);

        SelectionCategoryDefinition[] categories =
        {
            CreateCategory(SelectionCategoryType.Character, characterButton, 2),
            CreateCategory(SelectionCategoryType.Background, backgroundButton, 2),
            CreateCategory(SelectionCategoryType.Props, propsButton, 5)
        };

        SetField(controller, "backButton", backButton);
        SetField(controller, "submitButton", submitButton);
        SetField(controller, "itemRoot", itemRoot);
        SetField(controller, "itemViewPrefab", itemViewPrefab);
        SetField(controller, "categories", categories);
        SetField(
            controller,
            "requiredCategories",
            backgroundOnly
                ? new[] { SelectionCategoryType.Background }
                : new[]
                {
                    SelectionCategoryType.Character,
                    SelectionCategoryType.Background,
                    SelectionCategoryType.Props
                });
        SetField(
            controller,
            "initialCategory",
            backgroundOnly ? SelectionCategoryType.Background : SelectionCategoryType.Character);
        SetField(controller, "characterPreview", characterPreview);
        SetField(controller, "propsPreview", propsPreview);

        GameObject[] backgroundPreviews = categories[1].Items
            .Select(item => item.PreviewObject)
            .ToArray();

        return new TestPanel(
            panelObject,
            controller,
            backButton,
            submitButton,
            characterButton,
            backgroundButton,
            propsButton,
            itemRoot,
            backgroundPreviews,
            characterPreview,
            propsPreview);
    }

    private SelectionCategoryDefinition CreateCategory(
        SelectionCategoryType categoryType,
        Button categoryButton,
        int itemCount)
    {
        SelectionCategoryDefinition category = new SelectionCategoryDefinition();
        SetField(category, "categoryType", categoryType);
        SetField(category, "categoryButton", categoryButton);

        SelectionItemDefinition[] items = new SelectionItemDefinition[itemCount];
        for (int itemIndex = 0; itemIndex < itemCount; itemIndex++)
        {
            SelectionItemDefinition item = new SelectionItemDefinition();
            Sprite sprite = CreateSprite();
            SetField(item, "itemId", $"{categoryType}-{itemIndex}");
            SetField(item, "iconSprite", sprite);
            SetField(item, "previewSprite", sprite);

            if (categoryType == SelectionCategoryType.Background)
            {
                GameObject previewObject = Track(new GameObject($"BackgroundPreview-{itemIndex}"));
                previewObject.SetActive(false);
                SetField(item, "previewObject", previewObject);
            }

            items[itemIndex] = item;
        }

        SetField(category, "items", items);
        return category;
    }

    private SelectionItemView CreateItemViewPrefab(Transform parent)
    {
        GameObject prefabObject = new GameObject(
            "SelectionItemPrefab",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        prefabObject.transform.SetParent(parent);

        Button button = prefabObject.GetComponent<Button>();
        Image icon = CreateImage(prefabObject.transform, "Icon");
        GameObject selectedMark = CreateImage(prefabObject.transform, "SelectedMark").gameObject;
        SelectionItemView itemView = prefabObject.AddComponent<SelectionItemView>();
        SetField(itemView, "button", button);
        SetField(itemView, "iconImage", icon);
        SetField(itemView, "selectedMark", selectedMark);
        selectedMark.SetActive(false);
        prefabObject.SetActive(false);
        return itemView;
    }

    private void SelectFirstItemInEveryCategory(TestPanel panel)
    {
        GetActiveViews(panel.ItemRoot)[0].Button.onClick.Invoke();
        panel.BackgroundButton.onClick.Invoke();
        GetActiveViews(panel.ItemRoot)[0].Button.onClick.Invoke();
        panel.PropsButton.onClick.Invoke();
        GetActiveViews(panel.ItemRoot)[0].Button.onClick.Invoke();
        panel.CharacterButton.onClick.Invoke();
    }

    private static SelectionItemView[] GetActiveViews(Transform itemRoot)
    {
        return itemRoot
            .GetComponentsInChildren<SelectionItemView>(false)
            .OrderBy(view => view.transform.GetSiblingIndex())
            .ToArray();
    }

    private Button CreateButton(Transform parent, string name)
    {
        GameObject buttonObject = CreateChild(
            parent,
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        return buttonObject.GetComponent<Button>();
    }

    private Image CreateImage(Transform parent, string name)
    {
        GameObject imageObject = CreateChild(
            parent,
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        return imageObject.GetComponent<Image>();
    }

    private GameObject CreateChild(Transform parent, string name, params System.Type[] components)
    {
        GameObject child = new GameObject(name, components);
        child.transform.SetParent(parent);
        return child;
    }

    private Sprite CreateSprite()
    {
        Texture2D texture = Track(new Texture2D(2, 2));
        Sprite sprite = Track(Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f)));
        return sprite;
    }

    private T Track<T>(T createdObject) where T : Object
    {
        createdObjects.Add(createdObject);
        return createdObject;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Field {fieldName} was not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private sealed class TestPanel
    {
        public TestPanel(
            GameObject panelObject,
            SelectionPanelController controller,
            Button backButton,
            Button submitButton,
            Button characterButton,
            Button backgroundButton,
            Button propsButton,
            Transform itemRoot,
            GameObject[] backgroundPreviews,
            Image characterPreview,
            Image propsPreview)
        {
            PanelObject = panelObject;
            Controller = controller;
            BackButton = backButton;
            SubmitButton = submitButton;
            CharacterButton = characterButton;
            BackgroundButton = backgroundButton;
            PropsButton = propsButton;
            ItemRoot = itemRoot;
            BackgroundPreviews = backgroundPreviews;
            CharacterPreview = characterPreview;
            PropsPreview = propsPreview;
        }

        public GameObject PanelObject { get; }
        public SelectionPanelController Controller { get; }
        public Button BackButton { get; }
        public Button SubmitButton { get; }
        public Button CharacterButton { get; }
        public Button BackgroundButton { get; }
        public Button PropsButton { get; }
        public Transform ItemRoot { get; }
        public GameObject[] BackgroundPreviews { get; }
        public Image CharacterPreview { get; }
        public Image PropsPreview { get; }
    }
}
