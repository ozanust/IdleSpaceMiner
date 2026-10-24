using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lays out the planet info panel: a header with the planet image and name, a resources table
/// with fixed columns, and themed upgrade rows. Rearranges the existing objects, so all
/// references in PlanetInfoView stay intact. Run after Tools > UI > Apply Sci-Fi Theme.
/// </summary>
public static class PlanetPanelBuilder
{
	private const string ScenePath = "Assets/Scenes/GameScene.unity";
	private const string MiningItemPrefabPath = "Assets/Prefabs/UIPrefabs/MiningInfoItem.prefab";
	private const string RowSpritePath = "Assets/Sci-Fi Game UI 8/MAIN/PNG/Horizontal tab bar/Tab button default.png";
	private const string DividerSpritePath = "Assets/GFX/Sprites/UI/SciFi/ui_progress.png";

	private const float PanelHeight = 1200f;
	private const float Margin = 60f;
	private const float ContentWidth = 960f;
	private const float ResourceRowHeight = 76f;
	private const float UpgradeRowHeight = 120f;

	private static readonly Color MutedColor = new Color(0.55f, 0.85f, 0.92f, 1f);
	private static readonly Color TextColor = new Color(0.88f, 0.97f, 1f, 1f);

	// Resource table columns as (x, width) inside the 960 wide content area
	private static readonly Vector2 NameColumn = new Vector2(90, 290);
	private static readonly Vector2 YieldColumn = new Vector2(400, 160);
	private static readonly Vector2 RateColumn = new Vector2(570, 210);
	private static readonly Vector2 MinedColumn = new Vector2(790, 150);

	[MenuItem("Tools/UI/Build Planet Panel")]
	public static void BuildInOpenScene()
	{
		BuildMiningItemPrefab();
		Build();
		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
	}

	/// <summary>Batch mode entry point: updates the row prefab and GameScene and saves.</summary>
	public static void BuildAndSave()
	{
		BuildMiningItemPrefab();
		EditorSceneManager.OpenScene(ScenePath);
		Build();
		EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
	}

	// --- Resource row prefab ---

	private static void BuildMiningItemPrefab()
	{
		GameObject root = PrefabUtility.LoadPrefabContents(MiningItemPrefabPath);

		// Fixed columns instead of a layout that spaces texts by their width
		Object.DestroyImmediate(root.GetComponent<HorizontalLayoutGroup>());
		Object.DestroyImmediate(root.GetComponent<ContentSizeFitter>());

		Image background = GetOrAdd<Image>(root);
		SetSprite(background, AssetDatabase.LoadAssetAtPath<Sprite>(RowSpritePath));
		background.raycastTarget = false;

		LayoutElement layout = root.GetComponent<LayoutElement>();
		layout.ignoreLayout = false;
		layout.preferredHeight = ResourceRowHeight;
		layout.minHeight = ResourceRowHeight;

		RectTransform icon = (RectTransform)root.transform.Find("ResourceIcon");
		Place(icon, new Vector2(20, 0), new Vector2(56, 56), new Vector2(0, 0.5f));
		icon.GetComponent<Image>().preserveAspect = true;

		Column(root.transform.Find("ResourceName"), NameColumn, TextAlignmentOptions.Left);
		Column(root.transform.Find("ResourceYield"), YieldColumn, TextAlignmentOptions.Center);
		Column(root.transform.Find("ResourceRate"), RateColumn, TextAlignmentOptions.Center);
		Column(root.transform.Find("MinedAmountText"), MinedColumn, TextAlignmentOptions.Center);

		PrefabUtility.SaveAsPrefabAsset(root, MiningItemPrefabPath);
		PrefabUtility.UnloadPrefabContents(root);
	}

	private static void Column(Transform text, Vector2 column, TextAlignmentOptions alignment)
	{
		Object.DestroyImmediate(text.GetComponent<ContentSizeFitter>());
		Place((RectTransform)text, new Vector2(column.x, 0), new Vector2(column.y, ResourceRowHeight), new Vector2(0, 0.5f));
		TMP_Text tmp = text.GetComponent<TMP_Text>();
		tmp.alignment = alignment;
		tmp.fontSize = 32;
		tmp.enableAutoSizing = true;
		tmp.fontSizeMin = 22;
		tmp.fontSizeMax = 32;
		tmp.textWrappingMode = TextWrappingModes.NoWrap;
		tmp.color = TextColor;
	}

	// --- Panel ---

	private static void Build()
	{
		PlanetInfoView view = Object.FindFirstObjectByType<PlanetInfoView>(FindObjectsInactive.Include);
		RectTransform viewRect = (RectTransform)view.transform;
		viewRect.sizeDelta = new Vector2(viewRect.sizeDelta.x, PanelHeight);

		RectTransform panel = (RectTransform)view.transform.Find("Panel");
		Object.DestroyImmediate(panel.GetComponent<VerticalLayoutGroup>());

		BuildHeader(view, panel);
		BuildResources(panel);
		BuildUpgrades(panel);
	}

	private static void BuildHeader(PlanetInfoView view, RectTransform panel)
	{
		RectTransform image = (RectTransform)panel.Find("PlanetImage");
		RectTransform name = (RectTransform)image.Find("PlanetName") ?? (RectTransform)panel.Find("PlanetName");
		name.SetParent(panel, false);

		PlaceTop(image, new Vector2(Margin + 10, -Margin - 10), new Vector2(240, 240));
		Image planetImage = image.GetComponent<Image>();
		planetImage.preserveAspect = true;
		planetImage.raycastTarget = false;

		PlaceTop(name, new Vector2(340, -110), new Vector2(560, 90));
		TMP_Text nameText = name.GetComponent<TMP_Text>();
		nameText.fontSize = 64;
		nameText.fontStyle = FontStyles.Bold;
		nameText.alignment = TextAlignmentOptions.Left;
		nameText.color = TextColor;

		RectTransform divider = GetOrCreate(panel, "NameDivider");
		PlaceTop(divider, new Vector2(340, -205), new Vector2(560, 14));
		SetSprite(divider.GetComponent<Image>(), AssetDatabase.LoadAssetAtPath<Sprite>(DividerSpritePath));

		RectTransform close = (RectTransform)panel.Find("CloseButton");
		close.anchorMin = close.anchorMax = close.pivot = new Vector2(1, 1);
		close.anchoredPosition = new Vector2(-50, -50);
		close.sizeDelta = new Vector2(90, 90);

		SerializedObject so = new SerializedObject(view);
		so.FindProperty("planetImage").objectReferenceValue = planetImage;
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	private static void BuildResources(RectTransform panel)
	{
		RectTransform section = (RectTransform)panel.Find("MiningInfoPanel");
		PlaceSection(section, -340, 70 + 3 * (ResourceRowHeight + 10));

		VerticalLayoutGroup layout = section.GetComponent<VerticalLayoutGroup>();
		layout.padding = new RectOffset(0, 0, 64, 0);
		layout.spacing = 10;
		layout.childControlWidth = true;
		layout.childControlHeight = true;
		layout.childForceExpandWidth = true;
		layout.childForceExpandHeight = false;
		layout.childAlignment = TextAnchor.UpperCenter;

		Header(section.Find("ResourceTitle"), "Resource", NameColumn, TextAlignmentOptions.Left);
		Header(section.Find("YieldTitle"), "Yield", YieldColumn, TextAlignmentOptions.Center);
		Header(section.Find("RateTitle"), "Rate", RateColumn, TextAlignmentOptions.Center);
		Header(section.Find("MinedTitle"), "Mined", MinedColumn, TextAlignmentOptions.Center);

		// The scene's row prototype should follow the updated prefab exactly
		Transform prototype = section.GetComponentInChildren<MiningInfoItem>(true).transform;
		PrefabUtility.RevertPrefabInstance(prototype.gameObject, InteractionMode.AutomatedAction);

		// Rows are copies of this hidden template
		prototype.gameObject.SetActive(false);
		PrefabUtility.RecordPrefabInstancePropertyModifications(prototype.gameObject);
	}

	private static void Header(Transform header, string text, Vector2 column, TextAlignmentOptions alignment)
	{
		Object.DestroyImmediate(header.GetComponent<ContentSizeFitter>());
		RectTransform rect = (RectTransform)header;
		PlaceTop(rect, new Vector2(column.x, -10), new Vector2(column.y, 44));

		TMP_Text tmp = header.GetComponent<TMP_Text>();
		tmp.text = text.ToUpperInvariant();
		tmp.fontSize = 26;
		tmp.characterSpacing = 4;
		tmp.alignment = alignment;
		tmp.color = MutedColor;
	}

	private static void BuildUpgrades(RectTransform panel)
	{
		RectTransform section = (RectTransform)panel.Find("UpdatePanel");
		PlaceSection(section, -700, 56 + 3 * UpgradeRowHeight + 2 * 12);
		section.GetComponent<Image>().color = Color.clear;

		VerticalLayoutGroup layout = section.GetComponent<VerticalLayoutGroup>();
		layout.padding = new RectOffset(0, 0, 56, 0);
		layout.spacing = 12;
		layout.childControlWidth = true;
		layout.childControlHeight = true;
		layout.childForceExpandWidth = true;
		layout.childForceExpandHeight = false;

		RectTransform title = GetOrCreateText(section, "UpgradesTitle");
		title.GetComponent<LayoutElement>().ignoreLayout = true;
		PlaceTop(title, new Vector2(0, -6), new Vector2(400, 44));
		TMP_Text titleText = title.GetComponent<TMP_Text>();
		titleText.text = "UPGRADES";
		titleText.fontSize = 26;
		titleText.characterSpacing = 4;
		titleText.alignment = TextAlignmentOptions.Left;
		titleText.color = MutedColor;

		RectTransform info = (RectTransform)section.Find("InfoButton");
		PlaceTop(info, new Vector2(200, -2), new Vector2(48, 48));
		info.SetAsLastSibling();

		Sprite rowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RowSpritePath);
		foreach (string rowName in new[] { "MiningRateInfo", "ShipSpeedInfo", "ShipCargoInfo" })
		{
			UpgradeRow((RectTransform)section.Find(rowName), rowSprite);
		}
	}

	private static void UpgradeRow(RectTransform row, Sprite sprite)
	{
		Image background = GetOrAdd<Image>(row.gameObject);
		SetSprite(background, sprite);
		background.raycastTarget = false;

		LayoutElement layout = GetOrAdd<LayoutElement>(row.gameObject);
		layout.preferredHeight = UpgradeRowHeight;

		UpgradeText(row.Find("PropertyTitle"), new Vector2(36, -16), new Vector2(500, 50), 38, FontStyles.Bold, TextColor);
		UpgradeText(row.Find("PropertyLevel"), new Vector2(36, -68), new Vector2(140, 40), 28, FontStyles.Normal, MutedColor);
		UpgradeText(row.Find("PropertyValue"), new Vector2(180, -68), new Vector2(360, 40), 28, FontStyles.Normal, TextColor);

		RectTransform button = (RectTransform)row.Find("UpdateButton");
		button.anchorMin = button.anchorMax = new Vector2(1, 0.5f);
		button.pivot = new Vector2(1, 0.5f);
		button.anchoredPosition = new Vector2(-20, 0);
		button.sizeDelta = new Vector2(320, 100);

		TMP_Text price = button.GetComponentInChildren<TMP_Text>(true);
		RectTransform priceRect = price.rectTransform;
		priceRect.anchorMin = Vector2.zero;
		priceRect.anchorMax = Vector2.one;
		priceRect.offsetMin = new Vector2(0, 22);
		priceRect.offsetMax = Vector2.zero;
		price.fontSize = 36;
		price.fontStyle = FontStyles.Bold;
		price.color = TextColor;
	}

	private static void UpgradeText(Transform text, Vector2 position, Vector2 size, float fontSize, FontStyles style, Color color)
	{
		Object.DestroyImmediate(text.GetComponent<ContentSizeFitter>());
		PlaceTop((RectTransform)text, position, size);
		TMP_Text tmp = text.GetComponent<TMP_Text>();
		tmp.fontSize = fontSize;
		tmp.fontStyle = style;
		tmp.color = color;
		tmp.alignment = TextAlignmentOptions.Left;
		tmp.textWrappingMode = TextWrappingModes.NoWrap;
	}

	// --- Helpers ---

	/// <summary>Full content width section starting at y (from the panel top).</summary>
	private static void PlaceSection(RectTransform section, float y, float height)
	{
		section.anchorMin = new Vector2(0.5f, 1);
		section.anchorMax = new Vector2(0.5f, 1);
		section.pivot = new Vector2(0.5f, 1);
		section.anchoredPosition = new Vector2(0, y);
		section.sizeDelta = new Vector2(ContentWidth, height);
	}

	private static void PlaceTop(RectTransform rect, Vector2 position, Vector2 size)
	{
		rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
		rect.anchoredPosition = position;
		rect.sizeDelta = size;
	}

	private static void Place(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchorPivot)
	{
		rect.anchorMin = rect.anchorMax = rect.pivot = anchorPivot;
		rect.anchoredPosition = position;
		rect.sizeDelta = size;
	}

	private static RectTransform GetOrCreate(Transform parent, string name)
	{
		Transform existing = parent.Find(name);
		if (existing != null)
		{
			return (RectTransform)existing;
		}

		GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
		go.layer = parent.gameObject.layer;
		go.GetComponent<LayoutElement>().ignoreLayout = true;
		go.GetComponent<Image>().raycastTarget = false;
		go.transform.SetParent(parent, false);
		return (RectTransform)go.transform;
	}

	private static RectTransform GetOrCreateText(Transform parent, string name)
	{
		Transform existing = parent.Find(name);
		if (existing != null)
		{
			return (RectTransform)existing;
		}

		GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
		go.layer = parent.gameObject.layer;
		go.GetComponent<TextMeshProUGUI>().raycastTarget = false;
		go.transform.SetParent(parent, false);
		return (RectTransform)go.transform;
	}

	// GetComponent returns Unity's "fake null" in the editor, so ?? can't be used here
	private static T GetOrAdd<T>(GameObject go) where T : Component
	{
		T component = go.GetComponent<T>();
		return component != null ? component : go.AddComponent<T>();
	}

	private static void SetSprite(Image image, Sprite sprite)
	{
		image.sprite = sprite;
		image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
		image.color = Color.white;
	}
}
