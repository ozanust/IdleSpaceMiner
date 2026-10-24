using UnityEngine;
using UnityEngine.UI;
using Zenject;
using TMPro;

/// <summary>Settings window: music and sound toggles, and a progress reset behind a confirmation.</summary>
public class SettingsView : MonoBehaviour
{
	[Inject] SignalBus signalBus;
	[Inject] IAudioController audioController;
	[Inject] SaveController saveController;

	[SerializeField] private GameObject panel;
	[SerializeField] private Button closeButton;
	[SerializeField] private Button musicButton;
	[SerializeField] private Button sfxButton;
	[SerializeField] private Button resetButton;
	[SerializeField] private GameObject confirmPanel;
	[SerializeField] private Button confirmResetButton;
	[SerializeField] private Button cancelResetButton;

	private void Awake()
	{
		closeButton.onClick.AddListener(CloseView);
		musicButton.onClick.AddListener(() => { audioController.MusicEnabled = !audioController.MusicEnabled; Refresh(); });
		sfxButton.onClick.AddListener(() => { audioController.SfxEnabled = !audioController.SfxEnabled; Refresh(); });
		resetButton.onClick.AddListener(() => confirmPanel.SetActive(true));
		cancelResetButton.onClick.AddListener(() => confirmPanel.SetActive(false));
		confirmResetButton.onClick.AddListener(() => saveController.ResetProgress());
		signalBus.Subscribe<MenuOpenSignal>(OnMenuOpen);
	}

	private void OnDestroy()
	{
		signalBus.Unsubscribe<MenuOpenSignal>(OnMenuOpen);
	}

	private void OnMenuOpen(MenuOpenSignal signal)
	{
		if (signal.Type == MenuType.Settings)
		{
			panel.SetActive(!panel.activeInHierarchy);
			confirmPanel.SetActive(false);
			Refresh();
		}
	}

	private void CloseView()
	{
		panel.SetActive(false);
	}

	private void Refresh()
	{
		SetLabel(musicButton, "Music: " + (audioController.MusicEnabled ? "On" : "Off"));
		SetLabel(sfxButton, "Sound effects: " + (audioController.SfxEnabled ? "On" : "Off"));
	}

	private static void SetLabel(Button button, string text)
	{
		TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
		if (label != null)
		{
			label.text = text;
		}
	}
}
