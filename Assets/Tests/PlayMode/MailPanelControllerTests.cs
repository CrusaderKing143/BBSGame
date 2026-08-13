using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class MailPanelControllerTests
{
    [UnityTest]
    public IEnumerator MailContentsUseFixedViewportAndResetToTop()
    {
        GameObject root = new GameObject("Mail Scroll Test Root", typeof(RectTransform));
        MailPanelController controller = root.AddComponent<MailPanelController>();
        Texture2D texture = new Texture2D(947, 1131, TextureFormat.Alpha8, false);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f));

        StoryRoundData[] rounds =
        {
            CreateRound(root.transform, "MailContent_01", sprite),
            CreateRound(root.transform, "MailContent_02", sprite),
            CreateRound(root.transform, "MailContent_03", sprite)
        };

        controller.ShowContent(rounds, 0);
        AssertMailScroll(rounds[0].mail.contentImage, 946f, 0f);

        controller.ShowContent(rounds, 1);
        ScrollRect secondScroll = AssertMailScroll(
            rounds[1].mail.contentImage,
            995f,
            49f);
        secondScroll.verticalNormalizedPosition = 0f;

        controller.ShowContent(rounds, 2);
        AssertMailScroll(rounds[2].mail.contentImage, 1131f, 185f);

        controller.ShowContent(rounds, 1);
        Assert.That(secondScroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(0.001f));
        Assert.That(secondScroll.content.anchoredPosition.y, Is.EqualTo(0f).Within(0.001f));

        Object.Destroy(root);
        Object.Destroy(sprite);
        Object.Destroy(texture);
        yield return null;
    }

    private static StoryRoundData CreateRound(
        Transform parent,
        string contentName,
        Sprite sprite)
    {
        GameObject content = new GameObject(
            contentName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        content.transform.SetParent(parent, false);

        RectTransform rect = content.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(947f, 1131f);
        content.GetComponent<Image>().sprite = sprite;
        content.SetActive(false);

        return new StoryRoundData
        {
            mail = new MailData
            {
                contentImage = content
            }
        };
    }

    private static ScrollRect AssertMailScroll(
        GameObject contentRoot,
        float expectedContentHeight,
        float expectedScrollDistance)
    {
        RectTransform viewport = contentRoot.GetComponent<RectTransform>();
        ScrollRect scrollRect = contentRoot.GetComponent<ScrollRect>();
        RectMask2D mask = contentRoot.GetComponent<RectMask2D>();
        Image originalImage = contentRoot.GetComponent<Image>();
        RawImage mailImage = contentRoot.GetComponentInChildren<RawImage>();

        Assert.That(viewport.rect.width, Is.EqualTo(947f).Within(0.001f));
        Assert.That(viewport.rect.height, Is.EqualTo(946f).Within(0.001f));
        Assert.That(mask, Is.Not.Null);
        Assert.That(scrollRect, Is.Not.Null);
        Assert.That(scrollRect.horizontal, Is.False);
        Assert.That(scrollRect.vertical, Is.True);
        Assert.That(scrollRect.movementType, Is.EqualTo(ScrollRect.MovementType.Clamped));
        Assert.That(scrollRect.horizontalScrollbar, Is.Null);
        Assert.That(scrollRect.verticalScrollbar, Is.Null);
        Assert.That(scrollRect.content.rect.height, Is.EqualTo(expectedContentHeight).Within(0.001f));
        Assert.That(
            scrollRect.content.rect.height - viewport.rect.height,
            Is.EqualTo(expectedScrollDistance).Within(0.001f));
        Assert.That(originalImage.enabled, Is.False);
        Assert.That(mailImage, Is.Not.Null);
        Assert.That(mailImage.raycastTarget, Is.True);
        Assert.That(mailImage.uvRect.height, Is.EqualTo(expectedContentHeight / 1131f).Within(0.001f));
        return scrollRect;
    }
}
