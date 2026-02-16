using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class StartScreenNav : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject backButton;
    public GameObject settingsButton;
    public GameObject settingsMenu;
    [SerializeField] private GameObject sBackButton;
    
    public void EnterGame()
    {
        mainMenu.SetActive(false);
        backButton.SetActive(true);
    }

    public void OpenSettings()
    {
        mainMenu.SetActive(false);
        settingsMenu.SetActive(true);
    }

    public void StartBattle()
    {
        SceneManager.LoadScene("Main Scene");
    }

    public void ExitGame()
    {
        Debug.Log("Quitting");
        Application.Quit();
    }
}
