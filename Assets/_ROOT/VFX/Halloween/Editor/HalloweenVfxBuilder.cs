using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using PS = UnityEngine.ParticleSystem;

// Dựng toàn bộ VFX Halloween: texture -> material -> layer prefab -> aura theo Rarity -> zone -> library -> demo scene
// Mọi prefab dựng ở bán kính gốc 1 (footprint), HalloweenAura tự scale theo bounds của animal
public static partial class HalloweenVfxBuilder
{
    const string Root = "Assets/_ROOT/VFX/Halloween";
    const string TexDir = Root + "/Textures";
    const string MatDir = Root + "/Materials";
    const string LayerDir = Root + "/Prefabs/Layers";
    const string AuraDir = Root + "/Prefabs/Aura";
    const string ZoneDir = Root + "/Prefabs/Zone";
    const string DemoDir = Root + "/Demo";

    // Asset có sẵn được dùng lại
    const string CandyMatPath = "Assets/_ROOT/VFX/_Flash/Material/candy-s.mat";
    const string SkullTexPath = "Assets/_ROOT/VFX/Aura/Textures/Tx_Skull_01.png";
    const string TigerPath = "Assets/_MODES/StealEgg/Model/Animal/10.CherryBlossom/Prefabs/OniTiger.prefab";
    const string TRexPath = "Assets/_MODES/StealEgg/Model/Animal/8.Prehistoric/Prefabs/TRex.prefab";

    static readonly Color Orange = new Color(1f, 0.48f, 0.1f);
    static readonly Color Purple = new Color(0.55f, 0.2f, 1f);
    static readonly Color Green = new Color(0.5f, 1f, 0.25f);
    static readonly Color Cyan = new Color(0.6f, 0.95f, 1f);
    static readonly Color Gold = new Color(1f, 0.8f, 0.3f);
    static readonly Color Red = new Color(1f, 0.15f, 0.1f);
    static readonly Color Pink = new Color(1f, 0.4f, 0.85f);
    static readonly Color BatDark = new Color(0.22f, 0.08f, 0.3f);

    static Material mGlow, mRing, mRingThin, mWisp, mTrail, mSpark, mBeam, mSmoke, mBat, mPumpkin, mGhost, mCorn, mBubble, mRune, mWeb, mSkull, mCandy;
    static Texture2D tRingThin;
    static readonly Dictionary<string, Material> rimMats = new Dictionary<string, Material>();

    [MenuItem("Tools/Halloween VFX/Build All")]
    public static void BuildAll()
    {
        BuildAssets();
        BuildDemoScene();
    }

    [MenuItem("Tools/Halloween VFX/Build Assets (no scene)")]
    public static void BuildAssets()
    {
        foreach (var d in new[] { TexDir, MatDir, LayerDir, AuraDir, ZoneDir, DemoDir }) Directory.CreateDirectory(d);
        try
        {
            EditorUtility.DisplayProgressBar("Halloween VFX", "Textures", 0.1f);
            BuildTexturesAndMaterials();
            EditorUtility.DisplayProgressBar("Halloween VFX", "Layers", 0.5f);
            BuildLayerPrefabs();
            EditorUtility.DisplayProgressBar("Halloween VFX", "Auras", 0.7f);
            var auras = BuildAuraPrefabs();
            EditorUtility.DisplayProgressBar("Halloween VFX", "Zones", 0.85f);
            var zones = BuildZonePrefabs();
            BuildLibrary(auras, zones);
            AssetDatabase.SaveAssets();
        }
        finally { EditorUtility.ClearProgressBar(); }
    }

    // ================= Textures & Materials =================
    static void BuildTexturesAndMaterials()
    {
        string T(string n) => $"{TexDir}/{n}.png";
        var glow = HalloweenTextureGen.Glow(T("T_HW_Glow"));
        var ring = HalloweenTextureGen.RingSoft(T("T_HW_RingSoft"));
        tRingThin = HalloweenTextureGen.RingThin(T("T_HW_RingThin"));
        var wisp = HalloweenTextureGen.Wisp(T("T_HW_Wisp"));
        var trail = HalloweenTextureGen.Trail(T("T_HW_Trail"));
        var spark = HalloweenTextureGen.Spark(T("T_HW_Spark"));
        var beam = HalloweenTextureGen.Beam(T("T_HW_Beam"));
        var smoke = HalloweenTextureGen.Smoke2x2(T("T_HW_Smoke_2x2"));
        var bubble = HalloweenTextureGen.Bubble(T("T_HW_Bubble"));
        var bat = HalloweenTextureGen.BatSheet(T("T_HW_Bat_4x1"));
        var pumpkin = HalloweenTextureGen.Pumpkin(T("T_HW_Pumpkin"));
        var ghost = HalloweenTextureGen.Ghost(T("T_HW_Ghost"));
        var corn = HalloweenTextureGen.CandyCorn(T("T_HW_CandyCorn"));
        var rune = HalloweenTextureGen.RuneCircle(T("T_HW_RuneCircle"));
        var web = HalloweenTextureGen.Web(T("T_HW_Web"));

        mGlow = ParticleMat("M_HW_Glow_Add", glow, true);
        mRing = ParticleMat("M_HW_Ring_Add", ring, true);
        mRingThin = ParticleMat("M_HW_RingThin_Add", tRingThin, true);
        mWisp = ParticleMat("M_HW_Wisp_Add", wisp, true);
        mTrail = ParticleMat("M_HW_Trail_Add", trail, true);
        mSpark = ParticleMat("M_HW_Spark_Add", spark, true);
        mBeam = ParticleMat("M_HW_Beam_Add", beam, true);
        mSmoke = ParticleMat("M_HW_Smoke_Alpha", smoke, false);
        mBubble = ParticleMat("M_HW_Bubble_Add", bubble, true);
        mBat = ParticleMat("M_HW_Bat_Alpha", bat, false);
        mPumpkin = ParticleMat("M_HW_Pumpkin_Alpha", pumpkin, false);
        mGhost = ParticleMat("M_HW_Ghost_Alpha", ghost, false);
        mCorn = ParticleMat("M_HW_CandyCorn_Alpha", corn, false);
        mRune = ParticleMat("M_HW_RuneCircle_Add", rune, true);
        mWeb = ParticleMat("M_HW_Web_Add", web, true);
        mSkull = ParticleMat("M_HW_Skull_Add", AssetDatabase.LoadAssetAtPath<Texture2D>(SkullTexPath), true);
        mCandy = AssetDatabase.LoadAssetAtPath<Material>(CandyMatPath);

        rimMats.Clear();
        RimMat("Orange", Orange, 2.5f, 1.6f);
        RimMat("Purple", Purple, 2.2f, 1.8f);
        RimMat("Cyan", Cyan, 2.5f, 1.4f);
        RimMat("Green", Green, 2.2f, 1.6f);
        RimMat("Gold", Gold, 2f, 2f);
        RimMat("Red", Red, 2.2f, 1.8f);
        RimMat("White", new Color(1f, 0.95f, 0.85f), 1.8f, 2.2f);
    }

    static Material LoadOrCreate(string name, Shader shader)
    {
        string path = $"{MatDir}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else mat.shader = shader;
        return mat;
    }

    static Material ParticleMat(string name, Texture tex, bool additive)
    {
        var mat = LoadOrCreate(name, Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", additive ? 2f : 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        mat.SetFloat("_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_Cull", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.DisableKeyword("_ALPHAMODULATE_ON");
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void RimMat(string key, Color c, float power, float intensity)
    {
        var mat = LoadOrCreate("M_HW_Rim_" + key, Shader.Find("Halloween/RimGlow"));
        mat.SetColor("_RimColor", c);
        mat.SetFloat("_RimPower", power);
        mat.SetFloat("_Intensity", intensity);
        mat.SetFloat("_PulseSpeed", 2.5f);
        mat.SetFloat("_PulseAmount", 0.3f);
        EditorUtility.SetDirty(mat);
        rimMats[key] = mat;
    }

    static Material ZoneProgressMat(string key, Color c)
    {
        var mat = LoadOrCreate("M_HW_ZoneProgress_" + key, Shader.Find("Halloween/ZoneProgress"));
        mat.SetTexture("_MainTex", tRingThin);
        mat.SetColor("_Color", c);
        mat.SetFloat("_Fill", 0f);
        mat.SetFloat("_BgAlpha", 0.18f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ================= Particle helpers =================
    static PS.MinMaxCurve R(float a, float b) => new PS.MinMaxCurve(a, b);

    static AnimationCurve Crv(params float[] tv)
    {
        var c = new AnimationCurve();
        for (int i = 0; i < tv.Length; i += 2) c.AddKey(tv[i], tv[i + 1]);
        return c;
    }

    static Gradient Grad(Color from, Color to, params float[] alphaTv)
    {
        var g = new Gradient();
        var ak = new GradientAlphaKey[alphaTv.Length / 2];
        for (int i = 0; i < ak.Length; i++) ak[i] = new GradientAlphaKey(alphaTv[i * 2 + 1], alphaTv[i * 2]);
        g.SetKeys(new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) }, ak);
        return g;
    }

    static PS NewPS(string name, Transform parent, Material mat, float y = 0f, int max = 50)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0, y, 0);
        var ps = go.AddComponent<PS>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var m = ps.main;
        m.duration = 2f;
        m.loop = true;
        m.prewarm = true;
        m.playOnAwake = true;
        m.scalingMode = ParticleSystemScalingMode.Hierarchy;
        m.simulationSpace = ParticleSystemSimulationSpace.Local;
        m.maxParticles = max;
        m.startSpeed = 0f;
        m.startLifetime = 1f;
        m.startSize = 1f;
        var sh = ps.shape;
        sh.enabled = false;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        return ps;
    }

    static ParticleSystemRenderer Rend(PS ps) => ps.GetComponent<ParticleSystemRenderer>();

    static void Main(PS ps, float lifeA, float lifeB, float sizeA, float sizeB, float rotRandomDeg = 0f)
    {
        var m = ps.main;
        m.startLifetime = R(lifeA, lifeB);
        m.startSize = R(sizeA, sizeB);
        if (rotRandomDeg > 0f) m.startRotation = R(-rotRandomDeg * Mathf.Deg2Rad, rotRandomDeg * Mathf.Deg2Rad);
    }

    static void Rate(PS ps, float rate)
    {
        var e = ps.emission;
        e.enabled = true;
        e.rateOverTime = rate;
    }

    static void Burst(PS ps, short count, int cycles = 1, float interval = 0.01f)
    {
        var e = ps.emission;
        e.enabled = true;
        e.rateOverTime = 0f;
        e.SetBursts(new[] { new PS.Burst(0f, count, count, cycles, interval) });
    }

    // Hạt sống bằng đúng duration và burst lại mỗi vòng loop => hiển thị liên tục
    static void Permanent(PS ps, float duration, short count)
    {
        var m = ps.main;
        m.duration = duration;
        m.startLifetime = duration;
        Burst(ps, count);
    }

    static void OneShot(PS ps)
    {
        var m = ps.main;
        m.loop = false;
        m.prewarm = false;
        m.playOnAwake = false;
        m.duration = 1f;
    }

    static void CircleXZ(PS ps, float radius, float thickness = 1f, bool burstSpread = false)
    {
        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = radius;
        sh.radiusThickness = thickness;
        sh.rotation = new Vector3(90f, 0f, 0f);
        if (burstSpread) sh.arcMode = ParticleSystemShapeMultiModeValue.BurstSpread;
    }

    static void Sphere(PS ps, float radius, ParticleSystemShapeType type = ParticleSystemShapeType.Sphere)
    {
        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = type;
        sh.radius = radius;
        if (type == ParticleSystemShapeType.Hemisphere) sh.rotation = new Vector3(-90f, 0f, 0f);
    }

    static void Vel(PS ps, float yMin, float yMax, float orbitMin = 0f, float orbitMax = 0f)
    {
        VelXYZ(ps, Vector2.zero, new Vector2(yMin, yMax), Vector2.zero);
        var v = ps.velocityOverLifetime;
        v.orbitalY = R(orbitMin, orbitMax);
    }

    static void Noise(PS ps, float strength, float freq)
    {
        var n = ps.noise;
        n.enabled = true;
        n.strength = strength;
        n.frequency = freq;
        n.scrollSpeed = 0.3f;
        n.quality = ParticleSystemNoiseQuality.Medium;
    }

    static void SizeOL(PS ps, params float[] tv)
    {
        var s = ps.sizeOverLifetime;
        s.enabled = true;
        s.size = new PS.MinMaxCurve(1f, Crv(tv));
    }

    static void RotOL(PS ps, float degMin, float degMax)
    {
        var r = ps.rotationOverLifetime;
        r.enabled = true;
        r.z = R(degMin * Mathf.Deg2Rad, degMax * Mathf.Deg2Rad);
    }

    static void Fade(PS ps, Color from, Color to, params float[] alphaTv)
    {
        var c = ps.colorOverLifetime;
        c.enabled = true;
        c.color = new PS.MinMaxGradient(Grad(from, to, alphaTv));
    }

    static void Gravity(PS ps, float g)
    {
        var m = ps.main;
        m.gravityModifier = g;
    }

    static void RandomFrame(PS ps, int x, int y)
    {
        var t = ps.textureSheetAnimation;
        t.enabled = true;
        t.numTilesX = x;
        t.numTilesY = y;
        t.frameOverTime = R(0f, 0.999f);
        t.cycleCount = 1;
    }

    static void Horizontal(PS ps, float fudge = 0f)
    {
        var r = Rend(ps);
        r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        r.sortingFudge = fudge;
    }

    // ================= Layers (bán kính gốc 1, đáy y = 0) =================
    static PS GroundGlow(Transform p, Color c, float size = 2.6f, float alpha = 0.7f)
    {
        var ps = NewPS("GroundGlow", p, mGlow, 0.03f, 6);
        Main(ps, 2f, 2f, size, size, 180f);
        Rate(ps, 1.5f);
        Horizontal(ps, 40f);
        Fade(ps, c, c, 0f, 0f, 0.3f, alpha, 0.7f, alpha, 1f, 0f);
        return ps;
    }

    static PS PulseRing(Transform p, Color c, float size = 2.4f)
    {
        var ps = NewPS("PulseRing", p, mRing, 0.04f, 4);
        Main(ps, 1.6f, 1.6f, size, size);
        Rate(ps, 0.8f);
        Horizontal(ps, 30f);
        SizeOL(ps, 0f, 0.35f, 1f, 1f);
        Fade(ps, c, c, 0f, 0f, 0.15f, 0.9f, 1f, 0f);
        return ps;
    }

    static PS Embers(Transform p, Color c, float rate = 10f)
    {
        var ps = NewPS("Embers", p, mGlow, 0.05f, 60);
        Main(ps, 1.4f, 2.4f, 0.05f, 0.12f);
        CircleXZ(ps, 0.9f);
        Vel(ps, 0.5f, 1.1f);
        Noise(ps, 0.25f, 0.8f);
        SizeOL(ps, 0f, 1f, 0.7f, 1f, 1f, 0f);
        Fade(ps, c, Color.Lerp(c, Red, 0.6f), 0f, 0f, 0.15f, 1f, 1f, 0f);
        Rate(ps, rate);
        return ps;
    }

    static PS Smoke(Transform p, Color c, float alpha = 0.55f)
    {
        var ps = NewPS("CursedSmoke", p, mSmoke, 0.1f, 40);
        Main(ps, 2f, 3.2f, 0.8f, 1.4f, 180f);
        CircleXZ(ps, 0.8f);
        Vel(ps, 0.15f, 0.4f);
        SizeOL(ps, 0f, 0.5f, 1f, 1f);
        RotOL(ps, -20f, 20f);
        RandomFrame(ps, 2, 2);
        Fade(ps, c, c * 0.6f, 0f, 0f, 0.3f, alpha, 1f, 0f);
        Rate(ps, 8f);
        Rend(ps).sortingFudge = 10f;
        return ps;
    }

    static PS Wisps(Transform p, Color c, short count = 4, float y = 1f, float radius = 1.1f)
    {
        var ps = NewPS("GhostWisps", p, mWisp, y, 12);
        Permanent(ps, 4f, count);
        Main(ps, 4f, 4f, 0.22f, 0.3f);
        CircleXZ(ps, radius, 0f);
        Vel(ps, 0f, 0f, 1.6f, 1.6f);
        Noise(ps, 0.35f, 0.6f);
        Fade(ps, c, c, 0f, 0f, 0.1f, 1f, 0.9f, 1f, 1f, 0f);
        var t = ps.trails;
        t.enabled = true;
        t.ratio = 1f;
        t.lifetime = 0.35f;
        t.minVertexDistance = 0.05f;
        t.dieWithParticles = true;
        t.inheritParticleColor = true;
        t.widthOverTrail = new PS.MinMaxCurve(0.6f, Crv(0f, 1f, 1f, 0f));
        t.colorOverTrail = new PS.MinMaxGradient(Grad(Color.white, Color.white, 0f, 1f, 1f, 0f));
        Rend(ps).trailMaterial = mTrail;
        return ps;
    }

    static PS Bats(Transform p, Color c, float rate = 3f, float y = 1.5f)
    {
        var ps = NewPS("BatSwarm", p, mBat, y, 15);
        Main(ps, 3f, 4.5f, 0.28f, 0.42f);
        CircleXZ(ps, 1.3f, 0f);
        Vel(ps, -0.1f, 0.1f, 1f, 1.8f);
        Noise(ps, 0.5f, 0.4f);
        Flap(ps, 10);
        Fade(ps, c, c, 0f, 0f, 0.15f, 1f, 0.85f, 1f, 1f, 0f);
        Rate(ps, rate);
        return ps;
    }

    static PS Souls(Transform p, Color c, float rate = 1.5f)
    {
        var ps = NewPS("SoulRise", p, mGhost, 0.5f, 10);
        Main(ps, 2.2f, 3f, 0.35f, 0.55f);
        CircleXZ(ps, 0.7f);
        Vel(ps, 0.5f, 0.8f);
        Noise(ps, 0.3f, 0.5f);
        SizeOL(ps, 0f, 0.6f, 1f, 1f);
        Fade(ps, c, c, 0f, 0f, 0.2f, 0.85f, 0.7f, 0.6f, 1f, 0f);
        Rate(ps, rate);
        return ps;
    }

    static PS PumpkinOrbit(Transform p, short count = 3, float y = 0.6f, float radius = 1.25f)
    {
        var ps = NewPS("PumpkinOrbit", p, mPumpkin, y, 6);
        Permanent(ps, 6f, count);
        Main(ps, 6f, 6f, 0.4f, 0.5f, 15f);
        CircleXZ(ps, radius, 0f, true);
        Vel(ps, 0f, 0f, 0.9f, 0.9f);
        Noise(ps, 0.15f, 0.5f);
        Fade(ps, Color.white, Color.white, 0f, 0f, 0.08f, 1f, 0.92f, 1f, 1f, 0f);
        return ps;
    }

    static PS Flames(Transform p, Color hot, Color cool, float rate = 28f)
    {
        var ps = NewPS("PumpkinFire", p, mWisp, 0.05f, 60);
        Main(ps, 0.7f, 1.1f, 0.3f, 0.5f);
        CircleXZ(ps, 0.85f, 0.3f);
        Vel(ps, 0.9f, 1.5f);
        Noise(ps, 0.3f, 1.2f);
        SizeOL(ps, 0f, 0.6f, 0.2f, 1f, 1f, 0f);
        Fade(ps, hot, cool, 0f, 0f, 0.1f, 1f, 0.6f, 0.8f, 1f, 0f);
        Rate(ps, rate);
        return ps;
    }

    static PS WebHalo(Transform p, Color c, float alpha = 0.5f, float size = 3.2f)
    {
        var ps = NewPS("WebHalo", p, mWeb, 0.04f, 2);
        Permanent(ps, 12f, 1);
        Main(ps, 12f, 12f, size, size);
        Horizontal(ps, 35f);
        var r = ps.rotationOverLifetime;
        r.enabled = true;
        r.z = 30f * Mathf.Deg2Rad; // 30°/s * 12s = 360° => vòng lặp liền mạch
        Fade(ps, c, c, 0f, alpha, 1f, alpha);
        return ps;
    }

    static PS Candy(Transform p, float rate = 5f)
    {
        var ps = NewPS("CandyBurst", p, mCandy, 1.2f, 30);
        Main(ps, 1.2f, 1.8f, 0.3f, 0.45f, 180f);
        Sphere(ps, 0.7f);
        var m = ps.main;
        m.startSpeed = R(1.2f, 2.2f);
        Gravity(ps, 0.9f);
        RotOL(ps, -180f, 180f);
        RandomFrame(ps, 2, 2);
        SizeOL(ps, 0f, 0f, 0.1f, 1f, 0.8f, 1f, 1f, 0f);
        Rate(ps, rate);

        var corn = NewPS("CandyCorn", ps.transform, mCorn, 0f, 30);
        Main(corn, 1.2f, 1.8f, 0.22f, 0.32f, 180f);
        Sphere(corn, 0.7f);
        var cm = corn.main;
        cm.startSpeed = R(1.2f, 2.2f);
        Gravity(corn, 0.9f);
        RotOL(corn, -180f, 180f);
        SizeOL(corn, 0f, 0f, 0.1f, 1f, 0.8f, 1f, 1f, 0f);
        Rate(corn, rate);
        return ps;
    }

    static PS Skulls(Transform p, Color c, float rate = 0.8f)
    {
        var ps = NewPS("SkullRise", p, mSkull, 0.6f, 6);
        Main(ps, 2f, 2.8f, 0.3f, 0.45f);
        CircleXZ(ps, 0.6f);
        Vel(ps, 0.4f, 0.7f);
        Noise(ps, 0.2f, 0.5f);
        SizeOL(ps, 0f, 0.7f, 1f, 1f);
        Fade(ps, c, c, 0f, 0f, 0.2f, 1f, 0.8f, 0.6f, 1f, 0f);
        Rate(ps, rate);
        return ps;
    }

    static PS Bubbles(Transform p, Color c, float rate = 14f)
    {
        var ps = NewPS("ToxicBubbles", p, mBubble, 0.05f, 40);
        Main(ps, 1f, 1.6f, 0.08f, 0.22f);
        CircleXZ(ps, 0.85f);
        Vel(ps, 0.25f, 0.5f);
        Noise(ps, 0.1f, 1f);
        SizeOL(ps, 0f, 0.3f, 0.85f, 1f, 1f, 1.3f);
        Fade(ps, c, c, 0f, 0f, 0.1f, 1f, 0.85f, 1f, 1f, 0f);
        Rate(ps, rate);
        return ps;
    }

    static PS Sparks(Transform p, Color c, float rate = 10f)
    {
        var ps = NewPS("Sparks", p, mSpark, 1.1f, 30);
        Main(ps, 0.6f, 1.1f, 0.12f, 0.28f, 180f);
        Sphere(ps, 1.1f);
        SizeOL(ps, 0f, 0f, 0.3f, 1f, 1f, 0f);
        Fade(ps, Color.white, c, 0f, 1f, 1f, 1f);
        Rate(ps, rate);
        return ps;
    }

    static PS Beam(Transform p, Color c, float alpha = 0.45f)
    {
        var ps = NewPS("LightBeam", p, mBeam, 1.8f, 4);
        var m = ps.main;
        m.startLifetime = 2f;
        m.startSize3D = true;
        m.startSizeX = 1.8f;
        m.startSizeY = 3.6f;
        m.startSizeZ = 1f;
        Rate(ps, 0.8f);
        var r = Rend(ps);
        r.renderMode = ParticleSystemRenderMode.VerticalBillboard;
        r.sortingFudge = 20f;
        Fade(ps, c, c, 0f, 0f, 0.4f, alpha, 1f, 0f);
        return ps;
    }

    static PS Rune(Transform p, Color c, float size = 2.2f)
    {
        var ps = NewPS("RuneCircle", p, mRune, 0.05f, 2);
        Permanent(ps, 8f, 1);
        Main(ps, 8f, 8f, size, size);
        Horizontal(ps, 35f);
        var r = ps.rotationOverLifetime;
        r.enabled = true;
        r.z = 45f * Mathf.Deg2Rad; // 45°/s * 8s = 360°
        Fade(ps, c, c, 0f, 1f, 1f, 1f);
        return ps;
    }

    static PS Motes(Transform p, Color c, float rate = 20f)
    {
        var ps = NewPS("RisingMotes", p, mGlow, 0.05f, 50);
        Main(ps, 1f, 1.8f, 0.06f, 0.12f);
        CircleXZ(ps, 0.95f, 0f);
        Vel(ps, 0.6f, 1.2f);
        SizeOL(ps, 0f, 1f, 1f, 0f);
        Fade(ps, c, Pink, 0f, 0f, 0.1f, 1f, 1f, 0f);
        Rate(ps, rate);
        return ps;
    }

    static PS Fog(Transform p, Color c, float alpha = 0.35f)
    {
        var ps = NewPS("GraveFog", p, mSmoke, 0.2f, 30);
        Main(ps, 3f, 4f, 1f, 1.6f, 180f);
        CircleXZ(ps, 0.9f);
        var m = ps.main;
        m.startSpeed = R(0.05f, 0.15f);
        RotOL(ps, -10f, 10f);
        RandomFrame(ps, 2, 2);
        SizeOL(ps, 0f, 0.7f, 1f, 1f);
        Fade(ps, c, c, 0f, 0f, 0.3f, alpha, 0.7f, alpha, 1f, 0f);
        Rate(ps, 6f);
        Rend(ps).sortingFudge = 15f;
        return ps;
    }

    static PS PumpkinRingStatic(Transform p, short count = 8)
    {
        var ps = NewPS("PumpkinRing", p, mPumpkin, 0.2f, count * 2);
        Permanent(ps, 6f, count);
        Main(ps, 6f, 6f, 0.38f, 0.38f);
        CircleXZ(ps, 1f, 0f, true);
        return ps;
    }

    static PS CandleFlames(Transform p, short count = 8)
    {
        var ps = NewPS("CandleFlames", p, mWisp, 0.42f, 80);
        Main(ps, 0.5f, 0.6f, 0.16f, 0.2f);
        CircleXZ(ps, 1f, 0f, true);
        Vel(ps, 0.5f, 0.7f);
        Noise(ps, 0.08f, 2f);
        SizeOL(ps, 0f, 1f, 1f, 0f);
        Fade(ps, new Color(1f, 0.9f, 0.4f), Orange, 0f, 1f, 1f, 0f);
        var m = ps.main;
        m.duration = 1f;
        Burst(ps, count, 0, 0.06f); // cycles 0 = lặp vô hạn
        return ps;
    }

    // ================= Burst cho zone =================
    static PS RingBurst(Transform p, string name, Color c, float size = 2.6f)
    {
        var ps = NewPS(name, p, mRing, 0.05f, 2);
        OneShot(ps);
        Main(ps, 0.6f, 0.6f, size, size);
        Burst(ps, 1);
        Horizontal(ps);
        SizeOL(ps, 0f, 0.3f, 1f, 1.3f);
        Fade(ps, c, c, 0f, 1f, 1f, 0f);
        return ps;
    }

    static PS BurstChild(Transform p, string name, Material mat, Color c, short count, float sizeA, float sizeB, float speedA, float speedB, float gravity, float life = 0.9f)
    {
        var ps = NewPS(name, p, mat, 0.1f, count);
        OneShot(ps);
        Main(ps, life * 0.6f, life, sizeA, sizeB, 30f);
        Sphere(ps, 0.3f, ParticleSystemShapeType.Hemisphere);
        var m = ps.main;
        m.startSpeed = R(speedA, speedB);
        Gravity(ps, gravity);
        Burst(ps, count);
        Fade(ps, c, c, 0f, 1f, 0.7f, 1f, 1f, 0f);
        return ps;
    }

    static PS EnterBurst(Transform p, Color c)
    {
        var root = RingBurst(p, "EnterBurst", c);
        BurstChild(root.transform, "Sparks", mGlow, c, 25, 0.06f, 0.14f, 2f, 4f, 0.6f);
        return root;
    }

    static PS CompleteBurst(Transform p, Color c, System.Action<Transform> extra)
    {
        var root = NewPS("CompleteBurst", p, mGlow, 1f, 2);
        OneShot(root);
        Main(root, 0.4f, 0.4f, 4f, 4f);
        Burst(root, 1);
        SizeOL(root, 0f, 0.5f, 1f, 1.2f);
        Fade(root, c, c, 0f, 1f, 1f, 0f);
        RingBurst(root.transform, "Shockwave", c, 3.4f).transform.localPosition = new Vector3(0, -0.95f, 0);
        extra?.Invoke(root.transform);
        return root;
    }

    static PS BatBurst(Transform p, short count)
    {
        var ps = BurstChild(p, "BatBurst", mBat, BatDark, count, 0.3f, 0.45f, 3f, 5f, -0.2f, 1.4f);
        Flap(ps, 6);
        return ps;
    }

    // ================= Layer prefabs (màu mặc định, dùng tự do) =================
    static void BuildLayerPrefabs()
    {
        SaveLayer("HW_L_GroundGlow", t => GroundGlow(t, Orange));
        SaveLayer("HW_L_PulseRing", t => PulseRing(t, Purple));
        SaveLayer("HW_L_Embers", t => Embers(t, Orange));
        SaveLayer("HW_L_CursedSmoke", t => Smoke(t, Purple));
        SaveLayer("HW_L_GhostWisps", t => Wisps(t, Cyan));
        SaveLayer("HW_L_BatSwarm", t => Bats(t, BatDark));
        SaveLayer("HW_L_SoulRise", t => Souls(t, Cyan));
        SaveLayer("HW_L_PumpkinOrbit", t => PumpkinOrbit(t));
        SaveLayer("HW_L_PumpkinFire", t => Flames(t, new Color(1f, 0.85f, 0.3f), Red));
        SaveLayer("HW_L_WebHalo", t => WebHalo(t, Purple));
        SaveLayer("HW_L_Candy", t => Candy(t));
        SaveLayer("HW_L_SkullRise", t => Skulls(t, Green));
        SaveLayer("HW_L_ToxicBubbles", t => Bubbles(t, Green));
        SaveLayer("HW_L_Sparks", t => Sparks(t, Gold));
        SaveLayer("HW_L_LightBeam", t => Beam(t, Gold));
    }

    static void SaveLayer(string name, System.Func<Transform, PS> build)
    {
        var root = new GameObject(name);
        build(root.transform);
        PrefabUtility.SaveAsPrefabAsset(root, $"{LayerDir}/{name}.prefab");
        Object.DestroyImmediate(root);
    }

    // ================= Aura theo Rarity =================
    static List<HalloweenVfxLibrary.AuraEntry> BuildAuraPrefabs()
    {
        var list = new List<HalloweenVfxLibrary.AuraEntry>();
        void Add(Rarity r, string rim, System.Action<Transform> build)
        {
            var root = new GameObject("HW_Aura_" + r);
            var aura = root.AddComponent<HalloweenAura>();
            var so = new SerializedObject(aura);
            so.FindProperty("rarity").intValue = (int)r;
            so.FindProperty("rimMaterial").objectReferenceValue = rimMats[rim];
            so.ApplyModifiedPropertiesWithoutUndo();
            build(root.transform);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{AuraDir}/HW_Aura_{r}.prefab");
            Object.DestroyImmediate(root);
            list.Add(new HalloweenVfxLibrary.AuraEntry { rarity = r, prefab = prefab.GetComponent<HalloweenAura>() });
        }

        Add(Rarity.Common, "Orange", t => { GroundGlow(t, Orange, 2.4f, 0.5f); Embers(t, Orange, 8f); });
        Add(Rarity.Rare, "Purple", t => { GroundGlow(t, Purple, 2.6f, 0.6f); Smoke(t, Purple); Embers(t, Purple, 8f); });
        Add(Rarity.Epic, "Cyan", t => { PulseRing(t, Cyan); Smoke(t, new Color(0.3f, 0.5f, 0.7f), 0.45f); Wisps(t, Cyan); });
        Add(Rarity.Legendary, "Orange", t => { GroundGlow(t, Orange); Flames(t, new Color(1f, 0.85f, 0.3f), Red); PumpkinOrbit(t); Embers(t, Orange, 12f); });
        Add(Rarity.Mythical, "Purple", t => { PulseRing(t, Purple); Smoke(t, Purple); Bats(t, BatDark); Wisps(t, Pink, 3); });
        Add(Rarity.God, "Gold", t => { GroundGlow(t, Gold); Beam(t, Gold); Sparks(t, Gold); Souls(t, new Color(1f, 0.95f, 0.8f)); Bats(t, new Color(0.3f, 0.2f, 0.05f), 2f); });
        Add(Rarity.Secret, "Green", t => { GroundGlow(t, Green, 2.6f, 0.6f); Smoke(t, new Color(0.15f, 0.35f, 0.1f), 0.6f); Bubbles(t, Green); Skulls(t, Green); });
        Add(Rarity.OG, "Red", t => { WebHalo(t, Red, 0.6f); Flames(t, Orange, Red, 22f); Bats(t, new Color(0.25f, 0.03f, 0.03f)); Embers(t, Red, 12f); });
        Add(Rarity.Eternal, "Cyan", t => { WebHalo(t, Cyan, 0.45f); PulseRing(t, Cyan); Beam(t, Cyan, 0.4f); Wisps(t, Cyan, 5); Souls(t, Cyan); });
        Add(Rarity.Divine, "White", t => { PulseRing(t, Gold, 2.8f); Beam(t, Gold, 0.55f); Sparks(t, Gold, 14f); Wisps(t, Gold, 4, 1.3f); PumpkinOrbit(t, 4, 0.8f, 1.4f); Candy(t, 3f); });
        return list;
    }

    // ================= Zones =================
    static List<HalloweenZone> BuildZonePrefabs()
    {
        var list = new List<HalloweenZone>();

        list.Add(BuildZone("HW_Zone_MagicCircle", Purple,
            idle => { Rune(idle, Purple); GroundGlow(idle, Purple, 2.4f, 0.6f); Motes(idle, Purple); PulseRing(idle, Pink, 2.2f); Beam(idle, Purple, 0.25f); },
            fx => BatBurst(fx, 16)));

        list.Add(BuildZone("HW_Zone_Cauldron", Green,
            idle => { GroundGlow(idle, Green, 2.4f, 0.8f); Bubbles(idle, Green, 16f); Smoke(idle, new Color(0.3f, 0.7f, 0.2f), 0.35f); Skulls(idle, Green, 0.5f); PulseRing(idle, Green); },
            fx => { BurstChild(fx, "Bubbles", mBubble, Green, 20, 0.1f, 0.25f, 1.5f, 3f, 1f); BurstChild(fx, "Skulls", mSkull, Green, 6, 0.4f, 0.6f, 1.5f, 2.5f, -0.1f, 1.2f); }));

        list.Add(BuildZone("HW_Zone_GraveFog", Cyan,
            idle => { Fog(idle, new Color(0.55f, 0.65f, 0.8f)); Wisps(idle, Cyan, 5, 0.4f, 1f); Souls(idle, Cyan, 1f); PulseRing(idle, Cyan); },
            fx => BurstChild(fx, "Ghosts", mGhost, Color.white, 10, 0.4f, 0.6f, 1.5f, 3f, -0.3f, 1.4f)));

        list.Add(BuildZone("HW_Zone_PumpkinRing", Orange,
            idle => { PumpkinRingStatic(idle); CandleFlames(idle); GroundGlow(idle, Orange, 2.4f, 0.6f); Embers(idle, Orange); PulseRing(idle, Orange); },
            fx => { BurstChild(fx, "Candy", mCandy, Color.white, 15, 0.3f, 0.45f, 3f, 5f, 1f, 1.4f); BurstChild(fx, "CandyCorn", mCorn, Color.white, 15, 0.22f, 0.32f, 3f, 5f, 1f, 1.4f); }));

        return list;
    }

    static HalloweenZone BuildZone(string name, Color c, System.Action<Transform> idleBuild, System.Action<Transform> completeExtra)
    {
        var root = new GameObject(name);
        var idle = new GameObject("Idle").transform;
        idle.SetParent(root.transform, false);
        idleBuild(idle);

        var enter = EnterBurst(root.transform, c);
        var complete = CompleteBurst(root.transform, c, completeExtra);

        var ringGo = new GameObject("ProgressRing");
        ringGo.transform.SetParent(root.transform, false);
        ringGo.transform.localPosition = new Vector3(0, 0.06f, 0);
        ringGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ringGo.transform.localScale = Vector3.one * 2.3f;
        ringGo.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        var ringR = ringGo.AddComponent<MeshRenderer>();
        ringR.sharedMaterial = ZoneProgressMat(name.Replace("HW_Zone_", ""), c);
        ringR.shadowCastingMode = ShadowCastingMode.Off;
        ringR.receiveShadows = false;

        var col = root.AddComponent<CapsuleCollider>();
        col.isTrigger = true;
        col.radius = 1f;
        col.height = 3f;
        col.center = new Vector3(0, 1.5f, 0);
        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var zone = root.AddComponent<HalloweenZone>();
        var so = new SerializedObject(zone);
        so.FindProperty("idleRoot").objectReferenceValue = idle;
        so.FindProperty("enterBurst").objectReferenceValue = enter;
        so.FindProperty("completeBurst").objectReferenceValue = complete;
        so.FindProperty("progressRing").objectReferenceValue = ringR;
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{ZoneDir}/{name}.prefab");
        Object.DestroyImmediate(root);
        return prefab.GetComponent<HalloweenZone>();
    }

    static void BuildLibrary(List<HalloweenVfxLibrary.AuraEntry> auras, List<HalloweenZone> zones)
    {
        string path = $"{Root}/HalloweenVfxLibrary.asset";
        var lib = AssetDatabase.LoadAssetAtPath<HalloweenVfxLibrary>(path);
        if (!lib)
        {
            lib = ScriptableObject.CreateInstance<HalloweenVfxLibrary>();
            AssetDatabase.CreateAsset(lib, path);
        }
        lib.EditorSetup(auras, zones);
        EditorUtility.SetDirty(lib);
    }

    // ================= Demo scene =================
    [MenuItem("Tools/Halloween VFX/Build Demo Scene")]
    public static void BuildDemoScene()
    {
        var lib = AssetDatabase.LoadAssetAtPath<HalloweenVfxLibrary>($"{Root}/HalloweenVfxLibrary.asset");
        if (!lib) { Debug.LogError("[HalloweenVFX] Chưa có library, chạy Build Assets trước"); return; }

        // Scene đang mở có thay đổi chưa lưu thì mở demo dạng Additive để không mất dữ liệu
        string scenePath = $"{DemoDir}/HalloweenVfxDemo.unity";
        bool keepOpen = false;
        for (int i = UnityEngine.SceneManagement.SceneManager.sceneCount - 1; i >= 0; i--)
        {
            var s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (s.path == scenePath && UnityEngine.SceneManagement.SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(s, true);
            else if (s.isDirty) keepOpen = true;
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, keepOpen ? NewSceneMode.Additive : NewSceneMode.Single);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.16f, 0.11f, 0.22f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.05f, 0.03f, 0.08f);
        RenderSettings.fogStartDistance = 40f;
        RenderSettings.fogEndDistance = 110f;

        var lightGo = new GameObject("Moon Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.65f, 0.7f, 1f);
        light.intensity = 0.8f;
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(14f, 1f, 14f);
        var gMat = LoadOrCreate("M_HW_DemoGround", Shader.Find("Universal Render Pipeline/Lit"));
        gMat.SetColor("_BaseColor", new Color(0.07f, 0.06f, 0.09f));
        gMat.SetFloat("_Smoothness", 0.1f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = gMat;

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.03f, 0.07f);
        cam.fieldOfView = 50f;
        cam.farClipPlane = 300f;
        camGo.transform.position = new Vector3(0f, 22f, -34f);
        camGo.transform.LookAt(new Vector3(0f, 0f, 3f));
        camGo.AddComponent<AudioListener>();
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        BuildBloomVolume();

        var tiger = AssetDatabase.LoadAssetAtPath<GameObject>(TigerPath);
        var rarities = (Rarity[])System.Enum.GetValues(typeof(Rarity));
        for (int i = 0; i < rarities.Length; i++)
        {
            int row = i / 5, colIdx = i % 5;
            var pos = new Vector3((colIdx - 2) * 8f, 0f, row * 10f);
            SpawnAnimal(lib, tiger, rarities[i], pos, rarities[i].ToString(), camGo.transform);
        }

        var trex = AssetDatabase.LoadAssetAtPath<GameObject>(TRexPath);
        if (trex) SpawnAnimal(lib, trex, Rarity.Divine, new Vector3(28f, 0f, 22f), "Divine (TRex)", camGo.transform);

        var zones = lib.Zones;
        for (int i = 0; i < zones.Count; i++)
        {
            var z = (GameObject)PrefabUtility.InstantiatePrefab(zones[i].gameObject);
            z.transform.position = new Vector3((i - 1.5f) * 9f, 0f, -11f);
            z.transform.localScale = Vector3.one * 2.5f;
            Label(zones[i].name.Replace("HW_Zone_", "Zone: "), z.transform.position + new Vector3(0, 0.2f, -3.6f), camGo.transform);
        }

        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[HalloweenVFX] Demo scene: {scenePath}");
    }

    static void SpawnAnimal(HalloweenVfxLibrary lib, GameObject prefab, Rarity rarity, Vector3 pos, string label, Transform cam)
    {
        if (!prefab) return;
        var animal = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        animal.name = $"{prefab.name}_{label}";
        animal.transform.position = pos;
        animal.transform.rotation = Quaternion.Euler(0f, 200f, 0f);

        var auraPrefab = lib.GetAuraPrefab(rarity);
        var aura = (GameObject)PrefabUtility.InstantiatePrefab(auraPrefab.gameObject, animal.transform);
        var comp = aura.GetComponent<HalloweenAura>();
        comp.FitToTarget();
        comp.ApplyRim();

        Label(label, pos + new Vector3(0, 0.2f, -3.8f), cam);
    }

    static void Label(string text, Vector3 pos, Transform cam)
    {
        var go = new GameObject("Label_" + text);
        go.transform.position = pos;
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.fontSize = 48;
        tm.characterSize = 0.08f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 0.85f, 0.6f);
        go.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
        if (cam) go.transform.rotation = Quaternion.LookRotation(go.transform.position - cam.position);
    }

    static void BuildBloomVolume()
    {
        string path = $"{DemoDir}/HW_DemoVolume.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (!profile)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(1.2f);
            bloom.scatter.Override(0.6f);
            AssetDatabase.AddObjectToAsset(bloom, profile);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.3f);
            AssetDatabase.AddObjectToAsset(vig, profile);
            EditorUtility.SetDirty(profile);
        }
        var volGo = new GameObject("Global Volume");
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = profile;
    }
}
