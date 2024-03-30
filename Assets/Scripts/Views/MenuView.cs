using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuView : MonoBehaviour
{
    [SerializeField] private MenuButton startButton;
    [SerializeField] private MenuButton optionsButton;
    [SerializeField] private MenuButton exitButton;
    [SerializeField] private GameObject optionsPanel;

	private void Awake()
	{
        startButton.onClick += OnClickStart;
        optionsButton.onClick += OnClickOptions;
        exitButton.onClick += OnClickExit;
	}

	private void OnClickStart()
	{
        SceneManager.LoadScene(2);
	}

    private void OnClickOptions()
    {
        optionsPanel.SetActive(true);
    }

    private void OnClickExit()
    {
        Application.Quit();
    }
}
