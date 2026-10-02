using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Sinh texture Halloween bằng code (SDF + supersampling), lưu PNG vào thư mục Textures
public static class HalloweenTextureGen
{
    // f(tileIndex, p) với p trong [-1,1]^2, y hướng lên; trả màu thẳng (không premultiply)
    public static Texture2D Make(string path, int w, int h, int tilesX, int tilesY, Func<int, Vector2, Color> f, int ss = 2)
    {
        var px = new Color[w * h];
        int tw = w / tilesX, th = h / tilesY;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int tx = x / tw, ty = y / th;
            int tile = (tilesY - 1 - ty) * tilesX + tx; // tile 0 ở góc trên trái (đúng thứ tự texture sheet)
            float r = 0, g = 0, b = 0, a = 0;
            for (int sy = 0; sy < ss; sy++)
            for (int sx = 0; sx < ss; sx++)
            {
                float u = ((x - tx * tw) + (sx + 0.5f) / ss) / tw;
                float v = ((y - ty * th) + (sy + 0.5f) / ss) / th;
                var c = f(tile, new Vector2(u * 2f - 1f, v * 2f - 1f));
                r += c.r * c.a; g += c.g * c.a; b += c.b * c.a; a += c.a;
            }
            px[y * w + x] = a > 0.0001f ? new Color(r / a, g / a, b / a, a / (ss * ss)) : new Color(1, 1, 1, 0);
        }

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Default;
        imp.alphaSource = TextureImporterAlphaSource.FromInput;
        imp.alphaIsTransparency = true;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.mipmapEnabled = true;
        imp.maxTextureSize = Mathf.Max(w, h);
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ---------- Helpers ----------
    static float Sat(float x) => Mathf.Clamp01(x);
    static float Smooth(float e0, float e1, float x) { float t = Sat((x - e0) / (e1 - e0)); return t * t * (3 - 2 * t); }
    static Color W(float a) => new Color(1, 1, 1, Sat(a));

    static float SegDist(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 pa = p - a, ba = b - a;
        float h = Sat(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
        return (pa - ba * h).magnitude;
    }

    static bool InPoly(Vector2 p, Vector2[] poly)
    {
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                inside = !inside;
        }
        return inside;
    }

    static bool InEllipse(Vector2 p, Vector2 c, Vector2 r)
    {
        float dx = (p.x - c.x) / r.x, dy = (p.y - c.y) / r.y;
        return dx * dx + dy * dy < 1f;
    }

    static float Hash(int x, int y, int s)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + s * 1442695041;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0x7fffffff) / (float)int.MaxValue;
        }
    }

    static float VNoise(float x, float y, int s)
    {
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
        float fx = x - xi, fy = y - yi;
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        float a = Hash(xi, yi, s), b = Hash(xi + 1, yi, s), c = Hash(xi, yi + 1, s), d = Hash(xi + 1, yi + 1, s);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    static float Fbm(float x, float y, int s)
    {
        float sum = 0, amp = 0.5f;
        for (int i = 0; i < 4; i++) { sum += amp * VNoise(x, y, s + i * 17); x *= 2; y *= 2; amp *= 0.5f; }
        return sum;
    }

    // ---------- Textures ----------
    public static Texture2D Glow(string path) => Make(path, 128, 128, 1, 1, (t, p) =>
        W(Mathf.Pow(Sat(1f - p.magnitude), 2.2f)), 1);

    public static Texture2D RingSoft(string path) => Make(path, 256, 256, 1, 1, (t, p) =>
    {
        float r = p.magnitude;
        if (r > 1f) return W(0);
        float a = Mathf.Exp(-Mathf.Pow((r - 0.78f) / 0.07f, 2)) + 0.4f * Mathf.Exp(-Mathf.Pow((r - 0.78f) / 0.2f, 2));
        return W(a * Smooth(1f, 0.95f, r));
    }, 1);

    public static Texture2D RingThin(string path) => Make(path, 512, 512, 1, 1, (t, p) =>
    {
        float d = Mathf.Abs(p.magnitude - 0.88f);
        return W(d < 0.035f ? 1f : 0.5f * Mathf.Exp(-Mathf.Pow((d - 0.035f) / 0.04f, 2)));
    });

    public static Texture2D Wisp(string path) => Make(path, 128, 128, 1, 1, (t, p) =>
    {
        // Ngọn lửa ma: đầu tròn ở dưới, đuôi nhọn vuốt lên
        var c = new Vector2(0, -0.35f);
        float head = Smooth(1f, 0.5f, ((p - c) / 0.45f).magnitude);
        float tail = 0f;
        if (p.y > c.y && p.y < 0.95f)
        {
            float k = (p.y - c.y) / 1.3f;
            float w = 0.45f * Mathf.Pow(Sat(1f - k), 1.3f);
            if (w > 0.001f) tail = Smooth(1f, 0.4f, Mathf.Abs(p.x) / w) * Sat(1f - k * 0.6f);
        }
        return W(Mathf.Max(head, tail));
    }, 1);

    public static Texture2D Trail(string path) => Make(path, 128, 32, 1, 1, (t, p) =>
        W(Mathf.Pow(Sat(1f - Mathf.Abs(p.y)), 1.5f)), 1);

    public static Texture2D Spark(string path) => Make(path, 128, 128, 1, 1, (t, p) =>
    {
        float ax = Mathf.Abs(p.x), ay = Mathf.Abs(p.y);
        float star = Mathf.Max(Mathf.Exp(-ax * 14f) * Mathf.Exp(-ay * 2.8f), Mathf.Exp(-ay * 14f) * Mathf.Exp(-ax * 2.8f));
        float glow = 0.5f * Mathf.Pow(Sat(1f - p.magnitude), 3f);
        return W((star + glow) * Smooth(1f, 0.9f, p.magnitude));
    }, 1);

    public static Texture2D Beam(string path) => Make(path, 64, 256, 1, 1, (t, p) =>
    {
        float v = (p.y + 1f) * 0.5f;
        float vert = Mathf.Pow(Sat(v), 0.35f) * Mathf.Pow(Sat(1f - v), 1.2f) * 2.2f;
        return W(Mathf.Pow(Sat(1f - Mathf.Abs(p.x)), 2.5f) * vert);
    }, 1);

    public static Texture2D Smoke2x2(string path) => Make(path, 512, 512, 2, 2, (t, p) =>
    {
        float r = p.magnitude;
        float n = Fbm(p.x * 2.2f + t * 7.3f, p.y * 2.2f + t * 3.1f, 11 + t);
        float a = Sat((n - 0.25f) * 2.2f) * Smooth(1f, 0.2f, r);
        float s = 0.75f + 0.25f * n;
        return new Color(s, s, s, a);
    }, 1);

    public static Texture2D Bubble(string path) => Make(path, 128, 128, 1, 1, (t, p) =>
    {
        float r = p.magnitude;
        if (r > 1f) return W(0);
        float a = Smooth(0.72f, 0.95f, r) * Smooth(1f, 0.96f, r) + 0.12f;
        if ((p - new Vector2(-0.35f, 0.35f)).magnitude < 0.16f) a = 1f;
        return W(a);
    });

    public static Texture2D BatSheet(string path)
    {
        float[] tys = { 0.6f, 0.25f, -0.2f, 0.25f };
        return Make(path, 512, 128, 4, 1, (t, p) =>
        {
            float ty = tys[t];
            var q = new Vector2(Mathf.Abs(p.x), p.y);
            bool inside = InEllipse(p, new Vector2(0, -0.05f), new Vector2(0.13f, 0.22f))
                       || (p - new Vector2(0, 0.2f)).magnitude < 0.11f
                       || InPoly(q, new[] { new Vector2(0.02f, 0.27f), new Vector2(0.1f, 0.27f), new Vector2(0.085f, 0.42f) });
            if (!inside)
            {
                var wing = new[]
                {
                    new Vector2(0.08f, 0.10f), new Vector2(0.40f, 0.18f + ty * 0.55f), new Vector2(0.95f, ty + 0.05f),
                    new Vector2(0.78f, ty * 0.65f - 0.05f), new Vector2(0.66f, ty * 0.45f - 0.30f),
                    new Vector2(0.50f, ty * 0.35f - 0.12f), new Vector2(0.36f, -0.32f),
                    new Vector2(0.22f, -0.16f), new Vector2(0.06f, -0.22f)
                };
                inside = InPoly(q, wing);
            }
            return W(inside ? 1 : 0);
        }, 3);
    }

    public static Texture2D Pumpkin(string path)
    {
        var mouthList = new List<Vector2>();
        for (int i = 0; i <= 6; i++) mouthList.Add(new Vector2(-0.45f + i * 0.15f, i % 2 == 0 ? -0.22f : -0.30f));
        for (int i = 0; i <= 8; i++)
        {
            float x = 0.45f - i * 0.1125f;
            mouthList.Add(new Vector2(x, -0.22f - 0.28f * Mathf.Sqrt(Sat(1f - (x / 0.47f) * (x / 0.47f)))));
        }
        var mouth = mouthList.ToArray();
        var eyeTri = new[] { new Vector2(0.10f, -0.02f), new Vector2(0.36f, -0.02f), new Vector2(0.22f, 0.20f) };
        var noseTri = new[] { new Vector2(-0.06f, -0.13f), new Vector2(0.06f, -0.13f), new Vector2(0f, -0.03f) };
        return Make(path, 256, 256, 1, 1, (t, p) => PumpkinPixel(p, mouth, eyeTri, noseTri), 3);
    }

    static Color PumpkinPixel(Vector2 p, Vector2[] mouth, Vector2[] eyeTri, Vector2[] noseTri)
    {
        var glow = new Color(1f, 0.85f, 0.35f, 1f);
        var q = new Vector2(Mathf.Abs(p.x), p.y);

        // Mặt cắt: mắt, mũi, miệng răng cưa
        bool eye = InPoly(q, eyeTri);
        bool nose = InPoly(p, noseTri);
        bool mouthIn = InPoly(p, mouth);

        // Thân 3 múi
        Vector2[] centers = { new Vector2(0, -0.08f), new Vector2(-0.36f, -0.08f), new Vector2(0.36f, -0.08f) };
        Vector2[] radii = { new Vector2(0.42f, 0.58f), new Vector2(0.44f, 0.52f), new Vector2(0.44f, 0.52f) };
        for (int i = 0; i < 3; i++)
        {
            if (!InEllipse(p, centers[i], radii[i])) continue;
            if (eye || nose || mouthIn)
            {
                float k = 0.75f + 0.25f * Sat(1f - (p.y + 0.4f));
                return new Color(glow.r, glow.g * k, glow.b * k, 1);
            }
            float nx = (p.x - centers[i].x) / radii[i].x;
            float shade = Sat((1f - 0.55f * nx * nx) * (0.8f + 0.3f * (p.y + 0.6f)));
            var c = Color.Lerp(new Color(0.55f, 0.18f, 0.02f), new Color(1f, 0.55f, 0.1f), shade);
            c.a = 1;
            return c;
        }

        // Cuống
        float sx = p.x - 0.15f * (p.y - 0.4f) * (p.y - 0.4f) * 4f;
        if (p.y > 0.38f && p.y < 0.74f && Mathf.Abs(sx) < 0.065f) return new Color(0.35f, 0.25f, 0.1f, 1);
        return new Color(1, 1, 1, 0);
    }

    public static Texture2D Ghost(string path) => Make(path, 256, 256, 1, 1, (t, p) =>
    {
        var hc = new Vector2(0, 0.25f);
        float dHead = (p - hc).magnitude - 0.48f;
        Vector2 bd = new Vector2(Mathf.Abs(p.x) - 0.48f, Mathf.Abs(p.y - (-0.185f)) - 0.435f);
        float dBox = new Vector2(Mathf.Max(bd.x, 0), Mathf.Max(bd.y, 0)).magnitude + Mathf.Min(Mathf.Max(bd.x, bd.y), 0);
        float wave = -0.62f + 0.1f * Mathf.Cos(p.x / 0.48f * Mathf.PI * 2.5f);
        bool inside = dHead < 0 || (dBox < 0 && p.y > wave);
        if (inside)
        {
            var q = new Vector2(Mathf.Abs(p.x), p.y);
            if (InEllipse(q, new Vector2(0.17f, 0.27f), new Vector2(0.075f, 0.11f)) ||
                InEllipse(p, new Vector2(0, 0.02f), new Vector2(0.09f, 0.12f)))
                return new Color(0.08f, 0.08f, 0.15f, 1);
            return new Color(0.92f, 0.96f, 1f, 1);
        }
        float d = Mathf.Max(0f, Mathf.Min(dHead, dBox));
        return new Color(0.85f, 0.95f, 1f, 0.45f * Mathf.Exp(-d * 9f) * Smooth(1f, 0.9f, p.magnitude));
    }, 3);

    public static Texture2D CandyCorn(string path) => Make(path, 128, 128, 1, 1, (t, p) =>
    {
        bool inside = InPoly(p, new[] { new Vector2(0, 0.85f), new Vector2(0.55f, -0.7f), new Vector2(-0.55f, -0.7f) })
                   || InEllipse(p, new Vector2(0, -0.7f), new Vector2(0.55f, 0.15f));
        if (!inside) return new Color(1, 1, 1, 0);
        Color c = p.y > 0.3f ? new Color(1f, 0.98f, 0.9f) : p.y > -0.25f ? new Color(1f, 0.55f, 0.1f) : new Color(1f, 0.85f, 0.2f);
        float shade = 1f - 0.3f * Mathf.Abs(p.x) / 0.55f;
        return new Color(c.r * shade, c.g * shade, c.b * shade, 1);
    }, 3);

    public static Texture2D RuneCircle(string path)
    {
        var rnd = new System.Random(7);
        var glyphs = new List<(Vector2, Vector2)>();
        const int count = 24;
        float step = Mathf.PI * 2f / count;
        float[] gs = { -0.3f, 0f, 0.3f }, gt = { -0.8f, 0f, 0.8f };
        Vector2 Polar(float ang, float rad) => new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad;
        for (int k = 0; k < count; k++)
        {
            for (int s = 0; s < 3; s++)
            {
                int a = rnd.Next(9), b = rnd.Next(9);
                if (a == b) b = (b + 4) % 9;
                var pa = Polar(k * step + gs[a % 3] * step, 0.92f + gt[a / 3] * 0.028f);
                var pb = Polar(k * step + gs[b % 3] * step, 0.92f + gt[b / 3] * 0.028f);
                glyphs.Add((pa, pb));
            }
        }
        var hex = new Vector2[6];
        for (int i = 0; i < 6; i++) hex[i] = Polar(Mathf.PI * 0.5f + i * Mathf.PI / 3f, 0.86f);

        return Make(path, 1024, 1024, 1, 1, (t, p) =>
        {
            float r = p.magnitude;
            if (r > 1f) return W(0);
            if (Mathf.Abs(r - 0.965f) < 0.012f || Mathf.Abs(r - 0.875f) < 0.008f) return W(1);
            if (Mathf.Abs(r - 0.55f) < 0.008f || Mathf.Abs(r - 0.50f) < 0.004f || Mathf.Abs(r - 0.2f) < 0.005f) return W(1);
            if (r > 0.86f && r < 0.98f)
                foreach (var g in glyphs) if (SegDist(p, g.Item1, g.Item2) < 0.008f) return W(1);
            // Hai tam giác lồng nhau + vòng nhỏ ở đỉnh
            for (int i = 0; i < 6; i++)
            {
                if (SegDist(p, hex[i], hex[(i + 2) % 6]) < 0.006f) return W(1);
                if (Mathf.Abs((p - hex[i]).magnitude - 0.05f) < 0.006f) return W(1);
            }
            // Trăng khuyết ở tâm
            if (r < 0.16f && (p - new Vector2(0.07f, 0.05f)).magnitude > 0.14f) return W(1);
            return W(0);
        });
    }

    public static Texture2D Web(string path)
    {
        var rnd = new System.Random(3);
        const int spokes = 10;
        var ang = new float[spokes];
        for (int i = 0; i < spokes; i++) ang[i] = i * Mathf.PI * 2f / spokes + ((float)rnd.NextDouble() - 0.5f) * 0.2f;
        var segs = new List<(Vector2, Vector2)>();
        for (int i = 0; i < spokes; i++) segs.Add((Vector2.zero, new Vector2(Mathf.Cos(ang[i]), Mathf.Sin(ang[i])) * 0.98f));
        for (float rr = 0.15f; rr < 0.95f; rr += 0.1f)
        {
            for (int i = 0; i < spokes; i++)
            {
                var a = new Vector2(Mathf.Cos(ang[i]), Mathf.Sin(ang[i])) * rr;
                var b = new Vector2(Mathf.Cos(ang[(i + 1) % spokes]), Mathf.Sin(ang[(i + 1) % spokes])) * rr;
                var m = (a + b) * 0.5f * 0.92f;
                segs.Add((a, m));
                segs.Add((m, b));
            }
        }
        return Make(path, 512, 512, 1, 1, (t, p) =>
        {
            if (p.magnitude < 0.04f) return W(1);
            foreach (var s in segs) if (SegDist(p, s.Item1, s.Item2) < 0.006f) return W(1);
            return W(0);
        });
    }
}
