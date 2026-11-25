using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the settings window with its HUD button and the toast used for purchase feedback in
/// GameScene. Shares the tasks window helpers. Running it again replaces what it built before.
/// </summary>
public static class SettingsBuilder
{
	private const string ScenePath = "Assets/Scenes/GameScene.unity";
	private const string WindowName = "SettingsView";
	private const string HudButtonName = "SettingsButton";
	private const string ToastName = "ToastView";
	private const string TutorialName = "TutorialView";

	private const float RowHeight = 110f;
	private static readonly Color BackingColor = new Color(0.02f, 0.06f, 0.1f, 0.94f);
	private static readonly Color WarningColor = new Color(1f, 0.55f, 0.45f, 1f);
	private static readonly Color MutedColor = new Color(0.55f, 0.85f, 0.92f, 1f);

	[MenuItem("Tools/UI/Build Settings And Toast")]
	public static void BuildInOpenScene()
	{
		Build();
		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
	}

	/// <summary>Batch mode entry point: opens GameScene, builds and saves the scene.</summary>
	public static void BuildAndSaveScene()
	{
		EditorSceneManager.OpenScene(ScenePath);
		Build();
		EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
	}

	private static void Build()
	{
		Transform canvas = Object.FindAnyObjectByType<NavBarView>(FindObjectsInactive.Include).transform.parent;
		foreach (string name in new[] { WindowName, HudButtonName, ToastName })
		{
			Transform old = canvas.Find(name);
			if (old != null)
			{
				Object.DestroyImmediate(old.gameObject);
			}
		}

		TMP_Text textTemplate = canvas.Find(TasksWindowBuilder.TextTemplatePath).GetComponent<TMP_Text>();
		Transform buttonTemplate = canvas.Find(TasksWindowBuilder.ClaimButtonTemplatePath);

		BuildWindow(canvas, textTemplate, buttonTemplate);
		BuildHudButton(canvas);
		BuildToast(canvas, textTemplate);
	}

	private static void BuildWindow(Transform canvas, TMP_Text textTemplate, Transform buttonTemplate)
	{
		Transform panel;
		Button closeButton;
		ScrollRect scroll;
		GameObject window = TasksWindowBuilder.CreateListWindow(canvas, WindowName, "Settings", out panel, out closeButton, out scroll);
		RectTransform content = scroll.content;

		Button musicButton = Row(buttonTemplate, content, "MusicButton", "Music: On");
		Button sfxButton = Row(buttonTemplate, content, "SfxButton", "Sound effects: On");
		Button resetButton = Row(buttonTemplate, content, "ResetButton", "Reset progress");
		resetButton.GetComponentInChildren<TMP_Text>(true).color = WarningColor;

		TMP_Text version = TasksWindowBuilder.CreateText("VersionText", content, textTemplate, 28, MutedColor, TextAlignmentOptions.Center);
		version.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
		version.text = "Version " + PlayerSettings.bundleVersion;

		// Confirmation covers the list until the player decides
		GameObject confirm = new GameObject("ConfirmPanel", typeof(RectTransform), typeof(Image));
		confirm.layer = canvas.gameObject.layer;
		RectTransform confirmRect = (RectTransform)confirm.transform;
		confirmRect.SetParent(scroll.transform, false);
		TasksWindowBuilder.Stretch(confirmRect);
		confirm.GetComponent<Image>().color = BackingColor;

		TMP_Text question = TasksWindowBuilder.CreateText("QuestionText", confirmRect, textTemplate, 40, Color.white, TextAlignmentOptions.Center);
		TasksWindowBuilder.SetRect(question.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(-80, 200));
		question.textWrappingMode = TextWrappingModes.Normal;
		question.text = "Reset all progress?\nThis can't be undone.";

		Button confirmButton = TasksWindowBuilder.CloneButton(buttonTemplate, confirmRect, "ConfirmResetButton", "Reset");
		TasksWindowBuilder.SetRect((RectTransform)confirmButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1, 1), new Vector2(-20, -10), new Vector2(280, 90));
		confirmButton.GetComponentInChildren<TMP_Text>(true).color = WarningColor;

		Button cancelButton = TasksWindowBuilder.CloneButton(buttonTemplate, confirmRect, "CancelResetButton", "Cancel");
		TasksWindowBuilder.SetRect((RectTransform)cancelButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(20, -10), new Vector2(280, 90));
		confirm.SetActive(false);

		SettingsView view = window.AddComponent<SettingsView>();
		SerializedObject so = new SerializedObject(view);
		so.FindProperty("panel").objectReferenceValue = panel.gameObject;
		so.FindProperty("closeButton").objectReferenceValue = closeButton;
		so.FindProperty("musicButton").objectReferenceValue = musicButton;
		so.FindProperty("sfxButton").objectReferenceValue = sfxButton;
		so.FindProperty("resetButton").objectReferenceValue = resetButton;
		so.FindProperty("confirmPanel").objectReferenceValue = confirm;
		so.FindProperty("confirmResetButton").objectReferenceValue = confirmButton;
		so.FindProperty("cancelResetButton").objectReferenceValue = cancelButton;
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	private static Button Row(Transform template, Transform content, string name, string label)
	{
		Button button = TasksWindowBuilder.CloneButton(template, content, name, label);
		LayoutElement layout = button.GetComponent<LayoutElement>();
		if (layout == null)
		{
			layout = button.gameObject.AddComponent<LayoutElement>();
		}

		layout.preferredHeight = RowHeight;
		layout.ignoreLayout = false;
		return button;
	}

	private static void BuildHudButton(Transform canvas)
	{
		// Same look as the tasks button, above it on the right edge
		Transform navButton = canvas.GetComponentInChildren<NavBarView>(true).GetComponentInChildren<Button>(true).transform;
		Button button = TasksWindowBuilder.CloneButton(navButton, canvas, HudButtonName, "Settings");
		button.transform.SetSiblingIndex(canvas.Find("CurrenciesView").GetSiblingIndex() + 1);
		foreach (LayoutElement element in button.GetComponents<LayoutElement>())
		{
			Object.DestroyImmediate(element);
		}

		TasksWindowBuilder.SetRect((RectTransform)button.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -160), new Vector2(160, 100));

		MenuOpenButton opener = button.gameObject.AddComponent<MenuOpenButton>();
		SerializedObject so = new SerializedObject(opener);
		so.FindProperty("button").objectReferenceValue = button;
		so.FindProperty("type").enumValueIndex = (int)MenuType.Settings;
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	private static void BuildToast(Transform canvas, TMP_Text textTemplate)
	{
		GameObject toast = new GameObject(ToastName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
		toast.layer = canvas.gameObject.layer;
		RectTransform rect = (RectTransform)toast.transform;
		rect.SetParent(canvas, false);

		// Above the windows but under the tutorial overlay
		Transform tutorial = canvas.Find(TutorialName);
		if (tutorial != null)
		{
			rect.SetSiblingIndex(tutorial.GetSiblingIndex());
		}
		else
		{
			rect.SetAsLastSibling();
		}

		TasksWindowBuilder.SetRect(rect, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -330), new Vector2(700, 96));
		Image background = toast.GetComponent<Image>();
		background.color = BackingColor;
		background.raycastTarget = false;

		CanvasGroup group = toast.GetComponent<CanvasGroup>();
		group.alpha = 0f;
		group.blocksRaycasts = false;
		group.interactable = false;

		TMP_Text message = TasksWindowBuilder.CreateText("MessageText", rect, textTemplate, 34, WarningColor, TextAlignmentOptions.Center);
		TasksWindowBuilder.Stretch(message.rectTransform);
		message.rectTransform.offsetMin = new Vector2(24, 0);
		message.rectTransform.offsetMax = new Vector2(-24, 0);
		message.enableAutoSizing = true;
		message.fontSizeMin = 24;
		message.fontSizeMax = 34;
		message.text = "Not enough money";

		ToastView view = toast.AddComponent<ToastView>();
		SerializedObject so = new SerializedObject(view);
		so.FindProperty("group").objectReferenceValue = group;
		so.FindProperty("messageText").objectReferenceValue = message;
		so.ApplyModifiedPropertiesWithoutUndo();
	}
}
