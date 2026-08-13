using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MailPanelController : MonoBehaviour
{
    private const float MailViewportWidth = 947f;
    private const float MailViewportHeight = 946f;
    private const string RuntimeContentName = "Mail Scroll Content";
    private const string RuntimeImageName = "Mail Image";

    [SerializeField] private GameObject mailPanel;
    [SerializeField] private Button backButton;

    private readonly Dictionary<GameObject, ScrollRect> mailScrollRects =
        new Dictionary<GameObject, ScrollRect>();

    public Button BackButton => backButton;

    public void ShowPanel(StoryRoundData[] rounds, int currentRoundIndex)
    {
        HideAllContents(rounds);
        RefreshMailButtons(rounds, currentRoundIndex);
        SetActive(mailPanel, true);
    }

    public void ShowContent(StoryRoundData[] rounds, int roundIndex)
    {
        HideAllContents(rounds);

        if (!IsValidRound(rounds, roundIndex))
        {
            return;
        }

        GameObject contentImage = rounds[roundIndex].mail?.contentImage;
        ScrollRect scrollRect = EnsureMailScrollView(contentImage);
        SetActive(contentImage, true);
        ResetScrollToTop(scrollRect);
    }

    public void HidePanel(StoryRoundData[] rounds)
    {
        SetActive(mailPanel, false);
        HideAllContents(rounds);
    }

    public void RefreshMailButtons(StoryRoundData[] rounds, int currentRoundIndex)
    {
        if (rounds == null)
        {
            return;
        }

        for (int roundIndex = 0; roundIndex < rounds.Length; roundIndex++)
        {
            MailData mail = rounds[roundIndex]?.mail;
            SetActive(mail?.button?.gameObject, roundIndex <= currentRoundIndex);
        }
    }

    private void HideAllContents(StoryRoundData[] rounds)
    {
        if (rounds == null)
        {
            return;
        }

        foreach (StoryRoundData round in rounds)
        {
            SetActive(round?.mail?.contentImage, false);
        }
    }

    private static bool IsValidRound(StoryRoundData[] rounds, int roundIndex)
    {
        return rounds != null
            && roundIndex >= 0
            && roundIndex < rounds.Length
            && rounds[roundIndex] != null;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private ScrollRect EnsureMailScrollView(GameObject contentImage)
    {
        if (contentImage == null || !TryGetContentHeight(contentImage.name, out float contentHeight))
        {
            return null;
        }

        if (mailScrollRects.TryGetValue(contentImage, out ScrollRect cachedScrollRect)
            && cachedScrollRect != null)
        {
            return cachedScrollRect;
        }

        ScrollRect scrollRect = contentImage.GetComponent<ScrollRect>();
        if (scrollRect != null && scrollRect.content != null)
        {
            ConfigureScrollRect(scrollRect);
            mailScrollRects[contentImage] = scrollRect;
            return scrollRect;
        }

        RectTransform viewport = contentImage.GetComponent<RectTransform>();
        Image sourceImage = contentImage.GetComponent<Image>();
        if (viewport == null || sourceImage == null || sourceImage.sprite == null)
        {
            Debug.LogWarning(
                $"[MailPanelController] '{contentImage.name}' requires a RectTransform and an Image with a Sprite.",
                contentImage);
            return null;
        }

        KeepTopAndResizeViewport(viewport);

        RectMask2D mask = contentImage.GetComponent<RectMask2D>();
        if (mask == null)
        {
            mask = contentImage.AddComponent<RectMask2D>();
        }

        RectTransform content = CreateContent(viewport, contentHeight);
        CreateMailImage(content, sourceImage, contentHeight);

        sourceImage.raycastTarget = false;
        sourceImage.enabled = false;

        scrollRect = contentImage.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = contentImage.AddComponent<ScrollRect>();
        }

        scrollRect.content = content;
        scrollRect.viewport = viewport;
        ConfigureScrollRect(scrollRect);
        mailScrollRects[contentImage] = scrollRect;
        return scrollRect;
    }

    private static void KeepTopAndResizeViewport(RectTransform viewport)
    {
        float oldHeight = viewport.rect.height;
        if (oldHeight <= 0f)
        {
            oldHeight = viewport.sizeDelta.y;
        }

        Vector2 anchoredPosition = viewport.anchoredPosition;
        anchoredPosition.y += (oldHeight - MailViewportHeight) * (1f - viewport.pivot.y);
        viewport.anchoredPosition = anchoredPosition;
        viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MailViewportWidth);
        viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, MailViewportHeight);
    }

    private static RectTransform CreateContent(RectTransform viewport, float contentHeight)
    {
        GameObject contentObject = new GameObject(RuntimeContentName, typeof(RectTransform));
        contentObject.layer = viewport.gameObject.layer;
        contentObject.transform.SetParent(viewport, false);

        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0.5f, 1f);
        content.anchorMax = new Vector2(0.5f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(MailViewportWidth, contentHeight);
        return content;
    }

    private static void CreateMailImage(
        RectTransform content,
        Image sourceImage,
        float contentHeight)
    {
        GameObject imageObject = new GameObject(
            RuntimeImageName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));
        imageObject.layer = content.gameObject.layer;
        imageObject.transform.SetParent(content, false);

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        Sprite sprite = sourceImage.sprite;
        RawImage image = imageObject.GetComponent<RawImage>();
        image.texture = sprite.texture;
        image.material = sourceImage.material;
        image.color = sourceImage.color;
        image.raycastTarget = true;
        image.maskable = true;
        image.uvRect = GetTopCropUv(sprite, contentHeight);
    }

    private static Rect GetTopCropUv(Sprite sprite, float visibleHeight)
    {
        Rect spriteRect = sprite.rect;
        Texture texture = sprite.texture;
        float croppedHeight = Mathf.Min(visibleHeight, spriteRect.height);

        return new Rect(
            spriteRect.x / texture.width,
            (spriteRect.yMax - croppedHeight) / texture.height,
            spriteRect.width / texture.width,
            croppedHeight / texture.height);
    }

    private static void ConfigureScrollRect(ScrollRect scrollRect)
    {
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;
        scrollRect.scrollSensitivity = 25f;
        scrollRect.horizontalScrollbar = null;
        scrollRect.verticalScrollbar = null;
    }

    private static void ResetScrollToTop(ScrollRect scrollRect)
    {
        if (scrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        scrollRect.StopMovement();
        scrollRect.velocity = Vector2.zero;
        scrollRect.verticalNormalizedPosition = 1f;
        if (scrollRect.content != null)
        {
            Vector2 position = scrollRect.content.anchoredPosition;
            position.y = 0f;
            scrollRect.content.anchoredPosition = position;
        }
    }

    private static bool TryGetContentHeight(string contentName, out float contentHeight)
    {
        switch (contentName)
        {
            case "MailContent_01":
                contentHeight = 946f;
                return true;
            case "MailContent_02":
                contentHeight = 995f;
                return true;
            case "MailContent_03":
                contentHeight = 1131f;
                return true;
            default:
                contentHeight = 0f;
                return false;
        }
    }
}
