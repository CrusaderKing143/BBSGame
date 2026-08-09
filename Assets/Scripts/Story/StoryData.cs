using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public enum SelectionBranchCompletionMode
{
    OpenPost = 0,
    OpenPostThenEnding = 1,
    PlayEndingImmediately = 2
}

[Serializable]
public class MailData
{
    public Button button;
    public GameObject contentImage;

    public bool IsValid => button != null && contentImage != null;
}

[Serializable]
public class SelectionCollectibleData
{
    public Button button;
    public SelectionCategoryType categoryType;
    public string itemId;

    public bool IsValid => button != null
        && (categoryType == SelectionCategoryType.Character
            || categoryType == SelectionCategoryType.Props)
        && !string.IsNullOrWhiteSpace(itemId);
}

[Serializable]
public class PostData
{
    public Button button;
    [Tooltip("Optional shared list root used when multiple post buttons appear on one forum image.")]
    public GameObject listDisplayRoot;
    public GameObject contentImage;
    public SelectionCollectibleData[] collectibles;
    [Tooltip("Show this post in the same unlock stage as the previous post.")]
    public bool unlockWithPrevious;

    public bool IsValid => button != null && contentImage != null;
}

[Serializable]
public class SelectionPostBranchData
{
    public string itemId;
    public GameObject contentImage;
    public RawImage recordImage;
    public SelectionCollectibleData[] collectibles;
    public SelectionBranchCompletionMode completionMode;
    public VideoClip endingVideoClip;

    public bool IsValid
    {
        get
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            switch (completionMode)
            {
                case SelectionBranchCompletionMode.OpenPost:
                    return contentImage != null;
                case SelectionBranchCompletionMode.OpenPostThenEnding:
                    return contentImage != null && endingVideoClip != null;
                case SelectionBranchCompletionMode.PlayEndingImmediately:
                    return endingVideoClip != null;
                default:
                    return false;
            }
        }
    }
}

[Serializable]
public class SelectionPostData
{
    public SelectionCategoryType categoryType = SelectionCategoryType.Background;
    public Button button;
    public RawImage recordImage;
    public SelectionPostBranchData[] branches;

    public bool IsValid
    {
        get
        {
            if (branches == null || branches.Length == 0)
            {
                return false;
            }

            HashSet<string> itemIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (SelectionPostBranchData branch in branches)
            {
                if (branch == null || !branch.IsValid || !itemIds.Add(branch.itemId))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public GameObject GetContent(string itemId)
    {
        return GetBranch(itemId)?.contentImage;
    }

    public RawImage GetRecordImage(string itemId)
    {
        if (branches != null && !string.IsNullOrEmpty(itemId))
        {
            foreach (SelectionPostBranchData branch in branches)
            {
                if (branch != null
                    && string.Equals(branch.itemId, itemId, StringComparison.Ordinal)
                    && branch.recordImage != null)
                {
                    return branch.recordImage;
                }
            }
        }

        return recordImage;
    }

    public SelectionPostBranchData GetBranch(string itemId)
    {
        if (branches == null || string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        foreach (SelectionPostBranchData branch in branches)
        {
            if (branch != null && string.Equals(branch.itemId, itemId, StringComparison.Ordinal))
            {
                return branch;
            }
        }

        return null;
    }
}

[Serializable]
public class StoryRoundData
{
    public MailData mail;
    public PostData[] posts;
    public SelectionPostData selectionPost;

    public int PostCount => posts != null ? posts.Length : 0;

    public bool HasSelectionPost => selectionPost?.IsValid == true;

    public bool HasPosts
    {
        get
        {
            if (PostCount == 0)
            {
                return false;
            }

            foreach (PostData post in posts)
            {
                if (post == null || !post.IsValid)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
