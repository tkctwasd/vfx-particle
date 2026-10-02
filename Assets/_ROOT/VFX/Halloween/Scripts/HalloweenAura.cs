using System.Collections.Generic;
using UnityEngine;

// Aura Halloween gắn làm con của animal: tự căn theo bounds của model và tạo lớp vỏ RimGlow phủ toàn bộ model
public class HalloweenAura : MonoBehaviour
{
    const string ShellName = "HW_RimShell";

    [SerializeField] Rarity rarity;
    [SerializeField] Material rimMaterial;
    [SerializeField, Tooltip("Bán kính footprint mà prefab được dựng (đơn vị gốc)")] float baseRadius = 1f;
    [SerializeField] float radiusMultiplier = 1f;
    [SerializeField] bool fitOnStart = true;
    [SerializeField] bool applyRimOnStart = true;

    readonly List<GameObject> shells = new List<GameObject>();

    public Rarity Rarity => rarity;
    Transform Target => transform.parent;

    void Start()
    {
        if (fitOnStart) FitToTarget();
        if (applyRimOnStart) ApplyRim();
    }

    void OnDestroy()
    {
        RemoveRim();
    }

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

    // Material phụ chỉ vẽ submesh cuối, nên tạo renderer vỏ dùng chung mesh/bones với rim cho mọi submesh
    public void ApplyRim()
    {
        if (!rimMaterial || !Target || HasShell()) return;
        var list = new List<Renderer>(ModelRenderers());
        foreach (var r in list)
        {
            if (r is SkinnedMeshRenderer smr && smr.sharedMesh)
            {
                var go = NewShell(transform);
                var shell = go.AddComponent<SkinnedMeshRenderer>();
                shell.sharedMesh = smr.sharedMesh;
                shell.bones = smr.bones;
                shell.rootBone = smr.rootBone;
                shell.localBounds = smr.localBounds;
                shell.updateWhenOffscreen = smr.updateWhenOffscreen;
                shell.sharedMaterials = RimArray(smr.sharedMesh.subMeshCount);
                Setup(shell);
            }
            else if (r is MeshRenderer && r.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh)
            {
                var go = NewShell(r.transform);
                go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                var shell = go.AddComponent<MeshRenderer>();
                shell.sharedMaterials = RimArray(mf.sharedMesh.subMeshCount);
                Setup(shell);
            }
        }
    }

    public void RemoveRim()
    {
        foreach (var go in shells)
        {
            if (!go) continue;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
        shells.Clear();
    }

    bool HasShell()
    {
        shells.RemoveAll(s => !s);
        if (shells.Count > 0) return true;
        foreach (Transform c in transform)
            if (c.name == ShellName) { shells.Add(c.gameObject); return true; }
        return false;
    }

    GameObject NewShell(Transform parent)
    {
        var go = new GameObject(ShellName);
        go.transform.SetParent(parent, false);
        shells.Add(go);
        return go;
    }

    Material[] RimArray(int count)
    {
        var mats = new Material[Mathf.Max(1, count)];
        for (int i = 0; i < mats.Length; i++) mats[i] = rimMaterial;
        return mats;
    }

    static void Setup(Renderer r)
    {
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    IEnumerable<Renderer> ModelRenderers()
    {
        foreach (var r in Target.GetComponentsInChildren<Renderer>())
        {
            if (r.transform.IsChildOf(transform) || r.name == ShellName) continue;
            if (r is SkinnedMeshRenderer || r is MeshRenderer) yield return r;
        }
    }

    bool TryGetBounds(out Bounds b)
    {
        b = default;
        bool has = false;
        foreach (var r in ModelRenderers())
        {
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }
        return has;
    }
}
