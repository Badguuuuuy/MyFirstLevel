using System.Collections.Generic;
using UnityEngine;

public class CooldownManager : MonoBehaviour
{
    private Dictionary<string, float> cooldowns = new Dictionary<string, float>();

    // 쿨다운 시작
    public void StartCooldown(string key, float duration)
    {
        cooldowns[key] = Time.time + duration;
    }

    // 쿨다운 중인지 확인
    public bool IsOnCooldown(string key)
    {
        if (cooldowns.TryGetValue(key, out float endTime))
        {
            return Time.time < endTime;
        }
        return false;
    }

    // 남은 시간 반환 (끝나면 0 반환)
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