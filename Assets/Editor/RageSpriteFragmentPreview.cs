using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RageSpriteFragmentPreview
{
    private const int PreviewSize = 720;

    private const string PlayerPrefabPath =
        "Assets/Prefabs/Player/PlayerCharacter.prefab";

    private const string FragmentMaterialPath =
        "Assets/Materials/Effects/SpriteFragment.mat";

    private const string RunSpritePath =
        "Assets/Sprites/Player/Animations/Run/run_01_contact_a.png";

    private const string RunMaterialPath =
        "Assets/Materials/Player/HeroWalk.mat";

    public static void CapturePreview()
    {
        if (!Application.isBatchMode)
        {
            throw new InvalidOperationException(
                "This QA capture is intended for batch mode only."
            );
        }

        string outputDirectory = Path.GetFullPath(
            Path.Combine(
                Application.dataPath,
                "../Logs/DeathFragmentPreview"
            )
        );
        Directory.CreateDirectory(outputDirectory);

        Material fragmentMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                FragmentMaterialPath
            );

        if (fragmentMaterial == null)
        {
            throw new InvalidOperationException(
                "Sprite fragment material is missing."
            );
        }

        PreviewResult idle = CaptureSeries(
            "idle",
            outputDirectory,
            fragmentMaterial,
            null,
            null,
            false
        );

        Sprite runSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            RunSpritePath
        );
        Material runMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(RunMaterialPath);

        if (runSprite == null || runMaterial == null)
        {
            throw new InvalidOperationException(
                "Run preview sprite or material is missing."
            );
        }

        PreviewResult run = CaptureSeries(
            "run_flipped",
            outputDirectory,
            fragmentMaterial,
            runSprite,
            runMaterial,
            true
        );

        Debug.Log(
            "SPRITE_FRAGMENT_PREVIEW_OK: " +
            $"idle initial mismatch {idle.MismatchPercent:F3}%, " +
            $"flipped run initial mismatch " +
            $"{run.MismatchPercent:F3}%, " +
            $"20 moving pieces per pose. Output: " +
            outputDirectory
        );
    }

    private static PreviewResult CaptureSeries(
        string label,
        string outputDirectory,
        Material fragmentMaterial,
        Sprite overrideSprite,
        Material overrideMaterial,
        bool flipX
    )
    {
        Scene previewScene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single
        );

        try
        {
            GameObject playerPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PlayerPrefabPath
                );

            if (playerPrefab == null)
            {
                throw new InvalidOperationException(
                    "Player character prefab is missing."
                );
            }

            GameObject player =
                (GameObject)PrefabUtility.InstantiatePrefab(
                    playerPrefab,
                    previewScene
                );
            player.transform.SetPositionAndRotation(
                Vector3.zero,
                Quaternion.identity
            );

            SpriteRenderer renderer = FindPrimaryRenderer(player);

            if (overrideSprite != null)
            {
                renderer.sprite = overrideSprite;
            }

            if (overrideMaterial != null)
            {
                renderer.sharedMaterial = overrideMaterial;
            }

            renderer.flipX = flipX;

            // Use the same unlit sampling path for the reference image.
            // This isolates the actual question under test: whether all
            // clipped cells rebuild the same sprite without gaps, shifts
            // or flip errors before they begin moving.
            renderer.sharedMaterial = fragmentMaterial;
            Bounds sourceBounds = renderer.bounds;

            GameObject cameraObject = new GameObject("Preview Camera");
            SceneManager.MoveGameObjectToScene(
                cameraObject,
                previewScene
            );
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 3.25f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(
                0.035f,
                0.045f,
                0.075f,
                1f
            );
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.transform.position = new Vector3(
                sourceBounds.center.x,
                sourceBounds.center.y + 0.15f,
                -10f
            );

            // The first off-screen render can be consumed by shader
            // compilation on a fresh batch-mode graphics device.
            CaptureFrame(
                camera,
                Path.Combine(
                    outputDirectory,
                    $"{label}_warmup.png"
                )
            );

            Color32[] baseline = CaptureFrame(
                camera,
                Path.Combine(
                    outputDirectory,
                    $"{label}_baseline.png"
                )
            );

            SpriteFragmentBurst burst = SpriteFragmentBurst.Create(
                renderer,
                fragmentMaterial,
                Vector2.zero,
                1.25f,
                2.85f,
                0.9f,
                5.8f,
                0.28f,
                390f,
                0.72f,
                0.48f
            );

            if (burst == null || burst.FragmentCount != 20)
            {
                throw new InvalidOperationException(
                    "Fragment burst did not create exactly 20 pieces."
                );
            }

            renderer.enabled = false;

            Color32[] reconstructed = CaptureFrame(
                camera,
                Path.Combine(
                    outputDirectory,
                    $"{label}_fragments_000.png"
                )
            );

            float mismatchPercent = CalculateMismatchPercent(
                baseline,
                reconstructed
            );

            if (mismatchPercent > 1.5f)
            {
                throw new InvalidOperationException(
                    $"{label} fragments do not reconstruct the " +
                    $"source sprite. Mismatch: " +
                    $"{mismatchPercent:F3}%."
                );
            }

            burst.Simulate(0.18f);
            CaptureFrame(
                camera,
                Path.Combine(
                    outputDirectory,
                    $"{label}_fragments_180.png"
                )
            );

            burst.Simulate(0.24f);
            CaptureFrame(
                camera,
                Path.Combine(
                    outputDirectory,
                    $"{label}_fragments_420.png"
                )
            );

            burst.Simulate(0.24f);
            CaptureFrame(
                camera,
                Path.Combine(
                    outputDirectory,
                    $"{label}_fragments_660.png"
                )
            );

            return new PreviewResult(mismatchPercent);
        }
        finally
        {
            if (previewScene.IsValid())
            {
                foreach (
                    GameObject root in
                    previewScene.GetRootGameObjects()
                )
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }
    }

    private static SpriteRenderer FindPrimaryRenderer(GameObject root)
    {
        SpriteRenderer[] renderers =
            root.GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer renderer in renderers)
        {
            if (
                renderer != null &&
                renderer.enabled &&
                renderer.sprite != null
            )
            {
                return renderer;
            }
        }

        throw new InvalidOperationException(
            "No visible player SpriteRenderer was found."
        );
    }

    private static Color32[] CaptureFrame(
        Camera camera,
        string outputPath
    )
    {
        RenderTexture renderTexture = new RenderTexture(
            PreviewSize,
            PreviewSize,
            24,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB
        );
        renderTexture.antiAliasing = 1;
        renderTexture.Create();

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        Texture2D image = null;

        try
        {
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;

            image = new Texture2D(
                PreviewSize,
                PreviewSize,
                TextureFormat.RGBA32,
                false,
                false
            );
            image.ReadPixels(
                new Rect(0f, 0f, PreviewSize, PreviewSize),
                0,
                0,
                false
            );
            image.Apply(false, false);

            File.WriteAllBytes(outputPath, image.EncodeToPNG());
            return image.GetPixels32();
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;

            if (image != null)
            {
                UnityEngine.Object.DestroyImmediate(image);
            }

            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
        }
    }

    private static float CalculateMismatchPercent(
        Color32[] baseline,
        Color32[] reconstructed
    )
    {
        if (
            baseline == null ||
            reconstructed == null ||
            baseline.Length != reconstructed.Length ||
            baseline.Length == 0
        )
        {
            return 100f;
        }

        Color32 background = baseline[0];
        int visiblePixelCount = 0;
        int mismatchedPixelCount = 0;

        for (int index = 0; index < baseline.Length; index++)
        {
            Color32 source = baseline[index];
            Color32 rebuilt = reconstructed[index];

            bool visible =
                MaximumDifference(source, background) > 3 ||
                MaximumDifference(rebuilt, background) > 3;

            if (!visible)
            {
                continue;
            }

            visiblePixelCount++;

            if (MaximumDifference(source, rebuilt) > 3)
            {
                mismatchedPixelCount++;
            }
        }

        if (visiblePixelCount == 0)
        {
            return 100f;
        }

        return 100f * mismatchedPixelCount / visiblePixelCount;
    }

    private static int MaximumDifference(Color32 left, Color32 right)
    {
        return Mathf.Max(
            Mathf.Abs(left.r - right.r),
            Mathf.Abs(left.g - right.g),
            Mathf.Abs(left.b - right.b),
            Mathf.Abs(left.a - right.a)
        );
    }

    private readonly struct PreviewResult
    {
        public PreviewResult(float mismatchPercent)
        {
            MismatchPercent = mismatchPercent;
        }

        public float MismatchPercent { get; }
    }
}
