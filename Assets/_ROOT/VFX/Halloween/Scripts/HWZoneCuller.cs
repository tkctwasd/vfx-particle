using UnityEngine;

// Tắt VFX của khu ở xa camera theo trục X (map dạng hành lang dài) để tiết kiệm CPU trên mobile
public class HWZoneCuller : MonoBehaviour
{
    [SerializeField] float minX;
    [SerializeField] float maxX;
    [SerializeField] float margin = 120f;
    [SerializeField] float checkInterval = 0.5f;

    Transform cam;
    float timer;
    bool visible = true;

    public void Setup(float xMin, float xMax)
    {
        minX = xMin;
        maxX = xMax;
    }

    void Start()
    {
        var c = Camera.main;
        if (c) cam = c.transform;
        Refresh();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < checkInterval) return;
        timer = 0f;
        Refresh();
    }

    void Refresh()
    {
        if (!cam) return;
        float x = cam.position.x;
        bool v = x > minX - margin && x < maxX + margin;
        if (v == visible) return;
        visible = v;
        foreach (Transform c in transform) c.gameObject.SetActive(v);
    }
}
