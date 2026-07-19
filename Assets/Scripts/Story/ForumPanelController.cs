using UnityEngine;
using UnityEngine.UI;

public class ForumPanelController : MonoBehaviour
{
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private GameObject postListPanel;
    [SerializeField] private GameObject postContentPanel;
    [SerializeField] private Button enterForumButton;
    [SerializeField] private Button postBackButton;

    public Button EnterForumButton => enterForumButton;
    public Button PostBackButton => postBackButton;

    public void ShowWelcome(StoryRoundData[] rounds)
    {
        HideAllPostContents(rounds);
        SetActive(welcomePanel, true);
        SetActive(postListPanel, false);
        SetActive(postContentPanel, false);
    }

    public void ShowPostList(
        StoryRoundData[] rounds,
        int currentRoundIndex,
        int unlockedPostIndex,
        bool selectionPostUnlocked)
    {
        HideAllPostContents(rounds);
        RefreshPostButtons(rounds, currentRoundIndex, unlockedPostIndex, selectionPostUnlocked);
        SetActive(welcomePanel, false);
        SetActive(postListPanel, true);
        SetActive(postContentPanel, false);
    }

    public void ShowPostContent(StoryRoundData[] rounds, int roundIndex, int postIndex)
    {
        HideAllPostContents(rounds);
        SetActive(welcomePanel, false);
        SetActive(postListPanel, false);
        SetActive(postContentPanel, true);

        PostData post = GetPost(rounds, roundIndex, postIndex);
        SetActive(post?.contentImage, true);
    }

    public void ShowSelectionPostContent(StoryRoundData[] rounds, GameObject contentImage)
    {
        HideAllPostContents(rounds);
        SetActive(welcomePanel, false);
        SetActive(postListPanel, false);
        SetActive(postContentPanel, true);
        SetActive(contentImage, true);
    }

    public void HideAll(StoryRoundData[] rounds)
    {
        SetActive(welcomePanel, false);
        SetActive(postListPanel, false);
        SetActive(postContentPanel, false);
        HideAllPostContents(rounds);
    }

    public void RefreshPostButtons(
        StoryRoundData[] rounds,
        int currentRoundIndex,
        int unlockedPostIndex,
        bool selectionPostUnlocked)
    {
        if (rounds == null)
        {
            return;
        }

        for (int roundIndex = 0; roundIndex < rounds.Length; roundIndex++)
        {
            PostData[] posts = rounds[roundIndex]?.posts;
            if (posts != null)
            {
                for (int postIndex = 0; postIndex < posts.Length; postIndex++)
                {
                    Button button = posts[postIndex]?.button;
                    bool isCurrentPost = roundIndex == currentRoundIndex
                        && postIndex == unlockedPostIndex;
                    SetActive(button?.gameObject, isCurrentPost);
                    if (button != null)
                    {
                        button.interactable = isCurrentPost;
                    }
                }
            }

            bool resultPostUnlocked = roundIndex == currentRoundIndex && selectionPostUnlocked;
            Button resultPostButton = rounds[roundIndex]?.selectionPost?.button;
            SetActive(resultPostButton?.gameObject, resultPostUnlocked);
            if (resultPostButton != null)
            {
                resultPostButton.interactable = resultPostUnlocked;
            }
        }
    }

    private static PostData GetPost(StoryRoundData[] rounds, int roundIndex, int postIndex)
    {
        if (rounds == null || roundIndex < 0 || roundIndex >= rounds.Length)
        {
            return null;
        }

        PostData[] posts = rounds[roundIndex]?.posts;
        if (posts == null || postIndex < 0 || postIndex >= posts.Length)
        {
            return null;
        }

        return posts[postIndex];
    }

    private static void HideAllPostContents(StoryRoundData[] rounds)
    {
        if (rounds == null)
        {
            return;
        }

        foreach (StoryRoundData round in rounds)
        {
            if (round == null)
            {
                continue;
            }

            if (round.posts != null)
            {
                foreach (PostData post in round.posts)
                {
                    SetActive(post?.contentImage, false);
                }
            }

            if (round.selectionPost?.branches == null)
            {
                continue;
            }

            foreach (SelectionPostBranchData branch in round.selectionPost.branches)
            {
                SetActive(branch?.contentImage, false);
            }
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
