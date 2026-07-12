using UnityEngine;
using UnityEngine.UI;

public class StoryFlowController : MonoBehaviour
{
    [Header("Main Icons")]
    [SerializeField] private Button mailButton;
    [SerializeField] private Button forumButton;
    [SerializeField] private Button pictureButton;

    [Header("Panels")]
    [SerializeField] private MailPanelController mailPanelController;
    [SerializeField] private ForumPanelController forumPanelController;
    [SerializeField] private SelectionPanelController selectionPanelController;

    [Header("Story Rounds")]
    [SerializeField] private StoryRoundData[] rounds;

    private readonly StoryProgress progress = new StoryProgress();

    public int CurrentRoundIndex => progress.CurrentRoundIndex;

    private void Start()
    {
        BindButtons();
        ResetStory();
    }

    public void ResetStory()
    {
        progress.Reset();
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
                AddClick(round.posts[postIndex]?.button, () => OpenPost(capturedRoundIndex, capturedPostIndex));
            }
        }
    }

    private void OnDestroy()
    {
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
        if (selectionPost.GetContent(selectedItemId) == null || !progress.TrySubmitSelection())
        {
            return;
        }

        ShowPostList();
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
