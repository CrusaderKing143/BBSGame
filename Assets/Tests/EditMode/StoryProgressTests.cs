using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

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
    public void SelectionPostUsesTheRecordImageFromTheSelectedBranch()
    {
        GameObject fallbackObject = new GameObject("Fallback Record Image", typeof(RawImage));
        GameObject jiubaObject = new GameObject("Jiuba Record Image", typeof(RawImage));
        GameObject officeObject = new GameObject("Office Record Image", typeof(RawImage));
        GameObject jiubaContent = new GameObject("Jiuba Content");
        GameObject officeContent = new GameObject("Office Content");

        try
        {
            RawImage fallback = fallbackObject.GetComponent<RawImage>();
            RawImage jiuba = jiubaObject.GetComponent<RawImage>();
            RawImage office = officeObject.GetComponent<RawImage>();
            SelectionPostData selectionPost = new SelectionPostData
            {
                recordImage = fallback,
                branches = new[]
                {
                    new SelectionPostBranchData
                    {
                        itemId = "jiuBa",
                        contentImage = jiubaContent,
                        recordImage = jiuba
                    },
                    new SelectionPostBranchData
                    {
                        itemId = "office",
                        contentImage = officeContent,
                        recordImage = office
                    }
                }
            };

            Assert.That(selectionPost.GetRecordImage("jiuBa"), Is.SameAs(jiuba));
            Assert.That(selectionPost.GetRecordImage("office"), Is.SameAs(office));
            Assert.That(selectionPost.GetRecordImage("missing"), Is.SameAs(fallback));
        }
        finally
        {
            Object.DestroyImmediate(fallbackObject);
            Object.DestroyImmediate(jiubaObject);
            Object.DestroyImmediate(officeObject);
            Object.DestroyImmediate(jiubaContent);
            Object.DestroyImmediate(officeContent);
        }
    }

    [Test]
    public void ImmediateEndingBranchIsValidWithoutPostContent()
    {
        VideoClip endingClip = Resources.Load<VideoClip>("2");
        Assert.That(endingClip, Is.Not.Null);

        SelectionPostBranchData branch = new SelectionPostBranchData
        {
            itemId = "character-day3-assistant",
            completionMode = SelectionBranchCompletionMode.PlayEndingImmediately,
            endingVideoClip = endingClip
        };
        SelectionPostData selectionPost = new SelectionPostData
        {
            categoryType = SelectionCategoryType.Character,
            branches = new[] { branch }
        };

        Assert.That(branch.IsValid, Is.True);
        Assert.That(selectionPost.IsValid, Is.True);
        Assert.That(
            selectionPost.GetBranch("character-day3-assistant"),
            Is.SameAs(branch));
        Assert.That(selectionPost.GetContent("character-day3-assistant"), Is.Null);
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
    public void ParallelPostStageRequiresEveryPostBeforeUnlockingNextStage()
    {
        PostData[] posts =
        {
            new PostData(),
            new PostData(),
            new PostData { unlockWithPrevious = true },
            new PostData()
        };
        StoryProgress progress = new StoryProgress();
        progress.Reset();
        progress.MarkMailRead(posts);

        Assert.That(progress.IsPostAvailable(0, posts), Is.True);
        Assert.That(progress.IsPostAvailable(1, posts), Is.False);

        Assert.That(progress.TryOpenPost(0, 0, posts), Is.True);
        Assert.That(progress.CompleteOpenedPost(posts), Is.False);
        Assert.That(progress.UnlockedPostIndex, Is.EqualTo(2));
        Assert.That(progress.IsPostAvailable(1, posts), Is.True);
        Assert.That(progress.IsPostAvailable(2, posts), Is.True);
        Assert.That(progress.IsPostAvailable(3, posts), Is.False);

        Assert.That(progress.TryOpenPost(0, 1, posts), Is.True);
        Assert.That(progress.CompleteOpenedPost(posts), Is.False);
        Assert.That(progress.IsPostAvailable(1, posts), Is.False);
        Assert.That(progress.IsPostAvailable(2, posts), Is.True);
        Assert.That(progress.IsPostAvailable(3, posts), Is.False);

        Assert.That(progress.TryOpenPost(0, 2, posts), Is.True);
        Assert.That(progress.CompleteOpenedPost(posts), Is.False);
        Assert.That(progress.IsPostAvailable(2, posts), Is.False);
        Assert.That(progress.IsPostAvailable(3, posts), Is.True);
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
    public void ImmediateSelectionEndingCompletesWithoutOpeningResultPost()
    {
        StoryProgress progress = ReadyProgress();
        Assert.That(progress.TryOpenPost(0, 0, 1), Is.True);
        Assert.That(progress.CompleteOpenedPost(1, true), Is.False);
        Assert.That(progress.TrySubmitSelection(), Is.True);

        Assert.That(progress.TryCompleteSelectionImmediately(), Is.True);
        Assert.That(progress.Phase, Is.EqualTo(StoryRoundPhase.RoundCompleted));
        Assert.That(progress.SelectionPostOpened, Is.False);
        Assert.That(progress.TryOpenSelectionPost(0), Is.False);
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
