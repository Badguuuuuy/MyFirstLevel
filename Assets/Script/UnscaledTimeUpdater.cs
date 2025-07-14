using UnityEngine;

public class UnscaledTimeUpdater : MonoBehaviour
{
    void Update()
    {
        Shader.SetGlobalFloat("_UnscaledTime", Time.unscaledTime);
        //Debug.Log("ºŒ¿Ã¥ı ±€∑Œπ˙ ≈∏¿”: " + Shader.GetGlobalFloat("_UnscaledTime"));
    }
}