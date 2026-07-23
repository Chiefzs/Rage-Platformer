using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameArtBuilder
{
    private const string HeroRunFrameDirectory =
        "Assets/Sprites/Player/Animations/Run";

    private static readonly string[] HeroRunFramePaths =
    {
        HeroRunFrameDirectory + "/run_01_contact_a.png",
        HeroRunFrameDirectory + "/run_02_compression_a.png",
        HeroRunFrameDirectory + "/run_03_airborne_a.png",
        HeroRunFrameDirectory + "/run_04_contact_b.png",
        HeroRunFrameDirectory + "/run_05_compression_b.png",
        HeroRunFrameDirectory + "/run_06_airborne_b.png"
    };

    private static readonly float[] HeroRunFramePixelsPerUnit =
    {
        850f,
        855f,
        790f,
        800f,
        855f,
        730f
    };

    // Measured face bounds keep perceived character scale stable across
    // idle and every generated run pose, independent of pose extension.
    private static readonly Vector2[] HeroRunReferenceFacePixelSizes =
    {
        new Vector2(206f, 169f),
        new Vector2(207f, 171f),
        new Vector2(191f, 157f),
        new Vector2(195f, 157f),
        new Vector2(207f, 171f),
        new Vector2(169f, 140f)
    };

    private static readonly Vector2 HeroIdleReferenceFacePixelSize =
        new Vector2(107f, 85f);

    private const float HeroStatePixelsPerUnit = 434f;
    private const float HeroPerceivedWidthScaleTolerance = 0.065f;
    private const float HeroPerceivedHeightScaleTolerance = 0.035f;

    private static readonly float[] HeroRunFootClearance =
    {
        0f,
        0f,
        0.10f,
        0f,
        0f,
        0.015f
    };

    private const string HeroJumpTexturePath =
        "Assets/Sprites/Player/hero_jump.png";

    private const string HeroStateTexturePath =
        "Assets/Sprites/Player/hero_states.png";

    private const string SpikeTexturePath =
        "Assets/Sprites/Traps/spikes_white.png";

    private const string DoorTexturePath =
        "Assets/Sprites/LevelObjects/exit_arch.png";

    private const string UiSpriteDirectory =
        "Assets/Sprites/UI";

    private const string UiLeftTexturePath =
        UiSpriteDirectory + "/ui_left_circle.png";

    private const string UiRightTexturePath =
        UiSpriteDirectory + "/ui_right_circle.png";

    private const string UiJumpTexturePath =
        UiSpriteDirectory + "/ui_jump_circle.png";

    private const string UiCrouchTexturePath =
        UiSpriteDirectory + "/ui_crouch_circle.png";

    private const string UiGlyphSourceDirectory =
        UiSpriteDirectory + "/GlyphsV2";

    private const string UiJumpGlyphSourcePath =
        UiGlyphSourceDirectory + "/ui_jump_glyph_v2.png";

    private const string UiCrouchGlyphSourcePath =
        UiGlyphSourceDirectory + "/ui_crouch_glyph_v2.png";

    private const string UiLifeTexturePath =
        UiSpriteDirectory + "/ui_life_classic_walk_v2.png";

    private const string OriginalPlayerPrefabPath =
        "Assets/Prefabs/Player/Player.prefab";

    private const string MobileUiPrefabPath =
        "Assets/Prefabs/UI/MobileUI.prefab";

    private const string SpriteUnlitMaterialPath =
        "Assets/Materials/Shared/SpriteUnlit.mat";

    private const string HeroWalkMaterialPath =
        "Assets/Materials/Player/HeroWalk.mat";

    private const string HeroStateMaterialPath =
        "Assets/Materials/Player/HeroState.mat";

    private const string HeroJumpMaterialPath =
        "Assets/Materials/Player/HeroJump.mat";

    private const string NoFrictionMaterialPath =
        "Assets/Settings/Physics/PlayerNoFriction.physicsMaterial2D";

    private const string SpikeMaterialPath =
        "Assets/Materials/Traps/Spike.mat";

    private const string DoorMaterialPath =
        "Assets/Materials/LevelObjects/ExitDoor.mat";

    private const string PlayerCharacterPrefabPath =
        "Assets/Prefabs/Player/PlayerCharacter.prefab";

    private const string WhiteSpikePrefabPath =
        "Assets/Prefabs/Traps/WhiteSpike.prefab";

    private const string ExitDoorPrefabPath =
        "Assets/Prefabs/LevelObjects/ExitDoor.prefab";

    private const string TestLevelScenePath =
        "Assets/Scenes/TestLevel.unity";

    private const string DoorEnterAudioPath =
        "Assets/Audio/SFX/sfx_door_enter.wav";

    private const string LevelTwoScenePath =
        "Assets/Scenes/Level_02.unity";

    private const int GroundLayer = 6;
    private const int HazardLayer = 7;

    private static readonly Color BackdropColor =
        FromHex("#E6C98F");

    private static readonly Color FarBackdropColor =
        FromHex("#D4AF68");

    private static readonly Color PlatformColor =
        FromHex("#2B3447");

    private static readonly Color PlatformTopColor =
        FromHex("#6C7A90");

    private static readonly Color AccentColor =
        FromHex("#F26A24");

    private static readonly Color TextColor =
        FromHex("#1B2432");

    private static Sprite squareSprite;
    private static Material spriteUnlitMaterial;
    private static Material heroWalkMaterial;
    private static Material heroStateMaterial;
    private static Material heroJumpMaterial;
    private static Material spikeMaterial;
    private static Material doorMaterial;
    private static PhysicsMaterial2D noFrictionMaterial;
    private static TMP_FontAsset labelFont;

    private enum UiGlyph
    {
        Left,
        Right,
        Jump,
        Crouch,
        Life
    }

    [MenuItem("Tools/Rage Platformer/Build Game Art Test Level")]
    public static void Build()
    {
        GenerateMobileUiTextures();
        ConfigureHeroSpriteSheets();
        ConfigureSingleSprite(SpikeTexturePath, 320f);
        ConfigureSingleSprite(DoorTexturePath, 350f);
        ConfigureSingleSprite(UiLeftTexturePath, 100f, FilterMode.Bilinear);
        ConfigureSingleSprite(UiRightTexturePath, 100f, FilterMode.Bilinear);
        ConfigureSingleSprite(UiJumpTexturePath, 100f, FilterMode.Bilinear);
        ConfigureSingleSprite(UiCrouchTexturePath, 100f, FilterMode.Bilinear);
        ConfigureTrimmedSprite(
            UiLifeTexturePath,
            100f,
            FilterMode.Point
        );

        Sprite idleSprite = LoadSprite(
            HeroStateTexturePath,
            "Hero_Idle"
        );

        Sprite[] walkSprites = LoadWalkSprites();

        Sprite crouchSprite = LoadSprite(
            HeroStateTexturePath,
            "Hero_Crouch"
        );
        Sprite[] jumpSprites = LoadJumpSprites();
        Sprite spikeSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(SpikeTexturePath);
        Sprite doorSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(DoorTexturePath);
        Sprite lifeSprite = LoadLifeSprite();

        ValidateAssets(
            idleSprite,
            walkSprites,
            crouchSprite,
            jumpSprites,
            spikeSprite,
            doorSprite,
            lifeSprite
        );

        LoadSharedAssets();

        UpdateMobileUiPrefab(lifeSprite);

        CreatePlayerCharacterPrefab(
            idleSprite,
            walkSprites,
            crouchSprite,
            jumpSprites
        );
        CreateWhiteSpikePrefab(spikeSprite);
        CreateExitDoorPrefab(doorSprite);
        CreateTestLevelScene();
        AddTestLevelSceneToBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "Game art assets, prefabs and test scene were built " +
            "without modifying the existing level scene files."
        );
    }

    [MenuItem(
        "Tools/Rage Platformer/Upgrade Motion And Collisions " +
        "(Preserve Scene)"
    )]
    public static void UpgradeMotionAndCollisionsPreservingScene()
    {
        ConfigureHeroSpriteSheets();
        ConfigureSingleSprite(SpikeTexturePath, 320f);
        ConfigureTrimmedSprite(
            UiLifeTexturePath,
            100f,
            FilterMode.Point
        );

        Sprite idleSprite = LoadSprite(
            HeroStateTexturePath,
            "Hero_Idle"
        );

        Sprite[] walkSprites = LoadWalkSprites();

        Sprite crouchSprite = LoadSprite(
            HeroStateTexturePath,
            "Hero_Crouch"
        );
        Sprite[] jumpSprites = LoadJumpSprites();
        Sprite spikeSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(SpikeTexturePath);
        Sprite lifeSprite = LoadLifeSprite();

        ValidateMotionAssets(
            idleSprite,
            walkSprites,
            crouchSprite,
            jumpSprites,
            spikeSprite,
            lifeSprite
        );

        LoadSharedAssets();
        CreatePlayerCharacterPrefab(
            idleSprite,
            walkSprites,
            crouchSprite,
            jumpSprites
        );
        CreateWhiteSpikePrefab(spikeSprite);
        UpdateMobileUiPrefab(lifeSprite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "Motion sprites and collision prefabs upgraded. " +
            "The test level scene file was preserved."
        );
    }

    [MenuItem(
        "Tools/Rage Platformer/Upgrade Run Animation " +
        "(Preserve Scene And Prefab)"
    )]
    public static void UpgradeRunAnimationPreservingSceneAndPrefab()
    {
        ConfigureRunSpriteFrames();

        Sprite idleSprite = LoadSprite(
            HeroStateTexturePath,
            "Hero_Idle"
        );
        Sprite[] runSprites = LoadWalkSprites();
        Sprite crouchSprite = LoadSprite(
            HeroStateTexturePath,
            "Hero_Crouch"
        );
        Sprite[] jumpSprites = LoadJumpSprites();

        ValidateRunAnimationAssets(
            idleSprite,
            runSprites,
            crouchSprite,
            jumpSprites
        );

        Material stateMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                HeroStateMaterialPath
            );
        Material runMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                SpriteUnlitMaterialPath
            );
        Material jumpMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                HeroJumpMaterialPath
            );

        if (
            stateMaterial == null ||
            runMaterial == null ||
            jumpMaterial == null
        )
        {
            throw new InvalidOperationException(
                "Karakter animasyon materyallerinden biri bulunamadı."
            );
        }

        UpdatePlayerCharacterAnimationPrefab(
            idleSprite,
            runSprites,
            crouchSprite,
            jumpSprites,
            stateMaterial,
            runMaterial,
            jumpMaterial
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "Six-frame run animation upgraded in place. " +
            "The test level scene and unrelated prefab settings " +
            "were preserved."
        );
    }

    [MenuItem(
        "Tools/Rage Platformer/Fix Run Scale And Action Icons " +
        "(Preserve Scene)"
    )]
    public static void FixRunScaleAndActionIconsPreservingScene()
    {
        ConfigureRunSpriteFrames();

        WriteUiTexture(UiJumpTexturePath, UiGlyph.Jump);
        WriteUiTexture(UiCrouchTexturePath, UiGlyph.Crouch);

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        ConfigureSingleSprite(
            UiJumpTexturePath,
            100f,
            FilterMode.Bilinear
        );
        ConfigureSingleSprite(
            UiCrouchTexturePath,
            100f,
            FilterMode.Bilinear
        );

        Sprite[] runSprites = LoadWalkSprites();

        if (
            runSprites.Length != HeroRunFramePaths.Length ||
            runSprites.Any(sprite => sprite == null)
        )
        {
            throw new InvalidOperationException(
                "Run sprites could not be reloaded after scale repair."
            );
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "Run scale and jump/crouch inner icons were repaired " +
            "without saving a scene or changing the MobileUI layout."
        );
    }

    [MenuItem("Tools/Rage Platformer/Validate And Render Test Level")]
    public static void ValidateAndRenderPreview()
    {
        Scene scene = EditorSceneManager.OpenScene(
            TestLevelScenePath,
            OpenSceneMode.Single
        );

        ValidateSceneContents(scene);

        string previewPath = Path.GetFullPath(
            Path.Combine(
                Application.dataPath,
                "../Logs/TestLevelPreview.png"
            )
        );

        RenderScenePreview(previewPath);

        Debug.Log(
            "Test level validation passed. Preview: " +
            previewPath
        );
    }

    [MenuItem(
        "Tools/Rage Platformer/Validate Motion And Collisions"
    )]
    public static void ValidateMotionAndCollisions()
    {
        Scene scene = EditorSceneManager.OpenScene(
            TestLevelScenePath,
            OpenSceneMode.Single
        );

        ValidateSceneContents(scene);

        Debug.Log(
            "Motion and collision validation passed without " +
            "saving the test level scene."
        );
    }

    private static void GenerateMobileUiTextures()
    {
        string absoluteDirectory = Path.GetFullPath(
            Path.Combine(
                Application.dataPath,
                "..",
                UiSpriteDirectory
            )
        );

        Directory.CreateDirectory(absoluteDirectory);

        WriteUiTexture(UiLeftTexturePath, UiGlyph.Left);
        WriteUiTexture(UiRightTexturePath, UiGlyph.Right);
        WriteUiTexture(UiJumpTexturePath, UiGlyph.Jump);
        WriteUiTexture(UiCrouchTexturePath, UiGlyph.Crouch);

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }

    private static void WriteUiTexture(
        string assetPath,
        UiGlyph glyph
    )
    {
        const int size = 256;

        Texture2D texture = new Texture2D(
            size,
            size,
            TextureFormat.RGBA32,
            false
        );

        Color32[] clearPixels = new Color32[size * size];
        texture.SetPixels32(clearPixels);

        if (glyph == UiGlyph.Life)
        {
            DrawLifeGlyph(texture);
        }
        else
        {
            DrawControlCircle(texture);

            switch (glyph)
            {
                case UiGlyph.Left:
                    DrawChevron(texture, pointsLeft: true);
                    break;

                case UiGlyph.Right:
                    DrawChevron(texture, pointsLeft: false);
                    break;

                case UiGlyph.Jump:
                    DrawJumpGlyph(texture);
                    break;

                case UiGlyph.Crouch:
                    DrawCrouchGlyph(texture);
                    break;
            }
        }

        texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);

        string absolutePath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "..", assetPath)
        );

        File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static void DrawControlCircle(Texture2D texture)
    {
        Vector2 center = new Vector2(127.5f, 127.5f);
        Color fill = new Color(1f, 1f, 1f, 0.055f);
        Color ring = new Color(1f, 1f, 1f, 0.38f);

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float distance = Vector2.Distance(
                    new Vector2(x, y),
                    center
                );

                if (distance <= 111f)
                {
                    texture.SetPixel(x, y, fill);
                }

                if (distance >= 104f && distance <= 111f)
                {
                    texture.SetPixel(x, y, ring);
                }
            }
        }
    }

    private static void DrawChevron(
        Texture2D texture,
        bool pointsLeft
    )
    {
        float direction = pointsLeft ? -1f : 1f;
        Vector2 tip = new Vector2(128f + direction * 33f, 128f);
        Vector2 upper = new Vector2(128f - direction * 23f, 181f);
        Vector2 lower = new Vector2(128f - direction * 23f, 75f);
        Color color = new Color(1f, 1f, 1f, 0.72f);

        DrawThickLine(texture, upper, tip, 11f, color);
        DrawThickLine(texture, tip, lower, 11f, color);
    }

    private static void DrawJumpGlyph(Texture2D texture)
    {
        DrawGeneratedGlyph(
            texture,
            UiJumpGlyphSourcePath,
            new Vector2(124f, 176f),
            0.80f
        );
    }

    private static void DrawCrouchGlyph(Texture2D texture)
    {
        DrawGeneratedGlyph(
            texture,
            UiCrouchGlyphSourcePath,
            new Vector2(122f, 104f),
            0.80f
        );

        CenterTopGlyphComponent(texture, verticalOffset: 6);
    }

    private static void DrawGeneratedGlyph(
        Texture2D target,
        string sourcePath,
        Vector2 maximumSize,
        float opacity
    )
    {
        Texture2D source = LoadSourceTexture(sourcePath);

        try
        {
            RectInt sourceRect = GetOpaqueRect(
                source,
                alphaThreshold: 4,
                padding: 0
            );

            float scale = Mathf.Min(
                maximumSize.x / sourceRect.width,
                maximumSize.y / sourceRect.height
            );

            int targetWidth = Mathf.Max(
                1,
                Mathf.RoundToInt(sourceRect.width * scale)
            );
            int targetHeight = Mathf.Max(
                1,
                Mathf.RoundToInt(sourceRect.height * scale)
            );
            int targetX = (target.width - targetWidth) / 2;
            int targetY = (target.height - targetHeight) / 2;

            Color32[] sourcePixels = source.GetPixels32();

            for (int y = 0; y < targetHeight; y++)
            {
                int sourceY = sourceRect.yMin + Mathf.Min(
                    sourceRect.height - 1,
                    Mathf.FloorToInt(
                        (y + 0.5f) * sourceRect.height / targetHeight
                    )
                );

                for (int x = 0; x < targetWidth; x++)
                {
                    int sourceX = sourceRect.xMin + Mathf.Min(
                        sourceRect.width - 1,
                        Mathf.FloorToInt(
                            (x + 0.5f) * sourceRect.width / targetWidth
                        )
                    );

                    byte sourceAlpha = sourcePixels[
                        sourceY * source.width + sourceX
                    ].a;

                    if (sourceAlpha == 0)
                    {
                        continue;
                    }

                    int destinationX = targetX + x;
                    int destinationY = targetY + y;
                    Color existing = target.GetPixel(
                        destinationX,
                        destinationY
                    );
                    float glyphAlpha =
                        sourceAlpha / 255f * Mathf.Clamp01(opacity);
                    float combinedAlpha = glyphAlpha +
                        existing.a * (1f - glyphAlpha);

                    target.SetPixel(
                        destinationX,
                        destinationY,
                        new Color(1f, 1f, 1f, combinedAlpha)
                    );
                }
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
        }
    }

    private static void CenterTopGlyphComponent(
        Texture2D texture,
        int verticalOffset
    )
    {
        const byte glyphAlphaThreshold = 15;
        const int minimumComponentArea = 20;

        Color32[] pixels = texture.GetPixels32();
        bool[] visited = new bool[pixels.Length];
        List<int> selectedComponent = null;
        int selectedMinX = 0;
        int selectedMaxX = 0;
        int selectedMaxY = int.MinValue;

        for (int index = 0; index < pixels.Length; index++)
        {
            if (
                visited[index] ||
                pixels[index].a < glyphAlphaThreshold
            )
            {
                continue;
            }

            Queue<int> pending = new Queue<int>();
            List<int> component = new List<int>();
            pending.Enqueue(index);
            visited[index] = true;

            int minX = texture.width;
            int minY = texture.height;
            int maxX = -1;
            int maxY = -1;

            while (pending.Count > 0)
            {
                int current = pending.Dequeue();
                component.Add(current);

                int x = current % texture.width;
                int y = current / texture.width;
                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);

                TryQueueGlyphPixel(
                    x - 1,
                    y,
                    texture,
                    pixels,
                    visited,
                    pending,
                    glyphAlphaThreshold
                );
                TryQueueGlyphPixel(
                    x + 1,
                    y,
                    texture,
                    pixels,
                    visited,
                    pending,
                    glyphAlphaThreshold
                );
                TryQueueGlyphPixel(
                    x,
                    y - 1,
                    texture,
                    pixels,
                    visited,
                    pending,
                    glyphAlphaThreshold
                );
                TryQueueGlyphPixel(
                    x,
                    y + 1,
                    texture,
                    pixels,
                    visited,
                    pending,
                    glyphAlphaThreshold
                );
            }

            int componentWidth = maxX - minX + 1;
            int componentHeight = maxY - minY + 1;
            bool isControlRing =
                componentWidth >= texture.width * 0.75f ||
                componentHeight >= texture.height * 0.75f;

            if (
                isControlRing ||
                component.Count < minimumComponentArea ||
                maxY <= selectedMaxY
            )
            {
                continue;
            }

            selectedComponent = component;
            selectedMinX = minX;
            selectedMaxX = maxX;
            selectedMaxY = maxY;
        }

        if (selectedComponent == null)
        {
            throw new InvalidOperationException(
                "A detached top action indicator could not be found."
            );
        }

        float componentCenterX =
            (selectedMinX + selectedMaxX) * 0.5f;
        float textureCenterX = (texture.width - 1) * 0.5f;
        int horizontalOffset = Mathf.RoundToInt(
            textureCenterX - componentCenterX
        );

        Color32[] selectedColors = selectedComponent
            .Select(pixelIndex => pixels[pixelIndex])
            .ToArray();
        Color32 controlFill = new Color32(255, 255, 255, 14);

        for (int index = 0; index < selectedComponent.Count; index++)
        {
            pixels[selectedComponent[index]] = controlFill;
        }

        for (int index = 0; index < selectedComponent.Count; index++)
        {
            int sourceIndex = selectedComponent[index];
            int sourceX = sourceIndex % texture.width;
            int sourceY = sourceIndex / texture.width;
            int destinationX = sourceX + horizontalOffset;
            int destinationY = sourceY + verticalOffset;

            if (
                destinationX < 0 ||
                destinationX >= texture.width ||
                destinationY < 0 ||
                destinationY >= texture.height
            )
            {
                continue;
            }

            int destinationIndex =
                destinationY * texture.width + destinationX;

            if (selectedColors[index].a > pixels[destinationIndex].a)
            {
                pixels[destinationIndex] = selectedColors[index];
            }
        }

        texture.SetPixels32(pixels);
    }

    private static void TryQueueGlyphPixel(
        int x,
        int y,
        Texture2D texture,
        Color32[] pixels,
        bool[] visited,
        Queue<int> pending,
        byte alphaThreshold
    )
    {
        if (
            x < 0 ||
            x >= texture.width ||
            y < 0 ||
            y >= texture.height
        )
        {
            return;
        }

        int index = y * texture.width + x;

        if (visited[index] || pixels[index].a < alphaThreshold)
        {
            return;
        }

        visited[index] = true;
        pending.Enqueue(index);
    }

    private static void DrawLifeGlyph(Texture2D texture)
    {
        Color hair = FromHex("#151B3D");
        Color body = FromHex("#1D2A50");
        Color face = FromHex("#FFD8A0");
        Color scarf = FromHex("#F26A24");
        Color outline = FromHex("#0A1230");

        DrawThickLine(
            texture,
            new Vector2(126f, 115f),
            new Vector2(126f, 66f),
            27f,
            outline
        );
        DrawThickLine(
            texture,
            new Vector2(126f, 113f),
            new Vector2(126f, 69f),
            21f,
            body
        );
        DrawThickLine(
            texture,
            new Vector2(120f, 74f),
            new Vector2(99f, 39f),
            16f,
            outline
        );
        DrawThickLine(
            texture,
            new Vector2(134f, 74f),
            new Vector2(154f, 39f),
            16f,
            outline
        );
        DrawThickLine(
            texture,
            new Vector2(114f, 108f),
            new Vector2(94f, 78f),
            14f,
            body
        );
        DrawThickLine(
            texture,
            new Vector2(138f, 108f),
            new Vector2(155f, 82f),
            14f,
            body
        );
        DrawDisc(texture, new Vector2(127f, 161f), 37f, outline);
        DrawDisc(texture, new Vector2(136f, 159f), 29f, face);
        DrawDisc(texture, new Vector2(119f, 177f), 31f, hair);
        DrawThickLine(
            texture,
            new Vector2(99f, 132f),
            new Vector2(150f, 132f),
            14f,
            outline
        );
        DrawThickLine(
            texture,
            new Vector2(99f, 132f),
            new Vector2(150f, 132f),
            9f,
            scarf
        );
        DrawThickLine(
            texture,
            new Vector2(105f, 130f),
            new Vector2(82f, 115f),
            10f,
            scarf
        );
    }

    private static void DrawDisc(
        Texture2D texture,
        Vector2 center,
        float radius,
        Color color
    )
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
        int maxX = Mathf.Min(
            texture.width - 1,
            Mathf.CeilToInt(center.x + radius)
        );
        int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
        int maxY = Mathf.Min(
            texture.height - 1,
            Mathf.CeilToInt(center.y + radius)
        );
        float radiusSquared = radius * radius;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (
                    (new Vector2(x, y) - center).sqrMagnitude <=
                    radiusSquared
                )
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }

    private static void DrawThickLine(
        Texture2D texture,
        Vector2 start,
        Vector2 end,
        float thickness,
        Color color
    )
    {
        Vector2 segment = end - start;
        float segmentLengthSquared = segment.sqrMagnitude;
        float radius = thickness * 0.5f;

        int minX = Mathf.Max(
            0,
            Mathf.FloorToInt(Mathf.Min(start.x, end.x) - radius)
        );
        int maxX = Mathf.Min(
            texture.width - 1,
            Mathf.CeilToInt(Mathf.Max(start.x, end.x) + radius)
        );
        int minY = Mathf.Max(
            0,
            Mathf.FloorToInt(Mathf.Min(start.y, end.y) - radius)
        );
        int maxY = Mathf.Min(
            texture.height - 1,
            Mathf.CeilToInt(Mathf.Max(start.y, end.y) + radius)
        );

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 point = new Vector2(x, y);
                float t = segmentLengthSquared <= Mathf.Epsilon
                    ? 0f
                    : Mathf.Clamp01(
                        Vector2.Dot(point - start, segment) /
                        segmentLengthSquared
                    );
                Vector2 closest = start + segment * t;

                if ((point - closest).sqrMagnitude <= radius * radius)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }

    private static void ConfigureHeroSpriteSheets()
    {
        ConfigureRunSpriteFrames();

        ConfigureGridSpriteSheet(
            HeroJumpTexturePath,
            columns: 5,
            rows: 1,
            pixelsPerUnit: 400f,
            frameNames: new[]
            {
                "Hero_Jump_1_Takeoff",
                "Hero_Jump_2_Rise",
                "Hero_Jump_3_Apex",
                "Hero_Jump_4_Fall",
                "Hero_Jump_5_Impact"
            }
        );

        ConfigureGridSpriteSheet(
            HeroStateTexturePath,
            columns: 4,
            rows: 1,
            pixelsPerUnit: 434f,
            frameNames: new[]
            {
                "Hero_Idle",
                "Hero_Crouch",
                "Hero_Jump_Rise",
                "Hero_Jump_Fall"
            }
        );
    }

    private static void ConfigureRunSpriteFrames()
    {
        if (
            HeroRunFramePaths.Length !=
                HeroRunFramePixelsPerUnit.Length ||
            HeroRunFramePaths.Length != HeroRunFootClearance.Length ||
            HeroRunFramePaths.Length !=
                HeroRunReferenceFacePixelSizes.Length
        )
        {
            throw new InvalidOperationException(
                "Koşu kare yolu, PPU ve ayak boşluğu sayıları eşleşmiyor."
            );
        }

        ValidateRunPerceivedScaleConfiguration();

        for (int index = 0; index < HeroRunFramePaths.Length; index++)
        {
            ConfigureGroundAlignedSingleSprite(
                HeroRunFramePaths[index],
                HeroRunFramePixelsPerUnit[index],
                HeroRunFootClearance[index]
            );
        }
    }

    private static void ValidateRunPerceivedScaleConfiguration()
    {
        Vector2 idleFaceWorldSize =
            HeroIdleReferenceFacePixelSize / HeroStatePixelsPerUnit;

        for (int index = 0; index < HeroRunFramePaths.Length; index++)
        {
            Vector2 runFaceWorldSize =
                HeroRunReferenceFacePixelSizes[index] /
                HeroRunFramePixelsPerUnit[index];

            float widthDifference = Mathf.Abs(
                runFaceWorldSize.x / idleFaceWorldSize.x - 1f
            );
            float heightDifference = Mathf.Abs(
                runFaceWorldSize.y / idleFaceWorldSize.y - 1f
            );

            if (
                widthDifference > HeroPerceivedWidthScaleTolerance ||
                heightDifference > HeroPerceivedHeightScaleTolerance
            )
            {
                throw new InvalidOperationException(
                    $"Run frame {index + 1} perceived scale differs " +
                    "from idle by more than the allowed tolerance."
                );
            }
        }
    }

    private static void ConfigureGridSpriteSheet(
        string texturePath,
        int columns,
        int rows,
        float pixelsPerUnit,
        string[] frameNames
    )
    {
        if (frameNames.Length != columns * rows)
        {
            throw new ArgumentException(
                "Kare adedi grid hücresiyle eşleşmiyor.",
                nameof(frameNames)
            );
        }

        AssetDatabase.ImportAsset(
            texturePath,
            ImportAssetOptions.ForceUpdate
        );

        TextureImporter importer =
            AssetImporter.GetAtPath(texturePath) as TextureImporter;

        if (importer == null)
        {
            throw new InvalidOperationException(
                $"TextureImporter bulunamadı: {texturePath}"
            );
        }

        Texture2D source = LoadSourceTexture(texturePath);
        SpriteMetaData[] frames = new SpriteMetaData[frameNames.Length];

        try
        {
            int frameIndex = 0;

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    RectInt rect = GetGridRect(
                        source.width,
                        source.height,
                        columns,
                        rows,
                        column,
                        row
                    );

                    frames[frameIndex] = CreateFootAlignedFrame(
                        frameNames[frameIndex],
                        rect,
                        source
                    );

                    frameIndex++;
                }
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression =
            TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 4096;

#pragma warning disable CS0618
        importer.spritesheet = frames;
#pragma warning restore CS0618

        importer.SaveAndReimport();
    }

    private static void ConfigureGroundAlignedSingleSprite(
        string texturePath,
        float pixelsPerUnit,
        float footClearance
    )
    {
        AssetDatabase.ImportAsset(
            texturePath,
            ImportAssetOptions.ForceUpdate
        );

        TextureImporter importer =
            AssetImporter.GetAtPath(texturePath) as TextureImporter;

        if (importer == null)
        {
            throw new InvalidOperationException(
                $"TextureImporter bulunamadı: {texturePath}"
            );
        }

        Texture2D source = LoadSourceTexture(texturePath);
        SpriteMetaData frame;

        try
        {
            frame = CreateFootAlignedFrame(
                Path.GetFileNameWithoutExtension(texturePath),
                new RectInt(0, 0, source.width, source.height),
                source
            );

            frame.pivot = new Vector2(
                frame.pivot.x,
                Mathf.Clamp01(
                    frame.pivot.y -
                    footClearance * pixelsPerUnit / source.height
                )
            );
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression =
            TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 4096;

#pragma warning disable CS0618
        importer.spritesheet = new[] { frame };
#pragma warning restore CS0618

        importer.SaveAndReimport();
    }

    private static void ConfigureTrimmedSprite(
        string texturePath,
        float pixelsPerUnit,
        FilterMode filterMode
    )
    {
        AssetDatabase.ImportAsset(
            texturePath,
            ImportAssetOptions.ForceUpdate
        );

        TextureImporter importer =
            AssetImporter.GetAtPath(texturePath) as TextureImporter;

        if (importer == null)
        {
            throw new InvalidOperationException(
                $"TextureImporter bulunamadı: {texturePath}"
            );
        }

        Texture2D source = LoadSourceTexture(texturePath);
        RectInt opaqueRect;

        try
        {
            opaqueRect = GetOpaqueRect(
                source,
                alphaThreshold: 4,
                padding: 2
            );
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
        }

        SpriteMetaData frame = new SpriteMetaData
        {
            name = Path.GetFileNameWithoutExtension(texturePath),
            rect = new Rect(
                opaqueRect.x,
                opaqueRect.y,
                opaqueRect.width,
                opaqueRect.height
            ),
            alignment = (int)SpriteAlignment.Center,
            pivot = new Vector2(0.5f, 0.5f)
        };

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = filterMode;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression =
            TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;

#pragma warning disable CS0618
        importer.spritesheet = new[] { frame };
#pragma warning restore CS0618

        importer.SaveAndReimport();
    }

    private static RectInt GetOpaqueRect(
        Texture2D source,
        byte alphaThreshold,
        int padding
    )
    {
        Color32[] pixels = source.GetPixels32();
        int minX = source.width;
        int minY = source.height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < source.height; y++)
        {
            int rowOffset = y * source.width;

            for (int x = 0; x < source.width; x++)
            {
                if (pixels[rowOffset + x].a < alphaThreshold)
                {
                    continue;
                }

                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
        {
            throw new InvalidOperationException(
                "Sprite içinde görünür piksel bulunamadı."
            );
        }

        minX = Mathf.Max(0, minX - padding);
        minY = Mathf.Max(0, minY - padding);
        maxX = Mathf.Min(source.width - 1, maxX + padding);
        maxY = Mathf.Min(source.height - 1, maxY + padding);

        return new RectInt(
            minX,
            minY,
            maxX - minX + 1,
            maxY - minY + 1
        );
    }

    private static Texture2D LoadSourceTexture(string assetPath)
    {
        string absolutePath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "..", assetPath)
        );

        Texture2D source = new Texture2D(
            2,
            2,
            TextureFormat.RGBA32,
            false
        );

        if (!source.LoadImage(File.ReadAllBytes(absolutePath)))
        {
            UnityEngine.Object.DestroyImmediate(source);

            throw new InvalidOperationException(
                $"Sprite kaynak PNG okunamadı: {assetPath}"
            );
        }

        return source;
    }

    private static RectInt GetGridRect(
        int textureWidth,
        int textureHeight,
        int columns,
        int rows,
        int column,
        int rowFromTop
    )
    {
        int xMin = Mathf.RoundToInt(
            column * textureWidth / (float)columns
        );
        int xMax = Mathf.RoundToInt(
            (column + 1) * textureWidth / (float)columns
        );

        int top = Mathf.RoundToInt(
            rowFromTop * textureHeight / (float)rows
        );
        int bottomFromTop = Mathf.RoundToInt(
            (rowFromTop + 1) * textureHeight / (float)rows
        );

        int yMin = textureHeight - bottomFromTop;
        int yMax = textureHeight - top;

        return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    private static SpriteMetaData CreateFootAlignedFrame(
        string name,
        RectInt rect,
        Texture2D source
    )
    {
        Color32[] pixels = source.GetPixels32();

        int minX = rect.xMax;
        int maxX = rect.xMin;
        int minY = rect.yMax;
        bool foundOpaquePixel = false;

        for (int y = rect.yMin; y < rect.yMax; y++)
        {
            int rowOffset = y * source.width;

            for (int x = rect.xMin; x < rect.xMax; x++)
            {
                if (pixels[rowOffset + x].a < 64)
                {
                    continue;
                }

                foundOpaquePixel = true;
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y);
            }
        }

        Vector2 pivot = new Vector2(0.5f, 0f);

        if (foundOpaquePixel)
        {
            pivot.x = Mathf.Clamp01(
                ((minX + maxX + 1f) * 0.5f - rect.xMin) /
                rect.width
            );
            pivot.y = Mathf.Clamp01(
                (minY + 0.5f - rect.yMin) / rect.height
            );
        }

        return new SpriteMetaData
        {
            name = name,
            rect = new Rect(rect.x, rect.y, rect.width, rect.height),
            alignment = (int)SpriteAlignment.Custom,
            pivot = pivot
        };
    }

    private static void ConfigureSingleSprite(
        string texturePath,
        float pixelsPerUnit,
        FilterMode filterMode = FilterMode.Point
    )
    {
        AssetDatabase.ImportAsset(
            texturePath,
            ImportAssetOptions.ForceUpdate
        );

        TextureImporter importer =
            AssetImporter.GetAtPath(texturePath) as TextureImporter;

        if (importer == null)
        {
            throw new InvalidOperationException(
                $"TextureImporter bulunamadı: {texturePath}"
            );
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.spritePivot = new Vector2(0.5f, 0.5f);
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = filterMode;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression =
            TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    private static Sprite LoadSprite(
        string texturePath,
        string spriteName
    )
    {
        return AssetDatabase.LoadAllAssetsAtPath(texturePath)
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name == spriteName);
    }

    private static Sprite[] LoadWalkSprites()
    {
        return HeroRunFramePaths
            .Select(
                path => LoadSprite(
                    path,
                    Path.GetFileNameWithoutExtension(path)
                )
            )
            .ToArray();
    }

    private static Sprite LoadLifeSprite()
    {
        return LoadSprite(
            UiLifeTexturePath,
            Path.GetFileNameWithoutExtension(UiLifeTexturePath)
        );
    }

    private static Sprite[] LoadJumpSprites()
    {
        string[] names =
        {
            "Hero_Jump_1_Takeoff",
            "Hero_Jump_2_Rise",
            "Hero_Jump_3_Apex",
            "Hero_Jump_4_Fall",
            "Hero_Jump_5_Impact"
        };

        return names
            .Select(name => LoadSprite(HeroJumpTexturePath, name))
            .ToArray();
    }

    private static void ValidateAssets(
        Sprite idleSprite,
        Sprite[] walkSprites,
        Sprite crouchSprite,
        Sprite[] jumpSprites,
        Sprite spikeSprite,
        Sprite doorSprite,
        Sprite lifeSprite
    )
    {
        if (
            idleSprite == null ||
            crouchSprite == null ||
            spikeSprite == null ||
            doorSprite == null ||
            lifeSprite == null ||
            walkSprites.Length != HeroRunFramePaths.Length ||
            walkSprites.Any(sprite => sprite == null) ||
            walkSprites.Distinct().Count() != walkSprites.Length ||
            jumpSprites.Length != 5 ||
            jumpSprites.Any(sprite => sprite == null) ||
            jumpSprites.Distinct().Count() != jumpSprites.Length
        )
        {
            throw new InvalidOperationException(
                "Oyun sprite importu eksik kare üretti."
            );
        }
    }

    private static void ValidateMotionAssets(
        Sprite idleSprite,
        Sprite[] walkSprites,
        Sprite crouchSprite,
        Sprite[] jumpSprites,
        Sprite spikeSprite,
        Sprite lifeSprite
    )
    {
        if (
            idleSprite == null ||
            crouchSprite == null ||
            spikeSprite == null ||
            lifeSprite == null ||
            walkSprites.Length != HeroRunFramePaths.Length ||
            walkSprites.Any(sprite => sprite == null) ||
            walkSprites.Distinct().Count() != walkSprites.Length ||
            jumpSprites.Length != 5 ||
            jumpSprites.Any(sprite => sprite == null) ||
            jumpSprites.Distinct().Count() != jumpSprites.Length
        )
        {
            throw new InvalidOperationException(
                "Motion sprite importu eksik veya tekrar eden kare üretti."
            );
        }
    }

    private static void ValidateRunAnimationAssets(
        Sprite idleSprite,
        Sprite[] runSprites,
        Sprite crouchSprite,
        Sprite[] jumpSprites
    )
    {
        if (
            idleSprite == null ||
            crouchSprite == null ||
            runSprites.Length != HeroRunFramePaths.Length ||
            runSprites.Any(sprite => sprite == null) ||
            runSprites.Distinct().Count() != runSprites.Length ||
            jumpSprites.Length != 5 ||
            jumpSprites.Any(sprite => sprite == null) ||
            jumpSprites.Distinct().Count() != jumpSprites.Length
        )
        {
            throw new InvalidOperationException(
                "Koşu animasyonu assetleri eksik veya tekrar ediyor."
            );
        }
    }

    private static void UpdateMobileUiPrefab(Sprite lifeSprite)
    {
        Sprite leftSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(UiLeftTexturePath);
        Sprite rightSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(UiRightTexturePath);
        Sprite jumpSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(UiJumpTexturePath);
        Sprite crouchSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(UiCrouchTexturePath);

        if (
            leftSprite == null ||
            rightSprite == null ||
            jumpSprite == null ||
            crouchSprite == null
        )
        {
            throw new InvalidOperationException(
                "Mobil kontrol sprite importu tamamlanamadı."
            );
        }

        GameObject root = PrefabUtility.LoadPrefabContents(
            MobileUiPrefabPath
        );

        try
        {
            ConfigureMobileButton(root, "LeftButton", leftSprite);
            ConfigureMobileButton(root, "RightButton", rightSprite);
            ConfigureMobileButton(root, "JumpButton", jumpSprite);
            ConfigureMobileButton(root, "CrouchButton", crouchSprite);

            LivesDisplay livesDisplay =
                root.GetComponentInChildren<LivesDisplay>(true);

            if (livesDisplay == null)
            {
                throw new InvalidOperationException(
                    "MobileUI içinde LivesDisplay bulunamadı."
                );
            }

            livesDisplay.Configure(
                lifeSprite,
                new Vector2(46f, 64f),
                0f,
                new Color(1f, 1f, 1f, 0.62f),
                previewLives: 3
            );

            PrefabUtility.SaveAsPrefabAsset(root, MobileUiPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureMobileButton(
        GameObject root,
        string buttonName,
        Sprite sprite
    )
    {
        Transform buttonTransform = FindDescendant(
            root.transform,
            buttonName
        );

        if (buttonTransform == null)
        {
            throw new InvalidOperationException(
                $"MobileUI butonu bulunamadı: {buttonName}"
            );
        }

        Image image = buttonTransform.GetComponent<Image>();

        if (image == null)
        {
            throw new InvalidOperationException(
                $"MobileUI butonunda Image yok: {buttonName}"
            );
        }

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = new Color(1f, 1f, 1f, 0.92f);

        RectTransform rect =
            buttonTransform.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.sizeDelta = new Vector2(172f, 172f);
        }

        TMP_Text[] oldLabels =
            buttonTransform.GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text oldLabel in oldLabels)
        {
            oldLabel.text = string.Empty;
            oldLabel.gameObject.SetActive(false);
        }
    }

    private static Transform FindDescendant(
        Transform root,
        string objectName
    )
    {
        Transform[] transforms =
            root.GetComponentsInChildren<Transform>(true);

        return transforms.FirstOrDefault(
            candidate => candidate.name == objectName
        );
    }

    private static void LoadSharedAssets()
    {
        GameObject originalPlayer =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                OriginalPlayerPrefabPath
            );

        if (originalPlayer == null)
        {
            throw new InvalidOperationException(
                $"Player prefab bulunamadı: {OriginalPlayerPrefabPath}"
            );
        }

        SpriteRenderer originalRenderer =
            originalPlayer.GetComponentInChildren<SpriteRenderer>(true);

        squareSprite = originalRenderer != null
            ? originalRenderer.sprite
            : null;

        if (squareSprite == null)
        {
            squareSprite =
                AssetDatabase.GetBuiltinExtraResource<Sprite>(
                    "UI/Skin/Background.psd"
                );
        }

        if (squareSprite == null)
        {
            throw new InvalidOperationException(
                "Platformlar için kalıcı kare sprite bulunamadı."
            );
        }

        spriteUnlitMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                SpriteUnlitMaterialPath
            );

        if (spriteUnlitMaterial == null)
        {
            Shader spriteShader = Shader.Find("Sprites/Default");

            if (spriteShader == null)
            {
                throw new InvalidOperationException(
                    "Sprites/Default shader bulunamadı."
                );
            }

            spriteUnlitMaterial = new Material(spriteShader)
            {
                name = "SpriteUnlit"
            };

            AssetDatabase.CreateAsset(
                spriteUnlitMaterial,
                SpriteUnlitMaterialPath
            );
        }

        Shader generatedSpriteShader =
            Shader.Find("RagePlatformer/GeneratedSprite");

        if (generatedSpriteShader == null)
        {
            throw new InvalidOperationException(
                "RagePlatformer/GeneratedSprite shader bulunamadı."
            );
        }

        heroWalkMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                HeroWalkMaterialPath
            );

        if (heroWalkMaterial == null)
        {
            heroWalkMaterial = spriteUnlitMaterial;
        }

        heroStateMaterial = GetOrCreateGeneratedMaterial(
            HeroStateMaterialPath,
            generatedSpriteShader,
            HeroStateTexturePath
        );

        heroJumpMaterial = GetOrCreateGeneratedMaterial(
            HeroJumpMaterialPath,
            generatedSpriteShader,
            HeroJumpTexturePath
        );

        spikeMaterial = GetOrCreateGeneratedMaterial(
            SpikeMaterialPath,
            generatedSpriteShader,
            SpikeTexturePath
        );

        doorMaterial = GetOrCreateGeneratedMaterial(
            DoorMaterialPath,
            generatedSpriteShader,
            DoorTexturePath
        );

        noFrictionMaterial =
            AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(
                NoFrictionMaterialPath
            );

        if (noFrictionMaterial == null)
        {
            noFrictionMaterial = new PhysicsMaterial2D(
                "PlayerNoFriction"
            );

            AssetDatabase.CreateAsset(
                noFrictionMaterial,
                NoFrictionMaterialPath
            );
        }

        noFrictionMaterial.friction = 0f;
        noFrictionMaterial.bounciness = 0f;
        EditorUtility.SetDirty(noFrictionMaterial);

        labelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/" +
            "LiberationSans SDF.asset"
        );
    }

    private static void CreatePlayerCharacterPrefab(
        Sprite idleSprite,
        Sprite[] walkSprites,
        Sprite crouchSprite,
        Sprite[] jumpSprites
    )
    {
        GameObject source =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                OriginalPlayerPrefabPath
            );

        GameObject instance =
            PrefabUtility.InstantiatePrefab(source) as GameObject;

        if (instance == null)
        {
            throw new InvalidOperationException(
                "PlayerCharacter kopyası oluşturulamadı."
            );
        }

        PrefabUtility.UnpackPrefabInstance(
            instance,
            PrefabUnpackMode.Completely,
            InteractionMode.AutomatedAction
        );

        instance.name = "PlayerCharacter";
        instance.transform.SetPositionAndRotation(
            Vector3.zero,
            Quaternion.identity
        );
        instance.transform.localScale = Vector3.one;

        Transform visual =
            instance.transform.Find("PlayerVisual");

        if (visual == null)
        {
            UnityEngine.Object.DestroyImmediate(instance);
            throw new InvalidOperationException(
                "PlayerVisual child nesnesi bulunamadı."
            );
        }

        /*
         * Üretilen bütün karakter karelerinin pivotu ayak tabanında.
         * Oyuncu collider'ı 1 birim yüksek ve merkezde olduğu için
         * ayak çizgisini collider tabanına sabitliyoruz.
         */
        visual.localPosition = new Vector3(0f, -0.5f, 0f);
        visual.localScale = Vector3.one;

        SpriteRenderer renderer =
            visual.GetComponent<SpriteRenderer>();
        renderer.sprite = idleSprite;
        renderer.color = Color.white;
        renderer.sharedMaterial = heroStateMaterial;
        renderer.sortingOrder = 20;

        PlayerController2D controller =
            instance.GetComponent<PlayerController2D>();

        if (controller == null)
        {
            UnityEngine.Object.DestroyImmediate(instance);
            throw new InvalidOperationException(
                "PlayerCharacter üzerinde PlayerController2D bulunamadı."
            );
        }

        controller.ConfigureCollisionShape(
            standingSize: new Vector2(0.46f, 0.92f),
            standingOffset: new Vector2(0f, -0.04f),
            crouchingSize: new Vector2(0.48f, 0.50f),
            crouchingOffset: new Vector2(0f, -0.25f),
            groundedCheckSize: new Vector2(0.32f, 0.08f),
            physicsMaterial: noFrictionMaterial
        );

        PlayerSpriteAnimator animator =
            visual.GetComponent<PlayerSpriteAnimator>();

        if (animator == null)
        {
            animator =
                visual.gameObject.AddComponent<PlayerSpriteAnimator>();
        }

        animator.Configure(
            idleSprite,
            walkSprites,
            crouchSprite,
            jumpSprites,
            heroStateMaterial,
            heroWalkMaterial,
            heroJumpMaterial,
            12f
        );

        PrefabUtility.SaveAsPrefabAsset(
            instance,
            PlayerCharacterPrefabPath
        );
        UnityEngine.Object.DestroyImmediate(instance);
    }

    private static void UpdatePlayerCharacterAnimationPrefab(
        Sprite idleSprite,
        Sprite[] runSprites,
        Sprite crouchSprite,
        Sprite[] jumpSprites,
        Material stateMaterial,
        Material runMaterial,
        Material jumpMaterial
    )
    {
        GameObject root = PrefabUtility.LoadPrefabContents(
            PlayerCharacterPrefabPath
        );

        try
        {
            Transform visual = root.transform.Find("PlayerVisual");

            if (visual == null)
            {
                throw new InvalidOperationException(
                    "PlayerCharacter içinde PlayerVisual bulunamadı."
                );
            }

            PlayerSpriteAnimator animator =
                visual.GetComponent<PlayerSpriteAnimator>();
            SpriteRenderer renderer =
                visual.GetComponent<SpriteRenderer>();

            if (animator == null || renderer == null)
            {
                throw new InvalidOperationException(
                    "PlayerVisual animatör veya SpriteRenderer içermiyor."
                );
            }

            animator.Configure(
                idleSprite,
                runSprites,
                crouchSprite,
                jumpSprites,
                stateMaterial,
                runMaterial,
                jumpMaterial,
                12f
            );

            renderer.sprite = idleSprite;
            renderer.sharedMaterial = stateMaterial;

            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(renderer);

            PrefabUtility.SaveAsPrefabAsset(
                root,
                PlayerCharacterPrefabPath
            );
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void CreateWhiteSpikePrefab(Sprite spikeSprite)
    {
        GameObject root = new GameObject("WhiteSpike");
        root.layer = HazardLayer;

        SpriteRenderer renderer =
            root.AddComponent<SpriteRenderer>();
        renderer.sprite = spikeSprite;
        renderer.color = Color.white;
        renderer.sharedMaterial = spikeMaterial;
        renderer.sortingOrder = 15;

        float[] spikeCenters =
        {
            -0.96f,
            -0.32f,
            0.32f,
            0.96f
        };

        foreach (float centerX in spikeCenters)
        {
            PolygonCollider2D collider =
                root.AddComponent<PolygonCollider2D>();
            collider.isTrigger = true;
            collider.pathCount = 1;
            collider.SetPath(
                0,
                new[]
                {
                    new Vector2(centerX - 0.19f, -0.43f),
                    new Vector2(centerX, 0.50f),
                    new Vector2(centerX + 0.19f, -0.43f)
                }
            );
        }

        root.AddComponent<Hazard>();

        PrefabUtility.SaveAsPrefabAsset(
            root,
            WhiteSpikePrefabPath
        );
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateExitDoorPrefab(Sprite doorSprite)
    {
        GameObject root = new GameObject("ExitDoor");

        SpriteRenderer renderer =
            root.AddComponent<SpriteRenderer>();
        renderer.sprite = doorSprite;
        renderer.color = Color.white;
        renderer.sharedMaterial = doorMaterial;
        renderer.sortingOrder = 12;

        BoxCollider2D collider =
            root.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1.38f, 2.55f);
        collider.offset = new Vector2(0f, -0.02f);

        LevelExit levelExit = root.AddComponent<LevelExit>();
        AudioClip doorEnterClip =
            AssetDatabase.LoadAssetAtPath<AudioClip>(
                DoorEnterAudioPath
            );

        if (doorEnterClip != null)
        {
            levelExit.ConfigureAudio(doorEnterClip);
        }

        PrefabUtility.SaveAsPrefabAsset(
            root,
            ExitDoorPrefabPath
        );
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateTestLevelScene()
    {
        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single
        );

        CreateCamera();
        CreateBackdrop();
        CreateLevelGeometry();
        CreateStartMarker();
        CreateLabels();
        CreateGameplayObjects(scene);

        EditorSceneManager.MarkSceneDirty(scene);

        if (!EditorSceneManager.SaveScene(scene, TestLevelScenePath))
        {
            throw new InvalidOperationException(
                $"TestLevel sahnesi kaydedilemedi: {TestLevelScenePath}"
            );
        }
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position =
            new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.65f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = BackdropColor;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 1000f;

        cameraObject.AddComponent<AudioListener>();
    }

    private static void CreateBackdrop()
    {
        CreateVisualBlock(
            "Backdrop",
            Vector2.zero,
            new Vector2(24f, 13f),
            BackdropColor,
            -50
        );

        CreateVisualBlock(
            "Far_Block_Left",
            new Vector2(-7.8f, 1.1f),
            new Vector2(3.4f, 4.4f),
            FarBackdropColor,
            -40
        );

        CreateVisualBlock(
            "Far_Block_Middle",
            new Vector2(-1.2f, 1.8f),
            new Vector2(4.5f, 3.1f),
            FarBackdropColor,
            -40
        );

        CreateVisualBlock(
            "Far_Block_Right",
            new Vector2(6.7f, 1.2f),
            new Vector2(4.2f, 4.1f),
            FarBackdropColor,
            -40
        );
    }

    private static void CreateLevelGeometry()
    {
        CreatePlatform(
            "Start_Ground",
            new Vector2(-7.35f, -3f),
            new Vector2(5.1f, 1f)
        );

        CreatePlatform(
            "Middle_Ground",
            new Vector2(0f, -3f),
            new Vector2(4.4f, 1f)
        );

        CreatePlatform(
            "Raised_Platform",
            new Vector2(3.65f, -1.72f),
            new Vector2(2.9f, 0.5f)
        );

        CreatePlatform(
            "Exit_Ground",
            new Vector2(7.5f, -3f),
            new Vector2(4.9f, 1f)
        );

        CreatePlatform(
            "Crouch_Tunnel_Ceiling",
            new Vector2(6.55f, -1.45f),
            new Vector2(2.75f, 0.55f)
        );

        CreateVisualBlock(
            "Pit_Accent",
            new Vector2(-3.5f, -4.5f),
            new Vector2(2.55f, 0.18f),
            AccentColor,
            -1
        );
    }

    private static void CreateStartMarker()
    {
        CreateVisualBlock(
            "Start_Post",
            new Vector2(-8.75f, -1.55f),
            new Vector2(0.12f, 1.9f),
            TextColor,
            5
        );

        CreateVisualBlock(
            "Start_Flag",
            new Vector2(-8.2f, -0.85f),
            new Vector2(1.05f, 0.48f),
            AccentColor,
            6
        );
    }

    private static void CreateLabels()
    {
        CreateWorldLabel(
            "Title",
            "RAGE DENEME PARKURU",
            new Vector2(0f, 4.35f),
            4.2f,
            TextColor,
            13f
        );

        CreateWorldLabel(
            "Controls",
            "A / D veya ← / →   •   SPACE: ZIPLA   •   S / ↓: ÇÖMEL",
            new Vector2(0f, 3.65f),
            2.1f,
            FromHex("#536073"),
            18f
        );

        CreateWorldLabel(
            "Start_Label",
            "BAŞLA",
            new Vector2(-8.2f, -0.84f),
            1.9f,
            TextColor,
            2.2f
        );

        CreateWorldLabel(
            "Crouch_Label",
            "ÇÖMEL",
            new Vector2(6.55f, -0.97f),
            1.8f,
            TextColor,
            2.3f
        );

        CreateWorldLabel(
            "Exit_Label",
            "ÇIKIŞ",
            new Vector2(8.75f, 0.55f),
            2f,
            TextColor,
            2.5f
        );
    }

    private static void CreateGameplayObjects(Scene scene)
    {
        GameObject sessionObject = new GameObject("GameSession");
        sessionObject.AddComponent<GameSession>();

        GameObject playerPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                PlayerCharacterPrefabPath
            );

        GameObject player =
            PrefabUtility.InstantiatePrefab(playerPrefab, scene)
                as GameObject;
        player.transform.position = new Vector3(-7.65f, -2f, 0f);

        GameObject spikePrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                WhiteSpikePrefabPath
            );

        GameObject pitSpikes =
            PrefabUtility.InstantiatePrefab(spikePrefab, scene)
                as GameObject;
        pitSpikes.name = "Pit_Spikes";
        pitSpikes.transform.position =
            new Vector3(-3.5f, -3.2f, 0f);
        pitSpikes.transform.localScale =
            new Vector3(0.86f, 0.86f, 1f);

        GameObject platformSpikes =
            PrefabUtility.InstantiatePrefab(spikePrefab, scene)
                as GameObject;
        platformSpikes.name = "Platform_Spikes";
        platformSpikes.transform.position =
            new Vector3(4.15f, -1.06f, 0f);
        platformSpikes.transform.localScale =
            new Vector3(0.48f, 0.48f, 1f);

        GameObject doorPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                ExitDoorPrefabPath
            );

        GameObject door =
            PrefabUtility.InstantiatePrefab(doorPrefab, scene)
                as GameObject;
        door.transform.position = new Vector3(8.75f, -1.17f, 0f);

        CreateKillZone();
        InstantiateMobileUi(scene);
    }

    private static void CreateKillZone()
    {
        GameObject killZone = new GameObject("KillZone");
        killZone.layer = HazardLayer;
        killZone.transform.position = new Vector3(0f, -6.25f, 0f);

        BoxCollider2D collider =
            killZone.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(30f, 2.5f);

        killZone.AddComponent<Hazard>();
    }

    private static void InstantiateMobileUi(Scene scene)
    {
        GameObject mobileUiPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(MobileUiPrefabPath);

        if (mobileUiPrefab != null)
        {
            PrefabUtility.InstantiatePrefab(mobileUiPrefab, scene);
        }
        else
        {
            Debug.LogWarning(
                $"Mobile UI prefab bulunamadı: {MobileUiPrefabPath}"
            );
        }
    }

    private static GameObject CreatePlatform(
        string name,
        Vector2 position,
        Vector2 size
    )
    {
        GameObject platform = CreateVisualBlock(
            name,
            position,
            size,
            PlatformColor,
            0
        );
        platform.layer = GroundLayer;

        BoxCollider2D collider =
            platform.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;

        CreateVisualBlock(
            name + "_Top",
            position + Vector2.up * (size.y * 0.5f - 0.045f),
            new Vector2(size.x, 0.09f),
            PlatformTopColor,
            2
        );

        return platform;
    }

    private static GameObject CreateVisualBlock(
        string name,
        Vector2 position,
        Vector2 size,
        Color color,
        int sortingOrder
    )
    {
        GameObject block = new GameObject(name);
        block.transform.position =
            new Vector3(position.x, position.y, 0f);
        block.transform.localScale =
            new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer =
            block.AddComponent<SpriteRenderer>();
        renderer.sprite = squareSprite;
        renderer.color = color;
        renderer.sharedMaterial = spriteUnlitMaterial;
        renderer.sortingOrder = sortingOrder;

        return block;
    }

    private static void CreateWorldLabel(
        string name,
        string content,
        Vector2 position,
        float fontSize,
        Color color,
        float width
    )
    {
        if (labelFont == null)
        {
            return;
        }

        GameObject labelObject = new GameObject(name);
        labelObject.transform.position =
            new Vector3(position.x, position.y, 0f);

        TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
        label.font = labelFont;
        label.text = content;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.rectTransform.sizeDelta = new Vector2(width, 1.2f);
        label.renderer.sortingOrder = 100;
    }

    private static void AddTestLevelSceneToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes =
            EditorBuildSettings.scenes
                .Where(scene => scene.path != TestLevelScenePath)
                .ToList();

        int levelTwoIndex =
            scenes.FindIndex(scene => scene.path == LevelTwoScenePath);

        EditorBuildSettingsScene testLevelScene =
            new EditorBuildSettingsScene(TestLevelScenePath, true);

        if (levelTwoIndex >= 0)
        {
            scenes.Insert(levelTwoIndex, testLevelScene);
        }
        else
        {
            scenes.Add(testLevelScene);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void ValidateSceneContents(Scene scene)
    {
        PlayerController2D[] players =
            UnityEngine.Object.FindObjectsByType<PlayerController2D>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        PlayerSpriteAnimator[] animators =
            UnityEngine.Object.FindObjectsByType<PlayerSpriteAnimator>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        LevelExit[] exits =
            UnityEngine.Object.FindObjectsByType<LevelExit>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        Hazard[] hazards =
            UnityEngine.Object.FindObjectsByType<Hazard>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        GameSession[] sessions =
            UnityEngine.Object.FindObjectsByType<GameSession>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        LivesDisplay[] livesDisplays =
            UnityEngine.Object.FindObjectsByType<LivesDisplay>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        Image[] uiImages =
            UnityEngine.Object.FindObjectsByType<Image>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        if (players.Length != 1)
        {
            throw new InvalidOperationException(
                $"TestLevel sahnesinde 1 Player bekleniyordu; " +
                $"bulunan: {players.Length}."
            );
        }

        if (animators.Length != 1)
        {
            throw new InvalidOperationException(
                $"TestLevel sahnesinde 1 sprite animator bekleniyordu; " +
                $"bulunan: {animators.Length}."
            );
        }

        if (
            animators[0].WalkFrameCount != HeroRunFramePaths.Length
        )
        {
            throw new InvalidOperationException(
                $"Koşu animasyonunda {HeroRunFramePaths.Length} " +
                $"kare bekleniyordu; " +
                $"bulunan: {animators[0].WalkFrameCount}."
            );
        }

        if (animators[0].JumpFrameCount != 5)
        {
            throw new InvalidOperationException(
                $"Zıplama animasyonunda 5 kare bekleniyordu; " +
                $"bulunan: {animators[0].JumpFrameCount}."
            );
        }

        BoxCollider2D playerCollider =
            players[0].GetComponent<BoxCollider2D>();

        if (
            playerCollider == null ||
            playerCollider.size.x > 0.47f ||
            playerCollider.size.y > 0.93f
        )
        {
            throw new InvalidOperationException(
                "Oyuncu collider'ı görsele yakın ölçüde değil."
            );
        }

        if (
            playerCollider.sharedMaterial == null ||
            playerCollider.sharedMaterial.friction > 0.001f
        )
        {
            throw new InvalidOperationException(
                "Oyuncunun duvar sürtünmesini kaldıran fizik materyali eksik."
            );
        }

        if (exits.Length != 1)
        {
            throw new InvalidOperationException(
                $"TestLevel sahnesinde 1 çıkış bekleniyordu; " +
                $"bulunan: {exits.Length}."
            );
        }

        if (hazards.Length < 3)
        {
            throw new InvalidOperationException(
                $"TestLevel sahnesinde en az 3 hazard bekleniyordu; " +
                $"bulunan: {hazards.Length}."
            );
        }

        int spikeTipColliderCount = hazards
            .SelectMany(
                hazard => hazard.GetComponents<PolygonCollider2D>()
            )
            .Count();

        if (spikeTipColliderCount < 8)
        {
            throw new InvalidOperationException(
                $"Spike uçlarında en az 8 ayrı polygon collider " +
                $"bekleniyordu; bulunan: {spikeTipColliderCount}."
            );
        }

        if (sessions.Length != 1)
        {
            throw new InvalidOperationException(
                $"TestLevel sahnesinde 1 GameSession bekleniyordu; " +
                $"bulunan: {sessions.Length}."
            );
        }

        if (livesDisplays.Length != 1)
        {
            throw new InvalidOperationException(
                $"TestLevel sahnesinde 1 can göstergesi bekleniyordu; " +
                $"bulunan: {livesDisplays.Length}."
            );
        }

        TMP_Text oldLivesText =
            livesDisplays[0].GetComponent<TMP_Text>();

        if (
            oldLivesText == null ||
            oldLivesText.enabled ||
            !string.IsNullOrEmpty(oldLivesText.text)
        )
        {
            throw new InvalidOperationException(
                "Eski LIVES metni tamamen kapatılmadı."
            );
        }

        int lifeIconCount = livesDisplays[0]
            .GetComponentsInChildren<Image>(true)
            .Count(image => image.name.StartsWith("LifeIcon_"));

        if (lifeIconCount < 3)
        {
            throw new InvalidOperationException(
                $"En az 3 can ikonu bekleniyordu; bulunan: " +
                $"{lifeIconCount}."
            );
        }

        string[] controlNames =
        {
            "LeftButton",
            "RightButton",
            "JumpButton",
            "CrouchButton"
        };

        foreach (string controlName in controlNames)
        {
            Image controlImage = uiImages.FirstOrDefault(
                image => image.name == controlName
            );

            if (controlImage == null || controlImage.sprite == null)
            {
                throw new InvalidOperationException(
                    $"Mobil kontrol görseli eksik: {controlName}"
                );
            }
        }

        int missingScriptCount = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms =
                root.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in transforms)
            {
                missingScriptCount +=
                    GameObjectUtility
                        .GetMonoBehavioursWithMissingScriptCount(
                            child.gameObject
                        );
            }
        }

        if (missingScriptCount > 0)
        {
            throw new InvalidOperationException(
                $"TestLevel sahnesinde {missingScriptCount} eksik script var."
            );
        }

        int testLevelIndex = Array.FindIndex(
            EditorBuildSettings.scenes,
            buildScene => buildScene.path == TestLevelScenePath
        );

        if (
            testLevelIndex < 0 ||
            testLevelIndex + 1 >= EditorBuildSettings.scenes.Length ||
            EditorBuildSettings.scenes[testLevelIndex + 1].path !=
                LevelTwoScenePath
        )
        {
            throw new InvalidOperationException(
                "TestLevel çıkışının Level_02'ye gideceği Scene List " +
                "sırası doğrulanamadı."
            );
        }
    }

    private static void RenderScenePreview(string previewPath)
    {
        Camera camera = Camera.main;

        if (camera == null)
        {
            throw new InvalidOperationException(
                "TestLevel preview için Main Camera bulunamadı."
            );
        }

        const int width = 1600;
        const int height = 900;

        Canvas[] canvases =
            UnityEngine.Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        RenderMode[] originalRenderModes =
            new RenderMode[canvases.Length];
        Camera[] originalWorldCameras =
            new Camera[canvases.Length];
        float[] originalPlaneDistances =
            new float[canvases.Length];

        string[] previewHiddenNames =
        {
            "GameOverPanel",
            "InteractionMessagePanel"
        };
        GameObject[] previewHiddenObjects =
            new GameObject[previewHiddenNames.Length];
        bool[] previewHiddenStates =
            new bool[previewHiddenNames.Length];

        Transform[] sceneTransforms =
            UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int index = 0; index < previewHiddenNames.Length; index++)
        {
            Transform target = sceneTransforms.FirstOrDefault(
                item => item.name == previewHiddenNames[index]
            );

            if (target == null)
            {
                continue;
            }

            previewHiddenObjects[index] = target.gameObject;
            previewHiddenStates[index] = target.gameObject.activeSelf;
            target.gameObject.SetActive(false);
        }

        for (int index = 0; index < canvases.Length; index++)
        {
            Canvas canvas = canvases[index];
            originalRenderModes[index] = canvas.renderMode;
            originalWorldCameras[index] = canvas.worldCamera;
            originalPlaneDistances[index] = canvas.planeDistance;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }
        }

        RenderTexture renderTexture = new RenderTexture(
            width,
            height,
            24,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB
        );
        renderTexture.Create();

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;

        Texture2D previewTexture = new Texture2D(
            width,
            height,
            TextureFormat.RGB24,
            false
        );

        try
        {
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;

            previewTexture.ReadPixels(
                new Rect(0f, 0f, width, height),
                0,
                0
            );
            previewTexture.Apply();

            File.WriteAllBytes(
                previewPath,
                previewTexture.EncodeToPNG()
            );
        }
        finally
        {
            for (
                int index = 0;
                index < previewHiddenObjects.Length;
                index++
            )
            {
                if (previewHiddenObjects[index] != null)
                {
                    previewHiddenObjects[index].SetActive(
                        previewHiddenStates[index]
                    );
                }
            }

            for (int index = 0; index < canvases.Length; index++)
            {
                canvases[index].renderMode =
                    originalRenderModes[index];
                canvases[index].worldCamera =
                    originalWorldCameras[index];
                canvases[index].planeDistance =
                    originalPlaneDistances[index];
            }

            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            renderTexture.Release();

            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(previewTexture);
        }
    }

    private static Color FromHex(string htmlColor)
    {
        if (ColorUtility.TryParseHtmlString(htmlColor, out Color color))
        {
            return color;
        }

        return Color.white;
    }

    private static Material GetOrCreateGeneratedMaterial(
        string materialPath,
        Shader shader,
        string texturePath
    )
    {
        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(materialPath);

        if (material == null)
        {
            material = new Material(shader)
            {
                name = Path.GetFileNameWithoutExtension(materialPath)
            };

            AssetDatabase.CreateAsset(material, materialPath);
        }
        else
        {
            material.shader = shader;
        }

        Texture2D texture =
            AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

        if (texture == null)
        {
            throw new InvalidOperationException(
                $"Generated texture bulunamadı: {texturePath}"
            );
        }

        material.SetTexture("_ArtTex", texture);
        material.SetColor("_Color", Color.white);
        EditorUtility.SetDirty(material);

        return material;
    }
}
