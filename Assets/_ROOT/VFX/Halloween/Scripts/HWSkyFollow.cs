using UnityEngine;

// Giữ object trên trời (trăng, dơi, sao băng) đi theo camera theo trục ngang, giống skybox
public class HWSkyFollow : MonoBehaviour
{
    [SerializeField] bool followY;

    Transform cam;
    Vector3 offset;

    void Start()
    {
        var c = Camera.main;
        if (!c) return;
        cam = c.transform;
        offset = transform.position - cam.position;
    }

    void LateUpdate()
    {
        if (!cam) return;
        var p = cam.position + offset;
        if (!followY) p.y = transform.position.y;
        transform.position = p;
    }
}
