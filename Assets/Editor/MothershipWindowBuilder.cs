using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the mothership window in GameScene: a header to sell the galaxy above a list of room
/// rows, plus the sale confirmation. Shares the tasks window helpers and templates.
/// Running it again replaces what it built before.
/// </summary>
public static class MothershipWindowBuilder
{
	private const string ScenePath = "Assets/Scenes/GameScene.unity";
	private const string WindowName = "MothershipRoomsView";

	private const float HeaderHeight = 300f;
	private const float RowHeight = 190f;
	private const float ButtonWidth = 300f;

	private static readonly Color MutedColor = new Color(0.55f, 0.85f, 0.92f, 1f);
	private static readonly Color BonusColor = new Color(0.75f, 0.65f, 1f, 1f);
	private static readonly Color BackingColor = new Color(0.02f, 0.06f, 0.1f, 0.96f);

	[MenuItem("Tools/UI/Build Mothership Window")]
	public static void BuildInOpenScene()
	{
		Build();
		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
	}

	/// <summary>Batch mode entry point: opens GameScene, builds the window and saves the scene.</summary>
	public static void BuildAndSaveScene()
	{
		EditorSceneManager.OpenScene(ScenePath);
		Build();
		EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
	}

	private static void Build()
	{
		Transform canvas = Object.FindAnyObjectByType<NavBarView>(FindObjectsInactive.Include).transform.parent;
		Transform old = canvas.Find(WindowName);
		if (old != null)
		{
			Object.DestroyImmediate(old.gameObject);
		}

		TMP_Text textTemplate = canvas.Find(TasksWindowBuilder.TextTemplatePath).GetComponent<TMP_Text>();
		Transform buttonTemplate = canvas.Find(TasksWindowBuilder.ClaimButtonTemplatePath);
		float pad = TasksWindowBuilder.RowPadding;

		Transform panel;
		Button closeButton;
		ScrollRect scroll;
		GameObject window = TasksWindowBuilder.CreateListWindow(canvas, WindowName, "Mothership", out panel, out closeButton, out scroll);

		// The header takes the top of the scroll area
		RectTransform scrollRect = (RectTransform)scroll.transform;
		GameObject headerObject = new GameObject("Header", typeof(RectTransform));
		headerObject.layer = canvas.gameObject.layer;
		RectTransform header = (RectTransform)headerObject.transform;
		header.SetParent(scrollRect.parent, false);
		header.anchorMin = new Vector2(scrollRect.anchorMin.x, scrollRect.anchorMax.y);
		header.anchorMax = scrollRect.anchorMax;
		header.pivot = new Vector2(0.5f, 1);
		header.offsetMin = new Vector2(scrollRect.offsetMin.x, scrollRect.offsetMax.y - HeaderHeight);
		header.offsetMax = scrollRect.offsetMax;
		scrollRect.offsetMax = new Vector2(scrollRect.offsetMax.x, scrollRect.offsetMax.y - HeaderHeight);

		TMP_Text galaxyValueText = TasksWindowBuilder.CreateText("GalaxyValueText", header, textTemplate, 40, Color.white, TextAlignmentOptions.Left);
		TasksWindowBuilder.SetRect(galaxyValueText.rectTransform, new Vector2(0, 1), new Vector2(0.65f, 1), new Vector2(0, 1), new Vector2(pad, -24), new Vector2(-pad, 56));
		galaxyValueText.text = "Galaxy value: $52.4K";

		TMP_Text galaxiesText = TasksWindowBuilder.CreateText("GalaxiesText", header, textTemplate, 30, MutedColor, TextAlignmentOptions.Right);
		TasksWindowBuilder.SetRect(galaxiesText.rectTransform, new Vector2(0.65f, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-pad, -30), new Vector2(-pad, 50));
		galaxiesText.text = "Galaxies sold: 0";

		Button sellButton = TasksWindowBuilder.CloneButton(buttonTemplate, header, "SellGalaxyButton", "Sell galaxy  +11 DM");
		TasksWindowBuilder.SetRect((RectTransform)sellButton.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(-pad * 2, 100));
		AutoSize(sellButton);

		TMP_Text infoText = TasksWindowBuilder.CreateText("InfoText", header, textTemplate, 28, MutedColor, TextAlignmentOptions.Center);
		TasksWindowBuilder.SetRect(infoText.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(-pad * 2, 80));
		infoText.textWrappingMode = TextWrappingModes.Normal;
		infoText.text = "Sell the galaxy for Dark Matter, then build rooms that boost every galaxy after.";

		RoomItem prototype = BuildRow(canvas, scroll.content, textTemplate, buttonTemplate);

		// Confirmation covers the whole window until the player decides
		GameObject confirm = new GameObject("ConfirmPanel", typeof(RectTransform), typeof(Image));
		confirm.layer = canvas.gameObject.layer;
		RectTransform confirmRect = (RectTransform)confirm.transform;
		confirmRect.SetParent(scrollRect.parent, false);
		TasksWindowBuilder.Stretch(confirmRect);
		confirm.GetComponent<Image>().color = BackingColor;

		TMP_Text confirmText = TasksWindowBuilder.CreateText("QuestionText", confirmRect, textTemplate, 36, Color.white, TextAlignmentOptions.Center);
		TasksWindowBuilder.SetRect(confirmText.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(-100, 320));
		confirmText.textWrappingMode = TextWrappingModes.Normal;
		confirmText.text = "Sell this galaxy for 11 Dark Matter?\nPlanets, money, resources and research start over. Dark Matter, rooms, managers and achievements stay.";

		Button confirmButton = TasksWindowBuilder.CloneButton(buttonTemplate, confirmRect, "ConfirmSellButton", "Sell");
		TasksWindowBuilder.SetRect((RectTransform)confirmButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1, 1), new Vector2(-20, -10), new Vector2(280, 90));

		Button cancelButton = TasksWindowBuilder.CloneButton(buttonTemplate, confirmRect, "CancelSellButton", "Cancel");
		TasksWindowBuilder.SetRect((RectTransform)cancelButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(20, -10), new Vector2(280, 90));
		confirm.SetActive(false);

		MothershipRoomsView view = window.AddComponent<MothershipRoomsView>();
		SerializedObject so = new SerializedObject(view);
		so.FindProperty("panel").objectReferenceValue = panel.gameObject;
		so.FindProperty("closeButton").objectReferenceValue = closeButton;
		so.FindProperty("itemContainer").objectReferenceValue = scroll.content;
		so.FindProperty("itemPrototype").objectReferenceValue = prototype;
		so.FindProperty("galaxyValueText").objectReferenceValue = galaxyValueText;
		so.FindProperty("galaxiesText").objectReferenceValue = galaxiesText;
		so.FindProperty("sellButton").objectReferenceValue = sellButton;
		so.FindProperty("confirmPanel").objectReferenceValue = confirm;
		so.FindProperty("confirmText").objectReferenceValue = confirmText;
		so.FindProperty("confirmSellButton").objectReferenceValue = confirmButton;
		so.FindProperty("cancelSellButton").objectReferenceValue = cancelButton;
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	private static RoomItem BuildRow(Transform canvas, Transform content, TMP_Text textTemplate, Transform buttonTemplate)
	{
		Image backgroundTemplate = canvas.Find(TasksWindowBuilder.RowBackgroundTemplatePath).GetComponent<Image>();
		float pad = TasksWindowBuilder.RowPadding;

		GameObject row = new GameObject("RoomItem", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
		row.layer = canvas.gameObject.layer;
		RectTransform rowRect = (RectTransform)row.transform;
		rowRect.SetParent(content, false);

		Image background = row.GetComponent<Image>();
		background.sprite = backgroundTemplate.sprite;
		background.type = backgroundTemplate.type;
		background.color = backgroundTemplate.color;
		row.GetComponent<LayoutElement>().preferredHeight = RowHeight;

		float textWidth = -(pad * 3 + ButtonWidth);

		TMP_Text nameText = TasksWindowBuilder.CreateText("NameText", rowRect, textTemplate, 40, Color.white, TextAlignmentOptions.Left);
		TasksWindowBuilder.SetRect(nameText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(pad, -20), new Vector2(textWidth, 52));
		nameText.fontStyle = FontStyles.Bold;
		nameText.text = "Engineering";

		TMP_Text levelText = TasksWindowBuilder.CreateText("LevelText", rowRect, textTemplate, 30, MutedColor, TextAlignmentOptions.Left);
		TasksWindowBuilder.SetRect(levelText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(pad, -76), new Vector2(textWidth, 42));
		levelText.text = "Lv. 0 / 10";

		TMP_Text effectText = TasksWindowBuilder.CreateText("EffectText", rowRect, textTemplate, 30, BonusColor, TextAlignmentOptions.Left);
		TasksWindowBuilder.SetRect(effectText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(pad, -122), new Vector2(textWidth, 42));
		effectText.text = "Mining rate +0%  >  +25%";

		Button upgradeButton = TasksWindowBuilder.CloneButton(buttonTemplate, rowRect, "UpgradeButton", "Upgrade  5 DM");
		TasksWindowBuilder.SetRect((RectTransform)upgradeButton.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-pad, 0), new Vector2(ButtonWidth, 88));
		AutoSize(upgradeButton);

		RoomItem item = row.AddComponent<RoomItem>();
		SerializedObject so = new SerializedObject(item);
		so.FindProperty("nameText").objectReferenceValue = nameText;
		so.FindProperty("levelText").objectReferenceValue = levelText;
		so.FindProperty("effectText").objectReferenceValue = effectText;
		so.FindProperty("upgradeButton").objectReferenceValue = upgradeButton;
		so.ApplyModifiedPropertiesWithoutUndo();

		return item;
	}

	private static void AutoSize(Button button)
	{
		TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
		label.enableAutoSizing = true;
		label.fontSizeMin = 20;
		label.fontSizeMax = 36;
		label.alignment = TextAlignmentOptions.Center;
	}
}
