using System.Collections.Generic;

public enum StoryRoundPhase
{
    AwaitingMail,
    ReadingPosts,
    AwaitingSelection,
    AwaitingSelectionPost,
    RoundCompleted
}

public class StoryProgress
{
    private readonly HashSet<int> completedPostIndices = new HashSet<int>();

    public int CurrentRoundIndex { get; private set; }
    public int UnlockedPostIndex { get; private set; }
    public int UnlockedPostStage { get; private set; }
    public int OpenedPostIndex { get; private set; }
    public bool MailRead { get; private set; }
    public bool ForumJoined { get; private set; }
    public StoryRoundPhase Phase { get; private set; }
    public bool SelectionPostOpened { get; private set; }

    public bool CanOpenSelection => Phase == StoryRoundPhase.AwaitingSelection;
    public bool SelectionPostUnlocked => Phase == StoryRoundPhase.AwaitingSelectionPost;

    public void Reset()
    {
        ForumJoined = false;
        BeginRound(0);
    }

    public void BeginRound(int roundIndex)
    {
        CurrentRoundIndex = roundIndex;
        UnlockedPostIndex = -1;
        UnlockedPostStage = -1;
        OpenedPostIndex = -1;
        completedPostIndices.Clear();
        MailRead = false;
        Phase = StoryRoundPhase.AwaitingMail;
        SelectionPostOpened = false;
    }

    public void MarkMailRead(bool hasPosts)
    {
        MailRead = true;

        if (!hasPosts)
        {
            UnlockedPostIndex = -1;
        }
        else if (UnlockedPostIndex < 0)
        {
            UnlockedPostIndex = 0;
            UnlockedPostStage = 0;
        }

        if (hasPosts && Phase == StoryRoundPhase.AwaitingMail)
        {
            Phase = StoryRoundPhase.ReadingPosts;
        }
    }

    public void MarkMailRead(PostData[] posts)
    {
        bool hasPosts = posts != null && posts.Length > 0;
        MarkMailRead(hasPosts);
        if (hasPosts && UnlockedPostStage < 0)
        {
            UnlockedPostStage = 0;
        }

        if (hasPosts && completedPostIndices.Count == 0)
        {
            UnlockedPostIndex = FindLastPostIndexInStage(posts, UnlockedPostStage);
        }
    }

    public void MarkForumJoined()
    {
        ForumJoined = true;
    }

    public bool TryOpenPost(int roundIndex, int postIndex, int postCount)
    {
        bool canOpen = roundIndex == CurrentRoundIndex
            && MailRead
            && postIndex >= 0
            && postIndex < postCount
            && postIndex <= UnlockedPostIndex;

        if (!canOpen)
        {
            return false;
        }

        OpenedPostIndex = postIndex;
        return true;
    }

    public bool TryOpenPost(int roundIndex, int postIndex, PostData[] posts)
    {
        if (roundIndex != CurrentRoundIndex || !IsPostAvailable(postIndex, posts))
        {
            return false;
        }

        OpenedPostIndex = postIndex;
        return true;
    }

    public bool IsPostAvailable(int postIndex, PostData[] posts)
    {
        return IsPostInCurrentStage(postIndex, posts)
            && !completedPostIndices.Contains(postIndex);
    }

    public bool IsPostInCurrentStage(int postIndex, PostData[] posts)
    {
        return Phase == StoryRoundPhase.ReadingPosts
            && MailRead
            && posts != null
            && postIndex >= 0
            && postIndex < posts.Length
            && GetPostStage(posts, postIndex) == UnlockedPostStage;
    }

    public bool CompleteOpenedPost(int postCount, bool hasSelectionPost = false)
    {
        if (SelectionPostOpened)
        {
            SelectionPostOpened = false;
            Phase = StoryRoundPhase.RoundCompleted;
            return true;
        }

        if (OpenedPostIndex < 0)
        {
            return false;
        }

        bool completedRound = false;
        bool openedLatestPost = OpenedPostIndex == UnlockedPostIndex;

        if (openedLatestPost && postCount > 0 && Phase == StoryRoundPhase.ReadingPosts)
        {
            if (UnlockedPostIndex < postCount - 1)
            {
                UnlockedPostIndex++;
            }
            else if (hasSelectionPost)
            {
                Phase = StoryRoundPhase.AwaitingSelection;
            }
            else
            {
                completedRound = true;
                Phase = StoryRoundPhase.RoundCompleted;
            }
        }

        OpenedPostIndex = -1;
        return completedRound;
    }

    public bool CompleteOpenedPost(PostData[] posts, bool hasSelectionPost = false)
    {
        if (SelectionPostOpened)
        {
            SelectionPostOpened = false;
            Phase = StoryRoundPhase.RoundCompleted;
            return true;
        }

        if (OpenedPostIndex < 0)
        {
            return false;
        }

        int completedPostIndex = OpenedPostIndex;
        OpenedPostIndex = -1;
        if (posts == null
            || completedPostIndex >= posts.Length
            || Phase != StoryRoundPhase.ReadingPosts)
        {
            return false;
        }

        int completedStage = GetPostStage(posts, completedPostIndex);
        completedPostIndices.Add(completedPostIndex);
        if (completedStage != UnlockedPostStage
            || !IsStageCompleted(posts, completedStage))
        {
            return false;
        }

        int nextStage = completedStage + 1;
        int nextStageLastPost = FindLastPostIndexInStage(posts, nextStage);
        if (nextStageLastPost >= 0)
        {
            UnlockedPostStage = nextStage;
            UnlockedPostIndex = nextStageLastPost;
            return false;
        }

        if (hasSelectionPost)
        {
            Phase = StoryRoundPhase.AwaitingSelection;
            return false;
        }

        Phase = StoryRoundPhase.RoundCompleted;
        return true;
    }

    private bool IsStageCompleted(PostData[] posts, int stage)
    {
        bool foundPost = false;
        for (int postIndex = 0; postIndex < posts.Length; postIndex++)
        {
            if (GetPostStage(posts, postIndex) != stage)
            {
                continue;
            }

            foundPost = true;
            if (!completedPostIndices.Contains(postIndex))
            {
                return false;
            }
        }

        return foundPost;
    }

    private static int FindLastPostIndexInStage(PostData[] posts, int stage)
    {
        if (posts == null || stage < 0)
        {
            return -1;
        }

        int result = -1;
        for (int postIndex = 0; postIndex < posts.Length; postIndex++)
        {
            int postStage = GetPostStage(posts, postIndex);
            if (postStage == stage)
            {
                result = postIndex;
            }
            else if (postStage > stage)
            {
                break;
            }
        }

        return result;
    }

    private static int GetPostStage(PostData[] posts, int postIndex)
    {
        int stage = 0;
        for (int index = 1; index <= postIndex; index++)
        {
            if (posts[index] == null || !posts[index].unlockWithPrevious)
            {
                stage++;
            }
        }

        return stage;
    }

    public bool TrySubmitSelection()
    {
        if (Phase != StoryRoundPhase.AwaitingSelection)
        {
            return false;
        }

        Phase = StoryRoundPhase.AwaitingSelectionPost;
        return true;
    }

    public bool TryOpenSelectionPost(int roundIndex)
    {
        if (roundIndex != CurrentRoundIndex
            || Phase != StoryRoundPhase.AwaitingSelectionPost
            || SelectionPostOpened)
        {
            return false;
        }

        SelectionPostOpened = true;
        return true;
    }

    public bool TryCompleteSelectionImmediately()
    {
        if (Phase != StoryRoundPhase.AwaitingSelectionPost || SelectionPostOpened)
        {
            return false;
        }

        Phase = StoryRoundPhase.RoundCompleted;
        return true;
    }
}
