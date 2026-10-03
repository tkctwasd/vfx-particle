using System.Collections.Generic;
using UnityEngine;

// Lớp VFX phụ theo Biome, gắn chồng lên aura Rarity.
// Chỉ trang trí: giữ mật độ thấp và không thêm hình dạng lớn để không tranh chấp
// với silhouette của aura Rarity (rarity mới là tín hiệu người chơi cần đọc trước).
public class HalloweenBiomeAccent : MonoBehaviour
{
    [SerializeField] Biome biome;
    [SerializeField, Tooltip("Bán kính footprint mà prefab được dựng (đơn vị gốc)")] float baseRadius = 1f;
    [SerializeField] float radiusMultiplier = 1f;
    [SerializeField] bool fitOnStart = true;

    [SerializeField, Range(0f, 1f), Tooltip("Hệ số mật độ, library set theo Rarity khi spawn")]
    float density = 1f;

    readonly List<ParticleSystem> systems = new List<ParticleSystem>();
    readonly List<float> baseRates = new List<float>();
    readonly List<short> baseBursts = new List<short>();
    bool cached;

    public Biome Biome => biome;
    Transform Target => transform.parent;

    void Start()
    {
        if (fitOnStart) FitToTarget();
        ApplyDensity();
    }

    // Aura và accent fit độc lập nhau nên accent không phụ thuộc thứ tự spawn
    public void FitToTarget()
    {
        var target = Target;
        if (!target || !TryGetBounds(out var b)) return;

        float radius = (b.extents.x + b.extents.z) * 0.5f * radiusMultiplier;
        Vector3 s = target.lossyScale;
        float parentScale = Mathf.Max(0.0001f, (Mathf.Abs(s.x) + Mathf.Abs(s.y) + Mathf.Abs(s.z)) / 3f);

        transform.position = new Vector3(b.center.x, b.min.y, b.center.z);
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one * (radius / baseRadius / parentScale);
    }

    public void SetDensity(float value)
    {
        density = Mathf.Clamp01(value);
        ApplyDensity();
    }

    // Scale cả rateOverTime và burst count: layer nào cũng thưa đi theo đúng tỉ lệ
    public void ApplyDensity()
    {
        CacheSystems();
        for (int i = 0; i < systems.Count; i++)
        {
            var ps = systems[i];
            if (!ps) continue;

            var e = ps.emission;
            if (baseRates[i] > 0f) e.rateOverTime = baseRates[i] * density;

            if (baseBursts[i] > 0)
            {
                short n = (short)Mathf.Max(1, Mathf.RoundToInt(baseBursts[i] * density));
                var bursts = new ParticleSystem.Burst[e.burstCount];
                e.GetBursts(bursts);
                for (int b = 0; b < bursts.Length; b++)
                {
                    bursts[b].minCount = n;
                    bursts[b].maxCount = n;
                }
                e.SetBursts(bursts);
            }
        }
    }

    // Rate gốc phải đọc trước lần scale đầu, nếu không mỗi lần set sẽ nhân dồn
    void CacheSystems()
    {
        if (cached) return;
        cached = true;
        systems.Clear();
        baseRates.Clear();
        baseBursts.Clear();

        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
        {
            var e = ps.emission;
            systems.Add(ps);
            baseRates.Add(e.rateOverTime.constant);

            short burst = 0;
            if (e.burstCount > 0)
            {
                var bursts = new ParticleSystem.Burst[e.burstCount];
                e.GetBursts(bursts);
                burst = (short)bursts[0].maxCount;
            }
            baseBursts.Add(burst);
        }
    }

    bool TryGetBounds(out Bounds b)
    {
        b = default;
        bool has = false;
        foreach (var r in Target.GetComponentsInChildren<Renderer>())
        {
            // Bỏ qua chính accent và mọi VFX khác đã gắn, chỉ fit theo model thật
            if (r.transform.IsChildOf(transform)) continue;
            if (r is ParticleSystemRenderer) continue;
            if (!(r is SkinnedMeshRenderer || r is MeshRenderer)) continue;
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }
        return has;
    }
}
