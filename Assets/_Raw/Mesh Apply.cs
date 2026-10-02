using UnityEngine;
using UnityEditor;

public class ApplyTransformTool
{
    [MenuItem("Tools/Apply Transform (Freeze)")]
    public static void ApplyTransform()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("Vui lòng chọn ít nhất 1 GameObject để Apply Transform.");
            return;
        }

        foreach (GameObject obj in Selection.gameObjects)
        {
            BakeTransform(obj);
        }
        
        Debug.Log("Đã Apply Transform thành công cho các object được chọn!");
    }

    private static void BakeTransform(GameObject obj)
    {
        MeshFilter mf = obj.GetComponent<MeshFilter>();
        
        // Nếu object có Mesh, ta tiến hành "nướng" (bake) transform vào lưới
        if (mf != null && mf.sharedMesh != null)
        {
            // Clone mesh để không ghi đè làm hỏng file model gốc (.fbx, .obj)
            Mesh newMesh = Object.Instantiate(mf.sharedMesh);
            newMesh.name = mf.sharedMesh.name + "_Applied";

            Vector3 scale = obj.transform.localScale;
            Quaternion rotation = obj.transform.localRotation;

            Vector3[] vertices = newMesh.vertices;
            Vector3[] normals = newMesh.normals;

            for (int i = 0; i < vertices.Length; i++)
            {
                // Áp dụng Scale cho từng đỉnh
                Vector3 v = vertices[i];
                v.x *= scale.x;
                v.y *= scale.y;
                v.z *= scale.z;

                // Áp dụng Rotation cho từng đỉnh
                vertices[i] = rotation * v;

                // Xoay luôn cả Normal (pháp tuyến) để ánh sáng chiếu vào không bị sai
                if (normals.Length > i)
                {
                    normals[i] = rotation * normals[i];
                }
            }

            newMesh.vertices = vertices;
            if (normals.Length > 0) newMesh.normals = normals;

            newMesh.RecalculateBounds();

            // Ghi lại lịch sử để có thể Ctrl+Z (Undo)
            Undo.RecordObject(mf, "Apply Transform Mesh");
            mf.sharedMesh = newMesh;

            // Nếu object có dùng MeshCollider, cập nhật luôn mesh mới cho collider
            MeshCollider mc = obj.GetComponent<MeshCollider>();
            if (mc != null)
            {
                Undo.RecordObject(mc, "Apply Transform Collider");
                mc.sharedMesh = newMesh;
            }
        }

        // Cuối cùng, đưa Rotation về 0 và Scale về 1
        Undo.RecordObject(obj.transform, "Apply Transform Variables");
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
    }
}