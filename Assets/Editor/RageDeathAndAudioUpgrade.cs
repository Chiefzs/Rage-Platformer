using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class RageDeathAndAudioUpgrade
{
    private const int SampleRate = 44100;

    private const string LifeLostClipPath =
        "Assets/Audio/SFX/sfx_life_lost.wav";

    private const string DoorEnterClipPath =
        "Assets/Audio/SFX/sfx_door_enter.wav";

    private const string FragmentMaterialPath =
        "Assets/Materials/Effects/SpriteFragment.mat";

    private const string FragmentShaderName =
        "RagePlatformer/SpriteFragment";

    // Kept only for the legacy particle-authoring helpers below. The new
    // death presentation never instantiates or modifies this prefab.
    private const string DeathEffectPrefabPath =
        "Assets/Prefabs/Effects/PlayerExplosion.prefab";

    private static readonly string[] PlayerPrefabPaths =
    {
        "Assets/Prefabs/Player/Player.prefab",
        "Assets/Prefabs/Player/PlayerCharacter.prefab"
    };

    private static readonly string[] ExitPrefabPaths =
    {
        "Assets/Prefabs/LevelObjects/LevelExit.prefab",
        "Assets/Prefabs/LevelObjects/ExitDoor.prefab"
    };

    [MenuItem(
        "Tools/Rage Platformer/Upgrade Death And Audio " +
        "(Preserve Scenes)"
    )]
    public static void UpgradeDeathAndAudioPreservingScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException(
                "Exit Play Mode before upgrading death assets."
            );
        }

        AudioClip lifeLostClip = LoadRequiredClip(LifeLostClipPath);
        Material fragmentMaterial = EnsureFragmentMaterial();

        foreach (string prefabPath in PlayerPrefabPaths)
        {
            ConfigurePlayerPrefab(
                prefabPath,
                fragmentMaterial,
                lifeLostClip
            );
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ValidateUpgrade();

        Debug.Log(
            "SPRITE_FRAGMENT_DEATH_UPGRADE_OK: the current player " +
            "sprite now breaks into moving pieces; existing SFX " +
            "and scene files were preserved " +
            "without opening or saving a scene."
        );
    }

    [MenuItem("Tools/Rage Platformer/Validate Death And Audio")]
    public static void ValidateUpgrade()
    {
        AudioClip lifeLostClip = LoadRequiredClip(LifeLostClipPath);
        AudioClip doorEnterClip = LoadRequiredClip(DoorEnterClipPath);

        ValidateClip(lifeLostClip, 0.50f, 0.62f, "life lost");
        ValidateClip(doorEnterClip, 0.46f, 0.58f, "door enter");

        Material fragmentMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                FragmentMaterialPath
            );

        if (
            fragmentMaterial == null ||
            fragmentMaterial.shader == null ||
            fragmentMaterial.shader.name != FragmentShaderName
        )
        {
            throw new InvalidOperationException(
                "Sprite fragment material is missing or invalid."
            );
        }

        foreach (string prefabPath in PlayerPrefabPaths)
        {
            GameObject player =
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            PlayerDeathController death =
                player != null
                    ? player.GetComponent<PlayerDeathController>()
                    : null;

            if (
                death == null ||
                death.LifeLostClip != lifeLostClip ||
                death.FragmentMaterial != fragmentMaterial ||
                Mathf.Abs(death.DeathSequenceDuration - 0.82f) >
                    0.001f ||
                Mathf.Abs(death.LifeLostVolume - 0.88f) > 0.001f ||
                Mathf.Abs(death.FragmentHoldDuration - 0.035f) >
                    0.001f ||
                Mathf.Abs(death.FragmentLifetime - 0.72f) >
                    0.001f ||
                Mathf.Abs(death.FragmentFadeStart - 0.48f) >
                    0.001f
            )
            {
                throw new InvalidOperationException(
                    $"Death presentation is not configured on " +
                    $"{prefabPath}."
                );
            }
        }

        foreach (string prefabPath in ExitPrefabPaths)
        {
            GameObject exit =
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            LevelExit levelExit =
                exit != null ? exit.GetComponent<LevelExit>() : null;

            if (
                levelExit == null ||
                levelExit.DoorEnterClip != doorEnterClip ||
                Mathf.Abs(levelExit.DoorEnterVolume - 0.82f) >
                    0.001f
            )
            {
                throw new InvalidOperationException(
                    $"Door audio is not configured on {prefabPath}."
                );
            }
        }

        Debug.Log(
            "SPRITE_FRAGMENT_DEATH_VALIDATION_OK: fragment material, " +
            "both player prefabs, existing SFX and exit references " +
            "verified."
        );
    }

    private static void GenerateAudioClips()
    {
        string absoluteSfxDirectory = Path.GetFullPath(
            Path.Combine(Application.dataPath, "Audio/SFX")
        );

        Directory.CreateDirectory(absoluteSfxDirectory);

        WriteMonoPcm16Wave(
            Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "Audio/SFX/sfx_life_lost.wav"
                )
            ),
            GenerateLifeLostSamples()
        );

        WriteMonoPcm16Wave(
            Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "Audio/SFX/sfx_door_enter.wav"
                )
            ),
            GenerateDoorEnterSamples()
        );

        AssetDatabase.Refresh(
            ImportAssetOptions.ForceSynchronousImport |
            ImportAssetOptions.ForceUpdate
        );

        ConfigureAudioImporter(LifeLostClipPath);
        ConfigureAudioImporter(DoorEnterClipPath);
    }

    private static float[] GenerateLifeLostSamples()
    {
        const float duration = 0.56f;
        int sampleCount = Mathf.RoundToInt(SampleRate * duration);
        float[] samples = new float[sampleCount];
        System.Random random = new System.Random(71423);

        double mainPhase = 0d;
        double lowPhase = 0d;
        double echoPhase = 0d;
        double smoothedNoise = 0d;

        for (int index = 0; index < sampleCount; index++)
        {
            double time = (double)index / SampleRate;
            double progress = time / duration;

            double mainFrequency =
                570d * Math.Pow(0.255d, time / 0.44d);
            double lowFrequency =
                112d * Math.Pow(0.46d, time / duration);

            mainPhase += 2d * Math.PI * mainFrequency / SampleRate;
            lowPhase += 2d * Math.PI * lowFrequency / SampleRate;
            echoPhase += 2d * Math.PI *
                (325d * Math.Pow(0.54d, time / duration)) /
                SampleRate;

            double rawNoise = (random.NextDouble() * 2d) - 1d;
            smoothedNoise =
                (smoothedNoise * 0.72d) + (rawNoise * 0.28d);

            double attack = Math.Min(1d, time / 0.004d);
            double mainEnvelope = attack * Math.Exp(-time * 6.8d);
            double lowEnvelope = attack * Math.Exp(-time * 7.8d);
            double crackEnvelope = Math.Exp(-time * 48d);

            double mainTone = Math.Sin(mainPhase) * 0.52d;
            double digitalEdge =
                Math.Sign(Math.Sin(mainPhase)) * 0.075d;
            double lowThud = Math.Sin(lowPhase) * 0.34d;
            double echoAttack = Math.Clamp(
                (time - 0.085d) / 0.008d,
                0d,
                1d
            );
            double echo = Math.Sin(echoPhase) *
                Math.Exp(-Math.Max(0d, time - 0.085d) * 12d) *
                echoAttack * 0.15d;
            double crack = smoothedNoise * crackEnvelope * 0.44d;

            double endFade = SmoothEndFade(progress, 0.84d);
            double globalAttack = SmoothAttack(time, 0.002d);
            double mixed = (
                ((mainTone + digitalEdge) * mainEnvelope) +
                (lowThud * lowEnvelope) +
                echo +
                crack
            ) * endFade * globalAttack;

            samples[index] = (float)Math.Tanh(mixed * 1.22d);
        }

        Normalize(samples, 0.88f);
        return samples;
    }

    private static float[] GenerateDoorEnterSamples()
    {
        const float duration = 0.52f;
        int sampleCount = Mathf.RoundToInt(SampleRate * duration);
        float[] samples = new float[sampleCount];
        System.Random random = new System.Random(31907);

        double latchPhase = 0d;
        double bodyPhase = 0d;
        double passagePhase = 0d;
        double smoothedNoise = 0d;
        double previousNoise = 0d;

        for (int index = 0; index < sampleCount; index++)
        {
            double time = (double)index / SampleRate;
            double progress = time / duration;

            latchPhase += 2d * Math.PI * 920d / SampleRate;
            bodyPhase += 2d * Math.PI *
                (142d - (54d * progress)) /
                SampleRate;

            double passageFrequency = 245d + (365d * progress);
            passagePhase += 2d * Math.PI *
                passageFrequency / SampleRate;

            double rawNoise = (random.NextDouble() * 2d) - 1d;
            smoothedNoise =
                (smoothedNoise * 0.91d) + (rawNoise * 0.09d);
            double highNoise = rawNoise - previousNoise;
            previousNoise = rawNoise;

            double latchEnvelope = Math.Exp(-time * 70d);
            double latch = (
                (Math.Sin(latchPhase) * 0.22d) +
                (highNoise * 0.15d)
            ) * latchEnvelope;

            double body = Math.Sin(bodyPhase) *
                Math.Exp(-time * 13d) * 0.34d;

            double whooshProgress =
                Math.Clamp((time - 0.035d) / 0.34d, 0d, 1d);
            double whooshEnvelope =
                Math.Sin(Math.PI * whooshProgress);
            whooshEnvelope *= whooshEnvelope;
            double whoosh = smoothedNoise * whooshEnvelope * 0.27d;

            double passageAttack = Math.Clamp(
                (time - 0.055d) / 0.045d,
                0d,
                1d
            );
            double passageRelease = Math.Exp(
                -Math.Max(0d, time - 0.23d) * 10d
            );
            double passage = (
                Math.Sin(passagePhase) +
                (0.28d * Math.Sin(passagePhase * 2d))
            ) * passageAttack * passageRelease * 0.18d;

            double endFade = SmoothEndFade(progress, 0.80d);
            double globalAttack = SmoothAttack(time, 0.002d);
            double mixed = (
                latch + body + whoosh + passage
            ) * endFade * globalAttack;

            samples[index] = (float)Math.Tanh(mixed * 1.18d);
        }

        Normalize(samples, 0.84f);
        return samples;
    }

    private static double SmoothEndFade(
        double progress,
        double fadeStart
    )
    {
        if (progress <= fadeStart)
        {
            return 1d;
        }

        double fade = Math.Clamp(
            (progress - fadeStart) / (1d - fadeStart),
            0d,
            1d
        );

        return 1d - (fade * fade * (3d - (2d * fade)));
    }

    private static double SmoothAttack(double time, double duration)
    {
        double progress = Math.Clamp(time / duration, 0d, 1d);
        return progress * progress * (3d - (2d * progress));
    }

    private static void Normalize(float[] samples, float targetPeak)
    {
        float peak = 0f;

        foreach (float sample in samples)
        {
            peak = Mathf.Max(peak, Mathf.Abs(sample));
        }

        if (peak <= 0.00001f)
        {
            return;
        }

        float gain = targetPeak / peak;

        for (int index = 0; index < samples.Length; index++)
        {
            samples[index] = Mathf.Clamp(
                samples[index] * gain,
                -1f,
                1f
            );
        }
    }

    private static void WriteMonoPcm16Wave(
        string absolutePath,
        float[] samples
    )
    {
        using FileStream stream = new FileStream(
            absolutePath,
            FileMode.Create,
            FileAccess.Write
        );
        using BinaryWriter writer = new BinaryWriter(stream);

        const short channelCount = 1;
        const short bitsPerSample = 16;
        short blockAlign = (short)(
            channelCount * (bitsPerSample / 8)
        );
        int byteRate = SampleRate * blockAlign;
        int dataSize = samples.Length * blockAlign;

        writer.Write(new byte[]
        {
            (byte)'R', (byte)'I', (byte)'F', (byte)'F'
        });
        writer.Write(36 + dataSize);
        writer.Write(new byte[]
        {
            (byte)'W', (byte)'A', (byte)'V', (byte)'E'
        });
        writer.Write(new byte[]
        {
            (byte)'f', (byte)'m', (byte)'t', (byte)' '
        });
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channelCount);
        writer.Write(SampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write(new byte[]
        {
            (byte)'d', (byte)'a', (byte)'t', (byte)'a'
        });
        writer.Write(dataSize);

        foreach (float sample in samples)
        {
            writer.Write((short)Mathf.RoundToInt(
                Mathf.Clamp(sample, -1f, 1f) * short.MaxValue
            ));
        }
    }

    private static void ConfigureAudioImporter(string assetPath)
    {
        AudioImporter importer =
            AssetImporter.GetAtPath(assetPath) as AudioImporter;

        if (importer == null)
        {
            throw new InvalidOperationException(
                $"Audio importer could not be loaded: {assetPath}"
            );
        }

        importer.forceToMono = true;
        importer.loadInBackground = false;

        AudioImporterSampleSettings settings =
            importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        settings.sampleRateSetting =
            AudioSampleRateSetting.PreserveSampleRate;
        settings.preloadAudioData = true;
        settings.quality = 1f;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
    }

    private static AudioClip LoadRequiredClip(string assetPath)
    {
        AudioClip clip =
            AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);

        if (clip == null)
        {
            throw new InvalidOperationException(
                $"Audio clip could not be loaded: {assetPath}"
            );
        }

        return clip;
    }

    private static void ValidateClip(
        AudioClip clip,
        float minimumLength,
        float maximumLength,
        string label
    )
    {
        if (
            clip.channels != 1 ||
            clip.frequency != SampleRate ||
            clip.length < minimumLength ||
            clip.length > maximumLength
        )
        {
            throw new InvalidOperationException(
                $"Invalid {label} clip: {clip.channels} channel(s), " +
                $"{clip.frequency} Hz, {clip.length:F3} seconds."
            );
        }
    }

    private static void ConfigurePlayerPrefab(
        string prefabPath,
        Material fragmentMaterial,
        AudioClip lifeLostClip
    )
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            PlayerDeathController death =
                root.GetComponent<PlayerDeathController>();

            if (death == null)
            {
                throw new InvalidOperationException(
                    $"PlayerDeathController is missing on {prefabPath}."
                );
            }

            death.ConfigurePresentation(
                lifeLostClip,
                fragmentMaterial,
                0.82f
            );
            EditorUtility.SetDirty(death);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Material EnsureFragmentMaterial()
    {
        Shader shader = Shader.Find(FragmentShaderName);

        if (shader == null)
        {
            throw new InvalidOperationException(
                $"Shader could not be found: {FragmentShaderName}"
            );
        }

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                FragmentMaterialPath
            );

        if (material == null)
        {
            material = new Material(shader)
            {
                name = "Rage Sprite Fragment"
            };
            material.color = Color.white;
            AssetDatabase.CreateAsset(material, FragmentMaterialPath);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
            material.color = Color.white;
            EditorUtility.SetDirty(material);
        }

        return material;
    }

    private static void ConfigureExitPrefab(
        string prefabPath,
        AudioClip doorEnterClip
    )
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            LevelExit levelExit = root.GetComponent<LevelExit>();

            if (levelExit == null)
            {
                throw new InvalidOperationException(
                    $"LevelExit is missing on {prefabPath}."
                );
            }

            levelExit.ConfigureAudio(doorEnterClip, 0.82f);
            EditorUtility.SetDirty(levelExit);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureDeathEffectPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(
            DeathEffectPrefabPath
        );

        try
        {
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            for (int index = root.transform.childCount - 1;
                index >= 0;
                index--)
            {
                Transform child = root.transform.GetChild(index);

                if (child.name.StartsWith("DeathVfx_"))
                {
                    UnityEngine.Object.DestroyImmediate(
                        child.gameObject
                    );
                }
            }

            ParticleSystem shards =
                root.GetComponent<ParticleSystem>();
            ParticleSystemRenderer shardsRenderer =
                root.GetComponent<ParticleSystemRenderer>();

            if (shards == null || shardsRenderer == null)
            {
                throw new InvalidOperationException(
                    "PlayerExplosion needs a ParticleSystem and renderer."
                );
            }

            Material particleMaterial = shardsRenderer.sharedMaterial;

            ConfigureShards(shards, shardsRenderer, particleMaterial);

            CreateLayer(
                root.transform,
                "DeathVfx_CoreFlash",
                particleMaterial,
                ConfigureCoreFlash
            );
            CreateLayer(
                root.transform,
                "DeathVfx_ImpactRing",
                particleMaterial,
                ConfigureImpactRing
            );
            CreateLayer(
                root.transform,
                "DeathVfx_Embers",
                particleMaterial,
                ConfigureEmbers
            );

            PrefabUtility.SaveAsPrefabAsset(
                root,
                DeathEffectPrefabPath
            );
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void CreateLayer(
        Transform parent,
        string name,
        Material material,
        Action<ParticleSystem, ParticleSystemRenderer, Material>
            configure
    )
    {
        GameObject layer = new GameObject(name);
        layer.transform.SetParent(parent, false);

        ParticleSystem system =
            layer.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer =
            layer.GetComponent<ParticleSystemRenderer>();

        configure(system, renderer, material);
    }

    private static void ConfigureCommon(
        ParticleSystem system,
        ParticleSystemRenderer renderer,
        Material material,
        float duration,
        ParticleSystem.MinMaxCurve lifetime,
        ParticleSystem.MinMaxCurve speed,
        ParticleSystem.MinMaxCurve size,
        int burstCount,
        int sortingOrder
    )
    {
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        system.Clear(true);

        ParticleSystem.MainModule main = system.main;
        main.duration = duration;
        main.loop = false;
        main.prewarm = false;
        main.playOnAwake = true;
        main.startDelay = 0f;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = Color.white;
        main.startRotation = new ParticleSystem.MinMaxCurve(
            -Mathf.PI,
            Mathf.PI
        );
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.useUnscaledTime = true;
        main.maxParticles = Mathf.Max(32, burstCount * 2);
        main.stopAction = ParticleSystemStopAction.None;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)burstCount)
        });

        renderer.sharedMaterial = material;
        renderer.sortingOrder = sortingOrder;
        renderer.minParticleSize = 0f;
        renderer.maxParticleSize = 2f;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private static void ConfigureShards(
        ParticleSystem system,
        ParticleSystemRenderer renderer,
        Material material
    )
    {
        ConfigureCommon(
            system,
            renderer,
            material,
            0.56f,
            new ParticleSystem.MinMaxCurve(0.30f, 0.46f),
            new ParticleSystem.MinMaxCurve(1.35f, 2.85f),
            new ParticleSystem.MinMaxCurve(0.045f, 0.115f),
            16,
            36
        );

        ParticleSystem.MainModule main = system.main;
        main.gravityModifier = 0.62f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.07f;
        shape.radiusThickness = 1f;

        SetColorOverLifetime(
            system,
            new Color(1f, 0.13f, 0.07f),
            new Color(0.82f, 0.015f, 0.025f),
            new Color(0.22f, 0f, 0.01f)
        );
        SetSizeOverLifetime(
            system,
            new AnimationCurve(
                new Keyframe(0f, 0.72f),
                new Keyframe(0.10f, 1f),
                new Keyframe(0.70f, 0.68f),
                new Keyframe(1f, 0.08f)
            )
        );

        ParticleSystem.RotationOverLifetimeModule rotation =
            system.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-5.2f, 5.2f);

        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;
        noise.strength = 0.11f;
        noise.frequency = 1.2f;
        noise.damping = true;

        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.035f;
        renderer.lengthScale = 0.72f;
    }

    private static void ConfigureCoreFlash(
        ParticleSystem system,
        ParticleSystemRenderer renderer,
        Material material
    )
    {
        ConfigureCommon(
            system,
            renderer,
            material,
            0.22f,
            new ParticleSystem.MinMaxCurve(0.13f, 0.19f),
            new ParticleSystem.MinMaxCurve(0.03f, 0.16f),
            new ParticleSystem.MinMaxCurve(0.34f, 0.54f),
            4,
            33
        );

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.025f;

        SetColorOverLifetime(
            system,
            new Color(1f, 0.28f, 0.18f),
            new Color(0.95f, 0.025f, 0.04f),
            new Color(0.55f, 0f, 0.02f)
        );
        SetSizeOverLifetime(
            system,
            new AnimationCurve(
                new Keyframe(0f, 0.18f),
                new Keyframe(0.18f, 1f),
                new Keyframe(1f, 0f)
            )
        );
    }

    private static void ConfigureImpactRing(
        ParticleSystem system,
        ParticleSystemRenderer renderer,
        Material material
    )
    {
        ConfigureCommon(
            system,
            renderer,
            material,
            0.28f,
            new ParticleSystem.MinMaxCurve(0.18f, 0.25f),
            new ParticleSystem.MinMaxCurve(1.25f, 2.05f),
            new ParticleSystem.MinMaxCurve(0.035f, 0.065f),
            22,
            35
        );

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.12f;
        shape.radiusThickness = 1f;

        SetColorOverLifetime(
            system,
            new Color(1f, 0.31f, 0.19f),
            new Color(1f, 0.045f, 0.035f),
            new Color(0.50f, 0f, 0.015f)
        );
        SetSizeOverLifetime(
            system,
            new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.55f, 0.72f),
                new Keyframe(1f, 0f)
            )
        );
    }

    private static void ConfigureEmbers(
        ParticleSystem system,
        ParticleSystemRenderer renderer,
        Material material
    )
    {
        ConfigureCommon(
            system,
            renderer,
            material,
            0.70f,
            new ParticleSystem.MinMaxCurve(0.43f, 0.64f),
            new ParticleSystem.MinMaxCurve(0.42f, 1.55f),
            new ParticleSystem.MinMaxCurve(0.025f, 0.07f),
            12,
            34
        );

        ParticleSystem.MainModule main = system.main;
        main.gravityModifier = 0.18f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.055f;
        shape.radiusThickness = 1f;

        SetColorOverLifetime(
            system,
            new Color(1f, 0.20f, 0.08f),
            new Color(0.72f, 0.01f, 0.025f),
            new Color(0.18f, 0f, 0.01f)
        );
        SetSizeOverLifetime(
            system,
            new AnimationCurve(
                new Keyframe(0f, 0.65f),
                new Keyframe(0.16f, 1f),
                new Keyframe(0.78f, 0.55f),
                new Keyframe(1f, 0f)
            )
        );

        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.quality = ParticleSystemNoiseQuality.Low;
        noise.strength = 0.24f;
        noise.frequency = 0.75f;
        noise.scrollSpeed = 0.35f;
        noise.damping = true;
    }

    private static void SetColorOverLifetime(
        ParticleSystem system,
        Color start,
        Color middle,
        Color end
    )
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(start, 0f),
                new GradientColorKey(middle, 0.38f),
                new GradientColorKey(end, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.92f, 0.52f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        ParticleSystem.ColorOverLifetimeModule color =
            system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(gradient);
    }

    private static void SetSizeOverLifetime(
        ParticleSystem system,
        AnimationCurve curve
    )
    {
        ParticleSystem.SizeOverLifetimeModule size =
            system.sizeOverLifetime;
        size.enabled = true;
        size.separateAxes = false;
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }
}
