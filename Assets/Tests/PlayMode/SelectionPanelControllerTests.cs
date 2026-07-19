using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
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
    public IEnumerator CharacterAndPropsCreateOneInstancePerItemAndCommitComposition()
    {
        TestPanel panel = CreatePanel();
        int submittedCount = 0;
        panel.Controller.OnSubmitted.AddListener(() => submittedCount++);

        panel.Controller.OpenPanel();
        yield return null;

        Assert.That(GetActiveViews(panel.ItemRoot), Has.Length.EqualTo(2));
        Assert.That(panel.SubmitButton.interactable, Is.False);

        GetActiveViews(panel.ItemRoot)[1].Button.onClick.Invoke();
        GetActiveViews(panel.ItemRoot)[1].Button.onClick.Invoke();
        Assert.That(GetPlacedViews(panel.Layouts[0].CharacterPreview.rectTransform), Has.Length.EqualTo(1));
        Assert.That(GetActiveViews(panel.ItemRoot).Count(view => view.IsSelected), Is.EqualTo(1));

        panel.BackgroundButton.onClick.Invoke();
        GetActiveViews(panel.ItemRoot)[0].Button.onClick.Invoke();
        panel.PropsButton.onClick.Invoke();
        GetActiveViews(panel.ItemRoot)[4].Button.onClick.Invoke();

        Assert.That(GetPlacedViews(panel.Layouts[0].PropsPreview.rectTransform), Has.Length.EqualTo(1));
        Assert.That(panel.SubmitButton.interactable, Is.True);

        panel.SubmitButton.onClick.Invoke();
        Assert.That(panel.PanelObject.activeSelf, Is.False);
        Assert.That(submittedCount, Is.EqualTo(1));
        Assert.That(panel.Controller.GetCommittedPlacements(), Has.Length.EqualTo(2));
        Assert.That(panel.Controller.GetCommittedSelectionIndex(SelectionCategoryType.Character), Is.EqualTo(1));
        Assert.That(panel.Controller.GetCommittedSelectionIndex(SelectionCategoryType.Background), Is.EqualTo(0));
        Assert.That(panel.Controller.GetCommittedSelectionIndex(SelectionCategoryType.Props), Is.EqualTo(4));

        SelectionCompositionData composition = panel.Controller.GetCommittedComposition();
        Assert.That(composition.BackgroundItemId, Is.EqualTo("background-0"));
        Assert.That(composition.Placements.Select(data => data.ItemId),
            Is.EquivalentTo(new[] { "character-1", "props-4" }));
        Assert.That(composition.Placements.All(
            data => data.NormalizedPosition == new Vector2(0.5f, 0.5f)), Is.True);

        panel.Controller.OpenPanel();
        yield return null;

        Assert.That(GetActiveViews(panel.ItemRoot)[1].IsSelected, Is.True);
        Assert.That(GetPlacedViews(panel.Layouts[0].CharacterPreview.rectTransform), Has.Length.EqualTo(1));
        Assert.That(GetPlacedViews(panel.Layouts[0].PropsPreview.rectTransform), Has.Length.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator BackDiscardsAddedAndDeletedDraftInstances()
    {
        TestPanel panel = CreatePanel(backgroundOnly: true);
        panel.Controller.OpenPanel();
        yield return null;

        panel.CharacterButton.onClick.Invoke();
        GetActiveViews(panel.ItemRoot)[0].Button.onClick.Invoke();
        panel.SubmitButton.onClick.Invoke();
        Assert.That(panel.Controller.GetCommittedPlacements(), Has.Length.EqualTo(1));

        panel.Controller.OpenPanel();
        yield return null;

        GetActiveViews(panel.ItemRoot)[1].Button.onClick.Invoke();
        Assert.That(GetPlacedViews(panel.Layouts[0].CharacterPreview.rectTransform), Has.Length.EqualTo(2));
        Assert.That(panel.Controller.DeleteSelectedDraftItem(), Is.True);
        Assert.That(GetPlacedViews(panel.Layouts[0].CharacterPreview.rectTransform), Has.Length.EqualTo(1));

        GetActiveViews(panel.ItemRoot)[1].Button.onClick.Invoke();
        Assert.That(GetPlacedViews(panel.Layouts[0].CharacterPreview.rectTransform), Has.Length.EqualTo(2));
        Assert.That(panel.Controller.DeleteSelectedDraftItem(), Is.True);

        GetActiveViews(panel.ItemRoot)[0].Button.onClick.Invoke();
        Assert.That(panel.Controller.DeleteSelectedDraftItem(), Is.True);
        Assert.That(GetPlacedViews(panel.Layouts[0].CharacterPreview.rectTransform), Is.Empty);

        panel.BackButton.onClick.Invoke();
        panel.Controller.OpenPanel();
        yield return null;

        SelectionPlacedItemView[] restored =
            GetPlacedViews(panel.Layouts[0].CharacterPreview.rectTransform);
        Assert.That(restored, Has.Length.EqualTo(1));
        Assert.That(restored[0].ItemId, Is.EqualTo("character-main"));
        Assert.That(panel.Controller.GetCommittedPlacements(), Has.Length.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator UncommittedPlacementIsDiscardedAndInvalidConfigurationCannotSubmit()
    {
        TestPanel panel = CreatePanel();
        panel.Controller.OpenPanel();
        yield return null;

        GetActiveViews(panel.ItemRoot)[0].Button.onClick.Invoke();
        Assert.That(panel.CharacterPreview.enabled, Is.False);
        Assert.That(GetPlacedViews(panel.CharacterPreview.rectTransform), Has.Length.EqualTo(1));
        panel.BackButton.onClick.Invoke();

        panel.Controller.OpenPanel();
        yield return null;

        Assert.That(panel.Controller.GetCommittedSelectionIndex(SelectionCategoryType.Character), Is.EqualTo(-1));
        Assert.That(GetPlacedViews(panel.CharacterPreview.rectTransform), Is.Empty);
        Assert.That(panel.SubmitButton.interactable, Is.False);

        SelectionCategoryDefinition[] invalidCategories =
        {
            CreateCategory(SelectionCategoryType.Character, 2),
            CreateCategory(SelectionCategoryType.Background, 0),
            CreateCategory(SelectionCategoryType.Props, 5)
        };
        SetField(panel.Controller, "categories", invalidCategories);

        panel.Controller.OpenPanel();
        yield return null;

        Assert.That(panel.SubmitButton.interactable, Is.False);
        Assert.That(panel.Layouts.All(layout => !layout.Root.activeSelf), Is.True);
    }

    [UnityTest]
    public IEnumerator BackgroundDefaultsToFirstAndRestoresCommittedPreview()
    {
        TestPanel panel = CreatePanel(true);
        panel.Controller.OpenPanel();
        yield return null;

        Assert.That(GetActiveViews(panel.ItemRoot), Has.Length.EqualTo(2));
        Assert.That(GetActiveViews(panel.ItemRoot)[0].IsSelected, Is.True);
        Sprite defaultItemBackground = GetActiveViews(panel.ItemRoot)[0].BackgroundImage.sprite;
        Assert.That(panel.Layouts[0].Root.activeSelf, Is.True);
        Assert.That(panel.Layouts[1].Root.activeSelf, Is.False);
        Assert.That(panel.SubmitButton.interactable, Is.True);

        GetActiveViews(panel.ItemRoot)[1].Button.onClick.Invoke();
        Assert.That(panel.Layouts[0].Root.activeSelf, Is.False);
        Assert.That(panel.Layouts[1].Root.activeSelf, Is.True);
        Assert.That(GetActiveViews(panel.Layouts[1].ItemRoot), Has.Length.EqualTo(2));
        Assert.That(
            GetActiveViews(panel.Layouts[1].ItemRoot)[0].BackgroundImage.sprite,
            Is.SameAs(panel.Layouts[1].ItemBackgroundSprite));

        panel.Layouts[1].PropsButton.onClick.Invoke();
        Assert.That(GetActiveViews(panel.Layouts[1].ItemRoot), Has.Length.EqualTo(5));
        panel.Layouts[1].BackgroundButton.onClick.Invoke();
        Assert.That(GetActiveViews(panel.Layouts[1].ItemRoot)[1].IsSelected, Is.True);

        panel.Layouts[1].SubmitButton.onClick.Invoke();
        Assert.That(panel.Controller.GetCommittedSelectionIndex(SelectionCategoryType.Background), Is.EqualTo(1));

        panel.Controller.OpenPanel();
        yield return null;

        Assert.That(GetActiveViews(panel.Layouts[1].ItemRoot)[1].IsSelected, Is.True);
        Assert.That(panel.Layouts[0].Root.activeSelf, Is.False);
        Assert.That(panel.Layouts[1].Root.activeSelf, Is.True);

        GetActiveViews(panel.Layouts[1].ItemRoot)[0].Button.onClick.Invoke();
        Assert.That(
            GetActiveViews(panel.Layouts[0].ItemRoot)[0].BackgroundImage.sprite,
            Is.SameAs(defaultItemBackground));
        panel.Layouts[0].BackButton.onClick.Invoke();
        Assert.That(panel.Layouts[0].Root.activeSelf, Is.False);
        Assert.That(panel.Layouts[1].Root.activeSelf, Is.True);
        Assert.That(panel.Controller.GetCommittedSelectionIndex(SelectionCategoryType.Background), Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator SingleBackgroundIsAutomaticallySelected()
    {
        TestPanel panel = CreatePanel(true, 1, 1);
        panel.Controller.OpenPanel();
        yield return null;

        Assert.That(panel.Controller.IsConfigurationValid(), Is.True);
        Assert.That(GetActiveViews(panel.ItemRoot), Has.Length.EqualTo(1));
        Assert.That(GetActiveViews(panel.ItemRoot)[0].IsSelected, Is.True);
        Assert.That(panel.Layouts[0].Root.activeSelf, Is.True);
        Assert.That(panel.SubmitButton.interactable, Is.True);
    }

    [UnityTest]
    public IEnumerator CharacterAndPropsPlacementsFollowTheSelectedBackgroundLayout()
    {
        TestPanel panel = CreatePanel();
        panel.Controller.OpenPanel();
        yield return null;

        GetActiveViews(panel.Layouts[0].ItemRoot)[1].Button.onClick.Invoke();
        panel.Layouts[0].PropsButton.onClick.Invoke();
        GetActiveViews(panel.Layouts[0].ItemRoot)[4].Button.onClick.Invoke();

        SelectionPlacedItemView character =
            GetPlacedViews(panel.Layouts[0].CharacterPreview.rectTransform).Single();
        SelectionPlacedItemView props =
            GetPlacedViews(panel.Layouts[0].PropsPreview.rectTransform).Single();

        panel.Layouts[0].BackgroundButton.onClick.Invoke();
        GetActiveViews(panel.Layouts[0].ItemRoot)[1].Button.onClick.Invoke();

        Assert.That(character.transform.parent,
            Is.SameAs(panel.Layouts[1].CharacterPreview.rectTransform));
        Assert.That(props.transform.parent,
            Is.SameAs(panel.Layouts[1].PropsPreview.rectTransform));
        Assert.That(character.RectTransform.localPosition, Is.EqualTo(Vector3.zero));
        Assert.That(props.RectTransform.localPosition, Is.EqualTo(Vector3.zero));
        Assert.That(panel.Layouts[0].CharacterPreview.enabled, Is.False);
        Assert.That(panel.Layouts[1].CharacterPreview.enabled, Is.False);
        Assert.That(panel.Layouts[1].SubmitButton.interactable, Is.True);
    }

    [UnityTest]
    public IEnumerator CollectionFiltersCharacterAndPropsAndResetsForNewStory()
    {
        TestPanel panel = CreatePanel(unlockAllItems: false);
        panel.Controller.OpenPanel();
        yield return null;

        Assert.That(GetActiveViews(panel.ItemRoot), Has.Length.EqualTo(1));
        Assert.That(panel.Controller.IsItemAvailable(
            SelectionCategoryType.Character,
            "character-main"), Is.True);
        Assert.That(panel.Controller.IsItemAvailable(
            SelectionCategoryType.Character,
            "character-1"), Is.False);

        panel.PropsButton.onClick.Invoke();
        Assert.That(GetActiveViews(panel.ItemRoot), Is.Empty);
        Assert.That(panel.Controller.CollectItem(
            SelectionCategoryType.Props,
            "props-3"), Is.True);
        Assert.That(panel.Controller.TryGetItemIcon(
            SelectionCategoryType.Props,
            "props-3",
            out Sprite collectedIcon), Is.True);
        Assert.That(collectedIcon, Is.Not.Null);
        Assert.That(panel.Controller.CollectItem(
            SelectionCategoryType.Props,
            "props-3"), Is.False);
        Assert.That(panel.Controller.IsItemCollected(
            SelectionCategoryType.Props,
            "props-3"), Is.True);
        Assert.That(GetActiveViews(panel.ItemRoot), Has.Length.EqualTo(1));

        GetActiveViews(panel.ItemRoot)[0].Button.onClick.Invoke();
        panel.CharacterButton.onClick.Invoke();
        GetActiveViews(panel.ItemRoot)[0].Button.onClick.Invoke();
        Assert.That(panel.SubmitButton.interactable, Is.True);
        panel.SubmitButton.onClick.Invoke();
        Assert.That(panel.Controller.GetCommittedItemId(
            SelectionCategoryType.Props), Is.EqualTo("props-3"));

        panel.Controller.ResetCollectedItems();
        panel.Controller.OpenPanel();
        yield return null;

        Assert.That(panel.Controller.GetCommittedPlacements(), Has.Length.EqualTo(1));
        Assert.That(panel.Controller.GetCommittedPlacements()[0].ItemId,
            Is.EqualTo("character-main"));
        Assert.That(GetActiveViews(panel.ItemRoot), Has.Length.EqualTo(1));
        panel.PropsButton.onClick.Invoke();
        Assert.That(GetActiveViews(panel.ItemRoot), Is.Empty);
        Assert.That(panel.Controller.IsItemCollected(
            SelectionCategoryType.Props,
            "props-3"), Is.False);
    }

    [UnityTest]
    public IEnumerator MoveRotateAndScaleUpdateCommittedPlacement()
    {
        TestPanel panel = CreatePanel(backgroundOnly: true);
        panel.Controller.OpenPanel();
        yield return null;

        panel.CharacterButton.onClick.Invoke();
        GetActiveViews(panel.ItemRoot)[0].Button.onClick.Invoke();
        SelectionPlacedItemView placed =
            GetPlacedViews(panel.Layouts[0].CharacterPreview.rectTransform).Single();
        EventSystem eventSystem = CreateEventSystem();

        Vector2 center = RectTransformUtility.WorldToScreenPoint(
            null,
            placed.RectTransform.position);
        PointerEventData beginMove = new PointerEventData(eventSystem)
        {
            position = center
        };
        placed.OnBeginDrag(beginMove);
        PointerEventData move = new PointerEventData(eventSystem)
        {
            position = center + new Vector2(30f, 20f)
        };
        placed.OnDrag(move);

        panel.Controller.SetPlacementToolMode(SelectionPlacementToolMode.Rotate);
        Vector2 movedCenter = RectTransformUtility.WorldToScreenPoint(
            null,
            placed.RectTransform.position);
        PointerEventData beginRotate = new PointerEventData(eventSystem)
        {
            position = movedCenter + Vector2.right * 20f
        };
        placed.OnBeginDrag(beginRotate);
        PointerEventData rotate = new PointerEventData(eventSystem)
        {
            position = movedCenter + Vector2.up * 20f
        };
        placed.OnDrag(rotate);

        panel.Controller.SetPlacementToolMode(SelectionPlacementToolMode.Scale);
        PointerEventData beginScale = new PointerEventData(eventSystem)
        {
            position = movedCenter + Vector2.right * 20f
        };
        placed.OnBeginDrag(beginScale);
        PointerEventData scale = new PointerEventData(eventSystem)
        {
            position = movedCenter + Vector2.right * 40f
        };
        placed.OnDrag(scale);

        panel.SubmitButton.onClick.Invoke();
        Assert.That(panel.Controller.TryGetCommittedPlacement(
            SelectionCategoryType.Character,
            "character-main",
            out SelectionPlacedItemData committed), Is.True);
        Assert.That(committed.NormalizedPosition.x, Is.EqualTo(0.8f).Within(0.02f));
        Assert.That(committed.NormalizedPosition.y, Is.EqualTo(0.7f).Within(0.02f));
        Assert.That(committed.RotationDegrees, Is.EqualTo(90f).Within(0.5f));
        Assert.That(committed.Scale, Is.EqualTo(2f).Within(0.05f));

        SelectionPlacedItemData[] firstCopy = panel.Controller.GetCommittedPlacements();
        SelectionPlacedItemData[] secondCopy = panel.Controller.GetCommittedPlacements();
        Assert.That(firstCopy, Is.Not.SameAs(secondCopy));
        Assert.That(firstCopy[0], Is.Not.SameAs(secondCopy[0]));
    }

    [UnityTest]
    public IEnumerator SnapshotFillsRecordImageByCenterCroppingAndResetRestoresTarget()
    {
        TestPanel panel = CreatePanel(backgroundOnly: true);
        RawImage recordImage = CreateRawImage(
            panel.PanelObject.transform,
            "RecordImage");
        recordImage.rectTransform.sizeDelta = new Vector2(100f, 50f);
        recordImage.color = Color.black;

        Texture2D source = Track(new Texture2D(4, 4, TextureFormat.RGBA32, false));
        Color32[] sourcePixels = Enumerable
            .Repeat(new Color32(255, 0, 0, 255), 16)
            .ToArray();
        source.SetPixels32(sourcePixels);
        source.Apply();
        SetField(panel.Controller, "committedSnapshotSource", source);

        Assert.That(panel.Controller.ApplyCommittedSnapshot(recordImage), Is.True);
        Texture2D fitted = recordImage.texture as Texture2D;
        Assert.That(fitted, Is.Not.Null);
        Assert.That(fitted.width, Is.EqualTo(4));
        Assert.That(fitted.height, Is.EqualTo(2));
        Assert.That(fitted.GetPixel(0, 0), Is.EqualTo(Color.red));
        Assert.That(fitted.GetPixel(3, 1), Is.EqualTo(Color.red));
        Assert.That(recordImage.color, Is.EqualTo(Color.white));

        panel.Controller.ResetSelections();
        Assert.That(recordImage.texture, Is.Null);
        Assert.That(recordImage.color, Is.EqualTo(Color.black));
        yield return null;
    }

    [UnityTest]
    public IEnumerator InvalidBackgroundCountsCannotSubmit()
    {
        TestPanel empty = CreatePanel(true, 0, 0);
        empty.Controller.OpenPanel();
        yield return null;
        Assert.That(empty.Controller.IsConfigurationValid(), Is.False);

        TestPanel tooMany = CreatePanel(true, 3, 3);
        tooMany.Controller.OpenPanel();
        yield return null;
        Assert.That(tooMany.Controller.IsConfigurationValid(), Is.False);
        Assert.That(tooMany.SubmitButton.interactable, Is.False);

        TestPanel mismatched = CreatePanel(true, 2, 1);
        mismatched.Controller.OpenPanel();
        yield return null;
        Assert.That(mismatched.Controller.IsConfigurationValid(), Is.False);
        Assert.That(mismatched.SubmitButton.interactable, Is.False);

        TestPanel missingRoot = CreatePanel(true, 2, 2);
        SetField(missingRoot.Layouts[0].Definition, "layoutRoot", null);
        missingRoot.Controller.OpenPanel();
        yield return null;
        Assert.That(missingRoot.Controller.IsConfigurationValid(), Is.False);
        Assert.That(missingRoot.SubmitButton.interactable, Is.False);

        TestPanel duplicateRoot = CreatePanel(true, 2, 2);
        SetField(
            duplicateRoot.Layouts[1].Definition,
            "layoutRoot",
            duplicateRoot.Layouts[0].Root);
        duplicateRoot.Controller.OpenPanel();
        yield return null;
        Assert.That(duplicateRoot.Controller.IsConfigurationValid(), Is.False);
        Assert.That(duplicateRoot.SubmitButton.interactable, Is.False);

        TestPanel nestedRoot = CreatePanel(true, 2, 2);
        GameObject wrapper = CreateChild(nestedRoot.PanelObject.transform, "Wrapper");
        nestedRoot.Layouts[0].Root.transform.SetParent(wrapper.transform);
        nestedRoot.Controller.OpenPanel();
        yield return null;
        Assert.That(nestedRoot.Controller.IsConfigurationValid(), Is.False);
        Assert.That(nestedRoot.SubmitButton.interactable, Is.False);
    }

    private TestPanel CreatePanel(
        bool backgroundOnly = false,
        int backgroundItemCount = 2,
        int layoutCount = 2,
        bool unlockAllItems = true)
    {
        GameObject root = Track(new GameObject("Selection Test Root"));
        GameObject panelObject = new GameObject("BBSChoose", typeof(RectTransform));
        panelObject.transform.SetParent(root.transform);
        panelObject.SetActive(false);

        SelectionPanelController controller = panelObject.AddComponent<SelectionPanelController>();
        TestLayout[] layouts = new TestLayout[layoutCount];
        SelectionBackgroundLayoutDefinition[] layoutDefinitions =
            new SelectionBackgroundLayoutDefinition[layoutCount];
        for (int layoutIndex = 0; layoutIndex < layoutCount; layoutIndex++)
        {
            layouts[layoutIndex] = CreateLayout(panelObject.transform, layoutIndex);
            layoutDefinitions[layoutIndex] = layouts[layoutIndex].Definition;
        }

        SelectionItemView itemViewPrefab = CreateItemViewPrefab(root.transform);
        SelectionPlacedItemView placedItemViewPrefab = CreatePlacedItemViewPrefab(root.transform);

        SelectionCategoryDefinition[] categories =
        {
            CreateCategory(SelectionCategoryType.Character, 2),
            CreateCategory(SelectionCategoryType.Background, backgroundItemCount),
            CreateCategory(SelectionCategoryType.Props, 5)
        };

        SetField(controller, "itemViewPrefab", itemViewPrefab);
        SetField(controller, "placedItemViewPrefab", placedItemViewPrefab);
        SetField(controller, "categories", categories);
        SetField(controller, "backgroundLayouts", layoutDefinitions);
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

        if (unlockAllItems)
        {
            foreach (SelectionCategoryDefinition category in categories)
            {
                if (category.CategoryType == SelectionCategoryType.Background)
                {
                    continue;
                }

                foreach (SelectionItemDefinition item in category.Items)
                {
                    if (item != null && !item.UnlockedByDefault)
                    {
                        controller.CollectItem(category.CategoryType, item.ItemId);
                    }
                }
            }
        }

        return new TestPanel(panelObject, controller, layouts);
    }

    private TestLayout CreateLayout(Transform panelRoot, int layoutIndex)
    {
        GameObject layoutRoot = CreateChild(
            panelRoot,
            $"BackgroundLayout-{layoutIndex}",
            typeof(RectTransform));
        Button backButton = CreateButton(layoutRoot.transform, "Back");
        Button submitButton = CreateButton(layoutRoot.transform, "Post");
        Button characterButton = CreateButton(layoutRoot.transform, "Character");
        Button backgroundButton = CreateButton(layoutRoot.transform, "Background");
        Button propsButton = CreateButton(layoutRoot.transform, "Props");
        Transform itemRoot = CreateChild(
            layoutRoot.transform,
            "ItemRoot",
            typeof(RectTransform),
            typeof(GridLayoutGroup)).transform;
        Image characterPreview = CreateImage(layoutRoot.transform, "CharacterPreview");
        Image propsPreview = CreateImage(layoutRoot.transform, "PropsPreview");
        characterPreview.rectTransform.sizeDelta = new Vector2(100f, 100f);
        propsPreview.rectTransform.sizeDelta = new Vector2(100f, 100f);
        Sprite itemBackgroundSprite = layoutIndex == 0 ? null : CreateSprite();

        SelectionBackgroundLayoutDefinition definition =
            new SelectionBackgroundLayoutDefinition();
        SetField(definition, "layoutRoot", layoutRoot);
        SetField(definition, "backButton", backButton);
        SetField(definition, "submitButton", submitButton);
        SetField(definition, "characterButton", characterButton);
        SetField(definition, "backgroundButton", backgroundButton);
        SetField(definition, "propsButton", propsButton);
        SetField(definition, "itemRoot", itemRoot);
        SetField(definition, "characterPreview", characterPreview);
        SetField(definition, "propsPreview", propsPreview);
        SetField(definition, "itemBackgroundSprite", itemBackgroundSprite);
        layoutRoot.SetActive(false);

        return new TestLayout(
            definition,
            layoutRoot,
            backButton,
            submitButton,
            characterButton,
            backgroundButton,
            propsButton,
            itemRoot,
            characterPreview,
            propsPreview,
            itemBackgroundSprite);
    }

    private SelectionCategoryDefinition CreateCategory(
        SelectionCategoryType categoryType,
        int itemCount)
    {
        SelectionCategoryDefinition category = new SelectionCategoryDefinition();
        SetField(category, "categoryType", categoryType);

        SelectionItemDefinition[] items = new SelectionItemDefinition[itemCount];
        for (int itemIndex = 0; itemIndex < itemCount; itemIndex++)
        {
            SelectionItemDefinition item = new SelectionItemDefinition();
            Sprite sprite = CreateSprite();
            string itemId = categoryType == SelectionCategoryType.Character
                ? (itemIndex == 0 ? "character-main" : $"character-{itemIndex}")
                : categoryType == SelectionCategoryType.Props
                    ? $"props-{itemIndex}"
                    : $"background-{itemIndex}";
            SetField(item, "itemId", itemId);
            SetField(item, "iconSprite", sprite);
            SetField(item, "previewSprite", sprite);
            SetField(
                item,
                "unlockedByDefault",
                categoryType == SelectionCategoryType.Character && itemIndex == 0);
            SetField(item, "initialDisplayScale", 1f);

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
        Image backgroundImage = prefabObject.GetComponent<Image>();
        backgroundImage.sprite = CreateSprite();
        Image icon = CreateImage(prefabObject.transform, "Icon");
        GameObject selectedMark = CreateImage(prefabObject.transform, "SelectedMark").gameObject;
        SelectionItemView itemView = prefabObject.AddComponent<SelectionItemView>();
        SetField(itemView, "button", button);
        SetField(itemView, "backgroundImage", backgroundImage);
        SetField(itemView, "iconImage", icon);
        SetField(itemView, "selectedMark", selectedMark);
        selectedMark.SetActive(false);
        prefabObject.SetActive(false);
        return itemView;
    }

    private SelectionPlacedItemView CreatePlacedItemViewPrefab(Transform parent)
    {
        GameObject prefabObject = new GameObject(
            "SelectionPlacedItemPrefab",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        prefabObject.transform.SetParent(parent);

        Image image = prefabObject.GetComponent<Image>();
        SelectionPlacedItemView placedItemView = prefabObject.AddComponent<SelectionPlacedItemView>();
        SetField(placedItemView, "itemImage", image);
        prefabObject.SetActive(false);
        return placedItemView;
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

    private static SelectionPlacedItemView[] GetPlacedViews(Transform placementRoot)
    {
        return placementRoot
            .GetComponentsInChildren<SelectionPlacedItemView>(false)
            .OrderBy(view => view.transform.GetSiblingIndex())
            .ToArray();
    }

    private EventSystem CreateEventSystem()
    {
        GameObject eventSystemObject = Track(new GameObject("EventSystem", typeof(EventSystem)));
        return eventSystemObject.GetComponent<EventSystem>();
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

    private RawImage CreateRawImage(Transform parent, string name)
    {
        GameObject imageObject = CreateChild(
            parent,
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));
        return imageObject.GetComponent<RawImage>();
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
            TestLayout[] layouts)
        {
            PanelObject = panelObject;
            Controller = controller;
            Layouts = layouts;
        }

        public GameObject PanelObject { get; }
        public SelectionPanelController Controller { get; }
        public TestLayout[] Layouts { get; }
        public Button BackButton => Layouts[0].BackButton;
        public Button SubmitButton => Layouts[0].SubmitButton;
        public Button CharacterButton => Layouts[0].CharacterButton;
        public Button BackgroundButton => Layouts[0].BackgroundButton;
        public Button PropsButton => Layouts[0].PropsButton;
        public Transform ItemRoot => Layouts[0].ItemRoot;
        public Image CharacterPreview => Layouts[0].CharacterPreview;
        public Image PropsPreview => Layouts[0].PropsPreview;
    }

    private sealed class TestLayout
    {
        public TestLayout(
            SelectionBackgroundLayoutDefinition definition,
            GameObject root,
            Button backButton,
            Button submitButton,
            Button characterButton,
            Button backgroundButton,
            Button propsButton,
            Transform itemRoot,
            Image characterPreview,
            Image propsPreview,
            Sprite itemBackgroundSprite)
        {
            Definition = definition;
            Root = root;
            BackButton = backButton;
            SubmitButton = submitButton;
            CharacterButton = characterButton;
            BackgroundButton = backgroundButton;
            PropsButton = propsButton;
            ItemRoot = itemRoot;
            CharacterPreview = characterPreview;
            PropsPreview = propsPreview;
            ItemBackgroundSprite = itemBackgroundSprite;
        }

        public SelectionBackgroundLayoutDefinition Definition { get; }
        public GameObject Root { get; }
        public Button BackButton { get; }
        public Button SubmitButton { get; }
        public Button CharacterButton { get; }
        public Button BackgroundButton { get; }
        public Button PropsButton { get; }
        public Transform ItemRoot { get; }
        public Image CharacterPreview { get; }
        public Image PropsPreview { get; }
        public Sprite ItemBackgroundSprite { get; }
    }
}
