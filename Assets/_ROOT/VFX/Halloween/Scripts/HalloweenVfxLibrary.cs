using System;
using System.Collections.Generic;
using UnityEngine;

// Bảng tra aura theo Rarity và danh sách zone Halloween
[CreateAssetMenu(menuName = "VFX/Halloween Vfx Library", fileName = "HalloweenVfxLibrary")]
public class HalloweenVfxLibrary : ScriptableObject
{
    [Serializable]
    public class AuraEntry
    {
        public Rarity rarity;
        public HalloweenAura prefab;
    }

    [SerializeField] List<AuraEntry> auras = new List<AuraEntry>();
    [SerializeField] List<HalloweenZone> zones = new List<HalloweenZone>();

    public IReadOnlyList<HalloweenZone> Zones => zones;

    public HalloweenAura GetAuraPrefab(Rarity rarity)
    {
        foreach (var e in auras)
            if (e.rarity == rarity) return e.prefab;
        return null;
    }

    // Tạo aura làm con của animal, aura tự fit bounds + gắn rim ở Start
    public HalloweenAura SpawnAura(Transform animal, Rarity rarity)
    {
        var prefab = GetAuraPrefab(rarity);
        return prefab ? Instantiate(prefab, animal) : null;
    }

#if UNITY_EDITOR
    public void EditorSetup(List<AuraEntry> auraList, List<HalloweenZone> zoneList)
    {
        auras = auraList;
        zones = zoneList;
    }
#endif
}
