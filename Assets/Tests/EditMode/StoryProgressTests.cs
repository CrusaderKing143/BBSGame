using NUnit.Framework;
using UnityEngine;

public class StoryProgressTests
{
    [Test]
    public void SelectionPostDoesNotRequireTitleButton()
    {
        GameObject content = new GameObject("Selection Post Content");
        SelectionPostData selectionPost = new SelectionPostData
        {
            button = null,
            branches = new[]
            {
                new SelectionPostBranchData
                {
                    itemId = "jiuBa",
                    contentImage = content
                }
            }
        };
        StoryRoundData round = new StoryRoundData { selectionPost = selectionPost };

        Assert.That(selectionPost.IsValid, Is.True);
        Assert.That(round.HasSelectionPost, Is.True);

        Object.DestroyImmediate(content);
    }

    [Test]
    public void ResetStartsAtFirstUnreadRound()
    {
        StoryProgress progress = new StoryProgress();

        progress.Reset();

        Assert.That(progress.CurrentRoundIndex, Is.EqualTo(0));
        Assert.That(progress.UnlockedPostIndex, Is.EqualTo(-1));
        Assert.That(progress.OpenedPostIndex, Is.EqualTo(-1));
        Assert.That(progress.MailRead, Is.False);
        Assert.That(progress.ForumJoined, Is.False);
        Assert.That(progress.Phase, Is.EqualTo(StoryRoundPhase.AwaitingMail));
    }

    [Test]
    public void ReadingMailUnlocksFirstPostWhenPostsExist()
    {
        StoryProgress progress = new StoryProgress();
        progress.Reset();

        progress.MarkMailRead(true);

        Assert.That(progress.MailRead, Is.True);
        Assert.That(progress.UnlockedPostIndex, Is.EqualTo(0));
    }

    [Test]
    public void ReturningFromLatestPostUnlocksNextPost()
    {
        StoryProgress progress = ReadyProgress();
        Assert.That(progress.TryOpenPost(0, 0, 3), Is.True);

        bool completedRound = progress.CompleteOpenedPost(3);

        Assert.That(completedRound, Is.False);
        Assert.That(progress.UnlockedPostIndex, Is.EqualTo(1));
        Assert.That(progress.OpenedPostIndex, Is.EqualTo(-1));
    }

    [Test]
    public void ReturningFromOlderPostDoesNotAdvanceProgress()
    {
        StoryProgress progress = ReadyProgress();
        progress.TryOpenPost(0, 0, 3);
        progress.CompleteOpenedPost(3);
        progress.TryOpenPost(0, 0, 3);

        bool completedRound = progress.CompleteOpenedPost(3);

        Assert.That(completedRound, Is.False);
        Assert.That(progress.UnlockedPostIndex, Is.EqualTo(1));
    }

    [Test]
    public void ReadingMailAgainDoesNotRelockPosts()
    {
        StoryProgress progress = ReadyProgress();
        progress.TryOpenPost(0, 0, 3);
        progress.CompleteOpenedPost(3);

        progress.MarkMailRead(true);

        Assert.That(progress.UnlockedPostIndex, Is.EqualTo(1));
    }

    [Test]
    public void CompletingLastPostReportsRoundCompletion()
    {
        StoryProgress progress = ReadyProgress();
        for (int postIndex = 0; postIndex < 3; postIndex++)
        {
            progress.TryOpenPost(0, postIndex, 3);
            bool completed = progress.CompleteOpenedPost(3);

            Assert.That(completed, Is.EqualTo(postIndex == 2));
        }

        Assert.That(progress.Phase, Is.EqualTo(StoryRoundPhase.RoundCompleted));
    }

    [Test]
    public void SelectionRoundWaitsForSelectionAndResultPostBeforeCompletion()
    {
        StoryProgress progress = ReadyProgress();

        for (int postIndex = 0; postIndex < 3; postIndex++)
        {
            Assert.That(progress.TryOpenPost(0, postIndex, 3), Is.True);
            Assert.That(progress.CompleteOpenedPost(3, true), Is.False);
        }

        Assert.That(progress.Phase, Is.EqualTo(StoryRoundPhase.AwaitingSelection));
        Assert.That(progress.CanOpenSelection, Is.True);
        Assert.That(progress.SelectionPostUnlocked, Is.False);

        Assert.That(progress.TrySubmitSelection(), Is.True);
        Assert.That(progress.CanOpenSelection, Is.False);
        Assert.That(progress.SelectionPostUnlocked, Is.True);

        Assert.That(progress.TryOpenSelectionPost(0), Is.True);
        Assert.That(progress.CompleteOpenedPost(3, true), Is.True);
        Assert.That(progress.Phase, Is.EqualTo(StoryRoundPhase.RoundCompleted));
    }

    [Test]
    public void ReopeningNormalPostAfterSelectionDoesNotChangeSelectionPhase()
    {
        StoryProgress progress = ReadyProgress();
        for (int postIndex = 0; postIndex < 3; postIndex++)
        {
            progress.TryOpenPost(0, postIndex, 3);
            progress.CompleteOpenedPost(3, true);
        }

        progress.TrySubmitSelection();
        Assert.That(progress.TryOpenPost(0, 2, 3), Is.True);

        Assert.That(progress.CompleteOpenedPost(3, true), Is.False);
        Assert.That(progress.Phase, Is.EqualTo(StoryRoundPhase.AwaitingSelectionPost));
    }

    [Test]
    public void BeginningNextRoundKeepsForumJoinedButResetsRoundState()
    {
        StoryProgress progress = ReadyProgress();
        progress.MarkForumJoined();

        progress.BeginRound(1);

        Assert.That(progress.CurrentRoundIndex, Is.EqualTo(1));
        Assert.That(progress.MailRead, Is.False);
        Assert.That(progress.UnlockedPostIndex, Is.EqualTo(-1));
        Assert.That(progress.ForumJoined, Is.True);
    }

    private static StoryProgress ReadyProgress()
    {
        StoryProgress progress = new StoryProgress();
        progress.Reset();
        progress.MarkMailRead(true);
        return progress;
    }
}
