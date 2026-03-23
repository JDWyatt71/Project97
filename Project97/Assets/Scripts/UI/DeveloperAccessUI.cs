using UnityEngine;
using TMPro;
using System;

public class DeveloperAccessUI : MonoBehaviour
{
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject devToolsScreen;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TextMeshProUGUI errorMessage;
    [SerializeField] private GameObject dBackButton;

    private const string PASSWORD = "dev97";

    public void OpenLogin()
    {
        loginPanel.SetActive(true);
        devToolsScreen.SetActive(false);
        errorMessage.text = "";
        passwordInput.text = "";
    }

    public void SubmitPassword()
    {
         if (passwordInput.text == PASSWORD)
        {
            loginPanel.SetActive(false);
            devToolsScreen.SetActive(true);
        }
        else
        {
            errorMessage.text = "Incrorrect Password";
        }
    }

    public void OpenToolkit()
    {
        Application.OpenURL("http://localhost:8502");
    }

    public void OpenDashboard()
    {
        Application.OpenURL("http://localhost:8501");
    }

    public void CloseDevMenu()
    {
        loginPanel.SetActive(false);
        devToolsScreen.SetActive(false);
    }
}
