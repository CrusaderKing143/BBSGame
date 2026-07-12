using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class StoryFlowControllerTests
{
    [UnityTest]
    public IEnumerator FirstRoundSelectionUnlocksMappedResultPostBeforeAdvancing()
    {
        for (int selectedBackgroundIndex = 0; selectedBackgroundIndex < 2; selectedBackgroundIndex++)
        {
            GameObject root = new GameObject($"Branch Story Test Root {selectedBackgroundIndex}");
            MailPanelController mailController = root.AddComponent<MailPanelController>();
            ForumPanelController forumController = root.AddComponent<ForumPanelController>();
            StoryFlowController flow = root.AddComponent<StoryFlowController>();

            GameObject selectionPanelObject = CreateChild(root.transform, "Selection Panel");
            SelectionPanelController selectionPanel = selectionPanelObject.AddComponent<SelectionPanelController>();
            selectionPanelObject.SetActive(false);
            Sprite selectionSprite = ConfigureSelectionPanel(selectionPanelObject.transform, selectionPanel);

            Button mailIcon = CreateButton(root.transform, "Mail Icon");
            Button forumIcon = CreateButton(root.transform, "Forum Icon");
            Button pictureIcon = CreateButton(root.transform, "Picture Icon");
            Button mailBack = CreateButton(root.transform, "Mail Back");
            Button enterForum = CreateButton(root.transform, "Enter Forum");
            Button postBack = CreateButton(root.transform, "Post Back");

            GameObject mailPanel = CreateChild(root.transform, "Mail Panel");
            GameObject welcomePanel = CreateChild(root.transform, "Welcome Panel");
            GameObject postListPanel = CreateChild(root.transform, "Post List Panel");
            GameObject postContentPanel = CreateChild(root.transform, "Post Content Panel");

            StoryRoundData firstRound = CreateRound(root.transform, "01", 3);
            StoryRoundData secondRound = CreateRound(root.transform, "02", 1);
            Button resultPostButton = CreateButton(root.transform, "Post Button 04");
            GameObject jiuBaContent = CreateChild(root.transform, "Post Content 04");
            GameObject feijiContent = CreateChild(root.transform, "Post Content 05");
            firstRound.selectionPost = new SelectionPostData
            {
                categoryType = SelectionCategoryType.Background,
                button = resultPostButton,
                branches = new[]
                {
                    new SelectionPostBranchData { itemId = "jiuBa", contentImage = jiuBaContent },
                    new SelectionPostBranchData { itemId = "feiji", contentImage = feijiContent }
                }
            };
            StoryRoundData[] rounds = { firstRound, secondRound };

            SetField(mailController, "mailPanel", mailPanel);
            SetField(mailController, "backButton", mailBack);
            SetField(forumController, "welcomePanel", welcomePanel);
            SetField(forumController, "postListPanel", postListPanel);
            SetField(forumController, "postContentPanel", postContentPanel);
            SetField(forumController, "enterForumButton", enterForum);
            SetField(forumController, "postBackButton", postBack);

            SetField(flow, "mailButton", mailIcon);
            SetField(flow, "forumButton", forumIcon);
            SetField(flow, "pictureButton", pictureIcon);
            SetField(flow, "mailPanelController", mailController);
            SetField(flow, "forumPanelController", forumController);
            SetField(flow, "selectionPanelController", selectionPanel);
            SetField(flow, "rounds", rounds);

            yield return null;

            Assert.That(pictureIcon.interactable, Is.False);
            Assert.That(resultPostButton.gameObject.activeSelf, Is.False);

            mailIcon.onClick.Invoke();
            firstRound.mail.button.onClick.Invoke();
            mailBack.onClick.Invoke();
            forumIcon.onClick.Invoke();
            enterForum.onClick.Invoke();

            for (int postIndex = 0; postIndex < firstRound.posts.Length; postIndex++)
            {
                firstRound.posts[postIndex].button.onClick.Invoke();
                postBack.onClick.Invoke();
            }

            Assert.That(flow.CurrentRoundIndex, Is.EqualTo(0));
            Assert.That(pictureIcon.interactable, Is.True);
            Assert.That(resultPostButton.gameObject.activeSelf, Is.False);

            pictureIcon.onClick.Invoke();
            Assert.That(selectionPanelObject.activeSelf, Is.True);
            SelectionItemView[] backgroundViews = selectionPanelObject
                .GetComponentsInChildren<SelectionItemView>(false)
                .OrderBy(view => view.transform.GetSiblingIndex())
                .ToArray();
            Assert.That(backgroundViews, Has.Length.EqualTo(2));
            backgroundViews[selectedBackgroundIndex].Button.onClick.Invoke();

            GameObject jiuBaPreview = selectionPanelObject.transform.Find("jiuBa").gameObject;
            GameObject feijiPreview = selectionPanelObject.transform.Find("feiji").gameObject;
            Assert.That(jiuBaPreview.activeSelf, Is.EqualTo(selectedBackgroundIndex == 0));
            Assert.That(feijiPreview.activeSelf, Is.EqualTo(selectedBackgroundIndex == 1));

            Button selectionSubmit = selectionPanelObject.transform.Find("Selection Post").GetComponent<Button>();
            Assert.That(selectionSubmit.interactable, Is.True);
            selectionSubmit.onClick.Invoke();

            Assert.That(pictureIcon.interactable, Is.False);
            Assert.That(resultPostButton.gameObject.activeSelf, Is.True);
            Assert.That(postListPanel.activeSelf, Is.True);

            resultPostButton.onClick.Invoke();
            Assert.That(jiuBaContent.activeSelf, Is.EqualTo(selectedBackgroundIndex == 0));
            Assert.That(feijiContent.activeSelf, Is.EqualTo(selectedBackgroundIndex == 1));
            postBack.onClick.Invoke();

            Assert.That(flow.CurrentRoundIndex, Is.EqualTo(1));
            mailIcon.onClick.Invoke();
            Assert.That(firstRound.mail.button.gameObject.activeSelf, Is.True);
            Assert.That(secondRound.mail.button.gameObject.activeSelf, Is.True);
            firstRound.mail.button.onClick.Invoke();
            Assert.That(firstRound.mail.contentImage.activeSelf, Is.True);
            Assert.That(flow.CurrentRoundIndex, Is.EqualTo(1));
            Assert.That(forumIcon.interactable, Is.False);

            secondRound.mail.button.onClick.Invoke();
            Assert.That(forumIcon.interactable, Is.True);

            Object.Destroy(selectionSprite);
            Object.Destroy(root);
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator LinearFlowUnlocksPostsAndReturnsToNextMail()
    {
        GameObject root = new GameObject("Story Test Root");
        MailPanelController mailController = root.AddComponent<MailPanelController>();
        ForumPanelController forumController = root.AddComponent<ForumPanelController>();
        StoryFlowController flow = root.AddComponent<StoryFlowController>();

        GameObject selectionPanelObject = CreateChild(root.transform, "Selection Panel");
        SelectionPanelController selectionPanel = selectionPanelObject.AddComponent<SelectionPanelController>();
        selectionPanelObject.SetActive(false);
        Sprite selectionSprite = ConfigureSelectionPanel(selectionPanelObject.transform, selectionPanel);

        Button mailIcon = CreateButton(root.transform, "Mail Icon");
        Button forumIcon = CreateButton(root.transform, "Forum Icon");
        Button pictureIcon = CreateButton(root.transform, "Picture Icon");
        Button mailBack = CreateButton(root.transform, "Mail Back");
        Button enterForum = CreateButton(root.transform, "Enter Forum");
        Button postBack = CreateButton(root.transform, "Post Back");

        GameObject mailPanel = CreateChild(root.transform, "Mail Panel");
        GameObject welcomePanel = CreateChild(root.transform, "Welcome Panel");
        GameObject postListPanel = CreateChild(root.transform, "Post List Panel");
        GameObject postContentPanel = CreateChild(root.transform, "Post Content Panel");

        StoryRoundData firstRound = CreateRound(root.transform, "01", 2);
        StoryRoundData secondRound = CreateRound(root.transform, "02", 1);
        StoryRoundData thirdRound = CreateRound(root.transform, "03", 0);
        StoryRoundData[] rounds = { firstRound, secondRound, thirdRound };

        SetField(mailController, "mailPanel", mailPanel);
        SetField(mailController, "backButton", mailBack);
        SetField(forumController, "welcomePanel", welcomePanel);
        SetField(forumController, "postListPanel", postListPanel);
        SetField(forumController, "postContentPanel", postContentPanel);
        SetField(forumController, "enterForumButton", enterForum);
        SetField(forumController, "postBackButton", postBack);

        SetField(flow, "mailButton", mailIcon);
        SetField(flow, "forumButton", forumIcon);
        SetField(flow, "pictureButton", pictureIcon);
        SetField(flow, "mailPanelController", mailController);
        SetField(flow, "forumPanelController", forumController);
        SetField(flow, "selectionPanelController", selectionPanel);
        SetField(flow, "rounds", rounds);

        yield return null;

        Assert.That(mailIcon.interactable, Is.True);
        Assert.That(forumIcon.interactable, Is.False);
        Assert.That(pictureIcon.interactable, Is.False);

        mailIcon.onClick.Invoke();
        Assert.That(mailPanel.activeSelf, Is.True);
        Assert.That(firstRound.mail.button.gameObject.activeSelf, Is.True);
        Assert.That(secondRound.mail.button.gameObject.activeSelf, Is.False);

        firstRound.mail.button.onClick.Invoke();
        Assert.That(firstRound.mail.contentImage.activeSelf, Is.True);
        Assert.That(forumIcon.interactable, Is.True);

        mailBack.onClick.Invoke();
        forumIcon.onClick.Invoke();
        Assert.That(welcomePanel.activeSelf, Is.True);

        enterForum.onClick.Invoke();
        Assert.That(postListPanel.activeSelf, Is.True);
        Assert.That(firstRound.posts[0].button.gameObject.activeSelf, Is.True);
        Assert.That(firstRound.posts[1].button.gameObject.activeSelf, Is.False);

        firstRound.posts[0].button.onClick.Invoke();
        Assert.That(firstRound.posts[0].contentImage.activeSelf, Is.True);
        postBack.onClick.Invoke();
        Assert.That(firstRound.posts[1].button.gameObject.activeSelf, Is.True);

        mailIcon.onClick.Invoke();
        firstRound.mail.button.onClick.Invoke();
        mailBack.onClick.Invoke();
        forumIcon.onClick.Invoke();
        Assert.That(firstRound.posts[1].button.gameObject.activeSelf, Is.True);

        firstRound.posts[1].button.onClick.Invoke();
        postBack.onClick.Invoke();
        Assert.That(flow.CurrentRoundIndex, Is.EqualTo(1));
        Assert.That(mailPanel.activeSelf, Is.False);
        Assert.That(welcomePanel.activeSelf, Is.False);
        Assert.That(postListPanel.activeSelf, Is.False);
        Assert.That(postContentPanel.activeSelf, Is.False);
        Assert.That(forumIcon.interactable, Is.False);

        mailIcon.onClick.Invoke();
        Assert.That(firstRound.mail.button.gameObject.activeSelf, Is.True);
        Assert.That(secondRound.mail.button.gameObject.activeSelf, Is.True);

        secondRound.mail.button.onClick.Invoke();
        mailBack.onClick.Invoke();
        forumIcon.onClick.Invoke();
        Assert.That(welcomePanel.activeSelf, Is.False);
        Assert.That(postListPanel.activeSelf, Is.True);

        secondRound.posts[0].button.onClick.Invoke();
        postBack.onClick.Invoke();
        Assert.That(flow.CurrentRoundIndex, Is.EqualTo(2));

        mailIcon.onClick.Invoke();
        thirdRound.mail.button.onClick.Invoke();
        Assert.That(forumIcon.interactable, Is.False);

        Object.Destroy(selectionSprite);
        Object.Destroy(root);
        yield return null;
    }

    private static StoryRoundData CreateRound(Transform parent, string suffix, int postCount)
    {
        StoryRoundData round = new StoryRoundData
        {
            mail = new MailData
            {
                button = CreateButton(parent, $"Mail {suffix}"),
                contentImage = CreateChild(parent, $"Mail Content {suffix}")
            },
            posts = new PostData[postCount]
        };

        for (int postIndex = 0; postIndex < postCount; postIndex++)
        {
            round.posts[postIndex] = new PostData
            {
                button = CreateButton(parent, $"Post {suffix}-{postIndex}"),
                contentImage = CreateChild(parent, $"Post Content {suffix}-{postIndex}")
            };
        }

        return round;
    }

    private static Button CreateButton(Transform parent, string name)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent);
        return buttonObject.GetComponent<Button>();
    }

    private static Sprite ConfigureSelectionPanel(Transform panelRoot, SelectionPanelController controller)
    {
        Button backButton = CreateButton(panelRoot, "Selection Back");
        Button submitButton = CreateButton(panelRoot, "Selection Post");
        Button characterButton = CreateButton(panelRoot, "Selection Character");
        Button backgroundButton = CreateButton(panelRoot, "Selection Background");
        Button propsButton = CreateButton(panelRoot, "Selection Props");
        Transform itemRoot = CreateChild(panelRoot, "Selection Item Root").transform;
        Image characterPreview = CreateImage(panelRoot, "Selection Character Preview");
        Image propsPreview = CreateImage(panelRoot, "Selection Props Preview");

        GameObject prefabObject = new GameObject(
            "Selection Item Prefab",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        prefabObject.transform.SetParent(panelRoot);
        Image icon = CreateImage(prefabObject.transform, "Icon");
        GameObject selectedMark = CreateImage(prefabObject.transform, "SelectedMark").gameObject;
        SelectionItemView itemView = prefabObject.AddComponent<SelectionItemView>();
        SetField(itemView, "button", prefabObject.GetComponent<Button>());
        SetField(itemView, "iconImage", icon);
        SetField(itemView, "selectedMark", selectedMark);
        selectedMark.SetActive(false);
        prefabObject.SetActive(false);

        Texture2D texture = Texture2D.whiteTexture;
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f));

        SelectionCategoryDefinition[] categories =
        {
            CreateSelectionCategory(SelectionCategoryType.Character, characterButton, sprite, 1, panelRoot),
            CreateSelectionCategory(SelectionCategoryType.Background, backgroundButton, sprite, 2, panelRoot),
            CreateSelectionCategory(SelectionCategoryType.Props, propsButton, sprite, 1, panelRoot)
        };

        SetField(controller, "backButton", backButton);
        SetField(controller, "submitButton", submitButton);
        SetField(controller, "itemRoot", itemRoot);
        SetField(controller, "itemViewPrefab", itemView);
        SetField(controller, "categories", categories);
        SetField(controller, "requiredCategories", new[] { SelectionCategoryType.Background });
        SetField(controller, "initialCategory", SelectionCategoryType.Background);
        SetField(controller, "characterPreview", characterPreview);
        SetField(controller, "propsPreview", propsPreview);
        return sprite;
    }

    private static SelectionCategoryDefinition CreateSelectionCategory(
        SelectionCategoryType categoryType,
        Button button,
        Sprite sprite,
        int itemCount,
        Transform previewParent)
    {
        SelectionCategoryDefinition category = new SelectionCategoryDefinition();
        SetField(category, "categoryType", categoryType);
        SetField(category, "categoryButton", button);

        SelectionItemDefinition[] items = new SelectionItemDefinition[itemCount];
        for (int itemIndex = 0; itemIndex < itemCount; itemIndex++)
        {
            SelectionItemDefinition item = new SelectionItemDefinition();
            string itemId = categoryType == SelectionCategoryType.Background
                ? (itemIndex == 0 ? "jiuBa" : "feiji")
                : $"{categoryType}-{itemIndex}";
            SetField(item, "itemId", itemId);
            SetField(item, "iconSprite", sprite);
            SetField(item, "previewSprite", sprite);

            if (categoryType == SelectionCategoryType.Background)
            {
                GameObject previewObject = CreateChild(previewParent, itemId);
                previewObject.SetActive(false);
                SetField(item, "previewObject", previewObject);
            }

            items[itemIndex] = item;
        }

        SetField(category, "items", items);
        return category;
    }

    private static Image CreateImage(Transform parent, string name)
    {
        GameObject imageObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.transform.SetParent(parent);
        return imageObject.GetComponent<Image>();
    }

    private static GameObject CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent);
        return child;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Field {fieldName} was not found.");
        field.SetValue(target, value);
    }
}
