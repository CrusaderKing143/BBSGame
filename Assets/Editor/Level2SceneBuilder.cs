using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public static class Level2SceneBuilder
{
    private const string Level2Root = "Assets/Art/BBS/level2";

    private sealed class CollectibleSpec
    {
        public string Name;
        public string SpritePath;
        public SelectionCategoryType CategoryType;
        public string ItemId;
        public Vector2 SourcePosition;
    }

    private sealed class GeneratedPost
    {
        public GameObject Root;
        public readonly List<Button> CollectibleButtons = new List<Button>();
        public RawImage RecordImage;
    }

    [MenuItem("Tools/BBS Game/Build Level 2 Content")]
    public static void BuildFromMenu()
    {
        BuildLevel2Scene();
    }

    public static void BuildFromCommandLine()
    {
        EditorSceneManager.OpenScene(
            "Assets/Scenes/SampleScene.unity",
            OpenSceneMode.Single);
        BuildLevel2Scene();
    }

    private static void BuildLevel2Scene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()
            || !string.Equals(
                scene.path,
                "Assets/Scenes/SampleScene.unity",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Open Assets/Scenes/SampleScene.unity before building Level 2.");
        }

        StoryFlowController storyFlow = FindSceneComponent<StoryFlowController>();
        SelectionPanelController selection = FindSceneComponent<SelectionPanelController>();
        ForumPanelController forum = FindSceneComponent<ForumPanelController>();
        if (storyFlow == null || selection == null || forum == null)
        {
            throw new InvalidOperationException("Story, Selection, or Forum controller is missing from SampleScene.");
        }

        GameObject postListPanel = FindSceneObject("ForumPostListPanel");
        GameObject postContentPanel = FindSceneObject("ForumPostContentPanel");
        GameObject bbsChoose = FindSceneObject("BBSChoose");
        GameObject postButtonTemplate = FindSceneObject("PostButton_01");
        GameObject postContentTemplate = FindSceneObject("PostContent_00");
        GameObject feijiTemplate = FindDirectChild(bbsChoose.transform, "feiji")?.gameObject;
        GameObject jiuba = FindDirectChild(bbsChoose.transform, "jiuba")?.gameObject;
        if (postListPanel == null
            || postContentPanel == null
            || bbsChoose == null
            || postButtonTemplate == null
            || postContentTemplate == null
            || feijiTemplate == null
            || jiuba == null)
        {
            throw new InvalidOperationException("A Level 1 UI template required by the Level 2 builder was not found.");
        }

        for (int index = 1; index <= 5; index++)
        {
            DeleteGeneratedSceneObject(
                postListPanel.transform,
                $"PostButton_D2_{index:00}");
            DeleteGeneratedSceneObject(
                postContentPanel.transform,
                $"PostContent_D2_{index:00}");
        }
        DeleteGeneratedSceneObject(
            postListPanel.transform,
            "PostButtonGroup_D2_04_05");
        DeleteGeneratedSceneObject(postContentPanel.transform, "PostContent_D2_JiubaResult");
        DeleteGeneratedSceneObject(postContentPanel.transform, "PostContent_D2_OfficeResult");
        DeleteGeneratedSceneObject(bbsChoose.transform, "office");

        Button firstPostButton = CreatePostButton(
            postButtonTemplate,
            postListPanel.transform,
            "PostButton_D2_01",
            Level2Root + "/帖子/热门帖子/1/热门帖子1.png",
            Level2Root + "/帖子/热门帖子/1/标题.png");
        Button secondPostButton = CreatePostButton(
            postButtonTemplate,
            postListPanel.transform,
            "PostButton_D2_02",
            Level2Root + "/帖子/热门帖子/2/热门帖子2.png",
            Level2Root + "/帖子/热门帖子/2/标题.png");
        Button thirdPostButton = CreatePostButton(
            postButtonTemplate,
            postListPanel.transform,
            "PostButton_D2_03",
            Level2Root + "/帖子/热门帖子/3/热门帖子3.png",
            Level2Root + "/帖子/热门帖子/3/标题.png");
        GameObject sharedPostRoot;
        Button[] sharedPostButtons = CreateSharedPostButtons(
            postButtonTemplate,
            postListPanel.transform,
            out sharedPostRoot);
        Button[] postButtons =
        {
            firstPostButton,
            secondPostButton,
            thirdPostButton,
            sharedPostButtons[0],
            sharedPostButtons[1]
        };

        GeneratedPost leakPost = CreatePostContent(
            postContentTemplate,
            postContentPanel.transform,
            "PostContent_D2_01",
            Level2Root + "/帖子/爆料帖.png",
            Array.Empty<CollectibleSpec>(),
            false);

        GeneratedPost evidenceOnePost = CreatePostContent(
            postContentTemplate,
            postContentPanel.transform,
            "PostContent_D2_02",
            Level2Root + "/帖子/实锤1/实锤帖1.png",
            new[]
            {
                Collectible(
                    "OldInterview",
                    Level2Root + "/帖子/实锤1/1.png",
                    SelectionCategoryType.Props,
                    "props-day2-phone-evidence",
                    0.255f,
                    0.113f),
                Collectible(
                    "Ren",
                    Level2Root + "/帖子/实锤1/2.png",
                    SelectionCategoryType.Character,
                    "character-agent",
                    0.468f,
                    0.113f),
                Collectible(
                    "TheVibe",
                    Level2Root + "/帖子/实锤1/3.png",
                    SelectionCategoryType.Character,
                    "character-office-bartender",
                    0.300f,
                    0.337f),
                Collectible(
                    "WatchBox",
                    Level2Root + "/帖子/实锤1/4.png",
                    SelectionCategoryType.Props,
                    "props-day2-watch",
                    0.310f,
                    0.555f)
            },
            false);

        GeneratedPost evidenceTwoPost = CreatePostContent(
            postContentTemplate,
            postContentPanel.transform,
            "PostContent_D2_03",
            Level2Root + "/帖子/实锤2/实锤帖2.png",
            new[]
            {
                Collectible(
                    "SurveillanceDrive",
                    Level2Root + "/帖子/实锤2/1.png",
                    SelectionCategoryType.Props,
                    "props-day2-surveillance-drive",
                    0.410f,
                    0.178f),
                Collectible(
                    "ShiftSchedule",
                    Level2Root + "/帖子/实锤2/2.png",
                    SelectionCategoryType.Props,
                    "props-day2-schedule",
                    0.330f,
                    0.390f)
            },
            false);

        GeneratedPost clarificationOnePost = CreatePostContent(
            postContentTemplate,
            postContentPanel.transform,
            "PostContent_D2_04",
            Level2Root + "/帖子/澄清1/澄清1.png",
            new[]
            {
                Collectible(
                    "ResignationLetter",
                    Level2Root + "/帖子/澄清1/1.png",
                    SelectionCategoryType.Props,
                    "props-day2-resignation-letter",
                    0.350f,
                    0.132f),
                Collectible(
                    "OfficialSchedule",
                    Level2Root + "/帖子/澄清1/2.png",
                    SelectionCategoryType.Props,
                    "props-day2-official-schedule",
                    0.350f,
                    0.365f)
            },
            false);

        GeneratedPost clarificationTwoPost = CreatePostContent(
            postContentTemplate,
            postContentPanel.transform,
            "PostContent_D2_05",
            Level2Root + "/帖子/澄清2/澄清2.png",
            new[]
            {
                Collectible(
                    "NewsReport",
                    Level2Root + "/帖子/澄清2/1.png",
                    SelectionCategoryType.Props,
                    "props-day2-phone-clarification",
                    0.335f,
                    0.105f),
                Collectible(
                    "LimitedToy",
                    Level2Root + "/帖子/澄清2/2.png",
                    SelectionCategoryType.Props,
                    "props-day2-doll",
                    0.430f,
                    0.365f),
                Collectible(
                    "CarKeys",
                    Level2Root + "/帖子/澄清2/3.png",
                    SelectionCategoryType.Props,
                    "props-day2-car-keys",
                    0.655f,
                    0.365f)
            },
            false);

        GeneratedPost jiubaResult = CreatePostContent(
            postContentTemplate,
            postContentPanel.transform,
            "PostContent_D2_JiubaResult",
            Level2Root + "/帖子/结局帖2（实锤.png",
            Array.Empty<CollectibleSpec>(),
            true,
            new Rect(0.057f, 0.532f, 0.881f, 0.411f));

        GeneratedPost officeResult = CreatePostContent(
            postContentTemplate,
            postContentPanel.transform,
            "PostContent_D2_OfficeResult",
            Level2Root + "/帖子/结局帖1（澄清.png",
            Array.Empty<CollectibleSpec>(),
            true,
            new Rect(0.057f, 0.542f, 0.881f, 0.410f));

        RuntimeAnimatorController televisionController = CreateSpriteController(
            Level2Root + "/P图界面/工作室/背景/OfficeTelevision.controller",
            Level2Root + "/P图界面/工作室/背景/OfficeTelevision.anim",
            Level2Root + "/P图界面/工作室/背景/电视",
            3,
            10f);
        RuntimeAnimatorController smokeController = CreateSpriteController(
            Level2Root + "/P图界面/工作室/背景/OfficeSmoke.controller",
            Level2Root + "/P图界面/工作室/背景/OfficeSmoke.anim",
            Level2Root + "/P图界面/工作室/背景/烟雾",
            2,
            15f);

        GameObject office = CreateOfficeLayout(
            feijiTemplate,
            bbsChoose.transform,
            televisionController,
            smokeController);

        ConfigureSelectionItems(selection);
        ConfigureRoundTwoBackgrounds(selection, jiuba, office);
        ConfigureStoryRoundTwo(
            storyFlow,
            postButtons,
            sharedPostRoot,
            new[]
            {
                leakPost,
                evidenceOnePost,
                evidenceTwoPost,
                clarificationOnePost,
                clarificationTwoPost
            },
            jiubaResult,
            officeResult);

        EditorUtility.SetDirty(selection);
        EditorUtility.SetDirty(storyFlow);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Level2SceneBuilder] Level 2 five-post flow, shared clarification list, and Selection result posts were built successfully.");
    }

    private static Button CreatePostButton(
        GameObject template,
        Transform parent,
        string name,
        string postSpritePath,
        string titleSpritePath)
    {
        GameObject root = UnityEngine.Object.Instantiate(template, parent, false);
        root.name = name;
        root.SetActive(false);
        Undo.RegisterCreatedObjectUndo(root, "Build Level 2 Post Button");

        Button button = root.GetComponent<Button>();
        button.onClick.RemoveAllListeners();

        Image rootImage = root.GetComponent<Image>();
        rootImage.sprite = LoadSprite(postSpritePath);
        rootImage.preserveAspect = false;
        button.targetGraphic = rootImage;

        Image titleImage = root.transform.childCount > 0
            ? root.transform.GetChild(0).GetComponent<Image>()
            : null;
        if (titleImage != null)
        {
            titleImage.sprite = LoadSprite(titleSpritePath);
            titleImage.preserveAspect = true;
            titleImage.SetNativeSize();
            titleImage.raycastTarget = false;
        }

        return button;
    }

    private static Button[] CreateSharedPostButtons(
        GameObject template,
        Transform parent,
        out GameObject sharedRoot)
    {
        sharedRoot = UnityEngine.Object.Instantiate(template, parent, false);
        sharedRoot.name = "PostButtonGroup_D2_04_05";
        sharedRoot.SetActive(false);
        Undo.RegisterCreatedObjectUndo(sharedRoot, "Build Level 2 Shared Post Buttons");

        Button rootButton = sharedRoot.GetComponent<Button>();
        if (rootButton != null)
        {
            UnityEngine.Object.DestroyImmediate(rootButton);
        }

        Image rootImage = sharedRoot.GetComponent<Image>();
        rootImage.sprite = LoadSprite(Level2Root + "/帖子/热门帖子/4/热门帖子4.png");
        rootImage.preserveAspect = false;
        rootImage.raycastTarget = false;

        if (sharedRoot.transform.childCount == 0)
        {
            throw new InvalidOperationException(
                "The forum post button template has no title Image child.");
        }

        GameObject upperTitle = sharedRoot.transform.GetChild(0).gameObject;
        GameObject lowerTitle = UnityEngine.Object.Instantiate(
            upperTitle,
            sharedRoot.transform,
            false);
        Button upperButton = ConfigureSharedPostButton(
            upperTitle,
            "PostButton_D2_04",
            Level2Root + "/帖子/热门帖子/4/标题上.png",
            175f);
        Button lowerButton = ConfigureSharedPostButton(
            lowerTitle,
            "PostButton_D2_05",
            Level2Root + "/帖子/热门帖子/4/标题下.png",
            125f);
        return new[] { upperButton, lowerButton };
    }

    private static Button ConfigureSharedPostButton(
        GameObject root,
        string name,
        string titleSpritePath,
        float titleY)
    {
        root.name = name;
        root.SetActive(false);

        Image image = root.GetComponent<Image>();
        if (image == null)
        {
            throw new InvalidOperationException($"{name} has no title Image.");
        }

        image.sprite = LoadSprite(titleSpritePath);
        image.preserveAspect = true;
        image.SetNativeSize();
        image.raycastTarget = true;
        RectTransform rect = image.rectTransform;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, titleY);

        Button button = root.GetComponent<Button>();
        if (button == null)
        {
            button = root.AddComponent<Button>();
        }
        button.onClick.RemoveAllListeners();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.disabledColor = Color.white;
        button.colors = colors;
        return button;
    }

    private static GeneratedPost CreatePostContent(
        GameObject template,
        Transform parent,
        string name,
        string postSpritePath,
        IReadOnlyList<CollectibleSpec> collectibles,
        bool createRecordImage,
        Rect recordImageRect = default(Rect))
    {
        GameObject root = UnityEngine.Object.Instantiate(template, parent, false);
        root.name = name;
        root.SetActive(false);
        Undo.RegisterCreatedObjectUndo(root, "Build Level 2 Post Content");

        ScrollRect scrollRect = root.GetComponent<ScrollRect>();
        if (scrollRect == null || scrollRect.content == null)
        {
            throw new InvalidOperationException($"{name} has no valid ScrollRect template.");
        }

        Transform imageTransform = FindDirectChild(scrollRect.content, "Image");
        Image postImage = imageTransform != null ? imageTransform.GetComponent<Image>() : null;
        if (postImage == null)
        {
            throw new InvalidOperationException($"{name} has no content Image.");
        }

        for (int childIndex = postImage.transform.childCount - 1; childIndex >= 0; childIndex--)
        {
            UnityEngine.Object.DestroyImmediate(postImage.transform.GetChild(childIndex).gameObject);
        }

        Sprite postSprite = LoadSprite(postSpritePath);
        postImage.sprite = postSprite;
        postImage.preserveAspect = false;
        postImage.raycastTarget = false;

        RectTransform imageRect = postImage.rectTransform;
        float width = 947f;
        float height = width * postSprite.rect.height / postSprite.rect.width;
        imageRect.anchorMin = new Vector2(0.5f, 1f);
        imageRect.anchorMax = new Vector2(0.5f, 1f);
        imageRect.pivot = new Vector2(0.5f, 1f);
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.sizeDelta = new Vector2(width, height);

        RectTransform contentRect = scrollRect.content;
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, height);
        scrollRect.verticalNormalizedPosition = 1f;

        GeneratedPost result = new GeneratedPost { Root = root };
        if (createRecordImage)
        {
            result.RecordImage = CreateRecordImage(
                postImage.transform,
                width,
                height,
                recordImageRect);
        }

        foreach (CollectibleSpec collectible in collectibles)
        {
            Button button = CreateCollectibleButton(postImage.transform, collectible, width, height);
            result.CollectibleButtons.Add(button);
        }

        return result;
    }

    private static RawImage CreateRecordImage(
        Transform parent,
        float width,
        float height,
        Rect normalizedRect)
    {
        GameObject root = new GameObject(
            "RecordImage",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));
        root.transform.SetParent(parent, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(
            (normalizedRect.x + normalizedRect.width * 0.5f - 0.5f) * width,
            -(normalizedRect.y + normalizedRect.height * 0.5f) * height);
        rect.sizeDelta = new Vector2(
            normalizedRect.width * width,
            normalizedRect.height * height);

        RawImage image = root.GetComponent<RawImage>();
        image.color = Color.black;
        image.raycastTarget = false;
        return image;
    }

    private static Button CreateCollectibleButton(
        Transform parent,
        CollectibleSpec collectible,
        float width,
        float height)
    {
        GameObject root = new GameObject(
            "Collect_" + collectible.Name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        root.transform.SetParent(parent, false);

        Sprite sprite = LoadSprite(collectible.SpritePath);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(
            (collectible.SourcePosition.x - 0.5f) * width,
            -collectible.SourcePosition.y * height);
        float sourceScale = width / 1700f;
        rect.sizeDelta = sprite.rect.size * sourceScale;

        Image image = root.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = true;

        Button button = root.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.RemoveAllListeners();
        return button;
    }

    private static GameObject CreateOfficeLayout(
        GameObject template,
        Transform parent,
        RuntimeAnimatorController televisionController,
        RuntimeAnimatorController smokeController)
    {
        GameObject office = UnityEngine.Object.Instantiate(template, parent, false);
        office.name = "office";
        office.SetActive(false);
        Undo.RegisterCreatedObjectUndo(office, "Build Office Selection Layout");

        Transform selectionFrameTransform = FindDirectChild(office.transform, "SelectionFrame");
        Image selectionFrame = selectionFrameTransform.GetComponent<Image>();
        selectionFrame.sprite = LoadSprite(Level2Root + "/P图界面/工作室/UI/背景.png");

        Transform navigation = FindDirectChild(office.transform, "BottomNavigation");
        ConfigureButton(
            navigation,
            "BackButton",
            Level2Root + "/P图界面/工作室/UI/BACK小标题3.png",
            null);
        ConfigureButton(
            navigation,
            "PostButton",
            Level2Root + "/P图界面/工作室/UI/POST小标题3.png",
            null);
        ConfigureButton(
            navigation,
            "CharacterButton",
            Level2Root + "/P图界面/工作室/UI/CHAR小标题3.png",
            Level2Root + "/P图界面/工作室/UI/CHAR大标题3.png");
        ConfigureButton(
            navigation,
            "BackgroundButton",
            Level2Root + "/P图界面/工作室/UI/BACKGROUND小标题3.png",
            Level2Root + "/P图界面/工作室/UI/BACKGROUND大标题3.png");
        ConfigureButton(
            navigation,
            "PropsButton",
            Level2Root + "/P图界面/工作室/UI/PROPS小标题3.png",
            Level2Root + "/P图界面/工作室/UI/PROPS大标题3.png");

        Transform previewRoot = FindDirectChild(office.transform, "PreviewRoot");
        Transform background = FindDirectChild(previewRoot, "BackgroundPreview");
        for (int childIndex = background.childCount - 1; childIndex >= 0; childIndex--)
        {
            UnityEngine.Object.DestroyImmediate(background.GetChild(childIndex).gameObject);
        }

        Image backgroundImage = background.GetComponent<Image>();
        if (backgroundImage != null)
        {
            backgroundImage.sprite = null;
            backgroundImage.enabled = false;
        }

        Image backdrop = CreateImageChild(background, "OfficeBackdrop");
        Stretch(backdrop.rectTransform);
        backdrop.color = new Color(0.035f, 0.04f, 0.08f, 1f);
        backdrop.raycastTarget = false;

        Image television = CreateImageChild(background, "Television");
        television.sprite = LoadSprite(
            Level2Root + "/P图界面/工作室/背景/电视/工作室动画 拷贝_00000.png");
        television.preserveAspect = true;
        television.raycastTarget = false;
        television.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        television.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        television.rectTransform.anchoredPosition = new Vector2(220f, 20f);
        television.rectTransform.sizeDelta = new Vector2(894f, 776f);
        Animator televisionAnimator = television.gameObject.AddComponent<Animator>();
        televisionAnimator.runtimeAnimatorController = televisionController;

        Image smoke = CreateImageChild(background, "Smoke");
        smoke.sprite = LoadSprite(
            Level2Root + "/P图界面/工作室/背景/烟雾/烟雾_00000.png");
        smoke.preserveAspect = true;
        smoke.raycastTarget = false;
        smoke.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        smoke.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        smoke.rectTransform.anchoredPosition = new Vector2(610f, -10f);
        smoke.rectTransform.sizeDelta = new Vector2(381f, 880f);
        Animator smokeAnimator = smoke.gameObject.AddComponent<Animator>();
        smokeAnimator.runtimeAnimatorController = smokeController;

        return office;
    }

    private static void ConfigureButton(
        Transform navigation,
        string name,
        string normalSpritePath,
        string selectedSpritePath)
    {
        Transform buttonTransform = FindDirectChild(navigation, name);
        Button button = buttonTransform.GetComponent<Button>();
        Image image = buttonTransform.GetComponent<Image>();
        image.sprite = LoadSprite(normalSpritePath);
        image.preserveAspect = true;
        button.targetGraphic = image;

        SpriteState state = button.spriteState;
        state.disabledSprite = string.IsNullOrEmpty(selectedSpritePath)
            ? null
            : LoadSprite(selectedSpritePath);
        button.spriteState = state;
    }

    private static RuntimeAnimatorController CreateSpriteController(
        string controllerPath,
        string clipPath,
        string spriteFolder,
        int frameStep,
        float frameRate)
    {
        string[] paths = AssetDatabase.FindAssets("t:Sprite", new[] { spriteFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        if (paths.Length == 0)
        {
            throw new InvalidOperationException($"No animation sprites were found in {spriteFolder}.");
        }

        List<Sprite> sprites = new List<Sprite>();
        for (int index = 0; index < paths.Length; index += Mathf.Max(1, frameStep))
        {
            sprites.Add(LoadSprite(paths[index]));
        }

        AssetDatabase.DeleteAsset(controllerPath);
        AssetDatabase.DeleteAsset(clipPath);

        AnimationClip clip = new AnimationClip
        {
            name = Path.GetFileNameWithoutExtension(clipPath),
            frameRate = frameRate
        };
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
        for (int index = 0; index < sprites.Count; index++)
        {
            keyframes[index] = new ObjectReferenceKeyframe
            {
                time = index / frameRate,
                value = sprites[index]
            };
        }

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(
            string.Empty,
            typeof(Image),
            "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        SerializedObject clipObject = new SerializedObject(clip);
        SerializedProperty loopTime = clipObject.FindProperty("m_AnimationClipSettings.m_LoopTime");
        if (loopTime != null)
        {
            loopTime.boolValue = true;
            clipObject.ApplyModifiedPropertiesWithoutUndo();
        }

        AssetDatabase.CreateAsset(clip, clipPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddMotion(clip);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void ConfigureSelectionItems(SelectionPanelController selection)
    {
        SerializedObject serializedSelection = new SerializedObject(selection);
        SerializedProperty categories = serializedSelection.FindProperty("categories");

        AddSelectionItem(
            categories,
            SelectionCategoryType.Character,
            "character-office-bartender",
            Level2Root + "/char/酒保.png");
        AddSelectionItem(
            categories,
            SelectionCategoryType.Props,
            "props-day2-watch",
            Level2Root + "/道具/表.png");
        AddSelectionItem(
            categories,
            SelectionCategoryType.Props,
            "props-day2-car-keys",
            Level2Root + "/道具/车钥匙.png");
        AddSelectionItem(
            categories,
            SelectionCategoryType.Props,
            "props-day2-resignation-letter",
            Level2Root + "/道具/辞职信.png");
        AddSelectionItem(
            categories,
            SelectionCategoryType.Props,
            "props-day2-surveillance-drive",
            Level2Root + "/道具/监控硬盘.png");
        AddSelectionItem(
            categories,
            SelectionCategoryType.Props,
            "props-day2-official-schedule",
            Level2Root + "/道具/日程表.png");
        AddSelectionItem(
            categories,
            SelectionCategoryType.Props,
            "props-day2-doll",
            Level2Root + "/道具/手办.png");
        AddSelectionItem(
            categories,
            SelectionCategoryType.Props,
            "props-day2-phone-clarification",
            Level2Root + "/道具/手机澄清.png");
        AddSelectionItem(
            categories,
            SelectionCategoryType.Props,
            "props-day2-phone-evidence",
            Level2Root + "/道具/手机实锤.png");
        AddSelectionItem(
            categories,
            SelectionCategoryType.Props,
            "props-day2-schedule",
            Level2Root + "/道具/日程表.png");

        serializedSelection.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddSelectionItem(
        SerializedProperty categories,
        SelectionCategoryType categoryType,
        string itemId,
        string spritePath)
    {
        SerializedProperty category = FindCategory(categories, categoryType);
        if (category == null)
        {
            throw new InvalidOperationException($"Selection category {categoryType} was not found.");
        }

        SerializedProperty items = category.FindPropertyRelative("items");
        SerializedProperty item = FindItem(items, itemId);
        if (item == null)
        {
            int index = items.arraySize;
            items.InsertArrayElementAtIndex(index);
            item = items.GetArrayElementAtIndex(index);
        }

        Sprite sprite = LoadSprite(spritePath);
        item.FindPropertyRelative("itemId").stringValue = itemId;
        item.FindPropertyRelative("iconSprite").objectReferenceValue = sprite;
        item.FindPropertyRelative("previewSprite").objectReferenceValue = sprite;
        item.FindPropertyRelative("unlockedByDefault").boolValue = false;
        item.FindPropertyRelative("initialDisplayScale").floatValue = 1f;
    }

    private static void ConfigureRoundTwoBackgrounds(
        SelectionPanelController selection,
        GameObject jiuba,
        GameObject office)
    {
        SerializedObject serializedSelection = new SerializedObject(selection);
        SerializedProperty roundBackgrounds = serializedSelection.FindProperty("roundBackgrounds");
        SerializedProperty roundTwo = null;
        for (int index = 0; index < roundBackgrounds.arraySize; index++)
        {
            SerializedProperty candidate = roundBackgrounds.GetArrayElementAtIndex(index);
            if (candidate.FindPropertyRelative("roundIndex").intValue == 1)
            {
                roundTwo = candidate;
                break;
            }
        }

        if (roundTwo == null)
        {
            int index = roundBackgrounds.arraySize;
            roundBackgrounds.InsertArrayElementAtIndex(index);
            roundTwo = roundBackgrounds.GetArrayElementAtIndex(index);
        }

        roundTwo.FindPropertyRelative("roundIndex").intValue = 1;
        SerializedProperty items = roundTwo.FindPropertyRelative("items");
        items.arraySize = 2;

        SerializedProperty categories = serializedSelection.FindProperty("categories");
        SerializedProperty legacyBackground = FindCategory(
            categories,
            SelectionCategoryType.Background);
        SerializedProperty legacyJiuba = legacyBackground
            .FindPropertyRelative("items")
            .GetArrayElementAtIndex(0);
        CopySelectionItem(legacyJiuba, items.GetArrayElementAtIndex(0));

        SerializedProperty officeItem = items.GetArrayElementAtIndex(1);
        Sprite officeIcon = LoadSprite(
            Level2Root + "/P图界面/工作室/背景/电视/工作室动画 拷贝_00000.png");
        officeItem.FindPropertyRelative("itemId").stringValue = "office";
        officeItem.FindPropertyRelative("iconSprite").objectReferenceValue = officeIcon;
        officeItem.FindPropertyRelative("previewSprite").objectReferenceValue = officeIcon;
        officeItem.FindPropertyRelative("unlockedByDefault").boolValue = false;
        officeItem.FindPropertyRelative("initialDisplayScale").floatValue = 1f;

        SerializedProperty layouts = roundTwo.FindPropertyRelative("layouts");
        layouts.arraySize = 2;
        SerializedProperty legacyLayouts = serializedSelection.FindProperty("backgroundLayouts");
        CopyLayout(legacyLayouts.GetArrayElementAtIndex(0), layouts.GetArrayElementAtIndex(0));
        AssignLayout(layouts.GetArrayElementAtIndex(1), office);
        roundTwo.FindPropertyRelative("requiredCategoriesOverride").arraySize = 0;
        roundTwo.FindPropertyRelative("allowedCharacterItemIds").arraySize = 0;
        roundTwo.FindPropertyRelative("singleCharacterPlacement").boolValue = false;

        serializedSelection.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureStoryRoundTwo(
        StoryFlowController storyFlow,
        IReadOnlyList<Button> postButtons,
        GameObject sharedPostRoot,
        IReadOnlyList<GeneratedPost> posts,
        GeneratedPost jiubaResult,
        GeneratedPost officeResult)
    {
        SerializedObject serializedStory = new SerializedObject(storyFlow);
        SerializedProperty rounds = serializedStory.FindProperty("rounds");
        if (rounds.arraySize < 2)
        {
            throw new InvalidOperationException("StoryFlowController has no second round.");
        }

        SerializedProperty roundTwo = rounds.GetArrayElementAtIndex(1);
        SerializedProperty postsProperty = roundTwo.FindPropertyRelative("posts");
        postsProperty.arraySize = posts.Count;
        for (int postIndex = 0; postIndex < posts.Count; postIndex++)
        {
            SerializedProperty post = postsProperty.GetArrayElementAtIndex(postIndex);
            post.FindPropertyRelative("button").objectReferenceValue = postButtons[postIndex];
            post.FindPropertyRelative("listDisplayRoot").objectReferenceValue =
                postIndex == 3 || postIndex == 4 ? sharedPostRoot : null;
            post.FindPropertyRelative("contentImage").objectReferenceValue = posts[postIndex].Root;
            WriteCollectibles(post.FindPropertyRelative("collectibles"), posts[postIndex]);
            post.FindPropertyRelative("unlockWithPrevious").boolValue = postIndex == 4;
        }

        SerializedProperty selectionPost = roundTwo.FindPropertyRelative("selectionPost");
        selectionPost.FindPropertyRelative("categoryType").enumValueIndex =
            (int)SelectionCategoryType.Background;
        selectionPost.FindPropertyRelative("button").objectReferenceValue = null;
        selectionPost.FindPropertyRelative("recordImage").objectReferenceValue = null;

        SerializedProperty branches = selectionPost.FindPropertyRelative("branches");
        branches.arraySize = 2;
        WriteBranch(
            branches.GetArrayElementAtIndex(0),
            "jiuBa",
            jiubaResult,
            SelectionBranchCompletionMode.OpenPost,
            null);
        WriteBranch(
            branches.GetArrayElementAtIndex(1),
            "office",
            officeResult,
            SelectionBranchCompletionMode.OpenPostThenEnding,
            LoadVideoClip("Assets/Resources/1.mp4"));

        serializedStory.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WriteBranch(
        SerializedProperty branch,
        string itemId,
        GeneratedPost post,
        SelectionBranchCompletionMode completionMode,
        VideoClip endingVideoClip)
    {
        branch.FindPropertyRelative("itemId").stringValue = itemId;
        branch.FindPropertyRelative("contentImage").objectReferenceValue = post.Root;
        branch.FindPropertyRelative("recordImage").objectReferenceValue = post.RecordImage;
        WriteCollectibles(branch.FindPropertyRelative("collectibles"), post);
        branch.FindPropertyRelative("completionMode").enumValueIndex = (int)completionMode;
        branch.FindPropertyRelative("endingVideoClip").objectReferenceValue = endingVideoClip;
    }

    private static void WriteCollectibles(
        SerializedProperty collectibles,
        GeneratedPost post)
    {
        Transform image = post.Root.GetComponent<ScrollRect>().content.Find("Image");
        List<CollectibleSpec> specs = GetCollectibleSpecsFor(post.Root.name);
        collectibles.arraySize = Mathf.Min(post.CollectibleButtons.Count, specs.Count);
        for (int index = 0; index < collectibles.arraySize; index++)
        {
            SerializedProperty collectible = collectibles.GetArrayElementAtIndex(index);
            collectible.FindPropertyRelative("button").objectReferenceValue =
                post.CollectibleButtons[index];
            collectible.FindPropertyRelative("categoryType").enumValueIndex =
                (int)specs[index].CategoryType;
            collectible.FindPropertyRelative("itemId").stringValue = specs[index].ItemId;
        }
    }

    private static List<CollectibleSpec> GetCollectibleSpecsFor(string postName)
    {
        switch (postName)
        {
            case "PostContent_D2_02":
                return new List<CollectibleSpec>
                {
                    Spec(SelectionCategoryType.Props, "props-day2-phone-evidence"),
                    Spec(SelectionCategoryType.Character, "character-agent"),
                    Spec(SelectionCategoryType.Character, "character-office-bartender"),
                    Spec(SelectionCategoryType.Props, "props-day2-watch")
                };
            case "PostContent_D2_03":
                return new List<CollectibleSpec>
                {
                    Spec(SelectionCategoryType.Props, "props-day2-surveillance-drive"),
                    Spec(SelectionCategoryType.Props, "props-day2-schedule")
                };
            case "PostContent_D2_05":
                return new List<CollectibleSpec>
                {
                    Spec(SelectionCategoryType.Props, "props-day2-phone-clarification"),
                    Spec(SelectionCategoryType.Props, "props-day2-doll"),
                    Spec(SelectionCategoryType.Props, "props-day2-car-keys")
                };
            case "PostContent_D2_04":
                return new List<CollectibleSpec>
                {
                    Spec(SelectionCategoryType.Props, "props-day2-resignation-letter"),
                    Spec(SelectionCategoryType.Props, "props-day2-official-schedule")
                };
            default:
                return new List<CollectibleSpec>();
        }
    }

    private static CollectibleSpec Spec(SelectionCategoryType categoryType, string itemId)
    {
        return new CollectibleSpec { CategoryType = categoryType, ItemId = itemId };
    }

    private static CollectibleSpec Collectible(
        string name,
        string spritePath,
        SelectionCategoryType categoryType,
        string itemId,
        float sourceX,
        float sourceY)
    {
        return new CollectibleSpec
        {
            Name = name,
            SpritePath = spritePath,
            CategoryType = categoryType,
            ItemId = itemId,
            SourcePosition = new Vector2(sourceX, sourceY)
        };
    }

    private static void AssignLayout(SerializedProperty layout, GameObject root)
    {
        Transform navigation = FindDirectChild(root.transform, "BottomNavigation");
        Transform preview = FindDirectChild(root.transform, "PreviewRoot");

        layout.FindPropertyRelative("layoutRoot").objectReferenceValue = root;
        layout.FindPropertyRelative("backButton").objectReferenceValue =
            FindDirectChild(navigation, "BackButton").GetComponent<Button>();
        layout.FindPropertyRelative("submitButton").objectReferenceValue =
            FindDirectChild(navigation, "PostButton").GetComponent<Button>();
        layout.FindPropertyRelative("characterButton").objectReferenceValue =
            FindDirectChild(navigation, "CharacterButton").GetComponent<Button>();
        layout.FindPropertyRelative("backgroundButton").objectReferenceValue =
            FindDirectChild(navigation, "BackgroundButton").GetComponent<Button>();
        layout.FindPropertyRelative("propsButton").objectReferenceValue =
            FindDirectChild(navigation, "PropsButton").GetComponent<Button>();
        layout.FindPropertyRelative("itemRoot").objectReferenceValue =
            FindDirectChild(root.transform, "ItemRoot");
        layout.FindPropertyRelative("characterPreview").objectReferenceValue =
            FindDirectChild(preview, "CharacterPreview").GetComponent<Image>();
        layout.FindPropertyRelative("propsPreview").objectReferenceValue =
            FindDirectChild(preview, "PropsPreview").GetComponent<Image>();
        layout.FindPropertyRelative("captureRoot").objectReferenceValue =
            preview.GetComponent<RectTransform>();
        layout.FindPropertyRelative("selectionFrame").objectReferenceValue =
            FindDirectChild(root.transform, "SelectionFrame").gameObject;
        layout.FindPropertyRelative("bottomNavigation").objectReferenceValue =
            navigation.gameObject;
        layout.FindPropertyRelative("itemBackgroundSprite").objectReferenceValue =
            LoadSprite(Level2Root + "/P图界面/工作室/UI/空白格子.png");
    }

    private static void CopyLayout(SerializedProperty source, SerializedProperty destination)
    {
        string[] fields =
        {
            "layoutRoot",
            "backButton",
            "submitButton",
            "characterButton",
            "backgroundButton",
            "propsButton",
            "itemRoot",
            "characterPreview",
            "propsPreview",
            "captureRoot",
            "selectionFrame",
            "bottomNavigation",
            "itemBackgroundSprite"
        };
        foreach (string field in fields)
        {
            destination.FindPropertyRelative(field).objectReferenceValue =
                source.FindPropertyRelative(field).objectReferenceValue;
        }
    }

    private static void CopySelectionItem(
        SerializedProperty source,
        SerializedProperty destination)
    {
        destination.FindPropertyRelative("itemId").stringValue =
            source.FindPropertyRelative("itemId").stringValue;
        destination.FindPropertyRelative("iconSprite").objectReferenceValue =
            source.FindPropertyRelative("iconSprite").objectReferenceValue;
        destination.FindPropertyRelative("previewSprite").objectReferenceValue =
            source.FindPropertyRelative("previewSprite").objectReferenceValue;
        destination.FindPropertyRelative("unlockedByDefault").boolValue =
            source.FindPropertyRelative("unlockedByDefault").boolValue;
        destination.FindPropertyRelative("initialDisplayScale").floatValue =
            source.FindPropertyRelative("initialDisplayScale").floatValue;
    }

    private static SerializedProperty FindCategory(
        SerializedProperty categories,
        SelectionCategoryType categoryType)
    {
        for (int index = 0; index < categories.arraySize; index++)
        {
            SerializedProperty category = categories.GetArrayElementAtIndex(index);
            if (category.FindPropertyRelative("categoryType").enumValueIndex == (int)categoryType)
            {
                return category;
            }
        }

        return null;
    }

    private static SerializedProperty FindItem(SerializedProperty items, string itemId)
    {
        for (int index = 0; index < items.arraySize; index++)
        {
            SerializedProperty item = items.GetArrayElementAtIndex(index);
            if (string.Equals(
                item.FindPropertyRelative("itemId").stringValue,
                itemId,
                StringComparison.Ordinal))
            {
                return item;
            }
        }

        return null;
    }

    private static Image CreateImageChild(Transform parent, string name)
    {
        GameObject root = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        root.transform.SetParent(parent, false);
        return root.GetComponent<Image>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Sprite LoadSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null
            && (importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            sprite = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .FirstOrDefault();
        }
        if (sprite == null)
        {
            throw new InvalidOperationException($"Sprite could not be loaded: {path}");
        }

        return sprite;
    }

    private static VideoClip LoadVideoClip(string path)
    {
        VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(path);
        if (clip == null)
        {
            throw new InvalidOperationException($"VideoClip could not be loaded: {path}");
        }

        return clip;
    }

    private static T FindSceneComponent<T>() where T : Component
    {
        return Resources.FindObjectsOfTypeAll<T>()
            .FirstOrDefault(component =>
                component != null
                && component.gameObject.scene.IsValid()
                && component.gameObject.scene == SceneManager.GetActiveScene());
    }

    private static GameObject FindSceneObject(string name)
    {
        return Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(target =>
                target != null
                && target.scene.IsValid()
                && target.scene == SceneManager.GetActiveScene()
                && string.Equals(target.name, name, StringComparison.Ordinal));
    }

    private static Transform FindDirectChild(Transform parent, string name)
    {
        if (parent == null)
        {
            return null;
        }

        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (string.Equals(child.name, name, StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }

    private static void DeleteGeneratedSceneObject(Transform parent, string name)
    {
        Transform existing = FindDirectChild(parent, name);
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }
    }
}
