using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class HeartBurstButtonEffect : MonoBehaviour
{
    private const string BurstRootName = "Heart Burst";

    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Sprite heartSprite;

    [Header("Burst")]
    [SerializeField, Min(1)] private int heartCount = 5;
    [SerializeField] private Vector2 heartSize = new Vector2(42f, 36f);
    [SerializeField, Min(0f)] private float horizontalSpawnRange = 30f;
    [SerializeField] private Vector2 riseDistanceRange = new Vector2(65f, 110f);
    [SerializeField, Min(0f)] private float horizontalDriftRange = 24f;
    [SerializeField, Min(0f)] private float rotationRange = 18f;
    [SerializeField] private Vector2 durationRange = new Vector2(0.75f, 0.95f);
    [SerializeField, Range(0.01f, 1f)] private float startScale = 0.55f;
    [SerializeField, Range(0f, 1f)] private float fadeStartNormalized = 0.5f;
    [SerializeField] private int sortingOrder = 100;

    private readonly List<GameObject> activeBurstRoots = new List<GameObject>();
    private bool clickBound;

    private void Awake()
    {
        ResolveButton();
    }

    private void OnEnable()
    {
        ResolveButton();
        BindClick();
    }

    private void OnDisable()
    {
        UnbindClick();
        StopAllCoroutines();
        ClearBurstRoots();
    }

    private void OnDestroy()
    {
        UnbindClick();
        StopAllCoroutines();
        ClearBurstRoots();
    }

    private void OnValidate()
    {
        ResolveButton();
        heartCount = Mathf.Max(1, heartCount);
        heartSize.x = Mathf.Max(1f, heartSize.x);
        heartSize.y = Mathf.Max(1f, heartSize.y);
        horizontalSpawnRange = Mathf.Max(0f, horizontalSpawnRange);
        horizontalDriftRange = Mathf.Max(0f, horizontalDriftRange);
        rotationRange = Mathf.Max(0f, rotationRange);
        riseDistanceRange = SortPositiveRange(riseDistanceRange, 1f);
        durationRange = SortPositiveRange(durationRange, 0.01f);
    }

    private void ResolveButton()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    private void BindClick()
    {
        if (button == null || clickBound)
        {
            return;
        }

        button.onClick.AddListener(PlayBurst);
        clickBound = true;
    }

    private void UnbindClick()
    {
        if (button != null && clickBound)
        {
            button.onClick.RemoveListener(PlayBurst);
        }

        clickBound = false;
    }

    private void PlayBurst()
    {
        if (heartSprite == null)
        {
            Debug.LogWarning("[HeartBurstButtonEffect] Heart Sprite is missing.", this);
            return;
        }

        GameObject burstRoot = CreateBurstRoot();
        activeBurstRoots.Add(burstRoot);

        for (int index = 0; index < Mathf.Max(1, heartCount); index++)
        {
            float delay = index * 0.025f;
            StartCoroutine(AnimateHeart(burstRoot.transform, delay));
        }

        StartCoroutine(DestroyBurstAfterAnimations(burstRoot));
    }

    private GameObject CreateBurstRoot()
    {
        GameObject root = new GameObject(
            BurstRootName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(GraphicRaycaster));
        root.layer = gameObject.layer;
        root.transform.SetParent(transform, false);
        root.transform.SetAsLastSibling();

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        GraphicRaycaster raycaster = root.GetComponent<GraphicRaycaster>();
        raycaster.enabled = false;
        return root;
    }

    private IEnumerator AnimateHeart(Transform parent, float delay)
    {
        if (delay > 0f)
        {
            float delayElapsed = 0f;
            while (delayElapsed < delay && parent != null)
            {
                delayElapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (parent == null)
        {
            yield break;
        }

        GameObject heartObject = new GameObject(
            "Heart",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup));
        heartObject.layer = gameObject.layer;
        heartObject.transform.SetParent(parent, false);

        RectTransform rect = heartObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(
            Mathf.Max(1f, heartSize.x),
            Mathf.Max(1f, heartSize.y));

        Vector2 startPosition = new Vector2(
            Random.Range(-horizontalSpawnRange, horizontalSpawnRange),
            4f);
        Vector2 endPosition = startPosition + new Vector2(
            Random.Range(-horizontalDriftRange, horizontalDriftRange),
            Random.Range(
                Mathf.Min(riseDistanceRange.x, riseDistanceRange.y),
                Mathf.Max(riseDistanceRange.x, riseDistanceRange.y)));
        rect.anchoredPosition = startPosition;

        float startRotation = Random.Range(-rotationRange, rotationRange);
        float endRotation = startRotation + Random.Range(-rotationRange, rotationRange);
        rect.localRotation = Quaternion.Euler(0f, 0f, startRotation);
        rect.localScale = Vector3.one * Mathf.Max(0.01f, startScale);

        Image image = heartObject.GetComponent<Image>();
        image.sprite = heartSprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.maskable = false;

        CanvasGroup canvasGroup = heartObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float duration = Random.Range(
            Mathf.Min(durationRange.x, durationRange.y),
            Mathf.Max(durationRange.x, durationRange.y));
        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < duration && heartObject != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 2f);

            rect.anchoredPosition = Vector2.LerpUnclamped(
                startPosition,
                endPosition,
                easedProgress);
            rect.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Lerp(startRotation, endRotation, easedProgress));

            float popProgress = Mathf.Clamp01(progress / 0.22f);
            float scale = Mathf.SmoothStep(
                Mathf.Max(0.01f, startScale),
                1f,
                popProgress);
            rect.localScale = Vector3.one * scale;

            if (canvasGroup != null)
            {
                float fadeProgress = Mathf.InverseLerp(
                    Mathf.Clamp01(fadeStartNormalized),
                    1f,
                    progress);
                canvasGroup.alpha = 1f - fadeProgress;
            }

            yield return null;
        }

        if (heartObject != null)
        {
            Destroy(heartObject);
        }
    }

    private IEnumerator DestroyBurstAfterAnimations(GameObject burstRoot)
    {
        float maxDuration = Mathf.Max(durationRange.x, durationRange.y);
        float totalDuration = Mathf.Max(0.01f, maxDuration)
            + Mathf.Max(0, heartCount - 1) * 0.025f
            + 0.05f;
        float elapsed = 0f;

        while (elapsed < totalDuration && burstRoot != null)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        activeBurstRoots.Remove(burstRoot);
        if (burstRoot != null)
        {
            Destroy(burstRoot);
        }
    }

    private void ClearBurstRoots()
    {
        for (int index = activeBurstRoots.Count - 1; index >= 0; index--)
        {
            GameObject root = activeBurstRoots[index];
            if (root != null)
            {
                Destroy(root);
            }
        }

        activeBurstRoots.Clear();
    }

    private static Vector2 SortPositiveRange(Vector2 range, float minimum)
    {
        float min = Mathf.Max(minimum, Mathf.Min(range.x, range.y));
        float max = Mathf.Max(min, Mathf.Max(range.x, range.y));
        return new Vector2(min, max);
    }
}
