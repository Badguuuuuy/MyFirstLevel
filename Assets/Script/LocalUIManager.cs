using UnityEngine;

public class LocalUIManager : MonoBehaviour
{
    public GameObject pausePanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pausePanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
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
