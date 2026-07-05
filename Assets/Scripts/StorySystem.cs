using System;
using UnityEngine;
using UnityEngine.UI;

public class StorySystem : MonoBehaviour
{
    [Serializable]
    public class StoryChapter
    {
        public MailItem mail;
        public PostItem[] posts;
        public ChoiceItem choice;
    }

    [Serializable]
    public class MailItem
    {
        public Button button;
        public GameObject contentImage;
    }

    [Serializable]
    public class PostItem
    {
        public Button button;
        public GameObject contentImage;
    }

    [Serializable]
    public class ChoiceItem
    {
        public ChoiceOption firstOption;
        public ChoiceOption secondOption;
    }

    [Serializable]
    public class ChoiceOption
    {
        public Button button;
        public GameObject contentImage;

        [Tooltip("Use -1 to continue to the next chapter in order.")]
        public int nextChapterIndex = -1;
    }

    [Header("Navigation Buttons")]
    [SerializeField] private Button mailButton;
    [SerializeField] private Button forumButton;
    [SerializeField] private Button enterPostListButton;
    [SerializeField] private Button postBackButton;

    [SerializeField] private Button JoinBBSButton;
    [SerializeField] private Button choiceButton;
    [SerializeField] private Button choiceBackButton;

    [Header("Panels")]
    [SerializeField] private GameObject mailPanel;
    [SerializeField] private GameObject forumWelcomePanel;
    [SerializeField] private GameObject postListPanel;
    [SerializeField] private GameObject postContentPanel;
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private GameObject choiceContentPanel;

    [Header("Story Data")]
    [SerializeField] private StoryChapter[] chapters;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    private int currentChapterIndex;
    private int unlockedPostIndex = -1;
    private int openedPostIndex = -1;
    private int openedChoiceIndex = -1;
    private bool currentMailRead;

    private  bool JoinBBSRead;

    private bool choiceUnlocked;

    private void Start()
    {
        BindButtons();
        ResetStory();
    }

    public void ResetStory()
    {
        currentChapterIndex = 0;
        unlockedPostIndex = -1;
        openedPostIndex = -1;
        openedChoiceIndex = -1;
        currentMailRead = false;
        choiceUnlocked = false;

        ShowMainState();
        RefreshStoryObjects();
    }

    public void OpenMailPanel()
    {
        SetActive(mailPanel, true);
        SetActive(forumWelcomePanel, false);
        SetActive(postListPanel, false);
        SetActive(postContentPanel, false);
        SetActive(choicePanel, false);
        SetActive(choiceContentPanel, false);
        HideAllContents();
        RefreshStoryObjects();
    }

    public void OpenForumWelcome()
    {
        

        SetActive(mailPanel, false);
        if (!JoinBBSRead)
        {
            SetActive(forumWelcomePanel, true);
            SetActive(postContentPanel, false);
            
        }
        else
        {
            SetActive(forumWelcomePanel, false);
            SetActive(postListPanel, true);
            SetActive(postContentPanel, true);

        }
        
        //SetActive(postListPanel, false);
        
        SetActive(choicePanel, false);
        SetActive(choiceContentPanel, false);
        HideAllContents();
        RefreshStoryObjects();
    }

    public void OpenPostList()
    {
        if (!currentMailRead)
        {
            return;
        }

        SetActive(mailPanel, false);
        SetActive(forumWelcomePanel, false);
        SetActive(postListPanel, true);
        SetActive(postContentPanel, false);
        SetActive(choicePanel, false);
        SetActive(choiceContentPanel, false);
        HideAllContents();
        RefreshStoryObjects();
    }

    public void BackFromPostContent()
    {
        bool movedToNextChapter = false;
        bool isLatestOpenedPost = openedPostIndex == unlockedPostIndex;
        bool isLastPost = IsLastPost(openedPostIndex);
        bool hasChoice = HasChoiceForCurrentChapter();

        StoryLog("BackFromPostContent: "
            + $"currentChapterIndex={currentChapterIndex}, "
            + $"openedPostIndex={openedPostIndex}, "
            + $"unlockedPostIndex={unlockedPostIndex}, "
            + $"isLatestOpenedPost={isLatestOpenedPost}, "
            + $"isLastPost={isLastPost}, "
            + $"hasChoice={hasChoice}, "
            + $"choiceButtonAssigned={choiceButton != null}");

        if (isLatestOpenedPost)
        {
            if (isLastPost && hasChoice)
            {
                choiceUnlocked = true;
                StoryLog("Choice unlocked. choiceButton should become interactable after RefreshStoryObjects.");
            }
            else
            {
                movedToNextChapter = AdvanceAfterLatestPost();
                StoryLog($"AdvanceAfterLatestPost called. movedToNextChapter={movedToNextChapter}, unlockedPostIndex={unlockedPostIndex}, currentChapterIndex={currentChapterIndex}");
            }
        }
        else
        {
            StoryLog("No unlock happened because openedPostIndex is not equal to unlockedPostIndex.");
        }

        openedPostIndex = -1;

        if (movedToNextChapter)
        {
            OpenMailPanel();
        }
        else
        {
            OpenPostList();
        }
    }

    public void OpenChoicePanel()
    {
        StoryLog($"OpenChoicePanel clicked: choiceUnlocked={choiceUnlocked}, hasChoice={HasChoiceForCurrentChapter()}");

        if (!choiceUnlocked || !HasChoiceForCurrentChapter())
        {
            return;
        }

        SetActive(mailPanel, false);
        SetActive(forumWelcomePanel, false);
        SetActive(postListPanel, false);
        SetActive(postContentPanel, false);
        SetActive(choicePanel, true);
        SetActive(choiceContentPanel, false);
        HideAllContents();
        RefreshStoryObjects();
    }

    public void BackFromChoiceContent()
    {
        if (!HasCurrentChapter())
        {
            return;
        }

        if (openedChoiceIndex < 0)
        {
            OpenChoicePanel();
            return;
        }

        ChoiceOption selectedOption = GetChoiceOption(chapters[currentChapterIndex], openedChoiceIndex);
        AdvanceAfterChoice(selectedOption);
    }

    private void BindButtons()
    {
        AddClick(mailButton, OpenMailPanel);
        AddClick(forumButton, OpenForumWelcome);
        AddClick(enterPostListButton, OpenPostList);
        AddClick(postBackButton, BackFromPostContent);
        AddClick(choiceButton, OpenChoicePanel);
        AddClick(choiceBackButton, BackFromChoiceContent);
        AddClick(JoinBBSButton, JoinBBSButtonClick);

        if (chapters == null)
        {
            return;
        }

        for (int chapterIndex = 0; chapterIndex < chapters.Length; chapterIndex++)
        {
            if (chapters[chapterIndex] == null)
            {
                continue;
            }

            int capturedChapterIndex = chapterIndex;
            AddClick(chapters[chapterIndex].mail?.button, () => OpenMail(capturedChapterIndex));
            AddClick(chapters[chapterIndex].choice?.firstOption?.button, () => OpenChoice(capturedChapterIndex, 0));
            AddClick(chapters[chapterIndex].choice?.secondOption?.button, () => OpenChoice(capturedChapterIndex, 1));

            PostItem[] posts = chapters[chapterIndex].posts;
            if (posts == null)
            {
                continue;
            }

            for (int postIndex = 0; postIndex < posts.Length; postIndex++)
            {
                int capturedPostIndex = postIndex;
                AddClick(posts[postIndex].button, () => OpenPost(capturedChapterIndex, capturedPostIndex));
            }
        }
    }

    private void JoinBBSButtonClick()
    {
        JoinBBSRead = true;
        SetActive(forumWelcomePanel, false);
        SetActive(postListPanel, true);
        
    }

    private void OpenMail(int chapterIndex)
    {
        if (chapterIndex != currentChapterIndex || !HasCurrentChapter() || chapters[currentChapterIndex].mail == null)
        {
            return;
        }

        currentMailRead = true;
        if (unlockedPostIndex < 0)
        {
            unlockedPostIndex = 0;
        }

        HideAllContents();
        SetActive(chapters[currentChapterIndex].mail.contentImage, true);
        RefreshStoryObjects();
    }

    private void OpenPost(int chapterIndex, int postIndex)
    {
        StoryLog($"OpenPost clicked: chapterIndex={chapterIndex}, postIndex={postIndex}, currentChapterIndex={currentChapterIndex}, unlockedPostIndex={unlockedPostIndex}");

        if (chapterIndex != currentChapterIndex || !HasCurrentChapter() || postIndex > unlockedPostIndex)
        {
            StoryLog("OpenPost blocked by chapter/current/unlocked check.");
            return;
        }

        PostItem[] posts = chapters[currentChapterIndex].posts;
        if (posts == null || postIndex < 0 || postIndex >= posts.Length)
        {
            StoryLog($"OpenPost blocked because posts is null or postIndex is out of range. postsLength={(posts == null ? -1 : posts.Length)}");
            return;
        }

        openedPostIndex = postIndex;
        HideAllContents();
        SetActive(postListPanel, false);
        SetActive(postContentPanel, true);
        SetActive(choicePanel, false);
        SetActive(choiceContentPanel, false);
        SetActive(posts[postIndex].contentImage, true);
        RefreshStoryObjects();
    }

    private void OpenChoice(int chapterIndex, int optionIndex)
    {
        if (chapterIndex != currentChapterIndex || !choiceUnlocked || !HasCurrentChapter())
        {
            return;
        }

        ChoiceOption option = GetChoiceOption(chapters[currentChapterIndex], optionIndex);
        if (!HasChoiceOption(option))
        {
            return;
        }

        if (option.contentImage == null)
        {
            AdvanceAfterChoice(option);
            return;
        }

        openedChoiceIndex = optionIndex;
        HideAllContents();
        SetActive(choicePanel, false);
        SetActive(choiceContentPanel, true);
        SetActive(option.contentImage, true);
        RefreshStoryObjects();
    }

    private bool AdvanceAfterLatestPost()
    {
        if (!HasCurrentChapter())
        {
            return false;
        }

        PostItem[] posts = chapters[currentChapterIndex].posts;
        if (posts != null && unlockedPostIndex < posts.Length - 1)
        {
            unlockedPostIndex++;
            return false;
        }

        if (currentChapterIndex < chapters.Length - 1)
        {
            currentChapterIndex++;
            currentMailRead = false;
            unlockedPostIndex = -1;
            openedChoiceIndex = -1;
            choiceUnlocked = false;
            return true;
        }

        return false;
    }

    private void ShowMainState()
    {
        SetActive(mailPanel, false);
        SetActive(forumWelcomePanel, false);
        SetActive(postListPanel, false);
        SetActive(postContentPanel, false);
        SetActive(choicePanel, false);
        SetActive(choiceContentPanel, false);
        HideAllContents();
    }

    private void RefreshStoryObjects()
    {
        bool hasChapter = chapters != null && currentChapterIndex >= 0 && currentChapterIndex < chapters.Length;

        if (forumButton != null)
        {
            forumButton.interactable = hasChapter && currentMailRead;
        }

        if (choiceButton != null)
        {
            bool hasChoice = HasChoiceForCurrentChapter();
            bool canClickChoiceButton = hasChapter && choiceUnlocked && hasChoice;
            choiceButton.interactable = canClickChoiceButton;

            StoryLog("RefreshStoryObjects choiceButton: "
                + $"hasChapter={hasChapter}, "
                + $"choiceUnlocked={choiceUnlocked}, "
                + $"hasChoice={hasChoice}, "
                + $"interactable={canClickChoiceButton}");
        }
        else
        {
            StoryLog("RefreshStoryObjects choiceButton: choiceButton is not assigned in Inspector.");
        }

        if (chapters == null)
        {
            return;
        }

        for (int chapterIndex = 0; chapterIndex < chapters.Length; chapterIndex++)
        {
            StoryChapter chapter = chapters[chapterIndex];
            if (chapter == null)
            {
                continue;
            }

            SetActive(chapter.mail?.button?.gameObject, chapterIndex == currentChapterIndex);
            bool choiceVisible = chapterIndex == currentChapterIndex && choiceUnlocked;
            SetActive(chapter.choice?.firstOption?.button?.gameObject, choiceVisible);
            SetActive(chapter.choice?.secondOption?.button?.gameObject, choiceVisible);

            PostItem[] posts = chapter.posts;
            if (posts == null)
            {
                continue;
            }

            for (int postIndex = 0; postIndex < posts.Length; postIndex++)
            {
                bool unlocked = chapterIndex == currentChapterIndex && currentMailRead && postIndex <= unlockedPostIndex;
                SetActive(posts[postIndex].button?.gameObject, unlocked);
            }
        }
    }

    private void HideAllContents()
    {
        if (chapters == null)
        {
            return;
        }

        foreach (StoryChapter chapter in chapters)
        {
            if (chapter == null)
            {
                continue;
            }

            SetActive(chapter.mail?.contentImage, false);
            SetActive(chapter.choice?.firstOption?.contentImage, false);
            SetActive(chapter.choice?.secondOption?.contentImage, false);

            PostItem[] posts = chapter.posts;
            if (posts == null)
            {
                continue;
            }

            foreach (PostItem post in posts)
            {
                SetActive(post.contentImage, false);
            }
        }
    }

    private void AddClick(Button button, UnityEngine.Events.UnityAction action)
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

    private bool HasCurrentChapter()
    {
        return chapters != null && currentChapterIndex >= 0 && currentChapterIndex < chapters.Length && chapters[currentChapterIndex] != null;
    }

    private bool HasChoiceForCurrentChapter()
    {
        if (!HasCurrentChapter())
        {
            return false;
        }

        ChoiceItem choice = chapters[currentChapterIndex].choice;
        return choice != null && (HasChoiceOption(choice.firstOption) || HasChoiceOption(choice.secondOption));
    }

    private bool HasChoiceOption(ChoiceOption option)
    {
        return option != null && (option.button != null || option.contentImage != null);
    }

    private ChoiceOption GetChoiceOption(StoryChapter chapter, int optionIndex)
    {
        if (chapter == null || chapter.choice == null)
        {
            return null;
        }

        if (optionIndex == 0)
        {
            return chapter.choice.firstOption;
        }

        if (optionIndex == 1)
        {
            return chapter.choice.secondOption;
        }

        return null;
    }

    private bool IsLastPost(int postIndex)
    {
        if (!HasCurrentChapter())
        {
            return false;
        }

        PostItem[] posts = chapters[currentChapterIndex].posts;
        return posts != null && posts.Length > 0 && postIndex == posts.Length - 1;
    }

    private void AdvanceAfterChoice(ChoiceOption selectedOption)
    {
        int nextChapterIndex = selectedOption != null ? selectedOption.nextChapterIndex : -1;

        if (IsValidChapterIndex(nextChapterIndex))
        {
            currentChapterIndex = nextChapterIndex;
        }
        else
        {
            currentChapterIndex++;
        }

        currentMailRead = false;
        unlockedPostIndex = -1;
        openedPostIndex = -1;
        openedChoiceIndex = -1;
        choiceUnlocked = false;

        if (HasCurrentChapter())
        {
            OpenMailPanel();
        }
        else
        {
            ShowMainState();
            RefreshStoryObjects();
        }
    }

    private bool IsValidChapterIndex(int chapterIndex)
    {
        return chapters != null && chapterIndex >= 0 && chapterIndex < chapters.Length;
    }

    private void StoryLog(string message)
    {
        if (showDebugLog)
        {
            Debug.Log($"[StorySystem] {message}", this);
        }
    }
}
