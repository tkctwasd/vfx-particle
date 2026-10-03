using System;
using System.Collections.Generic;
using UnityEngine;

// Bảng tra VFX Halloween: aura theo Rarity, accent theo Biome, và danh sách zone.
// Hợp thành 2 lớp (10 aura + 14 accent) phủ hết mọi cặp Biome x Rarity mà không nhân tổ hợp prefab.
[CreateAssetMenu(menuName = "VFX/Halloween Vfx Library", fileName = "HalloweenVfxLibrary")]
public class HalloweenVfxLibrary : ScriptableObject
{
    [Serializable]
    public class AuraEntry
    {
        public Rarity rarity;
        public HalloweenAura prefab;
    }

    [Serializable]
    public class AccentEntry
    {
        public Biome biome;
        public HalloweenBiomeAccent prefab;
    }

    [SerializeField] List<AuraEntry> auras = new List<AuraEntry>();
    [SerializeField] List<AccentEntry> accents = new List<AccentEntry>();
    [SerializeField] List<HalloweenZone> zones = new List<HalloweenZone>();

    [Header("Mật độ accent theo Rarity")]
    [SerializeField, Tooltip("Index theo Rarity: con càng hiếm thì accent biome càng dày")]
    float[] accentDensityByRarity =
    {
        0.35f, // Common
        0.45f, // Rare
        0.55f, // Epic
        0.65f, // Legendary
        0.72f, // Mythical
        0.80f, // God
        0.80f, // Secret
        0.86f, // OG
        0.93f, // Eternal
        1.00f, // Divine
    };

    public IReadOnlyList<HalloweenZone> Zones => zones;

    public HalloweenAura GetAuraPrefab(Rarity rarity)
    {
        foreach (var e in auras)
            if (e.rarity == rarity) return e.prefab;
        return null;
    }

    public HalloweenBiomeAccent GetAccentPrefab(Biome biome)
    {
        foreach (var e in accents)
            if (e.biome == biome) return e.prefab;
        return null;
    }

    public float GetAccentDensity(Rarity rarity)
    {
        int i = (int)rarity;
        if (accentDensityByRarity == null || accentDensityByRarity.Length == 0) return 1f;
        return accentDensityByRarity[Mathf.Clamp(i, 0, accentDensityByRarity.Length - 1)];
    }

    // Tạo aura làm con của animal, aura tự fit bounds + gắn rim ở Start
    public HalloweenAura SpawnAura(Transform animal, Rarity rarity)
    {
        var prefab = GetAuraPrefab(rarity);
        return prefab ? Instantiate(prefab, animal) : null;
    }

    public HalloweenBiomeAccent SpawnAccent(Transform animal, Biome biome, Rarity rarity)
    {
        var prefab = GetAccentPrefab(biome);
        if (!prefab) return null;
        var accent = Instantiate(prefab, animal);
        // Set trước Start để ApplyDensity trong Start dùng đúng hệ số, tránh scale hai lần
        accent.SetDensity(GetAccentDensity(rarity));
        return accent;
    }

    // Điểm vào chính: aura mang tín hiệu Rarity, accent mang màu sắc Biome
    public void SpawnFor(Transform animal, Biome biome, Rarity rarity,
                         out HalloweenAura aura, out HalloweenBiomeAccent accent)
    {
        aura = SpawnAura(animal, rarity);
        accent = SpawnAccent(animal, biome, rarity);
    }

    public void SpawnFor(Transform animal, Biome biome, Rarity rarity)
        => SpawnFor(animal, biome, rarity, out _, out _);

#if UNITY_EDITOR
    public void EditorSetup(List<AuraEntry> auraList, List<HalloweenZone> zoneList)
    {
        auras = auraList;
        zones = zoneList;
    }

    public void EditorSetup(List<AuraEntry> auraList, List<HalloweenZone> zoneList, List<AccentEntry> accentList)
    {
        auras = auraList;
        zones = zoneList;
        accents = accentList;
    }
#endif
}
