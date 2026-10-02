using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using PS = UnityEngine.ParticleSystem;

// Môi trường Halloween cho map Steal an Egg HLW: làm trên bản copy của scene, ánh sáng + VFX trời + Base + 12 khu theo Biome
// Chạy lại được: xoá root HW_Environment cũ rồi dựng lại
public static partial class HalloweenVfxBuilder
{
    const string MapSrc = "Assets/_Raw/Scenes/Steal an Egg HLW.unity";
    const string MapDst = "Assets/_Raw/Scenes/Steal an Egg HLW_VFX.unity";
    const string MapDir = Root + "/Map";
    const string EnvRootName = "HW_Environment";
    const float FloorY = 1f;
    const float HalfWidth = 28f;

    struct ZoneInfo
    {
        public string name;
        public float xStart, xEnd; // xStart > xEnd, đường chạy theo -X
        public Biome biome;
        public Color color;
        public ZoneInfo(string n, float s, float e, Biome b, Color c) { name = n; xStart = s; xEnd = e; biome = b; color = c; }
    }

    // Giả định Z1..Z12 theo thứ tự enum Biome (đối chiếu màu nền: Z3 sa mạc, Z5 băng, Z6 lava, Z7 xanh đậm...)
    static readonly ZoneInfo[] MapZones =
    {
        new ZoneInfo("Z1", -40.3f, -79.4f, Biome.Forest, new Color(0.55f, 1f, 0.35f)),
        new ZoneInfo("Z2", -79.4f, -137.8f, Biome.Lake, new Color(0.4f, 0.85f, 1f)),
        new ZoneInfo("Z3", -137.8f, -222.2f, Biome.Desert, new Color(1f, 0.7f, 0.3f)),
        new ZoneInfo("Z4", -222.2f, -316.6f, Biome.Jungle, new Color(0.4f, 1f, 0.5f)),
        new ZoneInfo("Z5", -316.6f, -446.2f, Biome.Snow, new Color(0.7f, 0.9f, 1f)),
        new ZoneInfo("Z6", -446.2f, -600.2f, Biome.Volcano, new Color(1f, 0.4f, 0.1f)),
        new ZoneInfo("Z7", -600.2f, -771.8f, Biome.AbyssOcean, new Color(0.3f, 0.6f, 1f)),
        new ZoneInfo("Z8", -771.8f, -973.8f, Biome.Prehistoric, new Color(0.9f, 0.6f, 0.3f)),
        new ZoneInfo("Z9", -973.8f, -1229.8f, Biome.Cosmic, new Color(0.75f, 0.45f, 1f)),
        new ZoneInfo("Z10", -1229.8f, -1525.8f, Biome.CherryBlossom, new Color(1f, 0.5f, 0.8f)),
        new ZoneInfo("Z11", -1525.8f, -1851.7f, Biome.TitanTemple, new Color(1f, 0.85f, 0.4f)),
        new ZoneInfo("Z12", -1851.7f, -2207.5f, Biome.AngelsDemons, new Color(1f, 0.95f, 0.7f)),
    };

    static readonly Color Warm = new Color(1f, 0.85f, 0.45f);
    static readonly Color Lavender = new Color(0.62f, 0.5f, 0.85f);

    static Material mSkyMoon, mSkyGlow, mSkySpark, mSkyTrail, mPetal;

    [MenuItem("Tools/Halloween VFX/Build Map (Steal an Egg HLW)")]
    public static void BuildMap()
    {
        Directory.CreateDirectory(MapDir);
        try
        {
            EditorUtility.DisplayProgressBar("Halloween Map", "Materials", 0.1f);
            LoadMaterials();
            BuildMapMaterials();
            EditorUtility.DisplayProgressBar("Halloween Map", "Scene", 0.4f);
            var scene = OpenMapCopy();
            ApplyMapLighting(scene);
            EditorUtility.DisplayProgressBar("Halloween Map", "Environment VFX", 0.6f);
            BuildEnvironment(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[HalloweenVFX] Map: {MapDst}");
        }
        finally { EditorUtility.ClearProgressBar(); }
    }

    // ================= Materials =================
    static void LoadMaterials()
    {
        Material M(string n) => AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{n}.mat");
        mGlow = M("M_HW_Glow_Add");
        if (!mGlow) { BuildTexturesAndMaterials(); return; }
        mRing = M("M_HW_Ring_Add");
        mRingThin = M("M_HW_RingThin_Add");
        mWisp = M("M_HW_Wisp_Add");
        mTrail = M("M_HW_Trail_Add");
        mSpark = M("M_HW_Spark_Add");
        mBeam = M("M_HW_Beam_Add");
        mSmoke = M("M_HW_Smoke_Alpha");
        mBubble = M("M_HW_Bubble_Add");
        mBat = M("M_HW_Bat_Alpha");
        mPumpkin = M("M_HW_Pumpkin_Alpha");
        mGhost = M("M_HW_Ghost_Alpha");
        mCorn = M("M_HW_CandyCorn_Alpha");
        mRune = M("M_HW_RuneCircle_Add");
        mWeb = M("M_HW_Web_Add");
        mSkull = M("M_HW_Skull_Add");
        mCandy = AssetDatabase.LoadAssetAtPath<Material>(CandyMatPath);
    }

    static void BuildMapMaterials()
    {
        Texture2D T(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexDir}/{n}.png");
        var moon = HalloweenTextureGen.Moon($"{TexDir}/T_HW_Moon.png");
        var petal = HalloweenTextureGen.Petal($"{TexDir}/T_HW_Petal.png");
        mSkyMoon = SkyMat("M_HW_Sky_Moon", moon);
        mSkyGlow = SkyMat("M_HW_Sky_Glow", T("T_HW_Glow"));
        mSkySpark = SkyMat("M_HW_Sky_Spark", T("T_HW_Spark"));
        mSkyTrail = SkyMat("M_HW_Sky_Trail", T("T_HW_Trail"));
        mPetal = ParticleMat("M_HW_Petal_Alpha", petal, false);
    }

    static Material SkyMat(string name, Texture tex)
    {
        var mat = LoadOrCreate(name, Shader.Find("Halloween/SkyGlow"));
        mat.SetTexture("_MainTex", tex);
        mat.SetColor("_Color", Color.white);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ================= Scene & lighting =================
    static Scene OpenMapCopy()
    {
        if (!File.Exists(MapDst)) AssetDatabase.CopyAsset(MapSrc, MapDst);
        var sc = SceneManager.GetSceneByPath(MapDst);
        if (!sc.isLoaded) sc = EditorSceneManager.OpenScene(MapDst, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(sc);
        return sc;
    }

    static GameObject FindRoot(Scene sc, string name)
    {
        foreach (var go in sc.GetRootGameObjects())
            if (go.name == name) return go;
        return null;
    }

    static void ApplyMapLighting(Scene sc)
    {
        // Fog tím thay cho fog đen: xa thì mờ huyền ảo chứ không chìm vào bóng tối
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.2f, 0.12f, 0.32f);
        RenderSettings.fogStartDistance = 35f;
        RenderSettings.fogEndDistance = 280f;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.45f, 0.36f, 0.56f);
        RenderSettings.ambientEquatorColor = new Color(0.34f, 0.22f, 0.34f);
        RenderSettings.ambientGroundColor = new Color(0.24f, 0.13f, 0.1f);
        RenderSettings.ambientIntensity = 1f;

        // Đèn phụ: tắt bóng (hết cảnh báo shadow atlas), giảm cường độ vì đã có trăng + ambient
        foreach (var go in sc.GetRootGameObjects())
            foreach (var l in go.GetComponentsInChildren<Light>(true))
            {
                if (l.type == LightType.Directional || l.shadows == LightShadows.None) continue;
                l.shadows = LightShadows.None;
                l.intensity *= 0.6f;
            }

        foreach (var go in sc.GetRootGameObjects())
        {
            var cam = go.GetComponent<Camera>();
            if (cam) cam.farClipPlane = Mathf.Max(cam.farClipPlane, 800f);
        }

        ApplyMapVolume(sc);
    }

    static void ApplyMapVolume(Scene sc)
    {
        var volGo = FindRoot(sc, "Global Volume");
        var vol = volGo ? volGo.GetComponent<Volume>() : null;
        if (!vol) return;

        string path = $"{MapDir}/HW_MapVolume.asset";
        var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (!prof)
        {
            // Copy profile gốc để không ảnh hưởng scene khác đang dùng chung
            prof = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(prof, path);
            if (vol.sharedProfile)
                foreach (var c in vol.sharedProfile.components)
                {
                    var cc = Object.Instantiate(c);
                    cc.name = c.name;
                    prof.components.Add(cc);
                    AssetDatabase.AddObjectToAsset(cc, prof);
                }
        }

        T Get<T>() where T : VolumeComponent
        {
            if (prof.TryGet(out T comp)) return comp;
            comp = prof.Add<T>(true);
            AssetDatabase.AddObjectToAsset(comp, prof);
            return comp;
        }

        var bloom = Get<Bloom>();
        bloom.active = true;
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(0.9f);
        bloom.scatter.Override(0.65f);

        var ca = Get<ColorAdjustments>();
        ca.active = true;
        ca.postExposure.Override(0.25f);
        ca.contrast.Override(8f);
        ca.saturation.Override(12f);

        var vig = Get<Vignette>();
        vig.active = true;
        vig.intensity.Override(0.22f);
        vig.color.Override(new Color(0.08f, 0.02f, 0.12f));

        EditorUtility.SetDirty(prof);
        vol.sharedProfile = prof;
    }

    // ================= Environment =================
    static void BuildEnvironment(Scene sc)
    {
        var old = FindRoot(sc, EnvRootName);
        if (old) Object.DestroyImmediate(old);
        var env = new GameObject(EnvRootName).transform;

        BuildMoonLight(env);
        BuildSky(env, sc);
        BuildBase(env, sc);
        BuildLampGlows(env, sc);
        foreach (var z in MapZones) BuildMapZone(env, z);
    }

    static void BuildMoonLight(Transform env)
    {
        var go = new GameObject("HW_MoonLight");
        go.transform.SetParent(env, false);
        go.transform.rotation = Quaternion.Euler(40f, 60f, 0f);
        var l = go.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(0.7f, 0.68f, 1f);
        l.intensity = 0.9f;
        l.shadows = LightShadows.Soft;
        l.shadowStrength = 0.6f;
        RenderSettings.sun = l;
    }

    static void BuildSky(Transform env, Scene sc)
    {
        var sky = new GameObject("Sky").transform;
        sky.SetParent(env, false);
        Vector3 camPos = new Vector3(-26f, 3f, -17f);
        foreach (var go in sc.GetRootGameObjects())
            if (go.GetComponent<Camera>()) camPos = go.transform.position;
        sky.position = camPos;
        sky.gameObject.AddComponent<HWSkyFollow>();

        // Trăng + 2 lớp quầng sáng
        var moonPos = new Vector3(-220f, 120f, 140f);
        PointSprites("MoonHaloBig", sky, mSkyGlow, new[] { moonPos }, 150f, new Color(0.6f, 0.45f, 1f, 0.45f));
        PointSprites("MoonHalo", sky, mSkyGlow, new[] { moonPos }, 70f, new Color(1f, 0.92f, 0.8f, 0.6f));
        PointSprites("Moon", sky, mSkyMoon, new[] { moonPos }, 42f, Color.white);

        // Đàn dơi bay ngang trời, mỗi 8 giây
        var bats = NewPS("SkyBats", sky, mBat, 0f, 20);
        bats.transform.localPosition = new Vector3(-60f, 30f, 70f);
        var bm = bats.main;
        bm.prewarm = false;
        bm.duration = 8f;
        bm.simulationSpace = ParticleSystemSimulationSpace.World;
        Main(bats, 9f, 11f, 1.2f, 1.8f);
        Box(bats, new Vector3(40f, 10f, 4f));
        var be = bats.emission;
        be.enabled = true;
        be.rateOverTime = 0f;
        be.SetBursts(new[] { new PS.Burst(0.5f, 5, 8) });
        VelXYZ(bats, new Vector2(-2f, 2f), new Vector2(-0.5f, 0.8f), new Vector2(-18f, -14f));
        Noise(bats, 1.5f, 0.3f);
        Flap(bats, 25);
        Fade(bats, BatDark, BatDark, 0f, 0f, 0.05f, 1f, 0.9f, 1f, 1f, 0f);

        // Sao băng
        var stars = NewPS("ShootingStars", sky, mSkySpark, 0f, 4);
        stars.transform.localPosition = new Vector3(-150f, 140f, 0f);
        var sm = stars.main;
        sm.prewarm = false;
        sm.duration = 4f;
        sm.simulationSpace = ParticleSystemSimulationSpace.World;
        Main(stars, 1.1f, 1.3f, 2f, 3f);
        Box(stars, new Vector3(220f, 20f, 220f));
        var se = stars.emission;
        se.enabled = true;
        se.rateOverTime = 0f;
        se.SetBursts(new[] { new PS.Burst(1f, 1) { probability = 0.6f } });
        VelXYZ(stars, new Vector2(-80f, -60f), new Vector2(-35f, -25f), new Vector2(-10f, 10f));
        Fade(stars, Color.white, new Color(0.8f, 0.7f, 1f), 0f, 0f, 0.1f, 1f, 1f, 0f);
        var t = stars.trails;
        t.enabled = true;
        t.ratio = 1f;
        t.lifetime = 0.5f;
        t.minVertexDistance = 0.5f;
        t.inheritParticleColor = true;
        t.widthOverTrail = new PS.MinMaxCurve(0.5f, Crv(0f, 1f, 1f, 0f));
        Rend(stars).trailMaterial = mSkyTrail;
    }

    static void BuildBase(Transform env, Scene sc)
    {
        var root = new GameObject("Base").transform;
        root.SetParent(env, false);

        // Đèn bí ngô ở 4 góc mỗi chuồng trứng
        var pts = new System.Collections.Generic.List<Vector3>();
        var baseGo = FindRoot(sc, "Base");
        if (baseGo)
        {
            for (int i = 1; i <= 7; i++)
            {
                var pen = baseGo.transform.Find($"Cube_00{i}");
                if (!pen) continue;
                var rs = pen.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) continue;
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                float y = b.max.y + 0.7f;
                pts.Add(new Vector3(b.min.x, y, b.min.z));
                pts.Add(new Vector3(b.min.x, y, b.max.z));
                pts.Add(new Vector3(b.max.x, y, b.min.z));
                pts.Add(new Vector3(b.max.x, y, b.max.z));
            }
        }
        var lan = pts.ToArray();
        var ground = System.Array.ConvertAll(lan, p => new Vector3(p.x, FloorY + 0.9f, p.z));
        var flame = System.Array.ConvertAll(lan, p => p + new Vector3(0f, 0.6f, 0f));
        PointSprites("LanternGroundGlow", root, mGlow, ground, 8f, new Color(1f, 0.55f, 0.15f, 0.55f), true, 40f);
        PointSprites("LanternHalo", root, mGlow, lan, 3.5f, new Color(1f, 0.6f, 0.2f, 0.6f));
        PointSprites("LanternPumpkin", root, mPumpkin, lan, 1.5f, Color.white);
        PointFlames("LanternFlame", root, flame, 0.5f, new Color(1f, 0.9f, 0.45f), Orange);

        // Đom đóm + sương tím mỏng trên toàn Base
        Fireflies(root, new Vector3(-2.6f, FloorY + 4.5f, 0f), new Vector3(80f, 7f, 175f), Warm, 30f);
        var mist = AmbientBox("BaseMist", root, mSmoke, new Vector3(-2.6f, FloorY + 0.6f, 0f), new Vector3(80f, 0.5f, 175f), 8f, 50);
        Main(mist, 6f, 8f, 6f, 10f, 180f);
        RandomFrame(mist, 2, 2);
        RotOL(mist, -8f, 8f);
        SizeOL(mist, 0f, 0.6f, 1f, 1f);
        Fade(mist, Lavender, Lavender, 0f, 0f, 0.3f, 0.22f, 0.7f, 0.22f, 1f, 0f);
    }

    // Biến 17 point light thành đèn lồng bí ngô treo, có quầng sáng và vũng sáng dưới đất
    static void BuildLampGlows(Transform env, Scene sc)
    {
        var lightsGo = FindRoot(sc, "Lights");
        if (!lightsGo) return;
        var list = new System.Collections.Generic.List<Vector3>();
        foreach (var l in lightsGo.GetComponentsInChildren<Light>()) if (l.type == LightType.Point) list.Add(l.transform.position);
        if (list.Count == 0) return;

        var root = new GameObject("Lamps").transform;
        root.SetParent(env, false);
        var lamps = list.ToArray();
        var ground = System.Array.ConvertAll(lamps, p => new Vector3(p.x, FloorY + 0.08f, p.z));
        PointSprites("LampGroundPool", root, mGlow, ground, 30f, new Color(1f, 0.55f, 0.2f, 0.22f), true, 45f);
        PointSprites("LampHalo", root, mGlow, lamps, 14f, new Color(1f, 0.6f, 0.25f, 0.4f));
        PointSprites("LampPumpkin", root, mPumpkin, lamps, 3.2f, Color.white);
    }

    static void BuildMapZone(Transform env, ZoneInfo z)
    {
        var root = new GameObject($"Zone_{z.name}_{z.biome}").transform;
        root.SetParent(env, false);
        root.gameObject.AddComponent<HWZoneCuller>().Setup(z.xEnd, z.xStart);

        float len = z.xStart - z.xEnd;
        float cx = (z.xStart + z.xEnd) * 0.5f;

        BuildGate(root, z.xStart, z.color);
        Fireflies(root, new Vector3(cx, FloorY + 4.5f, 0f), new Vector3(len, 7f, HalfWidth * 2f), Color.Lerp(Warm, z.color, 0.35f), Mathf.Clamp(len * 0.2f, 6f, 30f));
        BiomeAccent(root, z, cx, len);
    }

    static void BuildGate(Transform parent, float x, Color c)
    {
        var g = new GameObject("Gate").transform;
        g.SetParent(parent, false);
        g.position = new Vector3(x, FloorY, 0f);

        // Cột sáng 2 bên tường theo màu biome
        foreach (float side in new[] { -1f, 1f })
        {
            var beam = NewPS("GateBeam", g, mBeam, 7f, 3);
            beam.transform.localPosition = new Vector3(0f, 7f, side * (HalfWidth + 1f));
            var m = beam.main;
            m.startLifetime = 2.5f;
            m.startSize3D = true;
            m.startSizeX = 6f;
            m.startSizeY = 14f;
            m.startSizeZ = 1f;
            Rate(beam, 0.8f);
            var r = Rend(beam);
            r.renderMode = ParticleSystemRenderMode.VerticalBillboard;
            r.sortingFudge = 20f;
            Fade(beam, c, c, 0f, 0f, 0.4f, 0.55f, 1f, 0f);
        }

        // Hạt sáng bay lên dọc vạch cổng
        var motes = NewPS("GateMotes", g, mGlow, 0.2f, 80);
        Box(motes, new Vector3(1.5f, 0.2f, HalfWidth * 2f));
        Main(motes, 1.5f, 2.5f, 0.15f, 0.3f);
        VelXYZ(motes, Vector2.zero, new Vector2(0.8f, 1.6f), Vector2.zero);
        SizeOL(motes, 0f, 1f, 1f, 0f);
        Fade(motes, c, Color.white, 0f, 0f, 0.15f, 1f, 1f, 0f);
        Rate(motes, 25f);

        // Sương tím thấp ở cửa khu
        var mist = NewPS("GateMist", g, mSmoke, 0.6f, 30);
        Box(mist, new Vector3(6f, 0.5f, HalfWidth * 2f));
        Main(mist, 4f, 6f, 4f, 7f, 180f);
        RandomFrame(mist, 2, 2);
        RotOL(mist, -10f, 10f);
        SizeOL(mist, 0f, 0.6f, 1f, 1f);
        Fade(mist, Color.Lerp(Lavender, c, 0.3f), Lavender, 0f, 0f, 0.3f, 0.3f, 0.7f, 0.3f, 1f, 0f);
        Rate(mist, 5f);
    }

    static void BiomeAccent(Transform root, ZoneInfo z, float cx, float len)
    {
        PS ps;
        switch (z.biome)
        {
            case Biome.Forest:
                GhostDrift(root, cx, len, new Color(0.85f, 1f, 0.9f));
                ps = Accent("GreenWisps", root, mWisp, cx, len, FloorY + 1f, 5f, 4f, 10, 4f, 6f, 0.4f, 0.6f, z.color, Warm);
                Noise(ps, 1f, 0.3f);
                break;
            case Biome.Lake:
                Bubbles(root, cx, len, z.color, 30f);
                break;
            case Biome.Desert:
                ps = Accent("SandMist", root, mSmoke, cx, len, FloorY, 1f, 7f, 30, 5f, 7f, 4f, 7f, new Color(0.85f, 0.55f, 0.4f), new Color(0.6f, 0.35f, 0.5f), 0.3f);
                VelXYZ(ps, new Vector2(-1.5f, -0.5f), Vector2.zero, new Vector2(-0.3f, 0.3f));
                RandomFrame(ps, 2, 2);
                RotOL(ps, -15f, 15f);
                ps = Accent("DesertWisps", root, mWisp, cx, len, FloorY + 1f, 4f, 3f, 10, 4f, 6f, 0.4f, 0.6f, z.color, Red);
                Noise(ps, 1f, 0.3f);
                break;
            case Biome.Jungle:
                ps = Accent("JungleFireflies", root, mGlow, cx, len, FloorY + 1f, 6f, 25f, 100, 3f, 5f, 0.15f, 0.3f, z.color, Warm);
                Noise(ps, 0.8f, 0.3f);
                ps = Accent("JungleBats", root, mBat, cx, len, FloorY + 7f, 6f, 3f, 12, 6f, 8f, 0.8f, 1.2f, BatDark, BatDark);
                VelXYZ(ps, new Vector2(-4f, 4f), Vector2.zero, new Vector2(-3f, 3f));
                Noise(ps, 1.5f, 0.3f);
                Flap(ps, 30);
                break;
            case Biome.Snow:
                ps = Accent("GhostSnow", root, mGlow, cx, len, FloorY + 14f, 2f, 60f, 300, 7f, 9f, 0.12f, 0.25f, new Color(0.75f, 0.85f, 1f), new Color(0.75f, 0.85f, 1f));
                VelXYZ(ps, new Vector2(-0.3f, 0.3f), new Vector2(-2.2f, -1.5f), new Vector2(-0.3f, 0.3f));
                Noise(ps, 0.4f, 0.5f);
                GhostDrift(root, cx, len, new Color(0.75f, 0.9f, 1f));
                break;
            case Biome.Volcano:
                Embers(root, cx, len, new Color(1f, 0.7f, 0.2f), new Color(1f, 0.2f, 0.05f), 50f);
                ps = Accent("AshSmoke", root, mSmoke, cx, len, FloorY, 2f, 4f, 25, 6f, 8f, 6f, 10f, new Color(0.35f, 0.15f, 0.2f), new Color(0.2f, 0.1f, 0.15f), 0.35f);
                VelXYZ(ps, Vector2.zero, new Vector2(0.3f, 0.6f), Vector2.zero);
                RandomFrame(ps, 2, 2);
                break;
            case Biome.AbyssOcean:
                Bubbles(root, cx, len, z.color, 25f);
                ps = Accent("GhostJelly", root, mWisp, cx, len, FloorY + 2f, 8f, 3f, 12, 6f, 8f, 0.8f, 1.2f, new Color(0.5f, 0.8f, 1f), new Color(0.8f, 0.5f, 1f));
                VelXYZ(ps, Vector2.zero, new Vector2(0.2f, 0.5f), Vector2.zero);
                Noise(ps, 0.8f, 0.2f);
                break;
            case Biome.Prehistoric:
                Embers(root, cx, len, new Color(1f, 0.6f, 0.2f), Red, 15f);
                GhostDrift(root, cx, len, new Color(0.8f, 1f, 0.7f));
                break;
            case Biome.Cosmic:
                ps = Accent("Twinkle", root, mSpark, cx, len, FloorY + 2f, 25f, 30f, 150, 1.5f, 3f, 0.4f, 1f, Color.white, z.color);
                SizeOL(ps, 0f, 0f, 0.5f, 1f, 1f, 0f);
                break;
            case Biome.CherryBlossom:
                ps = Accent("Petals", root, mPetal, cx, len, FloorY + 12f, 4f, 30f, 250, 8f, 10f, 0.25f, 0.4f, new Color(1f, 0.55f, 0.8f), new Color(0.75f, 0.45f, 1f));
                VelXYZ(ps, new Vector2(-0.8f, -0.2f), new Vector2(-1.4f, -0.8f), new Vector2(-0.3f, 0.3f));
                Noise(ps, 0.6f, 0.4f);
                RotOL(ps, -120f, 120f);
                break;
            case Biome.TitanTemple:
                Embers(root, cx, len, new Color(1f, 0.9f, 0.5f), new Color(1f, 0.6f, 0.2f), 20f);
                ps = Accent("GoldSparks", root, mSpark, cx, len, FloorY + 2f, 12f, 12f, 60, 1f, 2f, 0.3f, 0.7f, Color.white, z.color);
                SizeOL(ps, 0f, 0f, 0.5f, 1f, 1f, 0f);
                break;
            default: // AngelsDemons và biome khác
                ps = Accent("HolySparks", root, mSpark, cx, len, FloorY + 2f, 15f, 15f, 80, 1f, 2f, 0.3f, 0.8f, Color.white, Gold);
                SizeOL(ps, 0f, 0f, 0.5f, 1f, 1f, 0f);
                ps = GhostDrift(root, cx, len, Color.white);
                VelXYZ(ps, Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero);
                break;
        }
    }

    // ================= Map helpers =================
    static void Box(PS ps, Vector3 size)
    {
        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = size;
        sh.rotation = Vector3.zero;
    }

    static void VelXYZ(PS ps, Vector2 x, Vector2 y, Vector2 z)
    {
        var v = ps.velocityOverLifetime;
        v.enabled = true;
        v.space = ParticleSystemSimulationSpace.Local;
        v.x = R(x.x, x.y);
        v.y = R(y.x, y.y);
        v.z = R(z.x, z.y);
        // Orbital/offset/radial phải cùng mode (TwoConstants) nếu không Unity báo lỗi khi simulate
        v.orbitalX = R(0f, 0f);
        v.orbitalY = R(0f, 0f);
        v.orbitalZ = R(0f, 0f);
        v.orbitalOffsetX = R(0f, 0f);
        v.orbitalOffsetY = R(0f, 0f);
        v.orbitalOffsetZ = R(0f, 0f);
        v.radial = R(0f, 0f);
    }

    static void Flap(PS ps, int cycles)
    {
        var t = ps.textureSheetAnimation;
        t.enabled = true;
        t.numTilesX = 4;
        t.numTilesY = 1;
        t.frameOverTime = new PS.MinMaxCurve(1f, Crv(0f, 0f, 1f, 1f));
        t.cycleCount = cycles;
    }

    static PS AmbientBox(string name, Transform parent, Material mat, Vector3 center, Vector3 size, float rate, int max)
    {
        var ps = NewPS(name, parent, mat, 0f, max);
        ps.transform.position = center;
        Box(ps, size);
        Rate(ps, rate);
        return ps;
    }

    // Hiệu ứng rải đều trong hộp bao cả khu; rate tính theo mỗi 100m chiều dài
    static PS Accent(string name, Transform parent, Material mat, float cx, float len, float y0, float h, float ratePer100, int max,
        float lifeA, float lifeB, float sizeA, float sizeB, Color from, Color to, float alpha = 1f)
    {
        var ps = AmbientBox(name, parent, mat, new Vector3(cx, y0 + h * 0.5f, 0f), new Vector3(len, h, HalfWidth * 2f),
            Mathf.Clamp(len * ratePer100 / 100f, 2f, 60f), max);
        Main(ps, lifeA, lifeB, sizeA, sizeB, 180f);
        Fade(ps, from, to, 0f, 0f, 0.2f, alpha, 0.8f, alpha, 1f, 0f);
        return ps;
    }

    static PS Fireflies(Transform parent, Vector3 center, Vector3 size, Color c, float rate)
    {
        var ps = AmbientBox("Fireflies", parent, mGlow, center, size, rate, 150);
        Main(ps, 3f, 6f, 0.18f, 0.35f);
        Noise(ps, 0.8f, 0.25f);
        Fade(ps, c, c, 0f, 0f, 0.2f, 1f, 0.4f, 0.35f, 0.6f, 1f, 0.8f, 0.4f, 1f, 0f); // nhấp nháy nhẹ
        return ps;
    }

    static PS GhostDrift(Transform root, float cx, float len, Color c)
    {
        var ps = Accent("GhostDrift", root, mGhost, cx, len, FloorY + 1f, 6f, 1.5f, 8, 5f, 7f, 0.9f, 1.4f, c, c, 0.7f);
        Noise(ps, 0.6f, 0.2f);
        VelXYZ(ps, Vector2.zero, new Vector2(0.1f, 0.3f), Vector2.zero);
        return ps;
    }

    static PS Bubbles(Transform root, float cx, float len, Color c, float ratePer100)
    {
        var ps = Accent("GlowBubbles", root, mBubble, cx, len, FloorY, 0.3f, ratePer100, 60, 2f, 3.5f, 0.2f, 0.5f, c, c);
        VelXYZ(ps, Vector2.zero, new Vector2(1f, 2f), Vector2.zero);
        Noise(ps, 0.3f, 0.5f);
        return ps;
    }

    static PS Embers(Transform root, float cx, float len, Color hot, Color cool, float ratePer100)
    {
        var ps = Accent("Embers", root, mGlow, cx, len, FloorY, 0.5f, ratePer100, 200, 2f, 3.5f, 0.1f, 0.22f, hot, cool);
        VelXYZ(ps, Vector2.zero, new Vector2(1.5f, 3.5f), Vector2.zero);
        Noise(ps, 0.8f, 0.6f);
        return ps;
    }

    // Mesh mỗi điểm là 1 đỉnh, nối thành tam giác vòng (Shape module cần tam giác hợp lệ) để particle phát lần lượt tại từng điểm
    static Mesh PointMesh(string name, Vector3[] pts)
    {
        string path = $"{MapDir}/HW_Pts_{name}.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (!mesh)
        {
            mesh = new Mesh { name = "HW_Pts_" + name };
            AssetDatabase.CreateAsset(mesh, path);
        }
        mesh.Clear();
        mesh.vertices = pts;
        int n = pts.Length;
        var normals = new Vector3[n];
        var tris = new int[n * 3];
        for (int i = 0; i < n; i++)
        {
            normals[i] = Vector3.up;
            tris[i * 3] = i;
            tris[i * 3 + 1] = (i + 1) % n;
            tris[i * 3 + 2] = (i + 2) % n;
        }
        mesh.normals = normals;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    static void PointShape(PS ps, string meshName, Vector3[] pts)
    {
        var sh = ps.shape;
        if (pts.Length < 3)
        {
            // 1-2 điểm (trăng): đặt system tại điểm đầu, không cần mesh
            sh.enabled = false;
            ps.transform.localPosition = pts[0];
            return;
        }
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Mesh;
        sh.meshShapeType = ParticleSystemMeshShapeType.Vertex;
        sh.mesh = PointMesh(meshName, pts);
        sh.meshSpawnMode = ParticleSystemShapeMultiModeValue.Loop;
        sh.position = Vector3.zero;
        sh.rotation = Vector3.zero;
        sh.scale = Vector3.one;
    }

    // Sprite tĩnh tại nhiều điểm bằng 1 particle system: mỗi vòng loop phát đúng 1 hạt/điểm, hạt sống bằng duration
    static PS PointSprites(string name, Transform parent, Material mat, Vector3[] pts, float size, Color c, bool horizontal = false, float fudge = 0f)
    {
        // max = 2x: lúc chuyển vòng loop hạt cũ chưa chết kịp, nếu max = số điểm thì burst mới bị chặn
        var ps = NewPS(name, parent, mat, 0f, pts.Length * 2);
        ps.transform.localPosition = Vector3.zero;
        Permanent(ps, 10f, (short)pts.Length);
        var m = ps.main;
        m.startSize = size;
        m.startColor = c;
        PointShape(ps, $"{parent.name}_{name}", pts);
        if (horizontal) Horizontal(ps, fudge);
        return ps;
    }

    static PS PointFlames(string name, Transform parent, Vector3[] pts, float size, Color hot, Color cool)
    {
        var ps = NewPS(name, parent, mWisp, 0f, Mathf.Max(10, pts.Length * 10));
        ps.transform.localPosition = Vector3.zero;
        Main(ps, 0.45f, 0.6f, size * 0.8f, size);
        var m = ps.main;
        m.duration = 1f;
        Burst(ps, (short)pts.Length, 0, 0.07f); // cycles 0 = lặp vô hạn, mỗi đợt 1 ngọn lửa/điểm
        PointShape(ps, $"{parent.name}_{name}", pts);
        VelXYZ(ps, Vector2.zero, new Vector2(size * 1.5f, size * 2.2f), Vector2.zero);
        Noise(ps, size * 0.15f, 2f);
        SizeOL(ps, 0f, 1f, 1f, 0f);
        Fade(ps, hot, cool, 0f, 1f, 1f, 0f);
        return ps;
    }
}
