using UnityEngine;

/// <summary>폭탄 도색·낡은 금속용 타일링 프로시저럴 텍스처.</summary>
public static class CwslSurfaceTextureGenerator
{
    private const int Size = 256;
    private const int Seed = 43791;

    public static Texture2D CreateBombPaintAlbedo()
    {
        var pixels = new Color[Size * Size];
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                var u = x / (float)Size;
                var v = y / (float)Size;
                var shade = EvaluateBombPaintAlbedo(u, v);
                pixels[y * Size + x] = new Color(shade, shade, shade, 1f);
            }
        }

        return FinishTexture(pixels, "CwslBombPaint_Albedo");
    }

    public static Texture2D CreateBombPaintNormal()
        => CreateNormalFromHeight(EvaluateBombPaintHeight, "CwslBombPaint_Normal", bumpStrength: 2.4f);

    public static Texture2D CreateScratchedMetalAlbedo()
    {
        var pixels = new Color[Size * Size];
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                var u = x / (float)Size;
                var v = y / (float)Size;
                pixels[y * Size + x] = EvaluateScratchedMetalAlbedo(u, v);
            }
        }

        return FinishTexture(pixels, "CwslScratchedMetal_Albedo");
    }

    public static Texture2D CreateScratchedMetalNormal()
        => CreateNormalFromHeight(EvaluateScratchedMetalHeight, "CwslScratchedMetal_Normal", bumpStrength: 3.6f);

    private static float EvaluateBombPaintAlbedo(float u, float v)
    {
        var wear = Fbm(u * 7.5f, v * 7.5f, 4, Seed);
        var grime = Fbm(u * 19f + 12.7f, v * 19f - 4.2f, 3, Seed + 11);
        var chips = Fbm(u * 14f + 33f, v * 14f + 8f, 2, Seed + 23);

        var stripe = Mathf.Abs(Mathf.Sin((u + v) * Mathf.PI * 5.5f));
        var hazardDark = SmoothStep(0.78f, 0.9f, stripe);

        var scratches = ScratchField(u, v, density: 42f, sharpness: 0.88f, seedOffset: 101);

        var value = 0.94f;
        value *= Mathf.Lerp(1f, 0.58f, wear * 0.62f);
        value *= Mathf.Lerp(1f, 0.72f, grime * 0.45f);
        if (chips > 0.74f)
            value = Mathf.Lerp(value, 1.08f, (chips - 0.74f) / 0.26f);
        value = Mathf.Lerp(value, value * 0.42f, hazardDark * 0.38f);
        value += scratches * 0.16f;

        return Mathf.Clamp01(value);
    }

    private static float EvaluateBombPaintHeight(float u, float v)
    {
        var wear = Fbm(u * 7.5f, v * 7.5f, 4, Seed);
        var chips = Fbm(u * 14f + 33f, v * 14f + 8f, 2, Seed + 23);
        var scratches = ScratchField(u, v, density: 42f, sharpness: 0.88f, seedOffset: 101);
        var chipRelief = chips > 0.74f ? (chips - 0.74f) * 2.8f : 0f;

        return wear * 0.35f + chipRelief * 0.25f + scratches * 0.55f;
    }

    private static Color EvaluateScratchedMetalAlbedo(float u, float v)
    {
        var baseNoise = Fbm(u * 9f, v * 9f, 4, Seed + 50);
        var rust = Fbm(u * 5f + 21f, v * 5f - 9f, 3, Seed + 61);
        var rustMask = SmoothStep(0.58f, 0.82f, rust);

        var scratches = ScratchField(u, v, density: 58f, sharpness: 0.92f, seedOffset: 207);
        var brush = Mathf.Abs(Mathf.Sin((u * 0.35f + v * 1.8f) * Mathf.PI * 14f));
        brush = SmoothStep(0.55f, 0.95f, brush) * 0.12f;

        var gray = Mathf.Lerp(0.34f, 0.52f, baseNoise);
        gray += scratches * 0.22f;
        gray += brush;

        var rustColor = new Color(0.46f, 0.28f, 0.16f);
        var baseColor = new Color(gray, gray, gray * 1.02f);
        var color = Color.Lerp(baseColor, rustColor, rustMask * 0.55f);

        return new Color(
            Mathf.Clamp01(color.r),
            Mathf.Clamp01(color.g),
            Mathf.Clamp01(color.b),
            1f);
    }

    private static float EvaluateScratchedMetalHeight(float u, float v)
    {
        var baseNoise = Fbm(u * 9f, v * 9f, 4, Seed + 50);
        var rust = Fbm(u * 5f + 21f, v * 5f - 9f, 3, Seed + 61);
        var rustMask = SmoothStep(0.58f, 0.82f, rust);
        var scratches = ScratchField(u, v, density: 58f, sharpness: 0.92f, seedOffset: 207);
        var brush = Mathf.Abs(Mathf.Sin((u * 0.35f + v * 1.8f) * Mathf.PI * 14f));
        brush = SmoothStep(0.55f, 0.95f, brush) * 0.1f;

        return baseNoise * 0.2f + rustMask * 0.18f + scratches * 0.75f + brush;
    }

    private static Texture2D CreateNormalFromHeight(
        System.Func<float, float, float> heightFn,
        string name,
        float bumpStrength)
    {
        var pixels = new Color[Size * Size];
        const float step = 1f / Size;

        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                var u = x / (float)Size;
                var v = y / (float)Size;

                var hL = heightFn(Wrap01(u - step), v);
                var hR = heightFn(Wrap01(u + step), v);
                var hD = heightFn(u, Wrap01(v - step));
                var hU = heightFn(u, Wrap01(v + step));

                var dx = (hL - hR) * bumpStrength;
                var dy = (hD - hU) * bumpStrength;
                var normal = new Vector3(dx, dy, 1f).normalized;
                pixels[y * Size + x] = new Color(
                    normal.x * 0.5f + 0.5f,
                    normal.y * 0.5f + 0.5f,
                    normal.z * 0.5f + 0.5f,
                    1f);
            }
        }

        return FinishTexture(pixels, name, markNormal: true);
    }

    private static float ScratchField(float u, float v, float density, float sharpness, int seedOffset)
    {
        var sum = 0f;
        var cellSize = 1f / density;
        var cx = Mathf.FloorToInt(u / cellSize);
        var cy = Mathf.FloorToInt(v / cellSize);

        for (var oy = -1; oy <= 1; oy++)
        {
            for (var ox = -1; ox <= 1; ox++)
            {
                var cellX = cx + ox;
                var cellY = cy + oy;
                var hash = Hash(cellX, cellY, seedOffset);
                var angle = hash * Mathf.PI * 2f;
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var cellOrigin = new Vector2(cellX * cellSize, cellY * cellSize);
                var jitter = new Vector2(Hash(cellX, cellY, seedOffset + 3) * cellSize, Hash(cellX, cellY, seedOffset + 7) * cellSize);
                var origin = cellOrigin + jitter;

                var p = new Vector2(u, v) - origin;
                var along = Vector2.Dot(p, dir);
                var across = Vector2.Dot(p, new Vector2(-dir.y, dir.x));
                var length = Mathf.Lerp(cellSize * 0.55f, cellSize * 1.35f, Hash(cellX, cellY, seedOffset + 13));
                var width = Mathf.Lerp(cellSize * 0.004f, cellSize * 0.018f, Hash(cellX, cellY, seedOffset + 17));

                if (along < 0f || along > length)
                    continue;

                var line = 1f - Mathf.Clamp01(Mathf.Abs(across) / width);
                line = Mathf.Pow(line, sharpness);
                sum = Mathf.Max(sum, line);
            }
        }

        return sum;
    }

    private static float Fbm(float u, float v, int octaves, int seed)
    {
        var value = 0f;
        var amplitude = 0.55f;
        var frequency = 1f;
        var total = 0f;

        for (var i = 0; i < octaves; i++)
        {
            value += ValueNoise(u * frequency, v * frequency, seed + i * 17) * amplitude;
            total += amplitude;
            amplitude *= 0.5f;
            frequency *= 2.03f;
        }

        return total <= 0f ? 0f : value / total;
    }

    private static float ValueNoise(float u, float v, int seed)
    {
        var x0 = Mathf.FloorToInt(u);
        var y0 = Mathf.FloorToInt(v);
        var x1 = x0 + 1;
        var y1 = y0 + 1;

        var tx = u - x0;
        var ty = v - y0;
        tx = tx * tx * (3f - 2f * tx);
        ty = ty * ty * (3f - 2f * ty);

        var a = Hash01(x0, y0, seed);
        var b = Hash01(x1, y0, seed);
        var c = Hash01(x0, y1, seed);
        var d = Hash01(x1, y1, seed);

        var ab = Mathf.Lerp(a, b, tx);
        var cd = Mathf.Lerp(c, d, tx);
        return Mathf.Lerp(ab, cd, ty);
    }

    private static float Hash01(int x, int y, int seed)
        => (Hash(x, y, seed) % 10000) / 10000f;

    private static int Hash(int x, int y, int seed)
    {
        unchecked
        {
            var h = seed;
            h = h * 73856093 ^ x;
            h = h * 19349663 ^ y;
            h ^= h >> 13;
            h *= 1274126177;
            return h & 0x7fffffff;
        }
    }

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        var t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    private static float Wrap01(float value)
    {
        value %= 1f;
        if (value < 0f)
            value += 1f;
        return value;
    }

    private static Texture2D FinishTexture(Color[] pixels, string name, bool markNormal = false)
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true, markNormal)
        {
            name = name,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            anisoLevel = 1
        };
        texture.SetPixels(pixels);
        texture.Apply(true, false);
        return texture;
    }
}
