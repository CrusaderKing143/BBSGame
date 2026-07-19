using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StoryFlowController : MonoBehaviour
{
    private const string CollectibleFeedbackObjectName = "Collectible Feedback";

    private sealed class CollectibleFeedbackRuntime
    {
        public GameObject Root;
        public Coroutine Coroutine;
    }

    [Header("Main Icons")]
    [SerializeField] private Button mailButton;
    [SerializeField] private Button forumButton;
    [SerializeField] private Button pictureButton;

    [Header("Panels")]
    [SerializeField] private MailPanelController mailPanelController;
    [SerializeField] private ForumPanelController forumPanelController;
    [SerializeField] private SelectionPanelController selectionPanelController;

    [Header("Collectible Feedback")]
    [SerializeField] private Vector2 collectibleFeedbackSize = new Vector2(96f, 96f);
    [SerializeField] private Vector2 collectibleFeedbackOffset = new Vector2(12f, 12f);
    [SerializeField, Min(0.01f)] private float collectibleFeedbackDuration = 1.2f;

    [Header("Story Rounds")]
    [SerializeField] private StoryRoundData[] rounds;

    private readonly StoryProgress progress = new StoryProgress();
    private readonly List<CollectibleFeedbackRuntime> activeCollectibleFeedbacks =
        new List<CollectibleFeedbackRuntime>();

    public int CurrentRoundIndex => progress.CurrentRoundIndex;

    private void Start()
    {
        BindButtons();
        ResetStory();
    }

    public void ResetStory()
    {
        ClearCollectibleFeedbacks();
        progress.Reset();
        selectionPanelController?.ResetCollectedItems();
        selectionPanelController?.ResetSelections();
        selectionPanelController?.CancelAndClose();
        mailPanelController?.HidePanel(rounds);
        forumPanelController?.HideAll(rounds);
        RefreshView();
    }

    public void OpenMailPanel()
    {
        if (!HasCurrentRound())
        {
            return;
        }

        selectionPanelController?.CancelAndClose();
        forumPanelController?.HideAll(rounds);
        mailPanelController?.ShowPanel(rounds, progress.CurrentRoundIndex);
        RefreshView();
    }

    public void BackFromMail()
    {
        mailPanelController?.HidePanel(rounds);
        RefreshView();
    }

    public void OpenForum()
    {
        if (!CanOpenForum())
        {
            return;
        }

        selectionPanelController?.CancelAndClose();
        mailPanelController?.HidePanel(rounds);

        if (progress.ForumJoined)
        {
            ShowPostList();
        }
        else
        {
            forumPanelController?.ShowWelcome(rounds);
        }

        RefreshView();
    }

    public void OpenSelectionPanel()
    {
        if (selectionPanelController == null || !progress.CanOpenSelection)
        {
            return;
        }

        mailPanelController?.HidePanel(rounds);
        forumPanelController?.HideAll(rounds);
        selectionPanelController.OpenPanel();
        RefreshView();
    }

    public void EnterForum()
    {
        if (!CanOpenForum())
        {
            return;
        }

        progress.MarkForumJoined();
        ShowPostList();
        RefreshView();
    }

    public void BackFromPostContent()
    {
        if (!HasCurrentRound())
        {
            return;
        }

        StoryRoundData currentRound = rounds[progress.CurrentRoundIndex];
        bool completedRound = progress.CompleteOpenedPost(
            currentRound.PostCount,
            currentRound.HasSelectionPost);
        bool hasNextRound = progress.CurrentRoundIndex < rounds.Length - 1;

        if (completedRound && hasNextRound)
        {
            progress.BeginRound(progress.CurrentRoundIndex + 1);
            mailPanelController?.HidePanel(rounds);
            forumPanelController?.HideAll(rounds);
        }
        else
        {
            ShowPostList();
        }

        RefreshView();
    }

    private void BindButtons()
    {
        AddClick(mailButton, OpenMailPanel);
        AddClick(forumButton, OpenForum);
        AddClick(pictureButton, OpenSelectionPanel);
        AddClick(mailPanelController?.BackButton, BackFromMail);
        AddClick(forumPanelController?.EnterForumButton, EnterForum);
        AddClick(forumPanelController?.PostBackButton, BackFromPostContent);
        selectionPanelController?.OnSubmitted.AddListener(HandleSelectionSubmitted);

        if (rounds == null)
        {
            return;
        }

        for (int roundIndex = 0; roundIndex < rounds.Length; roundIndex++)
        {
            int capturedRoundIndex = roundIndex;
            StoryRoundData round = rounds[roundIndex];

            AddClick(round?.mail?.button, () => OpenMail(capturedRoundIndex));
            AddClick(round?.selectionPost?.button, () => OpenSelectionPost(capturedRoundIndex));

            if (round?.posts == null)
            {
                continue;
            }

            for (int postIndex = 0; postIndex < round.posts.Length; postIndex++)
            {
                int capturedPostIndex = postIndex;
                PostData post = round.posts[postIndex];
                AddClick(post?.button, () => OpenPost(capturedRoundIndex, capturedPostIndex));
                BindCollectibles(post?.collectibles, roundIndex, postIndex);
            }
        }
    }

    private void BindCollectibles(
        SelectionCollectibleData[] collectibles,
        int roundIndex,
        int postIndex)
    {
        if (collectibles == null)
        {
            return;
        }

        for (int collectibleIndex = 0; collectibleIndex < collectibles.Length; collectibleIndex++)
        {
            SelectionCollectibleData collectible = collectibles[collectibleIndex];
            if (collectible == null || !collectible.IsValid)
            {
                Debug.LogWarning(
                    $"[StoryFlowController] Invalid collectible at round {roundIndex}, post {postIndex}, index {collectibleIndex}.",
                    this);
                continue;
            }

            SelectionCategoryType capturedCategory = collectible.categoryType;
            string capturedItemId = collectible.itemId;
            Button capturedButton = collectible.button;
            AddClick(
                capturedButton,
                () => CollectItemAndShowFeedback(
                    capturedButton,
                    capturedCategory,
                    capturedItemId));
        }
    }

    private void CollectItemAndShowFeedback(
        Button sourceButton,
        SelectionCategoryType categoryType,
        string itemId)
    {
        if (selectionPanelController == null
            || !selectionPanelController.CollectItem(categoryType, itemId))
        {
            return;
        }

        if (!selectionPanelController.TryGetItemIcon(categoryType, itemId, out Sprite iconSprite))
        {
            Debug.LogWarning(
                $"[StoryFlowController] Collected item '{itemId}' in category {categoryType} has no IconSprite.",
                this);
            return;
        }

        ShowCollectibleFeedback(sourceButton, iconSprite);
    }

    private void ShowCollectibleFeedback(Button sourceButton, Sprite iconSprite)
    {
        if (sourceButton == null || iconSprite == null)
        {
            return;
        }

        GameObject feedbackObject = new GameObject(
            CollectibleFeedbackObjectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup));
        feedbackObject.layer = sourceButton.gameObject.layer;
        feedbackObject.transform.SetParent(sourceButton.transform, false);
        feedbackObject.transform.SetAsLastSibling();

        RectTransform feedbackRect = feedbackObject.GetComponent<RectTransform>();
        feedbackRect.anchorMin = Vector2.one;
        feedbackRect.anchorMax = Vector2.one;
        feedbackRect.pivot = Vector2.zero;
        feedbackRect.anchoredPosition = collectibleFeedbackOffset;
        feedbackRect.sizeDelta = new Vector2(
            collectibleFeedbackSize.x > 0f ? collectibleFeedbackSize.x : 96f,
            collectibleFeedbackSize.y > 0f ? collectibleFeedbackSize.y : 96f);
        feedbackRect.localScale = Vector3.one * 0.75f;

        Image feedbackImage = feedbackObject.GetComponent<Image>();
        feedbackImage.sprite = iconSprite;
        feedbackImage.color = Color.white;
        feedbackImage.preserveAspect = true;
        feedbackImage.raycastTarget = false;

        CanvasGroup canvasGroup = feedbackObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        CollectibleFeedbackRuntime feedback = new CollectibleFeedbackRuntime
        {
            Root = feedbackObject
        };
        activeCollectibleFeedbacks.Add(feedback);
        feedback.Coroutine = StartCoroutine(AnimateCollectibleFeedback(feedback));
    }

    private IEnumerator AnimateCollectibleFeedback(CollectibleFeedbackRuntime feedback)
    {
        float totalDuration = Mathf.Max(0.01f, collectibleFeedbackDuration);
        float popDuration = Mathf.Min(0.15f, totalDuration * 0.25f);
        float fadeDuration = Mathf.Min(0.35f, totalDuration * 0.5f);
        float fadeStart = totalDuration - fadeDuration;
        float elapsed = 0f;

        while (elapsed < totalDuration && feedback.Root != null)
        {
            elapsed += Time.unscaledDeltaTime;

            float popProgress = popDuration > 0f
                ? Mathf.Clamp01(elapsed / popDuration)
                : 1f;
            float scale = Mathf.SmoothStep(0.75f, 1f, popProgress);
            feedback.Root.transform.localScale = Vector3.one * scale;

            CanvasGroup canvasGroup = feedback.Root.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = elapsed <= fadeStart
                    ? 1f
                    : 1f - Mathf.Clamp01((elapsed - fadeStart) / fadeDuration);
            }

            yield return null;
        }

        CompleteCollectibleFeedback(feedback);
    }

    private void CompleteCollectibleFeedback(CollectibleFeedbackRuntime feedback)
    {
        activeCollectibleFeedbacks.Remove(feedback);
        feedback.Coroutine = null;
        if (feedback.Root != null)
        {
            Destroy(feedback.Root);
        }
    }

    private void ClearCollectibleFeedbacks()
    {
        for (int index = activeCollectibleFeedbacks.Count - 1; index >= 0; index--)
        {
            CollectibleFeedbackRuntime feedback = activeCollectibleFeedbacks[index];
            if (feedback.Coroutine != null)
            {
                StopCoroutine(feedback.Coroutine);
            }

            if (feedback.Root != null)
            {
                Destroy(feedback.Root);
            }
        }

        activeCollectibleFeedbacks.Clear();
    }

    private void OnDestroy()
    {
        ClearCollectibleFeedbacks();
        selectionPanelController?.OnSubmitted.RemoveListener(HandleSelectionSubmitted);
    }

    private void OpenMail(int roundIndex)
    {
        if (!HasCurrentRound()
            || roundIndex < 0
            || roundIndex > progress.CurrentRoundIndex
            || roundIndex >= rounds.Length)
        {
            return;
        }

        StoryRoundData selectedRound = rounds[roundIndex];
        if (selectedRound?.mail == null || !selectedRound.mail.IsValid)
        {
            return;
        }

        if (roundIndex == progress.CurrentRoundIndex)
        {
            progress.MarkMailRead(selectedRound.HasPosts);
        }

        mailPanelController?.ShowContent(rounds, roundIndex);
        RefreshView();
    }

    private void OpenPost(int roundIndex, int postIndex)
    {
        if (!HasCurrentRound() || roundIndex != progress.CurrentRoundIndex)
        {
            return;
        }

        StoryRoundData currentRound = rounds[progress.CurrentRoundIndex];
        if (currentRound.posts == null || postIndex < 0 || postIndex >= currentRound.posts.Length)
        {
            return;
        }

        PostData post = currentRound.posts[postIndex];
        if (post == null || !post.IsValid)
        {
            return;
        }

        if (!progress.TryOpenPost(roundIndex, postIndex, currentRound.PostCount))
        {
            return;
        }

        forumPanelController?.ShowPostContent(rounds, roundIndex, postIndex);
        RefreshView();
    }

    private void HandleSelectionSubmitted()
    {
        if (!HasCurrentRound() || selectionPanelController == null || !progress.CanOpenSelection)
        {
            return;
        }

        StoryRoundData currentRound = rounds[progress.CurrentRoundIndex];
        SelectionPostData selectionPost = currentRound.selectionPost;
        if (selectionPost == null || !selectionPost.IsValid)
        {
            return;
        }

        string selectedItemId = selectionPanelController.GetCommittedItemId(selectionPost.categoryType);
        GameObject contentImage = selectionPost.GetContent(selectedItemId);
        int roundIndex = progress.CurrentRoundIndex;
        if (contentImage == null
            || !progress.TrySubmitSelection()
            || !progress.TryOpenSelectionPost(roundIndex))
        {
            return;
        }

        selectionPanelController.ApplyCommittedSnapshot(selectionPost.recordImage);
        forumPanelController?.ShowSelectionPostContent(rounds, contentImage);
        RefreshView();
    }

    private void OpenSelectionPost(int roundIndex)
    {
        if (!HasCurrentRound() || roundIndex != progress.CurrentRoundIndex)
        {
            return;
        }

        SelectionPostData selectionPost = rounds[roundIndex].selectionPost;
        if (selectionPost == null || !selectionPost.IsValid || selectionPanelController == null)
        {
            return;
        }

        string selectedItemId = selectionPanelController.GetCommittedItemId(selectionPost.categoryType);
        GameObject contentImage = selectionPost.GetContent(selectedItemId);
        if (contentImage == null || !progress.TryOpenSelectionPost(roundIndex))
        {
            return;
        }

        selectionPanelController.ApplyCommittedSnapshot(selectionPost.recordImage);
        forumPanelController?.ShowSelectionPostContent(rounds, contentImage);
        RefreshView();
    }

    private void ShowPostList()
    {
        forumPanelController?.ShowPostList(
            rounds,
            progress.CurrentRoundIndex,
            progress.UnlockedPostIndex,
            progress.SelectionPostUnlocked);
    }

    private void RefreshView()
    {
        bool hasCurrentRound = HasCurrentRound();
        StoryRoundData currentRound = hasCurrentRound ? rounds[progress.CurrentRoundIndex] : null;

        if (mailButton != null)
        {
            mailButton.interactable = currentRound?.mail?.IsValid == true;
        }

        if (forumButton != null)
        {
            forumButton.interactable = CanOpenForum();
        }

        if (pictureButton != null)
        {
            pictureButton.interactable = currentRound?.HasSelectionPost == true
                && progress.CanOpenSelection
                && selectionPanelController != null
                && selectionPanelController.IsConfigurationValid();
        }

        mailPanelController?.RefreshMailButtons(rounds, progress.CurrentRoundIndex);
        forumPanelController?.RefreshPostButtons(
            rounds,
            progress.CurrentRoundIndex,
            progress.UnlockedPostIndex,
            progress.SelectionPostUnlocked);
    }

    private bool CanOpenForum()
    {
        return HasCurrentRound()
            && progress.MailRead
            && rounds[progress.CurrentRoundIndex].HasPosts;
    }

    private bool HasCurrentRound()
    {
        return rounds != null
            && progress.CurrentRoundIndex >= 0
            && progress.CurrentRoundIndex < rounds.Length
            && rounds[progress.CurrentRoundIndex] != null;
    }

    private static void AddClick(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.AddListener(action);
        }
    }
}
