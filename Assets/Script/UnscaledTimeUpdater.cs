using UnityEngine;

public class UnscaledTimeUpdater : MonoBehaviour
{
    void Update()
    {
        Shader.SetGlobalFloat("_UnscaledTime", Time.unscaledTime);
        //Debug.Log("���̴� �۷ι� Ÿ��: " + Shader.GetGlobalFloat("_UnscaledTime"));
    }
}