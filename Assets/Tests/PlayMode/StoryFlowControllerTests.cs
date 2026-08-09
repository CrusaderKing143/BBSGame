using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.Video;

public class StoryFlowControllerTests
{
    [UnityTest]
    public IEnumerator ForumKeepsSharedStageTitlesVisibleUntilEveryPostIsRead()
    {
        GameObject root = new GameObject("Parallel Forum Post Test Root");
        ForumPanelController forumController = root.AddComponent<ForumPanelController>();
        StoryRoundData round = CreateRound(root.transform, "D3Parallel", 4);
        round.posts[2].unlockWithPrevious = true;
        GameObject sharedListRoot = CreateChild(root.transform, "D3 Shared List Root");
        round.posts[1].button.transform.SetParent(sharedListRoot.transform, false);
        round.posts[2].button.transform.SetParent(sharedListRoot.transform, false);
        round.posts[1].listDisplayRoot = sharedListRoot;
        round.posts[2].listDisplayRoot = sharedListRoot;
        StoryRoundData[] rounds = { round };
        StoryProgress progress = new StoryProgress();
        progress.Reset();
        progress.MarkMailRead(round.posts);

        forumController.RefreshPostButtons(rounds, 0, progress);
        Assert.That(round.posts[0].button.gameObject.activeSelf, Is.True);
        Assert.That(round.posts[1].button.gameObject.activeSelf, Is.False);
        Assert.That(round.posts[2].button.gameObject.activeSelf, Is.False);
        Assert.That(sharedListRoot.activeSelf, Is.False);

        Assert.That(progress.TryOpenPost(0, 0, round.posts), Is.True);
        Assert.That(progress.CompleteOpenedPost(round.posts), Is.False);
        forumController.RefreshPostButtons(rounds, 0, progress);
        Assert.That(round.posts[0].button.gameObject.activeSelf, Is.False);
        Assert.That(round.posts[1].button.gameObject.activeSelf, Is.True);
        Assert.That(round.posts[2].button.gameObject.activeSelf, Is.True);
        Assert.That(round.posts[3].button.gameObject.activeSelf, Is.False);
        Assert.That(sharedListRoot.activeSelf, Is.True);

        Assert.That(progress.TryOpenPost(0, 1, round.posts), Is.True);
        Assert.That(progress.CompleteOpenedPost(round.posts), Is.False);
        forumController.RefreshPostButtons(rounds, 0, progress);
        Assert.That(round.posts[1].button.gameObject.activeSelf, Is.True);
        Assert.That(round.posts[2].button.gameObject.activeSelf, Is.True);
        Assert.That(round.posts[1].button.interactable, Is.False);
        Assert.That(round.posts[2].button.interactable, Is.True);
        Assert.That(round.posts[3].button.gameObject.activeSelf, Is.False);
        Assert.That(sharedListRoot.activeSelf, Is.True);

        Assert.That(progress.TryOpenPost(0, 2, round.posts), Is.True);
        Assert.That(progress.CompleteOpenedPost(round.posts), Is.False);
        forumController.RefreshPostButtons(rounds, 0, progress);
        Assert.That(round.posts[1].button.gameObject.activeSelf, Is.False);
        Assert.That(round.posts[2].button.gameObject.activeSelf, Is.False);
        Assert.That(round.posts[3].button.gameObject.activeSelf, Is.True);
        Assert.That(sharedListRoot.activeSelf, Is.False);

        Object.Destroy(root);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ClosingOfficeSelectionResultPlaysEndingAndStopsRoundAdvance()
    {
        GameObject root = new GameObject("Office Ending Story Test Root");
        MailPanelController mailController = root.AddComponent<MailPanelController>();
        ForumPanelController forumController = root.AddComponent<ForumPanelController>();
        StoryFlowController flow = root.AddComponent<StoryFlowController>();

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
        GameObject officeResultContent = CreateChild(root.transform, "PostContent_D2_OfficeResult");
        GameObject endVideoRoot = CreateChild(root.transform, "EndVideo");
        VideoClip endingVideoClip = Resources.Load<VideoClip>("1");
        Assert.That(endingVideoClip, Is.Not.Null);

        StoryRoundData secondDayRound = CreateRound(root.transform, "D2", 1);
        SelectionPostBranchData officeBranch = new SelectionPostBranchData
        {
            itemId = "office",
            contentImage = officeResultContent,
            completionMode = SelectionBranchCompletionMode.OpenPostThenEnding,
            endingVideoClip = endingVideoClip
        };
        secondDayRound.selectionPost = new SelectionPostData
        {
            categoryType = SelectionCategoryType.Background,
            branches = new[] { officeBranch }
        };
        StoryRoundData nextRound = CreateRound(root.transform, "D3", 0);
        StoryRoundData[] rounds = { secondDayRound, nextRound };

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
        SetField(flow, "endVideoRoot", endVideoRoot);
        SetField(flow, "rounds", rounds);

        yield return null;

        Assert.That(endVideoRoot.activeSelf, Is.False);

        FieldInfo progressField = typeof(StoryFlowController).GetField(
            "progress",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(progressField, Is.Not.Null);
        StoryProgress progress = (StoryProgress)progressField.GetValue(flow);
        progress.MarkMailRead(true);
        Assert.That(progress.TryOpenPost(0, 0, secondDayRound.PostCount), Is.True);
        Assert.That(
            progress.CompleteOpenedPost(secondDayRound.PostCount, true),
            Is.False);
        Assert.That(progress.TrySubmitSelection(), Is.True);
        Assert.That(progress.TryOpenSelectionPost(0), Is.True);
        SetField(flow, "openedSelectionBranch", officeBranch);

        mailPanel.SetActive(true);
        postContentPanel.SetActive(true);
        officeResultContent.SetActive(true);

        LogAssert.Expect(
            LogType.Warning,
            "[StoryFlowController] An ending was reached, but EndVideo Player is not configured.");
        flow.BackFromPostContent();

        Assert.That(flow.IsStoryEnded, Is.True);
        Assert.That(flow.CurrentRoundIndex, Is.EqualTo(0));
        Assert.That(endVideoRoot.activeSelf, Is.True);
        Assert.That(mailPanel.activeSelf, Is.False);
        Assert.That(postContentPanel.activeSelf, Is.False);
        Assert.That(officeResultContent.activeSelf, Is.False);
        Assert.That(mailIcon.interactable, Is.False);
        Assert.That(forumIcon.interactable, Is.False);
        Assert.That(pictureIcon.interactable, Is.False);

        flow.OpenMailPanel();
        flow.OpenForum();
        flow.OpenSelectionPanel();
        Assert.That(mailPanel.activeSelf, Is.False);
        Assert.That(postContentPanel.activeSelf, Is.False);

        flow.ResetStory();
        Assert.That(flow.IsStoryEnded, Is.False);
        Assert.That(flow.CurrentRoundIndex, Is.EqualTo(0));
        Assert.That(endVideoRoot.activeSelf, Is.False);
        Assert.That(mailIcon.interactable, Is.True);

        Object.Destroy(root);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ImmediateSelectionEndingSkipsResultPostAndEndsStory()
    {
        GameObject root = new GameObject("Immediate Selection Ending Test Root");
        MailPanelController mailController = root.AddComponent<MailPanelController>();
        ForumPanelController forumController = root.AddComponent<ForumPanelController>();
        StoryFlowController flow = root.AddComponent<StoryFlowController>();

        GameObject selectionPanelObject = CreateChild(root.transform, "Selection Panel");
        SelectionPanelController selectionPanel =
            selectionPanelObject.AddComponent<SelectionPanelController>();
        selectionPanelObject.SetActive(false);
        Sprite selectionSprite = ConfigureSelectionPanel(
            selectionPanelObject.transform,
            selectionPanel);

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
        GameObject endVideoRoot = CreateChild(root.transform, "EndVideo");

        VideoClip endingVideoClip = Resources.Load<VideoClip>("2");
        Assert.That(endingVideoClip, Is.Not.Null);
        StoryRoundData endingRound = CreateRound(root.transform, "D3", 1);
        endingRound.selectionPost = new SelectionPostData
        {
            categoryType = SelectionCategoryType.Character,
            branches = new[]
            {
                new SelectionPostBranchData
                {
                    itemId = "Character-0",
                    completionMode = SelectionBranchCompletionMode.PlayEndingImmediately,
                    endingVideoClip = endingVideoClip
                }
            }
        };
        StoryRoundData[] rounds = { endingRound };

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
        SetField(flow, "endVideoRoot", endVideoRoot);
        SetField(flow, "rounds", rounds);

        yield return null;

        FieldInfo committedSelectionsField = typeof(SelectionPanelController).GetField(
            "committedSelections",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(committedSelectionsField, Is.Not.Null);
        int[] committedSelections =
            (int[])committedSelectionsField.GetValue(selectionPanel);
        committedSelections[(int)SelectionCategoryType.Character] = 0;

        FieldInfo progressField = typeof(StoryFlowController).GetField(
            "progress",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(progressField, Is.Not.Null);
        StoryProgress progress = (StoryProgress)progressField.GetValue(flow);
        progress.MarkMailRead(true);
        Assert.That(progress.TryOpenPost(0, 0, 1), Is.True);
        Assert.That(progress.CompleteOpenedPost(1, true), Is.False);

        MethodInfo submitHandler = typeof(StoryFlowController).GetMethod(
            "HandleSelectionSubmitted",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(submitHandler, Is.Not.Null);
        LogAssert.Expect(
            LogType.Warning,
            "[StoryFlowController] An ending was reached, but EndVideo Player is not configured.");
        submitHandler.Invoke(flow, null);

        Assert.That(flow.IsStoryEnded, Is.True);
        Assert.That(progress.Phase, Is.EqualTo(StoryRoundPhase.RoundCompleted));
        Assert.That(progress.SelectionPostOpened, Is.False);
        Assert.That(endVideoRoot.activeSelf, Is.True);
        Assert.That(postContentPanel.activeSelf, Is.False);

        Object.Destroy(selectionSprite);
        Object.Destroy(root);
        yield return null;
    }

    [UnityTest]
    public IEnumerator SelectionSubmissionOpensMappedResultPostBeforeAdvancing()
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
            Button collectibleButton = CreateButton(
                firstRound.posts[0].contentImage.transform,
                "Collect Props");
            firstRound.posts[0].collectibles = new[]
            {
                new SelectionCollectibleData
                {
                    button = collectibleButton,
                    categoryType = SelectionCategoryType.Props,
                    itemId = "Props-0"
                }
            };
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
            SetField(flow, "collectibleFeedbackDuration", 0.05f);
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
                if (postIndex == 0)
                {
                    collectibleButton.onClick.Invoke();
                    Transform feedback = collectibleButton.transform.Find("Collectible Feedback");
                    Assert.That(feedback, Is.Not.Null);

                    Image feedbackImage = feedback.GetComponent<Image>();
                    RectTransform feedbackRect = feedback.GetComponent<RectTransform>();
                    Assert.That(feedbackImage.sprite, Is.SameAs(selectionSprite));
                    Assert.That(feedbackImage.preserveAspect, Is.True);
                    Assert.That(feedbackImage.raycastTarget, Is.False);
                    Assert.That(feedbackRect.anchorMin, Is.EqualTo(Vector2.one));
                    Assert.That(feedbackRect.anchorMax, Is.EqualTo(Vector2.one));
                    Assert.That(feedbackRect.anchoredPosition, Is.EqualTo(new Vector2(12f, 12f)));
                    Assert.That(feedbackRect.sizeDelta, Is.EqualTo(new Vector2(96f, 96f)));

                    collectibleButton.onClick.Invoke();
                    Assert.That(collectibleButton.transform.Cast<Transform>().Count(
                        child => child.name == "Collectible Feedback"), Is.EqualTo(1));

                    yield return new WaitForSecondsRealtime(0.1f);
                    Assert.That(collectibleButton.transform.Find("Collectible Feedback"), Is.Null);
                }
                postBack.onClick.Invoke();
            }

            Assert.That(flow.CurrentRoundIndex, Is.EqualTo(0));
            Assert.That(pictureIcon.interactable, Is.True);
            Assert.That(resultPostButton.gameObject.activeSelf, Is.False);

            pictureIcon.onClick.Invoke();
            Assert.That(selectionPanelObject.activeSelf, Is.True);
            Button propsCategoryButton = selectionPanelObject.transform
                .Find("jiuBa/Selection Props")
                .GetComponent<Button>();
            propsCategoryButton.onClick.Invoke();
            Assert.That(
                selectionPanelObject.GetComponentsInChildren<SelectionItemView>(false),
                Has.Length.EqualTo(1));
            selectionPanelObject.transform
                .Find("jiuBa/Selection Background")
                .GetComponent<Button>()
                .onClick.Invoke();
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

            string activeLayoutName = selectedBackgroundIndex == 0 ? "jiuBa" : "feiji";
            Button selectionSubmit = selectionPanelObject.transform
                .Find($"{activeLayoutName}/Selection Post")
                .GetComponent<Button>();
            Assert.That(selectionSubmit.interactable, Is.True);
            selectionSubmit.onClick.Invoke();

            Assert.That(pictureIcon.interactable, Is.False);
            Assert.That(resultPostButton.gameObject.activeSelf, Is.True);
            Assert.That(postListPanel.activeSelf, Is.False);
            Assert.That(postContentPanel.activeSelf, Is.True);
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
        Assert.That(firstRound.posts[0].button.interactable, Is.True);
        Assert.That(firstRound.posts[1].button.gameObject.activeSelf, Is.False);
        Assert.That(firstRound.posts[1].button.interactable, Is.False);

        firstRound.posts[0].button.onClick.Invoke();
        Assert.That(firstRound.posts[0].contentImage.activeSelf, Is.True);
        postBack.onClick.Invoke();
        Assert.That(firstRound.posts[0].button.gameObject.activeSelf, Is.False);
        Assert.That(firstRound.posts[1].button.gameObject.activeSelf, Is.True);
        Assert.That(firstRound.posts[1].button.interactable, Is.True);

        mailIcon.onClick.Invoke();
        firstRound.mail.button.onClick.Invoke();
        mailBack.onClick.Invoke();
        forumIcon.onClick.Invoke();
        Assert.That(firstRound.posts[0].button.gameObject.activeSelf, Is.False);
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
        Assert.That(firstRound.posts[0].button.gameObject.activeSelf, Is.False);
        Assert.That(firstRound.posts[1].button.gameObject.activeSelf, Is.False);
        Assert.That(secondRound.posts[0].button.gameObject.activeSelf, Is.True);
        Assert.That(secondRound.posts[0].button.interactable, Is.True);

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
        SelectionBackgroundLayoutDefinition[] layouts =
        {
            CreateSelectionLayout(panelRoot, "jiuBa"),
            CreateSelectionLayout(panelRoot, "feiji")
        };

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

        GameObject placedPrefabObject = new GameObject(
            "Selection Placed Item Prefab",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        placedPrefabObject.transform.SetParent(panelRoot);
        SelectionPlacedItemView placedItemView =
            placedPrefabObject.AddComponent<SelectionPlacedItemView>();
        SetField(placedItemView, "itemImage", placedPrefabObject.GetComponent<Image>());
        placedPrefabObject.SetActive(false);

        Texture2D texture = Texture2D.whiteTexture;
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f));

        SelectionCategoryDefinition[] categories =
        {
            CreateSelectionCategory(SelectionCategoryType.Character, sprite, 1),
            CreateSelectionCategory(SelectionCategoryType.Background, sprite, 2),
            CreateSelectionCategory(SelectionCategoryType.Props, sprite, 1)
        };

        SetField(controller, "itemViewPrefab", itemView);
        SetField(controller, "placedItemViewPrefab", placedItemView);
        SetField(controller, "categories", categories);
        SetField(controller, "backgroundLayouts", layouts);
        SetField(controller, "requiredCategories", new[] { SelectionCategoryType.Background });
        SetField(controller, "initialCategory", SelectionCategoryType.Background);
        return sprite;
    }

    private static SelectionBackgroundLayoutDefinition CreateSelectionLayout(
        Transform panelRoot,
        string name)
    {
        GameObject layoutRoot = CreateChild(panelRoot, name);
        Button backButton = CreateButton(layoutRoot.transform, "Selection Back");
        Button submitButton = CreateButton(layoutRoot.transform, "Selection Post");
        Button characterButton = CreateButton(layoutRoot.transform, "Selection Character");
        Button backgroundButton = CreateButton(layoutRoot.transform, "Selection Background");
        Button propsButton = CreateButton(layoutRoot.transform, "Selection Props");
        Transform itemRoot = CreateChild(layoutRoot.transform, "Selection Item Root").transform;
        Image characterPreview = CreateImage(layoutRoot.transform, "Selection Character Preview");
        Image propsPreview = CreateImage(layoutRoot.transform, "Selection Props Preview");

        SelectionBackgroundLayoutDefinition layout =
            new SelectionBackgroundLayoutDefinition();
        SetField(layout, "layoutRoot", layoutRoot);
        SetField(layout, "backButton", backButton);
        SetField(layout, "submitButton", submitButton);
        SetField(layout, "characterButton", characterButton);
        SetField(layout, "backgroundButton", backgroundButton);
        SetField(layout, "propsButton", propsButton);
        SetField(layout, "itemRoot", itemRoot);
        SetField(layout, "characterPreview", characterPreview);
        SetField(layout, "propsPreview", propsPreview);
        layoutRoot.SetActive(false);
        return layout;
    }

    private static SelectionCategoryDefinition CreateSelectionCategory(
        SelectionCategoryType categoryType,
        Sprite sprite,
        int itemCount)
    {
        SelectionCategoryDefinition category = new SelectionCategoryDefinition();
        SetField(category, "categoryType", categoryType);

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
