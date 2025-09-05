using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MeshTrail : MonoBehaviour
{
    private bool isTrailActive = false;
    private float elapsed = 0f;
    private SkinnedMeshRenderer[] skinnedMeshRenderers;
    private List<(MeshFilter, MeshRenderer)> meshObjects = new List<(MeshFilter, MeshRenderer)>();


    public Material trailMaterial;
    public float meshRefreshRate = 0.001f;
    public Transform positionToSpawn;

    private int trailCount = 0;
    private int maxTrailCount = 5;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        var filteredList = new List<SkinnedMeshRenderer>();
        foreach (var smr in skinnedMeshRenderers)
        {
            if (smr.gameObject.tag != "Outline")
                filteredList.Add(smr);
        }
        skinnedMeshRenderers = filteredList.ToArray();

        meshObjects.Clear();
        foreach (MeshFilter mf in GetComponentsInChildren<MeshFilter>())
        {
            if (mf.gameObject.tag != "Outline")
            {
                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                if (mr != null)
                    meshObjects.Add((mf, mr));
            }
        }
    }

    // Update is called once per frame
    void Update()
    { 
        /*
        if (isTrailActive)
        {
            elapsed += Time.deltaTime;
            while (elapsed >= meshRefreshRate)
            {
                Debug.Log("CreateTrail");
                elapsed -= meshRefreshRate;
                CreateTrail();
            }
        }
        */
    }
    private void FixedUpdate()
    {
        
    }

    private IEnumerator TrailEffectCoroutine()
    {
        //EnableTrail();
        trailCount = 0;
        while (trailCount < maxTrailCount)
        {
            //CreateTrail();
            trailCount++;
            yield return new WaitForSeconds(meshRefreshRate);
        }
        DisableTrail();
    }

    public void EnableTrail()
    {
        if (!isTrailActive)
        {
            StartCoroutine(TrailEffectCoroutine());
            isTrailActive = true;
            elapsed = 0f;
        }
    }
    public void DisableTrail()
    {
        if (isTrailActive)
        {
            isTrailActive = false;

            elapsed = 0f;
        }
    }

    public void CreateTrail()
    {
        foreach (SkinnedMeshRenderer smr in skinnedMeshRenderers) { 
            GameObject trailObj = new GameObject("charMeshTrail");
            trailObj.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);
            //trailObj.transform.localScale = smr.transform.lossyScale;

            MeshRenderer mr = trailObj.AddComponent<MeshRenderer>();
            MeshFilter mf = trailObj.AddComponent<MeshFilter>();

            Mesh mesh = new Mesh();

            smr.BakeMesh(mesh);

            Material mat = new Material(smr.material);

            mat.SetColor("_Color", new Color(mat.color.r, mat.color.g, mat.color.b, 0.3f)); 

            //mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.5f);

            mr.material = mat;
            mf.mesh = mesh;

            Destroy(trailObj, 1f);
        }
        foreach ((MeshFilter meshFilters, MeshRenderer meshRenderers) in meshObjects) { 
            GameObject trailObj = new GameObject("objMeshTrail");
            trailObj.transform.SetPositionAndRotation(meshFilters.transform.position, meshFilters.transform.rotation);
            trailObj.transform.localScale = meshFilters.transform.lossyScale;

            MeshRenderer mr = trailObj.AddComponent<MeshRenderer>();
            MeshFilter mf = trailObj.AddComponent<MeshFilter>();

            var mat = mr.material;

            mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.5f);

            mr.material = mat;
            mf.mesh = meshFilters.mesh;

            Destroy(trailObj, 1f);
        }
    }
}
