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
	[Inject] IConsentService consent;

	[SerializeField] private GameObject panel;
	[SerializeField] private Button closeButton;
	[SerializeField] private Button musicButton;
	[SerializeField] private Button sfxButton;
	[SerializeField] private Button resetButton;
	[SerializeField] private GameObject confirmPanel;
	[SerializeField] private Button confirmResetButton;
	[SerializeField] private Button cancelResetButton;

	private Button privacyButton;

	private void Awake()
	{
		closeButton.onClick.AddListener(CloseView);
		musicButton.onClick.AddListener(() => { audioController.MusicEnabled = !audioController.MusicEnabled; Refresh(); });
		sfxButton.onClick.AddListener(() => { audioController.SfxEnabled = !audioController.SfxEnabled; Refresh(); });
		resetButton.onClick.AddListener(() => confirmPanel.SetActive(true));
		cancelResetButton.onClick.AddListener(() => confirmPanel.SetActive(false));
		confirmResetButton.onClick.AddListener(() => saveController.ResetProgress());
		CreatePrivacyButton();
		signalBus.Subscribe<MenuOpenSignal>(OnMenuOpen);
		consent.OnConsent(RefreshPrivacyButton);
	}

	private void OnDestroy()
	{
		signalBus.Unsubscribe<MenuOpenSignal>(OnMenuOpen);
		consent.RemoveOnConsent(RefreshPrivacyButton);
	}

	// A row like the others, above Reset progress; shown only where players must be able to change consent
	private void CreatePrivacyButton()
	{
		privacyButton = Instantiate(resetButton, resetButton.transform.parent);
		privacyButton.name = "PrivacyButton";
		privacyButton.transform.SetSiblingIndex(resetButton.transform.GetSiblingIndex());
		privacyButton.onClick.RemoveAllListeners();
		privacyButton.onClick.AddListener(() => consent.ShowPrivacyOptions());
		TMP_Text label = privacyButton.GetComponentInChildren<TMP_Text>(true);
		if (label != null)
		{
			label.text = "Privacy settings";
			label.color = Color.white;
		}

		RefreshPrivacyButton();
	}

	private void RefreshPrivacyButton()
	{
		if (privacyButton != null)
		{
			privacyButton.gameObject.SetActive(consent.PrivacyOptionsRequired);
		}
	}

	private void OnMenuOpen(MenuOpenSignal signal)
	{
		// Opening another window closes this one
		if (MenuTypes.ClosesFor(signal.Type, MenuType.Settings))
		{
			panel.SetActive(false);
			return;
		}

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
