using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class SelectionPanelController
{
    private Coroutine submissionCoroutine;
    private bool submissionInProgress;
    private Texture2D committedSnapshotSource;
    private Texture2D committedSnapshotDisplay;
    private float committedSnapshotDisplayAspect;
    private RawImage committedSnapshotTarget;
    private Texture committedSnapshotTargetOriginalTexture;
    private Color committedSnapshotTargetOriginalColor;
    private Rect committedSnapshotTargetOriginalUvRect;
    private GameObject pendingSnapshotSelectionFrame;
    private bool pendingSnapshotSelectionFrameWasActive;
    private GameObject pendingSnapshotItemRoot;
    private bool pendingSnapshotItemRootWasActive;
    private GameObject pendingSnapshotBottomNavigation;
    private bool pendingSnapshotBottomNavigationWasActive;

    public bool ApplyCommittedSnapshot(RawImage target)
    {
        if (target == null)
        {
            WarnOnce("Cannot display the committed composition because RecordImage is not assigned.");
            return false;
        }

        if (committedSnapshotSource == null)
        {
            WarnOnce("Cannot display the committed composition because no snapshot was captured.");
            return false;
        }

        if (committedSnapshotTarget != target)
        {
            RestoreCommittedSnapshotTarget();
            committedSnapshotTarget = target;
            committedSnapshotTargetOriginalTexture = target.texture;
            committedSnapshotTargetOriginalColor = target.color;
            committedSnapshotTargetOriginalUvRect = target.uvRect;
        }

        Rect targetRect = target.rectTransform.rect;
        float targetAspect = targetRect.height > 0f
            ? targetRect.width / targetRect.height
            : (float)committedSnapshotSource.width / committedSnapshotSource.height;
        targetAspect = Mathf.Clamp(targetAspect, 0.1f, 10f);

        if (committedSnapshotDisplay == null
            || !Mathf.Approximately(committedSnapshotDisplayAspect, targetAspect))
        {
            DestroyTexture(ref committedSnapshotDisplay);
            committedSnapshotDisplay = CreateAspectFilledTexture(
                committedSnapshotSource,
                targetAspect);
            committedSnapshotDisplayAspect = targetAspect;
        }

        if (committedSnapshotDisplay == null)
        {
            WarnOnce("Failed to create the fitted composition snapshot.");
            return false;
        }

        target.texture = committedSnapshotDisplay;
        target.color = Color.white;
        target.uvRect = new Rect(0f, 0f, 1f, 1f);
        return true;
    }

    private void BeginSnapshotSubmission(RectTransform captureRoot)
    {
        captureRoot = ResolveCaptureRoot(captureRoot);
        if (captureRoot == null)
        {
            ClearCommittedSnapshot();
            WarnOnce("No CaptureRoot is assigned for the selected Background layout; the post will open without a composition snapshot.");
            FinishSubmission();
            return;
        }

        submissionInProgress = true;
        HideEditingUiForSnapshot(
            ResolveLayoutChild(
                activeLayout?.SelectionFrame,
                "SelectionFrame"),
            activeLayout?.ItemRoot != null
                ? activeLayout.ItemRoot.gameObject
                : ResolveLayoutChild(null, "ItemRoot"),
            ResolveLayoutChild(
                activeLayout?.BottomNavigation,
                "BottomNavigation"));
        RefreshSubmitButton();
        submissionCoroutine = StartCoroutine(
            CaptureAndFinishSubmission(captureRoot));
    }

    private IEnumerator CaptureAndFinishSubmission(RectTransform captureRoot)
    {
        yield return new WaitForEndOfFrame();

        try
        {
            ClearCommittedSnapshot();
            committedSnapshotSource = CaptureRectTransform(captureRoot);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[SelectionPanelController] Composition snapshot failed: {exception.Message}",
                this);
        }
        finally
        {
            RestoreEditingUiAfterSnapshot();
        }

        if (committedSnapshotSource == null)
        {
            WarnOnce("Failed to capture the selected composition; the post will still open.");
        }

        submissionCoroutine = null;
        FinishSubmission();
    }

    private void FinishSubmission()
    {
        submissionInProgress = false;
        onSubmitted?.Invoke();
        gameObject.SetActive(false);
    }

    private Texture2D CaptureRectTransform(RectTransform captureRoot)
    {
        if (captureRoot == null || !captureRoot.gameObject.activeInHierarchy)
        {
            return null;
        }

        Canvas.ForceUpdateCanvases();
        Canvas canvas = captureRoot.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        Rect screenRect = GetVisibleCaptureScreenRect(captureRoot, camera);

        Texture2D screenTexture = ScreenCapture.CaptureScreenshotAsTexture();
        if (screenTexture == null || Screen.width <= 0 || Screen.height <= 0)
        {
            DestroyTexture(ref screenTexture);
            return null;
        }

        float scaleX = (float)screenTexture.width / Screen.width;
        float scaleY = (float)screenTexture.height / Screen.height;
        int xMin = Mathf.Clamp(
            Mathf.FloorToInt(screenRect.xMin * scaleX),
            0,
            screenTexture.width);
        int yMin = Mathf.Clamp(
            Mathf.FloorToInt(screenRect.yMin * scaleY),
            0,
            screenTexture.height);
        int xMax = Mathf.Clamp(
            Mathf.CeilToInt(screenRect.xMax * scaleX),
            0,
            screenTexture.width);
        int yMax = Mathf.Clamp(
            Mathf.CeilToInt(screenRect.yMax * scaleY),
            0,
            screenTexture.height);

        int width = xMax - xMin;
        int height = yMax - yMin;
        if (width <= 0 || height <= 0)
        {
            DestroyTexture(ref screenTexture);
            return null;
        }

        Texture2D cropped = new Texture2D(
            width,
            height,
            TextureFormat.RGBA32,
            false);
        cropped.name = "SelectionCompositionSnapshot";
        cropped.SetPixels(screenTexture.GetPixels(xMin, yMin, width, height));
        cropped.Apply(false, false);
        DestroyTexture(ref screenTexture);
        return cropped;
    }

    private RectTransform ResolveCaptureRoot(RectTransform configuredRoot)
    {
        if (activeLayout?.LayoutRoot != null)
        {
            Transform backgroundPreview = activeLayout.LayoutRoot.transform.Find(
                "PreviewRoot/BackgroundPreview");
            if (backgroundPreview is RectTransform backgroundRect)
            {
                return backgroundRect;
            }
        }

        return configuredRoot;
    }

    private GameObject ResolveLayoutChild(GameObject configuredObject, string childName)
    {
        if (configuredObject != null)
        {
            return configuredObject;
        }

        Transform child = activeLayout?.LayoutRoot != null
            ? activeLayout.LayoutRoot.transform.Find(childName)
            : null;
        return child != null ? child.gameObject : null;
    }

    private static Rect GetVisibleCaptureScreenRect(
        RectTransform captureRoot,
        Camera camera)
    {
        Rect rootRect = GetScreenRect(captureRoot, camera);
        RectTransform visualBounds = FindLargestVisibleGraphicChild(captureRoot);
        if (visualBounds == null)
        {
            return rootRect;
        }

        Rect visualRect = GetScreenRect(visualBounds, camera);
        float xMin = Mathf.Max(rootRect.xMin, visualRect.xMin);
        float yMin = Mathf.Max(rootRect.yMin, visualRect.yMin);
        float xMax = Mathf.Min(rootRect.xMax, visualRect.xMax);
        float yMax = Mathf.Min(rootRect.yMax, visualRect.yMax);
        return xMax > xMin && yMax > yMin
            ? Rect.MinMaxRect(xMin, yMin, xMax, yMax)
            : rootRect;
    }

    private static RectTransform FindLargestVisibleGraphicChild(
        RectTransform captureRoot)
    {
        RectTransform largest = null;
        float largestArea = 0f;
        for (int childIndex = 0; childIndex < captureRoot.childCount; childIndex++)
        {
            RectTransform child = captureRoot.GetChild(childIndex) as RectTransform;
            Graphic graphic = child != null ? child.GetComponent<Graphic>() : null;
            if (child == null
                || graphic == null
                || !graphic.enabled
                || !child.gameObject.activeInHierarchy)
            {
                continue;
            }

            float area = Mathf.Abs(child.rect.width * child.rect.height)
                * Mathf.Abs(child.lossyScale.x * child.lossyScale.y);
            if (area > largestArea)
            {
                largest = child;
                largestArea = area;
            }
        }

        return largest;
    }

    private static Rect GetScreenRect(RectTransform rectTransform, Camera camera)
    {
        Vector3[] worldCorners = new Vector3[4];
        rectTransform.GetWorldCorners(worldCorners);
        Vector2 first = RectTransformUtility.WorldToScreenPoint(camera, worldCorners[0]);
        float xMin = first.x;
        float xMax = first.x;
        float yMin = first.y;
        float yMax = first.y;
        for (int cornerIndex = 1; cornerIndex < worldCorners.Length; cornerIndex++)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(
                camera,
                worldCorners[cornerIndex]);
            xMin = Mathf.Min(xMin, point.x);
            xMax = Mathf.Max(xMax, point.x);
            yMin = Mathf.Min(yMin, point.y);
            yMax = Mathf.Max(yMax, point.y);
        }

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static Texture2D CreateAspectFilledTexture(
        Texture2D source,
        float targetAspect)
    {
        if (source == null || source.width <= 0 || source.height <= 0)
        {
            return null;
        }

        float sourceAspect = (float)source.width / source.height;
        int cropWidth = source.width;
        int cropHeight = source.height;
        if (sourceAspect > targetAspect)
        {
            cropWidth = Mathf.Clamp(
                Mathf.RoundToInt(source.height * targetAspect),
                1,
                source.width);
        }
        else if (sourceAspect < targetAspect)
        {
            cropHeight = Mathf.Clamp(
                Mathf.RoundToInt(source.width / targetAspect),
                1,
                source.height);
        }

        Color32[] sourcePixels = source.GetPixels32();
        Color32[] outputPixels = new Color32[cropWidth * cropHeight];
        int sourceOffsetX = (source.width - cropWidth) / 2;
        int sourceOffsetY = (source.height - cropHeight) / 2;
        for (int outputY = 0; outputY < cropHeight; outputY++)
        {
            int sourceIndex = (outputY + sourceOffsetY) * source.width
                + sourceOffsetX;
            int outputIndex = outputY * cropWidth;
            Array.Copy(
                sourcePixels,
                sourceIndex,
                outputPixels,
                outputIndex,
                cropWidth);
        }

        Texture2D output = new Texture2D(
            cropWidth,
            cropHeight,
            TextureFormat.RGBA32,
            false);
        output.name = "SelectionCompositionSnapshotFilled";
        output.SetPixels32(outputPixels);
        output.Apply(false, false);
        return output;
    }

    private void CancelPendingSubmission()
    {
        if (submissionCoroutine != null)
        {
            StopCoroutine(submissionCoroutine);
            submissionCoroutine = null;
        }

        submissionInProgress = false;
        RestoreEditingUiAfterSnapshot();
    }

    private void HideEditingUiForSnapshot(
        GameObject selectionFrame,
        GameObject itemRoot,
        GameObject bottomNavigation)
    {
        RestoreEditingUiAfterSnapshot();
        if (selectionFrame == null)
        {
            WarnOnce("SelectionFrame is not assigned for the selected Background layout; it may appear in the composition snapshot.");
        }
        else
        {
            pendingSnapshotSelectionFrame = selectionFrame;
            pendingSnapshotSelectionFrameWasActive = selectionFrame.activeSelf;
            selectionFrame.SetActive(false);
        }

        if (itemRoot == null)
        {
            WarnOnce("ItemRoot is not assigned for the selected Background layout; SelectionItem buttons may appear in the composition snapshot.");
        }
        else
        {
            pendingSnapshotItemRoot = itemRoot;
            pendingSnapshotItemRootWasActive = itemRoot.activeSelf;
            itemRoot.SetActive(false);
        }

        if (bottomNavigation == null)
        {
            WarnOnce("BottomNavigation is not assigned for the selected Background layout; navigation buttons may appear in the composition snapshot.");
        }
        else
        {
            pendingSnapshotBottomNavigation = bottomNavigation;
            pendingSnapshotBottomNavigationWasActive = bottomNavigation.activeSelf;
            bottomNavigation.SetActive(false);
        }
    }

    private void RestoreEditingUiAfterSnapshot()
    {
        if (pendingSnapshotSelectionFrame != null)
        {
            pendingSnapshotSelectionFrame.SetActive(
                pendingSnapshotSelectionFrameWasActive);
        }

        pendingSnapshotSelectionFrame = null;
        pendingSnapshotSelectionFrameWasActive = false;

        if (pendingSnapshotItemRoot != null)
        {
            pendingSnapshotItemRoot.SetActive(
                pendingSnapshotItemRootWasActive);
        }

        pendingSnapshotItemRoot = null;
        pendingSnapshotItemRootWasActive = false;

        if (pendingSnapshotBottomNavigation != null)
        {
            pendingSnapshotBottomNavigation.SetActive(
                pendingSnapshotBottomNavigationWasActive);
        }

        pendingSnapshotBottomNavigation = null;
        pendingSnapshotBottomNavigationWasActive = false;
    }

    private void ClearCommittedSnapshot()
    {
        RestoreCommittedSnapshotTarget();
        DestroyTexture(ref committedSnapshotDisplay);
        DestroyTexture(ref committedSnapshotSource);
        committedSnapshotDisplayAspect = 0f;
    }

    private void RestoreCommittedSnapshotTarget()
    {
        if (committedSnapshotTarget != null)
        {
            committedSnapshotTarget.texture = committedSnapshotTargetOriginalTexture;
            committedSnapshotTarget.color = committedSnapshotTargetOriginalColor;
            committedSnapshotTarget.uvRect = committedSnapshotTargetOriginalUvRect;
        }

        committedSnapshotTarget = null;
        committedSnapshotTargetOriginalTexture = null;
        committedSnapshotTargetOriginalColor = default;
        committedSnapshotTargetOriginalUvRect = default;
    }

    private static void DestroyTexture(ref Texture2D texture)
    {
        if (texture == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(texture);
        }
        else
        {
            DestroyImmediate(texture);
        }

        texture = null;
    }
}
