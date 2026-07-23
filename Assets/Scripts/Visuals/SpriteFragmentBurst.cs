using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class SpriteFragmentBurst : MonoBehaviour
{
    private sealed class Fragment
    {
        public Transform Pivot;
        public SpriteRenderer Renderer;
        public Vector2 Velocity;
        public float AngularVelocity;
        public Color InitialColor;
    }

    private static readonly int FragmentRectId =
        Shader.PropertyToID("_FragmentLocalRect");

    private static readonly float[] ColumnCuts =
    {
        0f, 0.22f, 0.48f, 0.75f, 1f
    };

    private static readonly float[] RowCuts =
    {
        0f, 0.17f, 0.36f, 0.57f, 0.78f, 1f
    };

    private readonly List<Fragment> fragments =
        new List<Fragment>(20);

    private float lifetime;
    private float fadeStart;
    private float gravity;
    private float linearDrag;
    private float age;
    private bool initialized;
    private bool motionStarted;

    public int FragmentCount => fragments.Count;

    public void Begin()
    {
        motionStarted = initialized;
    }

    public static SpriteFragmentBurst Create(
        SpriteRenderer sourceRenderer,
        Material fragmentMaterial,
        Vector2 inheritedVelocity,
        float minimumSpeed,
        float maximumSpeed,
        float upwardBoost,
        float gravityStrength,
        float drag,
        float maximumAngularSpeed,
        float effectLifetime,
        float fadeStartTime
    )
    {
        if (
            sourceRenderer == null ||
            sourceRenderer.sprite == null ||
            fragmentMaterial == null
        )
        {
            return null;
        }

        GameObject root = new GameObject("Player Sprite Fragments");

        if (
            sourceRenderer.gameObject.scene.IsValid() &&
            sourceRenderer.gameObject.scene.isLoaded &&
            root.scene != sourceRenderer.gameObject.scene
        )
        {
            SceneManager.MoveGameObjectToScene(
                root,
                sourceRenderer.gameObject.scene
            );
        }

        SpriteFragmentBurst burst =
            root.AddComponent<SpriteFragmentBurst>();

        burst.Initialize(
            sourceRenderer,
            fragmentMaterial,
            inheritedVelocity,
            minimumSpeed,
            maximumSpeed,
            upwardBoost,
            gravityStrength,
            drag,
            maximumAngularSpeed,
            effectLifetime,
            fadeStartTime
        );

        if (burst.FragmentCount > 0)
        {
            return burst;
        }

        Destroy(root);
        return null;
    }

    public void Simulate(float deltaTime)
    {
        if (!initialized || deltaTime <= 0f)
        {
            return;
        }

        Advance(deltaTime);
    }

    private void Initialize(
        SpriteRenderer sourceRenderer,
        Material fragmentMaterial,
        Vector2 inheritedVelocity,
        float minimumSpeed,
        float maximumSpeed,
        float upwardBoost,
        float gravityStrength,
        float drag,
        float maximumAngularSpeed,
        float effectLifetime,
        float fadeStartTime
    )
    {
        lifetime = Mathf.Max(0.05f, effectLifetime);
        fadeStart = Mathf.Clamp(fadeStartTime, 0f, lifetime);
        gravity = Mathf.Max(0f, gravityStrength);
        linearDrag = Mathf.Max(0f, drag);

        float speedMinimum = Mathf.Max(0f, minimumSpeed);
        float speedMaximum = Mathf.Max(speedMinimum, maximumSpeed);
        float angularMaximum = Mathf.Max(0f, maximumAngularSpeed);

        Sprite sourceSprite = sourceRenderer.sprite;
        Rect localBounds = CalculateLocalBounds(sourceSprite);
        Vector3 visualCenter = sourceRenderer.bounds.center;
        Color fragmentColor = CalculateSourceColor(sourceRenderer);
        System.Random random = new System.Random(48317);

        for (int row = 0; row < RowCuts.Length - 1; row++)
        {
            float sourceYMin = Mathf.Lerp(
                localBounds.yMin,
                localBounds.yMax,
                RowCuts[row]
            );
            float sourceYMax = Mathf.Lerp(
                localBounds.yMin,
                localBounds.yMax,
                RowCuts[row + 1]
            );

            for (int column = 0;
                column < ColumnCuts.Length - 1;
                column++)
            {
                float sourceXMin = Mathf.Lerp(
                    localBounds.xMin,
                    localBounds.xMax,
                    ColumnCuts[column]
                );
                float sourceXMax = Mathf.Lerp(
                    localBounds.xMin,
                    localBounds.xMax,
                    ColumnCuts[column + 1]
                );

                Rect renderedRect = MirrorRectForFlip(
                    sourceXMin,
                    sourceYMin,
                    sourceXMax,
                    sourceYMax,
                    sourceRenderer.flipX,
                    sourceRenderer.flipY
                );

                Vector3 localCenter = new Vector3(
                    renderedRect.center.x,
                    renderedRect.center.y,
                    0f
                );
                Vector3 worldCenter =
                    sourceRenderer.transform.TransformPoint(localCenter);

                Fragment fragment = CreateFragment(
                    sourceRenderer,
                    fragmentMaterial,
                    fragmentColor,
                    renderedRect,
                    worldCenter,
                    row,
                    column
                );

                Vector2 radialDirection =
                    (Vector2)(worldCenter - visualCenter);

                if (radialDirection.sqrMagnitude < 0.0001f)
                {
                    float randomAngle =
                        NextRange(random, 0f, Mathf.PI * 2f);
                    radialDirection = new Vector2(
                        Mathf.Cos(randomAngle),
                        Mathf.Sin(randomAngle)
                    );
                }
                else
                {
                    radialDirection.Normalize();
                }

                radialDirection = Rotate(
                    radialDirection,
                    NextRange(random, -18f, 18f)
                );

                float speed = NextRange(
                    random,
                    speedMinimum,
                    speedMaximum
                );
                float extraLift = NextRange(
                    random,
                    upwardBoost * 0.72f,
                    upwardBoost * 1.18f
                );

                fragment.Velocity =
                    inheritedVelocity +
                    (radialDirection * speed) +
                    (Vector2.up * extraLift);

                float angularMagnitude = NextRange(
                    random,
                    angularMaximum * 0.35f,
                    angularMaximum
                );
                fragment.AngularVelocity =
                    random.Next(0, 2) == 0
                        ? -angularMagnitude
                        : angularMagnitude;

                fragments.Add(fragment);
            }
        }

        initialized = fragments.Count > 0;
    }

    private Fragment CreateFragment(
        SpriteRenderer sourceRenderer,
        Material fragmentMaterial,
        Color fragmentColor,
        Rect localRect,
        Vector3 worldCenter,
        int row,
        int column
    )
    {
        GameObject pivotObject = new GameObject(
            $"Fragment_{row + 1}_{column + 1}"
        );
        pivotObject.transform.SetParent(transform, false);
        pivotObject.transform.position = worldCenter;

        GameObject rendererObject = new GameObject("Sprite");
        rendererObject.transform.SetPositionAndRotation(
            sourceRenderer.transform.position,
            sourceRenderer.transform.rotation
        );
        rendererObject.transform.localScale =
            sourceRenderer.transform.lossyScale;
        rendererObject.transform.SetParent(
            pivotObject.transform,
            worldPositionStays: true
        );

        SpriteRenderer fragmentRenderer =
            rendererObject.AddComponent<SpriteRenderer>();
        fragmentRenderer.sprite = sourceRenderer.sprite;
        fragmentRenderer.sharedMaterial = fragmentMaterial;
        fragmentRenderer.color = fragmentColor;
        fragmentRenderer.flipX = sourceRenderer.flipX;
        fragmentRenderer.flipY = sourceRenderer.flipY;
        fragmentRenderer.sortingLayerID =
            sourceRenderer.sortingLayerID;
        fragmentRenderer.sortingOrder =
            sourceRenderer.sortingOrder +
            (row * (ColumnCuts.Length - 1)) + column;
        fragmentRenderer.maskInteraction =
            sourceRenderer.maskInteraction;
        fragmentRenderer.spriteSortPoint =
            sourceRenderer.spriteSortPoint;
        fragmentRenderer.renderingLayerMask =
            sourceRenderer.renderingLayerMask;

        MaterialPropertyBlock propertyBlock =
            new MaterialPropertyBlock();
        sourceRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetVector(
            FragmentRectId,
            new Vector4(
                localRect.xMin,
                localRect.yMin,
                localRect.xMax,
                localRect.yMax
            )
        );
        fragmentRenderer.SetPropertyBlock(propertyBlock);

        return new Fragment
        {
            Pivot = pivotObject.transform,
            Renderer = fragmentRenderer,
            InitialColor = fragmentColor
        };
    }

    private static Rect CalculateLocalBounds(Sprite sprite)
    {
        Vector2[] vertices = sprite.vertices;

        if (vertices == null || vertices.Length == 0)
        {
            Bounds bounds = sprite.bounds;
            return new Rect(
                bounds.min.x,
                bounds.min.y,
                bounds.size.x,
                bounds.size.y
            );
        }

        float minimumX = vertices[0].x;
        float maximumX = vertices[0].x;
        float minimumY = vertices[0].y;
        float maximumY = vertices[0].y;

        for (int index = 1; index < vertices.Length; index++)
        {
            Vector2 vertex = vertices[index];
            minimumX = Mathf.Min(minimumX, vertex.x);
            maximumX = Mathf.Max(maximumX, vertex.x);
            minimumY = Mathf.Min(minimumY, vertex.y);
            maximumY = Mathf.Max(maximumY, vertex.y);
        }

        const float padding = 0.002f;
        return Rect.MinMaxRect(
            minimumX - padding,
            minimumY - padding,
            maximumX + padding,
            maximumY + padding
        );
    }

    private static Rect MirrorRectForFlip(
        float sourceXMin,
        float sourceYMin,
        float sourceXMax,
        float sourceYMax,
        bool flipX,
        bool flipY
    )
    {
        float xMin = flipX ? -sourceXMax : sourceXMin;
        float xMax = flipX ? -sourceXMin : sourceXMax;
        float yMin = flipY ? -sourceYMax : sourceYMin;
        float yMax = flipY ? -sourceYMin : sourceYMax;

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static Color CalculateSourceColor(
        SpriteRenderer sourceRenderer
    )
    {
        Color color = sourceRenderer.color;
        Material sourceMaterial = sourceRenderer.sharedMaterial;

        if (
            sourceMaterial != null &&
            sourceMaterial.HasProperty("_Color")
        )
        {
            color *= sourceMaterial.GetColor("_Color");
        }

        return color;
    }

    private void Update()
    {
        if (motionStarted)
        {
            Simulate(Time.unscaledDeltaTime);
        }
    }

    private void Advance(float deltaTime)
    {
        age += deltaTime;

        float visibility = 1f;

        if (age > fadeStart)
        {
            float fadeDuration = Mathf.Max(
                0.0001f,
                lifetime - fadeStart
            );
            float fade = Mathf.Clamp01(
                (age - fadeStart) / fadeDuration
            );
            float easedFade = fade * fade * (3f - (2f * fade));
            visibility = 1f - easedFade;
        }

        float dragMultiplier = Mathf.Exp(-linearDrag * deltaTime);

        foreach (Fragment fragment in fragments)
        {
            fragment.Velocity += Vector2.down * gravity * deltaTime;
            fragment.Velocity *= dragMultiplier;
            fragment.Pivot.position +=
                (Vector3)(fragment.Velocity * deltaTime);
            fragment.Pivot.Rotate(
                0f,
                0f,
                fragment.AngularVelocity * deltaTime,
                Space.Self
            );
            fragment.Pivot.localScale = Vector3.one * Mathf.Lerp(
                0.72f,
                1f,
                visibility
            );

            if (fragment.Renderer != null)
            {
                Color color = fragment.InitialColor;
                color.a *= visibility;
                fragment.Renderer.color = color;
            }
        }

        if (age >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private static float NextRange(
        System.Random random,
        float minimum,
        float maximum
    )
    {
        return Mathf.Lerp(
            minimum,
            maximum,
            (float)random.NextDouble()
        );
    }

    private static Vector2 Rotate(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cosine = Mathf.Cos(radians);
        float sine = Mathf.Sin(radians);

        return new Vector2(
            (vector.x * cosine) - (vector.y * sine),
            (vector.x * sine) + (vector.y * cosine)
        );
    }
}
