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

public static class Level3SceneBuilder
{
    private const string Level3Root = "Assets/Art/BBS/level3";
    private const int RoundIndex = 2;
    private const string AssistantItemId = "character-day3-assistant";
    private const string PaparazziItemId = "character-day3-paparazzi";

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
        public readonly List<CollectibleSpec> CollectibleSpecs = new List<CollectibleSpec>();
    }

    [MenuItem("Tools/BBS Game/Build Level 3 Content")]
    public static void BuildFromMenu()
    {
        BuildLevel3Scene();
    }

    public static void BuildFromCommandLine()
    {
        EditorSceneManager.OpenScene(
            "Assets/Scenes/SampleScene.unity",
            OpenSceneMode.Single);
        BuildLevel3Scene();
    }

    private static void BuildLevel3Scene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()
            || !string.Equals(
                scene.path,
                "Assets/Scenes/SampleScene.unity",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Open Assets/Scenes/SampleScene.unity before building Level 3.");
        }

        StoryFlowController storyFlow = FindSceneComponent<StoryFlowController>();
        SelectionPanelController selection = FindSceneComponent<SelectionPanelController>();
        ForumPanelController forum = FindSceneComponent<ForumPanelController>();
        if (storyFlow == null || selection == null || forum == null)
        {
            throw new InvalidOperationException(
                "Story, Selection, or Forum controller is missing from SampleScene.");
        }

        GameObject postListPanel = FindSceneObject("ForumPostListPanel");
        GameObject postContentPanel = FindSceneObject("ForumPostContentPanel");
        GameObject bbsChoose = FindSceneObject("BBSChoose");
        GameObject postButtonTemplate = FindSceneObject("PostButton_01");
        GameObject postContentTemplate = FindSceneObject("PostContent_00");
        GameObject feijiTemplate = FindDirectChild(bbsChoose?.transform, "feiji")?.gameObject;
        if (postListPanel == null
            || postContentPanel == null
            || bbsChoose == null
            || postButtonTemplate == null
            || postContentTemplate == null
            || feijiTemplate == null)
        {
            throw new InvalidOperationException(
                "A Selection or Forum template required by the Level 3 builder was not found.");
        }

        for (int index = 1; index <= 5; index++)
        {
            DeleteGeneratedSceneObject(
                postListPanel.transform,
                $"PostButton_D3_{index:00}");
            DeleteGeneratedSceneObject(
                postContentPanel.transform,
                $"PostContent_D3_{index:00}");
        }
        DeleteGeneratedSceneObject(
            postListPanel.transform,
            "PostButtonGroup_D3_02_03");

        DeleteGeneratedSceneObject(bbsChoose.transform, "cafe");

        Button firstPostButton = CreatePostButton(
            postButtonTemplate,
            postListPanel.transform,
            "PostButton_D3_01",
            Level3Root + "/帖子/热门帖子/1/热门帖子1.png",
            Level3Root + "/帖子/热门帖子/1/标题.png",
            175f);
        GameObject sharedPostRoot;
        Button[] sharedPostButtons = CreateSharedPostButtons(
            postButtonTemplate,
            postListPanel.transform,
            out sharedPostRoot);
        Button fourthPostButton = CreatePostButton(
            postButtonTemplate,
            postListPanel.transform,
            "PostButton_D3_04",
            Level3Root + "/帖子/热门帖子/3/热门帖子5.png",
            Level3Root + "/帖子/热门帖子/3/标题.png",
            175f);
        Button fifthPostButton = CreatePostButton(
            postButtonTemplate,
            postListPanel.transform,
            "PostButton_D3_05",
            Level3Root + "/帖子/热门帖子/4/热门帖子4.png",
            Level3Root + "/帖子/热门帖子/4/标题.png",
            175f);
        Button[] postButtons =
        {
            firstPostButton,
            sharedPostButtons[0],
            sharedPostButtons[1],
            fourthPostButton,
            fifthPostButton
        };

        GeneratedPost leakPost = CreatePostContent(
            postContentTemplate,
            postContentPanel.transform,
            "PostContent_D3_01",
            Level3Root + "/帖子/帖子本体/爆料贴.png",
            Array.Empty<CollectibleSpec>());

        GeneratedPost evidenceOnePost = CreatePostContent(
            postContentTemplate,
            postContentPanel.transform,
            "PostContent_D3_02",
            Level3Root + "/帖子/帖子本体/实锤1/实锤1.png",
            new[]
            {
                Collectible(
                    "BaseballCap",
                    Level3Root + "/帖子/帖子本体/实锤1/1.png",
                    SelectionCategoryType.Props,
                    "props-day3-cap",
                    0.350f,
                    0.166f),
                Collectible(
                    "CustomRing",
                    Level3Root + "/帖子/帖子本体/实锤1/2.png",
                    SelectionCategoryType.Props,
                    "props-day3-wristband",
                    0.365f,
                    0.409f)
            });

        GeneratedPost clarificationOnePost = CreatePostContent(
            postContentTemplate,
            postContentPanel.transform,
            "PostContent_D3_03",
            Level3Root + "/帖子/帖子本体/澄清1/澄清1.png",
            new[]
            {
                Collectible(
                    "Interview",
                    Level3Root + "/帖子/帖子本体/澄清1/1.png",
                    SelectionCategoryType.Props,
                    "props-day3-cash",
                    0.435f,
                    0.140f),
                Collectible(
                    "BusinessContract",
                    Level3Root + "/帖子/帖子本体/澄清1/2.png",
                    SelectionCategoryType.Props,
                    "props-day3-contract",
                    0.425f,
                    0.322f)
            });

        GeneratedPost clarificationTwoPost = CreatePostContent(
            postContentTemplate,
            postContentPanel.transform,
            "PostContent_D3_04",
            Level3Root + "/帖子/帖子本体/澄清2/澄清2.png",
            new[]
            {
                Collectible(
                    "Assistant",
                    Level3Root + "/帖子/帖子本体/澄清2/1.png",
                    SelectionCategoryType.Character,
                    AssistantItemId,
                    0.405f,
                    0.145f),
                Collectible(
                    "HospitalWristband",
                    Level3Root + "/帖子/帖子本体/澄清2/2.png",
                    SelectionCategoryType.Props,
                    "props-day3-wristband",
                    0.405f,
                    0.174f),
                Collectible(
                    "Medicine",
                    Level3Root + "/帖子/帖子本体/澄清2/3.png",
                    SelectionCategoryType.Props,
                    "props-day3-medicine",
                    0.375f,
                    0.342f)
            });

        GeneratedPost evidenceTwoPost = CreatePostContent(
            postContentTemplate,
            postContentPanel.transform,
            "PostContent_D3_05",
            Level3Root + "/帖子/帖子本体/实锤2/实锤2.png",
            new[]
            {
                Collectible(
                    "Paparazzi",
                    Level3Root + "/帖子/帖子本体/实锤2/1.png",
                    SelectionCategoryType.Character,
                    PaparazziItemId,
                    0.160f,
                    0.126f),
                Collectible(
                    "ConfidentialityAgreement",
                    Level3Root + "/帖子/帖子本体/实锤2/2.png",
                    SelectionCategoryType.Props,
                    "props-day3-laptop",
                    0.315f,
                    0.163f),
                Collectible(
                    "GuestCheck",
                    Level3Root + "/帖子/帖子本体/实锤2/3.png",
                    SelectionCategoryType.Props,
                    "props-day3-guest-check",
                    0.480f,
                    0.353f)
            });

        RuntimeAnimatorController birdController = CreateSpriteController(
            Level3Root + "/P图界面/咖啡厅/动画/CafeBird.controller",
            Level3Root + "/P图界面/咖啡厅/动画/CafeBird.anim",
            Level3Root + "/P图界面/咖啡厅/动画/鸟",
            24f);
        RuntimeAnimatorController lightingController = CreateLightingController(
            Level3Root + "/P图界面/咖啡厅/动画/CafeLighting.controller",
            Level3Root + "/P图界面/咖啡厅/动画/CafeLighting.anim",
            Level3Root + "/P图界面/咖啡厅/动画/前景暗.png",
            Level3Root + "/P图界面/咖啡厅/动画/前景亮.png");

        GameObject cafe = CreateCafeLayout(
            feijiTemplate,
            bbsChoose.transform,
            birdController,
            lightingController);

        ConfigureSelectionItems(selection);
        ConfigureRoundThreeSelection(selection, cafe);
        ConfigureStoryRoundThree(
            storyFlow,
            postButtons,
            sharedPostRoot,
            new[]
            {
                leakPost,
                evidenceOnePost,
                clarificationOnePost,
                clarificationTwoPost,
                evidenceTwoPost
            });
        ConfigureExistingOfficeEnding(storyFlow);

        EditorUtility.SetDirty(selection);
        EditorUtility.SetDirty(storyFlow);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        Debug.Log(
            "[Level3SceneBuilder] Level 3 posts, collectibles, cafe Selection, and dual video endings were built successfully.");
    }

    private static Button CreatePostButton(
        GameObject template,
        Transform parent,
        string name,
        string postSpritePath,
        string titleSpritePath,
        float titleY)
    {
        GameObject root = UnityEngine.Object.Instantiate(template, parent, false);
        root.name = name;
        root.SetActive(false);
        Undo.RegisterCreatedObjectUndo(root, "Build Level 3 Post Button");

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
            RectTransform rect = titleImage.rectTransform;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, titleY);
        }

        return button;
    }

    private static Button[] CreateSharedPostButtons(
        GameObject template,
        Transform parent,
        out GameObject sharedRoot)
    {
        sharedRoot = UnityEngine.Object.Instantiate(template, parent, false);
        sharedRoot.name = "PostButtonGroup_D3_02_03";
        sharedRoot.SetActive(false);
        Undo.RegisterCreatedObjectUndo(sharedRoot, "Build Level 3 Shared Post Buttons");

        Button rootButton = sharedRoot.GetComponent<Button>();
        if (rootButton != null)
        {
            UnityEngine.Object.DestroyImmediate(rootButton);
        }

        Image rootImage = sharedRoot.GetComponent<Image>();
        rootImage.sprite = LoadSprite(Level3Root + "/帖子/热门帖子/2/热门帖子2.png");
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
            "PostButton_D3_02",
            Level3Root + "/帖子/热门帖子/2/标题上.png",
            175f);
        Button lowerButton = ConfigureSharedPostButton(
            lowerTitle,
            "PostButton_D3_03",
            Level3Root + "/帖子/热门帖子/2/标题下.png",
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
        IReadOnlyList<CollectibleSpec> collectibles)
    {
        GameObject root = UnityEngine.Object.Instantiate(template, parent, false);
        root.name = name;
        root.SetActive(false);
        Undo.RegisterCreatedObjectUndo(root, "Build Level 3 Post Content");

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
        foreach (CollectibleSpec collectible in collectibles)
        {
            result.CollectibleSpecs.Add(collectible);
            result.CollectibleButtons.Add(
                CreateCollectibleButton(postImage.transform, collectible, width, height));
        }

        return result;
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

    private static GameObject CreateCafeLayout(
        GameObject template,
        Transform parent,
        RuntimeAnimatorController birdController,
        RuntimeAnimatorController lightingController)
    {
        GameObject cafe = UnityEngine.Object.Instantiate(template, parent, false);
        cafe.name = "cafe";
        cafe.SetActive(false);
        Undo.RegisterCreatedObjectUndo(cafe, "Build Cafe Selection Layout");

        Transform selectionFrameTransform = FindDirectChild(cafe.transform, "SelectionFrame");
        Image selectionFrame = selectionFrameTransform.GetComponent<Image>();
        selectionFrame.sprite = LoadSprite(Level3Root + "/P图界面/咖啡厅/UI/背景.png");

        Transform navigation = FindDirectChild(cafe.transform, "BottomNavigation");
        ConfigureButton(
            navigation,
            "BackButton",
            Level3Root + "/P图界面/咖啡厅/UI/BACK小标题4.png",
            null);
        ConfigureButton(
            navigation,
            "PostButton",
            Level3Root + "/P图界面/咖啡厅/UI/POST小标题4.png",
            null);
        ConfigureButton(
            navigation,
            "CharacterButton",
            Level3Root + "/P图界面/咖啡厅/UI/CHAR小标题4.png",
            Level3Root + "/P图界面/咖啡厅/UI/CHAR大标题4.png");
        ConfigureButton(
            navigation,
            "BackgroundButton",
            Level3Root + "/P图界面/咖啡厅/UI/BACKGROUND小标题4.png",
            Level3Root + "/P图界面/咖啡厅/UI/BACKGROUND大标题4.png");
        ConfigureButton(
            navigation,
            "PropsButton",
            Level3Root + "/P图界面/咖啡厅/UI/PROPS小标题4.png",
            Level3Root + "/P图界面/咖啡厅/UI/PROPS大标题4.png");

        Transform previewRoot = FindDirectChild(cafe.transform, "PreviewRoot");
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

        Image backdrop = CreateImageChild(background, "CafeBackdrop");
        Stretch(backdrop.rectTransform);
        backdrop.sprite = LoadSprite(Level3Root + "/P图界面/咖啡厅/动画/后景.png");
        backdrop.preserveAspect = false;
        backdrop.raycastTarget = false;

        Image bird = CreateImageChild(background, "CafeBird");
        bird.sprite = LoadSprite(
            Level3Root + "/P图界面/咖啡厅/动画/鸟/咖啡厅改_00000_00000.png");
        bird.preserveAspect = true;
        bird.raycastTarget = false;
        bird.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        bird.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        bird.rectTransform.anchoredPosition = new Vector2(-560f, 245f);
        bird.rectTransform.sizeDelta = new Vector2(250f, 266f);
        Animator birdAnimator = bird.gameObject.AddComponent<Animator>();
        birdAnimator.runtimeAnimatorController = birdController;

        Image foreground = CreateImageChild(previewRoot, "CafeForeground");
        Stretch(foreground.rectTransform);
        foreground.sprite = LoadSprite(Level3Root + "/P图界面/咖啡厅/动画/前景暗.png");
        foreground.preserveAspect = false;
        foreground.raycastTarget = false;
        foreground.transform.SetAsLastSibling();
        Animator lightingAnimator = foreground.gameObject.AddComponent<Animator>();
        lightingAnimator.runtimeAnimatorController = lightingController;

        return cafe;
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
        float frameRate)
    {
        string[] paths = AssetDatabase.FindAssets("t:Sprite", new[] { spriteFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        if (paths.Length == 0)
        {
            throw new InvalidOperationException(
                $"No animation sprites were found in {spriteFolder}.");
        }

        List<Sprite> sprites = paths.Select(LoadSprite).ToList();
        return CreateSpriteController(controllerPath, clipPath, sprites, frameRate);
    }

    private static RuntimeAnimatorController CreateLightingController(
        string controllerPath,
        string clipPath,
        string darkSpritePath,
        string lightSpritePath)
    {
        Sprite dark = LoadSprite(darkSpritePath);
        Sprite light = LoadSprite(lightSpritePath);

        AssetDatabase.DeleteAsset(controllerPath);
        AssetDatabase.DeleteAsset(clipPath);

        AnimationClip clip = new AnimationClip
        {
            name = Path.GetFileNameWithoutExtension(clipPath),
            frameRate = 1f
        };
        ObjectReferenceKeyframe[] keyframes =
        {
            new ObjectReferenceKeyframe { time = 0f, value = dark },
            new ObjectReferenceKeyframe { time = 2f, value = light },
            new ObjectReferenceKeyframe { time = 4f, value = dark }
        };
        SetSpriteCurveAndLoop(clip, keyframes);

        AssetDatabase.CreateAsset(clip, clipPath);
        AnimatorController controller =
            AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddMotion(clip);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static RuntimeAnimatorController CreateSpriteController(
        string controllerPath,
        string clipPath,
        IReadOnlyList<Sprite> sprites,
        float frameRate)
    {
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

        SetSpriteCurveAndLoop(clip, keyframes);
        AssetDatabase.CreateAsset(clip, clipPath);
        AnimatorController controller =
            AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddMotion(clip);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void SetSpriteCurveAndLoop(
        AnimationClip clip,
        ObjectReferenceKeyframe[] keyframes)
    {
        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(
            string.Empty,
            typeof(Image),
            "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        SerializedObject clipObject = new SerializedObject(clip);
        SerializedProperty loopTime =
            clipObject.FindProperty("m_AnimationClipSettings.m_LoopTime");
        if (loopTime != null)
        {
            loopTime.boolValue = true;
            clipObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ConfigureSelectionItems(SelectionPanelController selection)
    {
        SerializedObject serializedSelection = new SerializedObject(selection);
        SerializedProperty categories = serializedSelection.FindProperty("categories");

        AddSelectionItem(
            categories,
            SelectionCategoryType.Character,
            AssistantItemId,
            Level3Root + "/char/助理.png");
        AddSelectionItem(
            categories,
            SelectionCategoryType.Character,
            PaparazziItemId,
            Level3Root + "/char/狗仔.png");
        AddSelectionItem(
            categories,
            SelectionCategoryType.Props,
            "props-day3-cap",
            Level3Root + "/道具/帽子.png");
        AddSelectionItem(
            categories,
            SelectionCategoryType.Props,
            "props-day3-wristband",
            Level3Root + "/道具/手环.png");
        AddSelectionItem(
            categories,
            SelectionCategoryType.Props,
            "props-day3-cash",
            Level3Root + "/道具/现金.png");
        AddSelectionItem(
            categories,
            SelectionCategoryType.Props,
            "props-day3-contract",
            Level3Root + "/道具/商业合同.png");
        AddSelectionItem(
            categories,
            SelectionCategoryType.Props,
            "props-day3-medicine",
            Level3Root + "/道具/药.png");
        AddSelectionItem(
            categories,
            SelectionCategoryType.Props,
            "props-day3-laptop",
            Level3Root + "/道具/笔记本.png");
        AddSelectionItem(
            categories,
            SelectionCategoryType.Props,
            "props-day3-guest-check",
            Level3Root + "/道具/签单.png");

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
            throw new InvalidOperationException(
                $"Selection category {categoryType} was not found.");
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

    private static void ConfigureRoundThreeSelection(
        SelectionPanelController selection,
        GameObject cafe)
    {
        SerializedObject serializedSelection = new SerializedObject(selection);
        SerializedProperty roundBackgrounds = serializedSelection.FindProperty("roundBackgrounds");
        SerializedProperty roundThree = FindRoundDefinition(roundBackgrounds, RoundIndex);
        if (roundThree == null)
        {
            int index = roundBackgrounds.arraySize;
            roundBackgrounds.InsertArrayElementAtIndex(index);
            roundThree = roundBackgrounds.GetArrayElementAtIndex(index);
        }

        roundThree.FindPropertyRelative("roundIndex").intValue = RoundIndex;

        SerializedProperty items = roundThree.FindPropertyRelative("items");
        items.arraySize = 1;
        SerializedProperty cafeItem = items.GetArrayElementAtIndex(0);
        Sprite cafeIcon = LoadSprite(Level3Root + "/P图界面/咖啡厅/动画/后景.png");
        cafeItem.FindPropertyRelative("itemId").stringValue = "cafe";
        cafeItem.FindPropertyRelative("iconSprite").objectReferenceValue = cafeIcon;
        cafeItem.FindPropertyRelative("previewSprite").objectReferenceValue = cafeIcon;
        cafeItem.FindPropertyRelative("unlockedByDefault").boolValue = false;
        cafeItem.FindPropertyRelative("initialDisplayScale").floatValue = 1f;

        SerializedProperty layouts = roundThree.FindPropertyRelative("layouts");
        layouts.arraySize = 1;
        AssignLayout(layouts.GetArrayElementAtIndex(0), cafe);

        SerializedProperty requiredCategories =
            roundThree.FindPropertyRelative("requiredCategoriesOverride");
        requiredCategories.arraySize = 2;
        requiredCategories.GetArrayElementAtIndex(0).enumValueIndex =
            (int)SelectionCategoryType.Character;
        requiredCategories.GetArrayElementAtIndex(1).enumValueIndex =
            (int)SelectionCategoryType.Background;

        SerializedProperty allowedCharacters =
            roundThree.FindPropertyRelative("allowedCharacterItemIds");
        allowedCharacters.arraySize = 2;
        allowedCharacters.GetArrayElementAtIndex(0).stringValue = AssistantItemId;
        allowedCharacters.GetArrayElementAtIndex(1).stringValue = PaparazziItemId;
        roundThree.FindPropertyRelative("singleCharacterPlacement").boolValue = true;

        serializedSelection.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureStoryRoundThree(
        StoryFlowController storyFlow,
        IReadOnlyList<Button> postButtons,
        GameObject sharedPostRoot,
        IReadOnlyList<GeneratedPost> posts)
    {
        SerializedObject serializedStory = new SerializedObject(storyFlow);
        SerializedProperty rounds = serializedStory.FindProperty("rounds");
        if (rounds.arraySize <= RoundIndex)
        {
            throw new InvalidOperationException(
                "StoryFlowController has no third round. Preserve and configure the existing Round 3 mail first.");
        }

        SerializedProperty roundThree = rounds.GetArrayElementAtIndex(RoundIndex);
        SerializedProperty postsProperty = roundThree.FindPropertyRelative("posts");
        postsProperty.arraySize = posts.Count;
        for (int postIndex = 0; postIndex < posts.Count; postIndex++)
        {
            SerializedProperty post = postsProperty.GetArrayElementAtIndex(postIndex);
            post.FindPropertyRelative("button").objectReferenceValue = postButtons[postIndex];
            post.FindPropertyRelative("listDisplayRoot").objectReferenceValue =
                postIndex == 1 || postIndex == 2 ? sharedPostRoot : null;
            post.FindPropertyRelative("contentImage").objectReferenceValue = posts[postIndex].Root;
            WriteCollectibles(post.FindPropertyRelative("collectibles"), posts[postIndex]);
            post.FindPropertyRelative("unlockWithPrevious").boolValue = postIndex == 2;
        }

        SerializedProperty selectionPost = roundThree.FindPropertyRelative("selectionPost");
        selectionPost.FindPropertyRelative("categoryType").enumValueIndex =
            (int)SelectionCategoryType.Character;
        selectionPost.FindPropertyRelative("button").objectReferenceValue = null;
        selectionPost.FindPropertyRelative("recordImage").objectReferenceValue = null;

        SerializedProperty branches = selectionPost.FindPropertyRelative("branches");
        branches.arraySize = 2;
        WriteImmediateEndingBranch(
            branches.GetArrayElementAtIndex(0),
            AssistantItemId,
            LoadVideoClip("Assets/Resources/2.mp4"));
        WriteImmediateEndingBranch(
            branches.GetArrayElementAtIndex(1),
            PaparazziItemId,
            LoadVideoClip("Assets/Resources/3.mp4"));

        serializedStory.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureExistingOfficeEnding(StoryFlowController storyFlow)
    {
        SerializedObject serializedStory = new SerializedObject(storyFlow);
        SerializedProperty rounds = serializedStory.FindProperty("rounds");
        if (rounds.arraySize < 2)
        {
            return;
        }

        SerializedProperty branches = rounds.GetArrayElementAtIndex(1)
            .FindPropertyRelative("selectionPost")
            .FindPropertyRelative("branches");
        for (int index = 0; index < branches.arraySize; index++)
        {
            SerializedProperty branch = branches.GetArrayElementAtIndex(index);
            if (!string.Equals(
                branch.FindPropertyRelative("itemId").stringValue,
                "office",
                StringComparison.Ordinal))
            {
                continue;
            }

            branch.FindPropertyRelative("completionMode").enumValueIndex =
                (int)SelectionBranchCompletionMode.OpenPostThenEnding;
            branch.FindPropertyRelative("endingVideoClip").objectReferenceValue =
                LoadVideoClip("Assets/Resources/1.mp4");
            break;
        }

        serializedStory.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WriteImmediateEndingBranch(
        SerializedProperty branch,
        string itemId,
        VideoClip endingVideoClip)
    {
        branch.FindPropertyRelative("itemId").stringValue = itemId;
        branch.FindPropertyRelative("contentImage").objectReferenceValue = null;
        branch.FindPropertyRelative("recordImage").objectReferenceValue = null;
        branch.FindPropertyRelative("collectibles").arraySize = 0;
        branch.FindPropertyRelative("completionMode").enumValueIndex =
            (int)SelectionBranchCompletionMode.PlayEndingImmediately;
        branch.FindPropertyRelative("endingVideoClip").objectReferenceValue = endingVideoClip;
    }

    private static void WriteCollectibles(
        SerializedProperty collectibles,
        GeneratedPost post)
    {
        collectibles.arraySize = Mathf.Min(
            post.CollectibleButtons.Count,
            post.CollectibleSpecs.Count);
        for (int index = 0; index < collectibles.arraySize; index++)
        {
            CollectibleSpec spec = post.CollectibleSpecs[index];
            SerializedProperty collectible = collectibles.GetArrayElementAtIndex(index);
            collectible.FindPropertyRelative("button").objectReferenceValue =
                post.CollectibleButtons[index];
            collectible.FindPropertyRelative("categoryType").enumValueIndex =
                (int)spec.CategoryType;
            collectible.FindPropertyRelative("itemId").stringValue = spec.ItemId;
        }
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
            LoadSprite(Level3Root + "/P图界面/咖啡厅/UI/空白格子.png");
    }

    private static SerializedProperty FindRoundDefinition(
        SerializedProperty roundDefinitions,
        int roundIndex)
    {
        for (int index = 0; index < roundDefinitions.arraySize; index++)
        {
            SerializedProperty candidate = roundDefinitions.GetArrayElementAtIndex(index);
            if (candidate.FindPropertyRelative("roundIndex").intValue == roundIndex)
            {
                return candidate;
            }
        }

        return null;
    }

    private static SerializedProperty FindCategory(
        SerializedProperty categories,
        SelectionCategoryType categoryType)
    {
        for (int index = 0; index < categories.arraySize; index++)
        {
            SerializedProperty category = categories.GetArrayElementAtIndex(index);
            if (category.FindPropertyRelative("categoryType").enumValueIndex ==
                (int)categoryType)
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
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .FirstOrDefault();
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
