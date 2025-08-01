using System.Collections.Generic;
using UnityEngine;

public class CooldownManager : MonoBehaviour
{
    private Dictionary<string, float> cooldowns = new Dictionary<string, float>();

    // ��ٿ� ����
    public void StartCooldown(string key, float duration)
    {
        cooldowns[key] = Time.time + duration;
    }

    // ��ٿ� ������ Ȯ��
    public bool IsOnCooldown(string key)
    {
        if (cooldowns.TryGetValue(key, out float endTime))
        {
            return Time.time < endTime;
        }
        return false;
    }

    // ���� �ð� ��ȯ (������ 0 ��ȯ)
    public float GetRemainingTime(string key)
    {
        if (cooldowns.TryGetValue(key, out float endTime))
        {
            float remaining = endTime - Time.time;
            return remaining > 0 ? remaining : 0f;
        }
        return 0f;
    }
}