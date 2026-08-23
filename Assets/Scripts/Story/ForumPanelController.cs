using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ForumPanelController : MonoBehaviour
{
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private GameObject postListPanel;
    [SerializeField] private GameObject postContentPanel;
    [SerializeField] private GameObject postListMainButton;
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
        StoryProgress progress)
    {
        HideAllPostContents(rounds);
        RefreshPostButtons(rounds, currentRoundIndex, progress);
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
        StoryProgress progress)
    {
        SetActive(ResolvePostListMainButton(), progress?.CanOpenSelection != true);

        if (rounds == null)
        {
            return;
        }

        for (int roundIndex = 0; roundIndex < rounds.Length; roundIndex++)
        {
            PostData[] posts = rounds[roundIndex]?.posts;
            if (posts != null)
            {
                Dictionary<GameObject, bool> sharedRootVisibility =
                    new Dictionary<GameObject, bool>();
                for (int postIndex = 0; postIndex < posts.Length; postIndex++)
                {
                    PostData post = posts[postIndex];
                    Button button = post?.button;
                    bool isCurrentRound = roundIndex == currentRoundIndex;
                    bool isCurrentPost = isCurrentRound
                        && progress != null
                        && progress.IsPostAvailable(postIndex, posts);
                    GameObject sharedRoot = post?.listDisplayRoot;
                    bool isVisible = sharedRoot != null
                        ? isCurrentRound
                            && progress != null
                            && progress.IsPostInCurrentStage(postIndex, posts)
                        : isCurrentPost;
                    SetActive(button?.gameObject, isVisible);
                    if (button != null)
                    {
                        button.interactable = isCurrentPost;
                    }

                    if (sharedRoot != null)
                    {
                        bool wasVisible = sharedRootVisibility.TryGetValue(
                            sharedRoot,
                            out bool visible) && visible;
                        sharedRootVisibility[sharedRoot] = wasVisible || isVisible;
                    }
                }

                foreach (KeyValuePair<GameObject, bool> entry in sharedRootVisibility)
                {
                    SetActive(entry.Key, entry.Value);
                }
            }

            bool resultPostUnlocked = roundIndex == currentRoundIndex
                && progress?.SelectionPostUnlocked == true;
            Button resultPostButton = rounds[roundIndex]?.selectionPost?.button;
            SetActive(resultPostButton?.gameObject, resultPostUnlocked);
            if (resultPostButton != null)
            {
                resultPostButton.interactable = resultPostUnlocked;
            }
        }
    }

    private GameObject ResolvePostListMainButton()
    {
        if (postListMainButton == null && postListPanel != null)
        {
            Transform buttonTransform = postListPanel.transform.Find("BBSMain");
            if (buttonTransform != null)
            {
                postListMainButton = buttonTransform.gameObject;
            }
        }

        return postListMainButton;
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
