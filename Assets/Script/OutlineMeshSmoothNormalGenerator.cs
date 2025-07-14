using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class OutlineMeshSmoothNormalGenerator : MonoBehaviour
{
    [MenuItem("Tools/Generate Smooth Normal Outline Mesh")]
    static void GenerateOutlineMesh()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogError("������Ʈ�� �����ϼ���!");
            return;
        }

        MeshFilter mf = selected.GetComponent<MeshFilter>();
        SkinnedMeshRenderer smr = selected.GetComponent<SkinnedMeshRenderer>();

        Mesh originalMesh = null;
        if (mf != null && mf.sharedMesh != null)
        {
            originalMesh = mf.sharedMesh;
        }
        else if (smr != null && smr.sharedMesh != null)
        {
            originalMesh = smr.sharedMesh;
        }
        else
        {
            Debug.LogError("MeshFilter �Ǵ� SkinnedMeshRenderer�� ���ų� �޽��� �����ϴ�.");
            return;
        }

        // �޽� ����
        Mesh outlineMesh = Instantiate(originalMesh);

        // ������ ��� ���
        Vector3[] smoothNormals = CalculateSmoothNormals(outlineMesh);

        // ������ ��ַ� �����
        outlineMesh.normals = smoothNormals;

        // �̸� �ٲٱ�
        outlineMesh.name = originalMesh.name + "_OutlineSmooth";

        // ������ ���� ���
        string folderPath = "Assets/GeneratedMeshes";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }

        // ������ ���� ��� (���� �̸� �ߺ� ����)
        string assetPath = folderPath + "/" + outlineMesh.name + ".asset";
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        // ���� ����
        AssetDatabase.CreateAsset(outlineMesh, assetPath);
        AssetDatabase.SaveAssets();

        // �ܰ����� ���ӿ�����Ʈ ����
        GameObject outlineObj = new GameObject(selected.name + "_Outline");
        outlineObj.transform.SetParent(selected.transform.parent);
        outlineObj.transform.position = selected.transform.position;
        outlineObj.transform.rotation = selected.transform.rotation;
        outlineObj.transform.localScale = selected.transform.localScale;

        // ���� �޽� ������ �߰�
        if (mf != null)
        {
            MeshFilter outlineMF = outlineObj.AddComponent<MeshFilter>();
            outlineMF.sharedMesh = outlineMesh; // ���� ���� ����
            MeshRenderer outlineMR = outlineObj.AddComponent<MeshRenderer>();
            outlineMR.sharedMaterials = selected.GetComponent<MeshRenderer>().sharedMaterials;
        }
        else if (smr != null)
        {
            SkinnedMeshRenderer outlineSMR = outlineObj.AddComponent<SkinnedMeshRenderer>();
            outlineSMR.sharedMesh = outlineMesh; // ���� ����
            outlineSMR.rootBone = smr.rootBone;
            outlineSMR.bones = smr.bones;
            outlineSMR.sharedMaterials = smr.sharedMaterials;
        }

        Debug.Log("�ܰ����� ������ ��� �޽� ���� �� ���� ���� �Ϸ�: " + outlineObj.name);
    }

    static Vector3[] CalculateSmoothNormals(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector3[] smoothNormals = new Vector3[vertices.Length];

        // ��ġ���� ���� �׷���
        Dictionary<Vector3, List<int>> vertexGroups = new Dictionary<Vector3, List<int>>();

        float precision = 0.0001f;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 pos = vertices[i];
            pos.x = Mathf.Round(pos.x / precision) * precision;
            pos.y = Mathf.Round(pos.y / precision) * precision;
            pos.z = Mathf.Round(pos.z / precision) * precision;

            if (!vertexGroups.ContainsKey(pos))
                vertexGroups[pos] = new List<int>();

            vertexGroups[pos].Add(i);
        }

        foreach (var kvp in vertexGroups)
        {
            Vector3 sumNormal = Vector3.zero;
            foreach (int idx in kvp.Value)
            {
                sumNormal += normals[idx];
            }
            sumNormal.Normalize();

            foreach (int idx in kvp.Value)
            {
                smoothNormals[idx] = sumNormal;
            }
        }

        return smoothNormals;
    }
}