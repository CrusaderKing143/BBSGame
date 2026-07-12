using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class MailData
{
    public Button button;
    public GameObject contentImage;

    public bool IsValid => button != null && contentImage != null;
}

[Serializable]
public class PostData
{
    public Button button;
    public GameObject contentImage;

    public bool IsValid => button != null && contentImage != null;
}

[Serializable]
public class SelectionPostBranchData
{
    public string itemId;
    public GameObject contentImage;

    public bool IsValid => !string.IsNullOrWhiteSpace(itemId) && contentImage != null;
}

[Serializable]
public class SelectionPostData
{
    public SelectionCategoryType categoryType = SelectionCategoryType.Background;
    public Button button;
    public SelectionPostBranchData[] branches;

    public bool IsValid
    {
        get
        {
            if (button == null || branches == null || branches.Length == 0)
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
        if (branches == null || string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        foreach (SelectionPostBranchData branch in branches)
        {
            if (branch != null && string.Equals(branch.itemId, itemId, StringComparison.Ordinal))
            {
                return branch.contentImage;
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
