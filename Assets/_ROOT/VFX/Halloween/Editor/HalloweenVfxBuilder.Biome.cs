using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PS = UnityEngine.ParticleSystem;

// Accent theo Biome: lớp trang trí mỏng chồng lên aura Rarity.
//
// Nguyên tắc thiết kế (quan trọng khi sửa về sau):
//  - Rarity giữ kênh "hình dạng lớn" (ring, beam, bats, skull) vì đó là tín hiệu giá trị
//    mà người chơi phải đọc được từ xa và phải GIỐNG NHAU ở mọi biome.
//  - Biome chỉ được dùng kênh "hạt nhỏ + tint", tối đa 2 hệ hạt, mật độ thấp.
//    Người chơi đã thấy biome qua môi trường nên accent không cần tải thông tin.
//  - Mọi màu đều lệch về palette Halloween (cam/tím/xanh độc/xương) để 14 biome
//    vẫn đọc ra cùng một theme thay vì thành 14 theme rời rạc.
public static partial class HalloweenVfxBuilder
{
    const string AccentDir = Root + "/Prefabs/Accent";

    // ================= Palette biome =================
    static readonly Color LeafRust = new Color(0.85f, 0.45f, 0.12f);
    static readonly Color Firefly = new Color(0.75f, 1f, 0.35f);
    static readonly Color PaleCyan = new Color(0.5f, 0.85f, 0.95f);
    static readonly Color Sand = new Color(0.9f, 0.75f, 0.45f);
    static readonly Color Toxic = new Color(0.45f, 0.95f, 0.35f);
    static readonly Color Frost = new Color(0.8f, 0.92f, 1f);
    static readonly Color Lava = new Color(1f, 0.35f, 0.08f);
    static readonly Color Abyss = new Color(0.2f, 0.8f, 0.85f);
    static readonly Color Amber = new Color(1f, 0.7f, 0.25f);
    static readonly Color Ash = new Color(0.55f, 0.5f, 0.48f);
    static readonly Color Violet = new Color(0.7f, 0.5f, 1f);
    static readonly Color Sakura = new Color(1f, 0.6f, 0.82f);
    static readonly Color Holy = new Color(1f, 0.97f, 0.9f);
    static readonly Color Sick = new Color(0.55f, 0.85f, 0.2f);
    static readonly Color Rot = new Color(1f, 0.3f, 0.8f);

    // mPetal được gán trong partial Map, có thể còn null nếu chỉ chạy Build Assets
    static Material PetalMat()
    {
        if (mPetal) return mPetal;
        var tex = HalloweenTextureGen.Petal($"{TexDir}/T_HW_Petal.png");
        mPetal = ParticleMat("M_HW_Petal_Alpha", tex, false);
        return mPetal;
    }

    // ================= Helper riêng cho accent =================

    // Hạt rơi từ trên xuống: lá, tuyết, tro, cánh hoa
    static PS Fall(Transform p, string name, Material mat, Color from, Color to,
                   float rate, float sizeA, float sizeB, float fallA, float fallB,
                   float spin = 0f, float yTop = 2.2f, float radius = 1.1f, int max = 30)
    {
        var ps = NewPS(name, p, mat, yTop, max);
        Main(ps, 2.2f, 3.4f, sizeA, sizeB, 180f);
        CircleXZ(ps, radius);
        VelXYZ(ps, new Vector2(-0.25f, 0.25f), new Vector2(-fallB, -fallA), new Vector2(-0.25f, 0.25f));
        Noise(ps, 0.3f, 0.5f);
        if (spin > 0f) RotOL(ps, -spin, spin);
        Fade(ps, from, to, 0f, 0f, 0.15f, 1f, 0.8f, 1f, 1f, 0f);
        Rate(ps, rate);
        return ps;
    }

    // Hạt nhỏ bay lên / lơ lửng quanh chân: mote, bụi, đốm phát sáng
    static PS Drift(Transform p, string name, Material mat, Color from, Color to,
                    float rate, float sizeA, float sizeB, float riseA, float riseB,
                    float radius = 1f, float y = 0.05f, float orbit = 0f, int max = 40)
    {
        var ps = NewPS(name, p, mat, y, max);
        Main(ps, 1.6f, 2.6f, sizeA, sizeB);
        CircleXZ(ps, radius, 0.6f);
        Vel(ps, riseA, riseB, orbit, orbit);
        Noise(ps, 0.28f, 0.6f);
        SizeOL(ps, 0f, 0.2f, 0.6f, 1f, 1f, 0f);
        Fade(ps, from, to, 0f, 0f, 0.15f, 1f, 0.85f, 1f, 1f, 0f);
        Rate(ps, rate);
        return ps;
    }

    // Màn khói/sương bám sát mặt đất, alpha thấp để không che model
    static PS LowHaze(Transform p, string name, Color c, float alpha, float rate = 4f)
    {
        var ps = NewPS(name, p, mSmoke, 0.12f, 18);
        Main(ps, 2.6f, 3.8f, 0.9f, 1.5f, 180f);
        CircleXZ(ps, 0.9f);
        Vel(ps, 0.05f, 0.18f);
        RotOL(ps, -12f, 12f);
        RandomFrame(ps, 2, 2);
        SizeOL(ps, 0f, 0.6f, 1f, 1f);
        Fade(ps, c, c * 0.65f, 0f, 0f, 0.3f, alpha, 0.75f, alpha, 1f, 0f);
        Rate(ps, rate);
        Rend(ps).sortingFudge = 12f;
        return ps;
    }

    // ================= Accent prefabs =================
    static List<HalloweenVfxLibrary.AccentEntry> BuildBiomeAccentPrefabs()
    {
        System.IO.Directory.CreateDirectory(AccentDir);
        var list = new List<HalloweenVfxLibrary.AccentEntry>();

        void Add(Biome b, System.Action<Transform> build)
        {
            var root = new GameObject("HW_Accent_" + b);
            var accent = root.AddComponent<HalloweenBiomeAccent>();
            var so = new SerializedObject(accent);
            so.FindProperty("biome").intValue = (int)b;
            so.ApplyModifiedPropertiesWithoutUndo();
            build(root.transform);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{AccentDir}/HW_Accent_{b}.prefab");
            Object.DestroyImmediate(root);
            list.Add(new HalloweenVfxLibrary.AccentEntry
            {
                biome = b,
                prefab = prefab.GetComponent<HalloweenBiomeAccent>()
            });
        }

        // Lá khô rơi + đom đóm: rừng về đêm
        Add(Biome.Forest, t =>
        {
            Fall(t, "DeadLeaves", PetalMat(), LeafRust, new Color(0.45f, 0.22f, 0.06f), 5f, 0.16f, 0.26f, 0.5f, 0.9f, 140f);
            Drift(t, "Fireflies", mGlow, Firefly, Firefly * 0.4f, 5f, 0.05f, 0.1f, 0.15f, 0.5f, 1.05f, 0.4f, 0.8f, 24);
        });

        // Bọt nước nổi + sương mặt hồ
        Add(Biome.Lake, t =>
        {
            Drift(t, "WaterBubbles", mBubble, PaleCyan, PaleCyan, 8f, 0.07f, 0.16f, 0.3f, 0.6f, 0.9f, 0.04f, 0f, 28);
            LowHaze(t, "LakeMist", new Color(0.6f, 0.8f, 0.88f), 0.22f);
        });

        // Bụi cát xoáy quanh chân + hạt cát lấp lánh
        Add(Biome.Desert, t =>
        {
            LowHaze(t, "SandHaze", Sand, 0.26f, 5f);
            Drift(t, "SandGrains", mSpark, Sand, new Color(0.7f, 0.55f, 0.3f), 7f, 0.04f, 0.09f, 0.1f, 0.35f, 1f, 0.05f, 1.1f, 30);
        });

        // Bào tử độc lơ lửng + hơi ẩm xanh
        Add(Biome.Jungle, t =>
        {
            Drift(t, "Spores", mGlow, Toxic, Toxic * 0.35f, 9f, 0.05f, 0.12f, 0.12f, 0.4f, 1.05f, 0.1f, 0.5f, 34);
            LowHaze(t, "JungleDamp", new Color(0.2f, 0.45f, 0.18f), 0.24f);
        });

        // Bông tuyết rơi chậm + hơi lạnh
        Add(Biome.Snow, t =>
        {
            Fall(t, "Snowflakes", mSpark, Frost, Frost, 9f, 0.05f, 0.11f, 0.25f, 0.5f, 0f, 2.4f, 1.15f, 36);
            LowHaze(t, "ColdBreath", new Color(0.75f, 0.88f, 1f), 0.2f, 3.5f);
        });

        // Than hồng bay lên + khói tối: nguồn nhiệt dưới chân
        Add(Biome.Volcano, t =>
        {
            Drift(t, "LavaEmbers", mGlow, new Color(1f, 0.8f, 0.3f), Lava, 10f, 0.05f, 0.12f, 0.6f, 1.2f, 0.95f, 0.05f, 0f, 36);
            LowHaze(t, "HeatSmoke", new Color(0.18f, 0.12f, 0.1f), 0.3f, 4.5f);
        });

        // Bọt khí áp suất sâu + đốm phát quang sinh học
        Add(Biome.AbyssOcean, t =>
        {
            Drift(t, "PressureBubbles", mBubble, Abyss, Abyss, 7f, 0.06f, 0.18f, 0.35f, 0.75f, 0.9f, 0.04f, 0f, 30);
            Drift(t, "Bioluminescence", mGlow, Abyss, new Color(0.35f, 0.5f, 1f), 4f, 0.06f, 0.13f, 0.1f, 0.3f, 1.1f, 0.6f, 0.9f, 20);
        });

        // Tro núi lửa cổ rơi + hổ phách lơ lửng
        Add(Biome.Prehistoric, t =>
        {
            Fall(t, "AshFall", mSmoke, Ash, Ash * 0.5f, 5f, 0.12f, 0.24f, 0.3f, 0.6f, 60f, 2.3f, 1.1f, 24);
            Drift(t, "AmberMotes", mGlow, Amber, Amber * 0.4f, 5f, 0.05f, 0.11f, 0.15f, 0.45f, 1f, 0.1f, 0.6f, 22);
        });

        // Bụi sao quay quanh + tia lấp lánh
        Add(Biome.Cosmic, t =>
        {
            Drift(t, "Stardust", mSpark, Violet, new Color(0.95f, 0.85f, 1f), 8f, 0.05f, 0.12f, 0.1f, 0.35f, 1.15f, 0.5f, 1.4f, 32);
            Drift(t, "VoidMotes", mGlow, new Color(0.45f, 0.25f, 0.9f), Violet, 4f, 0.07f, 0.15f, 0.2f, 0.5f, 0.95f, 0.15f, 0f, 20);
        });

        // Cánh hoa rơi + đốm hồng: giữ nguyên nét Oni/Nhật của biome
        Add(Biome.CherryBlossom, t =>
        {
            Fall(t, "Petals", PetalMat(), Sakura, new Color(0.8f, 0.4f, 0.7f), 7f, 0.14f, 0.24f, 0.45f, 0.85f, 150f);
            Drift(t, "BlossomMotes", mGlow, Sakura, Sakura * 0.4f, 4f, 0.05f, 0.1f, 0.15f, 0.45f, 1.05f, 0.1f, 0.5f, 20);
        });

        // Bụi vàng + mảnh rune nhỏ: đền của Titan
        Add(Biome.TitanTemple, t =>
        {
            Drift(t, "GoldDust", mSpark, Gold, new Color(1f, 0.6f, 0.2f), 8f, 0.05f, 0.12f, 0.15f, 0.45f, 1.05f, 0.08f, 0.9f, 30);
            var rune = Rune(t, Gold, 1.7f);
            // Rune của zone dày quá cho accent, hạ alpha để không tranh với ring của aura
            Fade(rune, Gold, Gold, 0f, 0.3f, 1f, 0.3f);
        });

        // Hai nửa đối nghịch: tàn lửa đỏ chìm xuống, hạt sáng bay lên
        Add(Biome.AngelsDemons, t =>
        {
            Drift(t, "HolyMotes", mGlow, Holy, new Color(1f, 0.85f, 0.45f), 6f, 0.05f, 0.12f, 0.5f, 1f, 0.9f, 0.1f, 0f, 26);
            Fall(t, "FallenEmbers", mGlow, new Color(1f, 0.35f, 0.2f), new Color(0.5f, 0.05f, 0.05f), 6f, 0.05f, 0.11f, 0.4f, 0.8f, 0f, 2.1f, 1f, 26);
        });

        // Nhớt nhỏ giọt + khí thối
        Add(Biome.Monster, t =>
        {
            Fall(t, "SlimeDrip", mBubble, Sick, new Color(0.25f, 0.45f, 0.08f), 5f, 0.08f, 0.18f, 0.7f, 1.2f, 0f, 1.9f, 0.95f, 24);
            LowHaze(t, "StenchFog", new Color(0.3f, 0.2f, 0.35f), 0.28f);
        });

        // Hỗn loạn có kiểm soát: confetti kẹo + tia nhiễu
        Add(Biome.Brainrot, t =>
        {
            var conf = Fall(t, "Confetti", mCorn, Rot, Firefly, 8f, 0.1f, 0.2f, 0.5f, 1f, 220f, 2.2f, 1.1f, 32);
            Noise(conf, 0.8f, 1.6f);
            Drift(t, "GlitchSparks", mSpark, Rot, new Color(0.3f, 1f, 0.9f), 7f, 0.05f, 0.14f, 0.2f, 0.7f, 1.1f, 0.3f, 2f, 28);
        });

        return list;
    }
}
