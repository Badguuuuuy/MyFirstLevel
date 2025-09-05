using Unity.Cinemachine;
using UnityEngine;

public class PlayerEffectManager : MonoBehaviour
{
    [SerializeField] private PlayerAttackEffectController playerAttackEffectController;
    private AimCameraEffectController aimCameraEffectController;
    private PlayerCharacterEffectController playerCharacterEffectController;
    [HideInInspector] public CinemachineCamera mainCamInstance;
    [HideInInspector] public CinemachineCamera uiCamInstance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerCharacterEffectController = GetComponent<PlayerCharacterEffectController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initialize(CinemachineCamera main, CinemachineCamera ui)
    {
        mainCamInstance = main;
        uiCamInstance = ui;

        aimCameraEffectController = mainCamInstance.GetComponentInChildren<AimCameraEffectController>();
        // 기타 초기화
    }

    public void TriggerAttackVFX(int type)
    {
        playerAttackEffectController.TriggerVFX(type);
    }
    public void TriggerDodgeVFX(float duration, float cycle)
    {
        playerCharacterEffectController.TriggerTrail(duration, cycle);
        aimCameraEffectController.TriggerLensDistortion(duration);
        aimCameraEffectController.TriggerSpeedLine(duration);
    }
}
