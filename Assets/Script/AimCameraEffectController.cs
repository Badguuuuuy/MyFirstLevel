using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.VFX;

[Serializable]
public class CamVfxDict
{
    [SerializeField]
    CamVfxDictItem[] thisCamVfxDictItems;

    public Dictionary<string, VisualEffect> ToDictionary()
    {
        Dictionary<string, VisualEffect> newDict = new Dictionary<string, VisualEffect>();

        foreach (var item in thisCamVfxDictItems)
        {
            newDict.Add(item.name, item.obj);
        }
        return newDict;
    }
}

[Serializable]
public class CamVfxDictItem
{
    public string name;
    public VisualEffect obj;
}

public class AimCameraEffectController : MonoBehaviour
{
    [SerializeField] private CinemachineVolumeSettings maincamPostProcessing;
    

    [SerializeField]
    CamVfxDict vfxDict;

    [SerializeField]
    Dictionary<string, VisualEffect> vfxObj;

    [Header("SpeedLine")]
    Coroutine speedLineCoroutine;

    [Header("LensDistortion")]
    Coroutine lensDistortionCoroutine;
    float lensDistortionElapsed = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        vfxObj = vfxDict.ToDictionary();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TriggerLensDistortion(float duration)
    {
        if (lensDistortionCoroutine != null)
            StopCoroutine(lensDistortionCoroutine);
        lensDistortionCoroutine = StartCoroutine(LensDistortionCoroutine(duration));
    }
    private IEnumerator LensDistortionCoroutine(float duration)
    {
        lensDistortionElapsed = 0f;
        maincamPostProcessing.Weight = 0f;
        while (lensDistortionElapsed < duration / 4f) { 
            lensDistortionElapsed += Time.deltaTime;
            maincamPostProcessing.Weight = Mathf.Lerp(0f, 1f, lensDistortionElapsed / (duration / 4f));
            yield return null;
        }
        maincamPostProcessing.Weight = 1f;
        lensDistortionElapsed = duration / 4f;
        while (lensDistortionElapsed < duration)
        {
            lensDistortionElapsed += Time.deltaTime;
            maincamPostProcessing.Weight = Mathf.Lerp(1f, 0f, lensDistortionElapsed / duration);
            yield return null;
        }
        maincamPostProcessing.Weight = 0f;
        lensDistortionElapsed = 0f;
    }
    public void TriggerSpeedLine(float duration)
    {
        if(speedLineCoroutine != null)
            StopCoroutine(speedLineCoroutine);
        speedLineCoroutine = StartCoroutine(SpeedLineCoroutine(duration));
    }
    private IEnumerator SpeedLineCoroutine(float duration)
    {
        vfxObj["SpeedLine"].Play();
        yield return new WaitForSeconds(duration);
        vfxObj["SpeedLine"].Stop();
    }
}
