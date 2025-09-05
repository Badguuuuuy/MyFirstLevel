using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class VfxDict
{
    [SerializeField]
    VfxDictItem[] thisVfxDictItems;

    public Dictionary<string, ParticleSystem> ToDictionary()
    {
        Dictionary<string, ParticleSystem> newDict = new Dictionary<string, ParticleSystem>();

        foreach(var item in thisVfxDictItems)
        {
            newDict.Add(item.name, item.obj);
        }
        return newDict;
    }
}

[Serializable]
public class VfxDictItem
{
    public string name;
    public ParticleSystem obj;
}

[Serializable]
public class VfxItemInfo
{
    //public int type;
    public Vector3 position;
    public Vector3 rotation;            //type을 배열 순서와 잘 조정해보기
}

public class PlayerAttackEffectController : MonoBehaviour
{
    public GameObject playerModel;
    public Transform vfxPoint;
    private PlayerModelEventHandler playerModelEventHandler;

    VFXPool vfxPool;

    [SerializeField]
    VfxDict vfxDict;

    [SerializeField]
    Dictionary<string, ParticleSystem> vfxObj;

    [SerializeField]
    VfxItemInfo[] vfxItemInfos;

    void Start()
    {
        playerModelEventHandler = playerModel.GetComponent<PlayerModelEventHandler>();
        vfxPool = transform.GetComponent<VFXPool>();
        //playerModelEventHandler.TriggerAttackVFX+= TriggerVFX;
        vfxObj = vfxDict.ToDictionary();
    }

    public void TriggerVFX(int type)
    {
        switch (type)
        {
            case 0:
                vfxObj.TryGetValue("Slash1", out ParticleSystem obj1);
                obj1.transform.position = vfxPoint.transform.position + vfxPoint.transform.TransformDirection(vfxItemInfos[type].position);
                obj1.transform.rotation = vfxPoint.transform.rotation * Quaternion.Euler(vfxItemInfos[type].rotation);
                obj1.Stop();
                obj1.Play();
                Debug.Log("vfx 1번타입 작동함");
                break;
            case 1:
                vfxObj.TryGetValue("Slash1", out ParticleSystem obj2);
                obj2.transform.position = vfxPoint.transform.position + vfxPoint.transform.TransformDirection(vfxItemInfos[type].position);
                obj2.transform.rotation = vfxPoint.transform.rotation * Quaternion.Euler(vfxItemInfos[type].rotation);
                obj2.Stop();
                obj2.Play();
                Debug.Log("vfx 2번타입 작동함");
                break;
        }
    }
}
