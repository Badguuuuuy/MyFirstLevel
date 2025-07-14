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
            Debug.LogError("오브젝트를 선택하세요!");
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
            Debug.LogError("MeshFilter 또는 SkinnedMeshRenderer가 없거나 메쉬가 없습니다.");
            return;
        }

        // 메쉬 복사
        Mesh outlineMesh = Instantiate(originalMesh);

        // 스무스 노멀 계산
        Vector3[] smoothNormals = CalculateSmoothNormals(outlineMesh);

        // 스무스 노멀로 덮어쓰기
        outlineMesh.normals = smoothNormals;

        // 이름 바꾸기
        outlineMesh.name = originalMesh.name + "_OutlineSmooth";

        // 저장할 폴더 경로
        string folderPath = "Assets/GeneratedMeshes";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }

        // 저장할 에셋 경로 (같은 이름 중복 방지)
        string assetPath = folderPath + "/" + outlineMesh.name + ".asset";
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        // 에셋 생성
        AssetDatabase.CreateAsset(outlineMesh, assetPath);
        AssetDatabase.SaveAssets();

        // 외곽선용 게임오브젝트 생성
        GameObject outlineObj = new GameObject(selected.name + "_Outline");
        outlineObj.transform.SetParent(selected.transform.parent);
        outlineObj.transform.position = selected.transform.position;
        outlineObj.transform.rotation = selected.transform.rotation;
        outlineObj.transform.localScale = selected.transform.localScale;

        // 복제 메쉬 렌더러 추가
        if (mf != null)
        {
            MeshFilter outlineMF = outlineObj.AddComponent<MeshFilter>();
            outlineMF.sharedMesh = outlineMesh; // 이제 에셋 참조
            MeshRenderer outlineMR = outlineObj.AddComponent<MeshRenderer>();
            outlineMR.sharedMaterials = selected.GetComponent<MeshRenderer>().sharedMaterials;
        }
        else if (smr != null)
        {
            SkinnedMeshRenderer outlineSMR = outlineObj.AddComponent<SkinnedMeshRenderer>();
            outlineSMR.sharedMesh = outlineMesh; // 에셋 참조
            outlineSMR.rootBone = smr.rootBone;
            outlineSMR.bones = smr.bones;
            outlineSMR.sharedMaterials = smr.sharedMaterials;
        }

        Debug.Log("외곽선용 스무스 노멀 메쉬 생성 및 에셋 저장 완료: " + outlineObj.name);
    }

    static Vector3[] CalculateSmoothNormals(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector3[] smoothNormals = new Vector3[vertices.Length];

        // 위치별로 정점 그룹핑
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