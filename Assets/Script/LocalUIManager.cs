using UnityEngine;
using TMPro;

public class LocalUIManager : MonoBehaviour
{
    public GameObject pausePanel;

    public GameObject player;

    public GameObject debugText;

    private PlayerMovementController playerMovementController;

    private TMP_Text debugTextMesh;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pausePanel.SetActive(false);
        playerMovementController = player.GetComponent<PlayerMovementController>();
        debugTextMesh = debugText.GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
        debugTextMesh.text = ("Grounded: " + playerMovementController.grounded);
    }

    public void EnablePauseMenu()
    {
        pausePanel.SetActive(true);
    }
    public void DisablePauseMenu()
    {
        pausePanel.SetActive(false);
    }
}
