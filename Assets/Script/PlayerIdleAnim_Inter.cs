using UnityEngine;
using System;

public class PlayerIdleAnim_Inter : MonoBehaviour
{
    public static event Action OnUnequiping;

    public static void Unequiping()
    {
        OnUnequiping?.Invoke();
    }
}
