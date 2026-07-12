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
    public int CurrentRoundIndex { get; private set; }
    public int UnlockedPostIndex { get; private set; }
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
        OpenedPostIndex = -1;
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
        }

        if (hasPosts && Phase == StoryRoundPhase.AwaitingMail)
        {
            Phase = StoryRoundPhase.ReadingPosts;
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
}
