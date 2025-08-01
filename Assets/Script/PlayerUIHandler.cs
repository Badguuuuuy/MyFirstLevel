using UnityEngine;

public class PlayerUIHandler : MonoBehaviour
{
    [Header("UI Prefab")]
    public GameObject localUIPrefab;

    public MainCamController mainCam;

    [Header("Parameters")]
    public bool useTimeStop = true;

    private LocalUIManager localUIManager;

    private float ts;

    private IPlayerState prePlayerState;

    private PlayerController playerController;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerController = GetComponent<PlayerController>();
        prePlayerState = playerController.CurrentState;
        if (localUIPrefab != null)
        {
            GameObject uiInstance = Instantiate(localUIPrefab);
            localUIManager = uiInstance.GetComponent<LocalUIManager>();
            localUIManager.transform.GetComponent<Canvas>().worldCamera = mainCam.uiCam;
            localUIManager.player = gameObject;
        }
        else
        {
            Debug.LogError("Local UI Prefab이 할당되지 않았습니다.");
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ts = Time.timeScale;
    }

    // Update is called once per frame
    void Update()
    {
        /*
        if(Cursor.lockState == CursorLockMode.None)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Time.timeScale = ts;
            }
        }
        Debug.Log(Cursor.lockState);
        */
        //Debug.Log(Cursor.lockState);
        
    }

    public void TogglePauseMenu()
    {
        if (Cursor.lockState == CursorLockMode.None)
        {
            playerController.SwitchState(prePlayerState);

            mainCam.EnableMainCamera();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = ts;
            localUIManager.DisablePauseMenu();
            
        }
        else if (Cursor.lockState == CursorLockMode.Locked)
        {
            Debug.Log("오이 마떼");
            prePlayerState = playerController.CurrentState;
            playerController.SwitchState(playerController.uiState);

            mainCam.EnableUICamera();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (useTimeStop)
            {
                ts = Time.timeScale;
                Time.timeScale = 0f;
                Debug.Log("시간멈춤");
            }
            localUIManager.EnablePauseMenu();
        }
    }
}
