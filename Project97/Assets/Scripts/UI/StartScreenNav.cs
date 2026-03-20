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
    [SerializeField] private GameObject diffSelectMenu;
    [SerializeField] private GameObject sBackButton;
    [SerializeField] private GameObject easyButton;
    [SerializeField] private GameObject normalButton;
    [SerializeField] private GameObject hardButton;
    public void EnterGame()
    {
        SceneManager.LoadScene(1);
    }

    public void StartGame()
    {
        mainMenu.SetActive(false);
        diffSelectMenu.SetActive(true);
        backButton.SetActive(true);
        settingsMenu.SetActive(false);
        easyButton.SetActive(true);
        normalButton.SetActive(true);
        hardButton.SetActive(true);
    }

    public void OpenSettings()
    {
        settingsMenu.SetActive(true);
    }

    public void CloseSetttings()
    {
        settingsMenu.SetActive(false);
    }

    public void ExitGame()
    {
        Debug.Log("Quitting");
        Application.Quit();
    }
}
