using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ComputerBackgroundLiteAnimator : MonoBehaviour
{
    [SerializeField] private Vector2 referenceSize = new Vector2(2560f, 1440f);

    [Header("Smoke")]
    [SerializeField] private string smokeResourcePath = "ComputerBackground/Smoke";
    [SerializeField] private Rect smokePixelRect = new Rect(0f, 0f, 420f, 560f);
    [SerializeField] private float smokeFramesPerSecond = 6f;

    [Header("Geese")]
    [SerializeField] private string geeseResourcePath = "ComputerBackground/Geese/Geese";
    [SerializeField] private Rect geesePixelRect = new Rect(1870f, 365f, 275f, 160f);
    [SerializeField] private Vector2 geeseMoveOffset = new Vector2(-240f, 28f);
    [SerializeField] private float geeseMoveDuration = 16f;

    private Sprite[] smokeFrames = Array.Empty<Sprite>();
    private Image smokeImage;
    private Image geeseImage;
    private Vector2 geeseStartPosition;
    private float elapsed;
    private int nextLayerSiblingIndex = 1;

    private void Awake()
    {
        LoadSmokeLayer();
        LoadGeeseLayer();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        UpdateSmoke();
        UpdateGeese();
    }

    private void LoadSmokeLayer()
    {
        smokeFrames = Resources.LoadAll<Sprite>(smokeResourcePath)
            .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
            .ToArray();

        if (smokeFrames.Length == 0)
        {
            return;
        }

        smokeImage = CreateLayer("Smoke Layer", smokePixelRect);
        smokeImage.sprite = smokeFrames[0];
    }

    private void LoadGeeseLayer()
    {
        Sprite geeseSprite = Resources.LoadAll<Sprite>(geeseResourcePath)
            .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
            .FirstOrDefault();

        if (geeseSprite == null)
        {
            return;
        }

        geeseImage = CreateLayer("Geese Layer", geesePixelRect);
        geeseImage.sprite = geeseSprite;
        geeseStartPosition = geeseImage.rectTransform.anchoredPosition;
    }

    private void UpdateSmoke()
    {
        if (smokeImage == null || smokeFrames.Length == 0 || smokeFramesPerSecond <= 0f)
        {
            return;
        }

        int frameIndex = Mathf.FloorToInt(elapsed * smokeFramesPerSecond) % smokeFrames.Length;
        smokeImage.sprite = smokeFrames[frameIndex];
    }

    private void UpdateGeese()
    {
        if (geeseImage == null || geeseMoveDuration <= 0f)
        {
            return;
        }

        float pingPong = Mathf.PingPong(elapsed / geeseMoveDuration, 1f);
        float eased = Mathf.SmoothStep(0f, 1f, pingPong);
        geeseImage.rectTransform.anchoredPosition = geeseStartPosition + geeseMoveOffset * eased;
    }

    private Image CreateLayer(string layerName, Rect pixelRect)
    {
        GameObject layer = new GameObject(layerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        layer.transform.SetParent(transform, false);

        RectTransform rectTransform = (RectTransform)layer.transform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(pixelRect.width, pixelRect.height);
        rectTransform.anchoredPosition = PixelRectToAnchoredPosition(pixelRect);
        rectTransform.SetSiblingIndex(Mathf.Min(nextLayerSiblingIndex++, transform.childCount - 1));

        Image image = layer.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = false;
        return image;
    }

    private Vector2 PixelRectToAnchoredPosition(Rect pixelRect)
    {
        float x = pixelRect.x + pixelRect.width * 0.5f - referenceSize.x * 0.5f;
        float y = referenceSize.y * 0.5f - pixelRect.y - pixelRect.height * 0.5f;
        return new Vector2(x, y);
    }
}
