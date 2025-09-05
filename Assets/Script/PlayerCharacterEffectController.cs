using System.Collections;
using UnityEngine;

public class PlayerCharacterEffectController : MonoBehaviour
{
    [Header("MeshTrail")]
    MeshTrail meshTrail;
    bool isTrailActive = false;
    float elapsed = 0f;
    float trailDuration = 0f;
    float trailCycle = 0f;
    Coroutine trailCoroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        meshTrail = GetComponent<MeshTrail>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void TriggerTrail(float duration, float cycle)
    {
        if (trailCoroutine != null)
            StopCoroutine(trailCoroutine);

        trailCoroutine = StartCoroutine(TrailRoutine(duration, cycle));
    }

    private IEnumerator TrailRoutine(float duration, float cycle)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // 원하는 위치/회전으로 트레일 생성
            meshTrail.CreateTrail();
            yield return new WaitForSeconds(cycle);
            elapsed += cycle;
        }
    }
}
