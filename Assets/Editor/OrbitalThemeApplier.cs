using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Restyles the game UI with the "Orbital Mobile Idle Kit": gunmetal frames with cyan and amber lights,
/// buttons with the kit's normal, highlighted and pressed states, icon tabs for the bottom bar and HUD,
/// module icons in the planet upgrade rows, and kit icons for currencies, locks and ranks.
/// Finds UI by the sprites earlier themes gave it, so it runs again safely after other UI tools.
/// Planets, ships, ores and items keep their own art.
/// </summary>
public static class OrbitalThemeApplier
{
	private const string InterfaceFolder = "Assets/OrbitalMobile/Interface/";
	private const string SpritesFolder = "Assets/OrbitalMobile/Sprites/";
	private const string HudSheet = "Assets/OrbitalUI/OrbitalUI.png";
	private const string ScenePath = "Assets/Scenes/GameScene.unity";
	private const string PrefabFolder = "Assets/Prefabs/UIPrefabs";
	private const string GameSettingsPath = "Assets/Resources/Settings/GameSettings.asset";

	private static readonly Color TextColor = new Color(0.9f, 0.97f, 1f, 1f);
	private static readonly Color LabelColor = new Color(0.78f, 0.93f, 1f, 1f);
	private static readonly Color ContainerColor = new Color(0f, 0.02f, 0.06f, 0.35f);

	private static Dictionary<string, Sprite> sprites;

	[MenuItem("Tools/UI/Apply Orbital Theme")]
	public static void ApplyInOpenScene()
	{
		LoadSprites();
		AssignFx();
		ApplyToPrefabs();
		ApplyToCanvas();
		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
	}

	/// <summary>Batch mode entry point: restyles the UI prefabs and GameScene and saves both.</summary>
	public static void ApplyAndSave()
	{
		LoadSprites();
		AssignFx();
		ApplyToPrefabs();
		EditorSceneManager.OpenScene(ScenePath);
		ApplyToCanvas();
		EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
		AssetDatabase.SaveAssets();
	}

	// --- Sprites ---

	private static void LoadSprites()
	{
		sprites = new Dictionary<string, Sprite>();
		foreach (string sheet in new[]
		{
			InterfaceFolder + "Panels.png", InterfaceFolder + "Controls.png", InterfaceFolder + "Navigation.png",
			SpritesFolder + "Economy.png", SpritesFolder + "Modules.png", SpritesFolder + "FX.png", HudSheet
		})
		{
			foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(sheet))
			{
				if (asset is Sprite sprite)
				{
					sprites[sprite.name] = sprite;
				}
			}
		}
	}

	private static Sprite S(string name)
	{
		Sprite sprite;
		if (!sprites.TryGetValue(name, out sprite))
		{
			throw new System.Exception("Orbital kit sprite missing: " + name);
		}

		return sprite;
	}

	/// <summary>Effect frames and asteroid rocks the game plays in the world, kept in GameSettings.</summary>
	private static void AssignFx()
	{
		GameSettings settings = AssetDatabase.LoadAssetAtPath<GameSettings>(GameSettingsPath);
		SerializedObject so = new SerializedObject(settings);
		SetSprites(so.FindProperty("fxSettings.ImpactFrames"), "FX_Impact_01", "FX_Impact_02", "FX_Impact_03", "FX_Impact_04");
		SetSprites(so.FindProperty("fxSettings.ScanFrames"), "FX_Scan_01", "FX_Scan_02", "FX_Scan_03", "FX_Scan_04");
		so.FindProperty("fxSettings.LootGlint").objectReferenceValue = S("FX_LootGlint");

		List<Sprite> rocks = new List<Sprite>();
		foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(SpritesFolder + "Asteroids.png"))
		{
			if (asset is Sprite rock)
			{
				rocks.Add(rock);
			}
		}

		SerializedProperty asteroids = so.FindProperty("fxSettings.AsteroidSprites");
		asteroids.arraySize = rocks.Count;
		for (int i = 0; i < rocks.Count; i++)
		{
			asteroids.GetArrayElementAtIndex(i).objectReferenceValue = rocks[i];
		}

		so.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(settings);
	}

	private static void SetSprites(SerializedProperty array, params string[] names)
	{
		array.arraySize = names.Length;
		for (int i = 0; i < names.Length; i++)
		{
			array.GetArrayElementAtIndex(i).objectReferenceValue = S(names[i]);
		}
	}

	// --- Applying ---

	private static void ApplyToPrefabs()
	{
		foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			GameObject root = PrefabUtility.LoadPrefabContents(path);
			Restyle(root.transform);
			FitTexts(root.transform);
			PrefabUtility.SaveAsPrefabAsset(root, path);
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	private static void ApplyToCanvas()
	{
		NavBarView navBar = Object.FindAnyObjectByType<NavBarView>(FindObjectsInactive.Include);
		Transform canvas = navBar.transform.parent;
		Restyle(canvas);

		StyleNavBar(navBar);
		StyleHudButton(canvas.Find("SettingsButton"), "Nav_Settings", -150);
		StyleHudButton(canvas.Find("TasksButton"), "Nav_Missions", -325);
		StyleHudButton(canvas.Find("AchievementsButton"), "Nav_Milestone", -500);
		StyleArkButton(canvas.Find("BoostHud/ArkButton"));
		StyleCurrencies(canvas.Find("CurrenciesView"));
		StyleToast(canvas.Find("ToastView"));
		StyleTutorial(canvas.Find("TutorialView/Root"));
		StylePlanetUpgradeRows(canvas.Find("PlanetInfoView/Panel/UpdatePanel"));
		StyleWindowTitles(canvas);
		StyleResearchFrames(canvas);
		FitTexts(canvas);

		// Below the Ark button and its label
		Transform boostTimer = canvas.Find("BoostHud/BoostTimerText");
		if (boostTimer != null)
		{
			RectTransform timerRect = (RectTransform)boostTimer;
			timerRect.anchoredPosition = new Vector2(timerRect.anchoredPosition.x, -545f);
			Record(timerRect);
		}
	}

	/// <summary>The title chip has a chevron on its left, so titles start after it.</summary>
	private static void StyleWindowTitles(Transform canvas)
	{
		foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true))
		{
			if (t.name != "TitleBG")
			{
				continue;
			}

			Transform title = t.Find("ViewTitle");
			if (title != null)
			{
				TMP_Text text = title.GetComponent<TMP_Text>();
				// Right margin keeps long titles clear of the close button
				text.margin = new Vector4(34f, 0f, 44f, 0f);
				text.enableAutoSizing = true;
				text.fontSizeMin = 24f;
				text.fontSizeMax = 36f;
				text.textWrappingMode = TextWrappingModes.NoWrap;
				text.fontStyle |= FontStyles.Bold;
				text.color = TextColor;
				Record(text);
			}
		}
	}

	/// <summary>Research icons get the slot frame around them with the icon showing through the middle.</summary>
	private static void StyleResearchFrames(Transform canvas)
	{
		foreach (ResearchItem item in canvas.GetComponentsInChildren<ResearchItem>(true))
		{
			Transform border = item.transform.Find("Border");
			if (border == null)
			{
				continue;
			}

			Image image = border.GetComponent<Image>();
			Set(image, S("Slot_Empty"));
			image.fillCenter = false;
			SetPixelsPerUnit(image, 2.2f);
			RectTransform rect = (RectTransform)border;
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = new Vector2(-16f, -16f);
			rect.offsetMax = new Vector2(16f, 16f);
			Record(rect);
		}

		// The hub the research tree grows from was still a plain grey circle
		foreach (Image image in canvas.GetComponentsInChildren<Image>(true))
		{
			if (image.name != "CenterCircle")
			{
				continue;
			}

			Set(image, S("Nav_Research"), true);
			image.rectTransform.sizeDelta = new Vector2(100f, 100f);
			Record(image.rectTransform);
		}
	}

	private static void Restyle(Transform root)
	{
		foreach (Image image in root.GetComponentsInChildren<Image>(true))
		{
			RestyleImage(image);
		}

		foreach (Button button in root.GetComponentsInChildren<Button>(true))
		{
			StyleButtonLabel(button);
		}
	}

	// Old sprite names from the Sci-Fi pack, plus the kit's names so running again finds themed UI
	private static readonly HashSet<string> WindowSprites = new HashSet<string> { "Panel", "Futuro_WindowBack", "Panel_ResearchDialog" };
	private static readonly HashSet<string> ActionSprites = new HashSet<string> { "Apply_Cancel button", "UISprite", "Futuro_Bar", "Tab button active", "Button_Primary_Normal", "Button_Secondary_Normal" };
	private static readonly HashSet<string> BuySprites = new HashSet<string> { "Buy button", "Button_Claim_Normal" };
	private static readonly HashSet<string> RowSprites = new HashSet<string> { "Tab button default", "Shopping item slott", "Panel_FleetRow", "Panel_UpgradeRow", "Panel_StatusChip" };
	private static readonly HashSet<string> RowComponents = new HashSet<string> { "TaskItem", "RoomItem", "ManagerItem" };
	private static readonly HashSet<string> BoostRowNames = new HashSet<string> { "MineBoost", "Ark" };

	private static void RestyleImage(Image image)
	{
		string sprite = image.sprite != null ? image.sprite.name : "";
		string name = image.gameObject.name;
		Button button = image.GetComponent<Button>();
		Transform parent = image.transform.parent;

		// Parts styled separately or kept as they are
		if (image.GetComponentInParent<NavBarView>(true) != null && image.GetComponent<NavBarView>() == null
			|| image.GetComponentInParent<TasksButton>(true) != null || image.GetComponentInParent<MenuOpenButton>(true) != null
			|| image.GetComponentInParent<TutorialView>(true) != null || image.GetComponent<ToastView>() != null
			|| name == "ModuleIcon" || name == "TabIcon" || name == "Icon" && parent != null && parent.name == "ArkButton")
		{
			return;
		}

		Slider slider = image.GetComponentInParent<Slider>(true);
		bool inSlider = slider != null && slider.gameObject != image.gameObject;

		if (name == "Backdrop")
		{
			// Kit panels are solid, the dark backdrop behind the old see-through frames isn't needed
			image.enabled = false;
			Record(image);
		}
		else if (image.GetComponent<Scrollbar>() != null)
		{
			SetBuiltin(image, new Color(0f, 0.04f, 0.1f, 0.25f));
		}
		else if (image.GetComponentInParent<Scrollbar>(true) != null && name == "Handle")
		{
			SetBuiltin(image, new Color(0.3f, 0.85f, 0.95f, 0.35f));
		}
		else if (name == "CloseButton" || name == "RemoveRecipeButton")
		{
			Set(image, S("Button_Close"), preserveAspect: true);
			HideChildImages(image);
		}
		else if (name == "InfoButton")
		{
			Set(image, S("Nav_Missions"), preserveAspect: true);
		}
		else if (inSlider && button == null)
		{
			if (name == "Background") Set(image, S("Meter_Track"));
			else if (name == "Fill") Set(image, S("Fill_Energy"));
			else if (name == "Handle") Set(image, S("Slot_Selected"), preserveAspect: true);
		}
		else if (name == "NameDivider" || sprite == "ui_progress" || sprite == "Fill_Energy")
		{
			Set(image, S("Fill_Energy"));
		}
		else if (IsItemCard(image))
		{
			Set(image, S("Panel_Cargo"));
		}
		else if (name == "TitleBG")
		{
			Set(image, S("Panel_StatusChip"));
		}
		else if (name == "ClaimableBadge" || sprite == "FX_LootGlint")
		{
			Set(image, S("FX_LootGlint"), preserveAspect: true);
			ScaleRect(image, 56);
		}
		else if (sprite == "border" || sprite == "Slot_Empty")
		{
			Set(image, S("Slot_Empty"));
		}
		else if (sprite == "option buton active" || sprite == "Nav_Milestone")
		{
			Set(image, S("Nav_Milestone"), preserveAspect: true);
		}
		else if (sprite == "lock" || sprite == "Nav_Locked")
		{
			Set(image, S("Nav_Locked"), preserveAspect: true);
		}
		else if (sprite == "dollar" || sprite == "Item_Credits") Set(image, S("Item_Credits"), preserveAspect: true);
		else if (sprite == "dark-matter" || sprite == "Item_WarpCrystal") Set(image, S("Item_WarpCrystal"), preserveAspect: true);
		else if (sprite == "dice-d10-512" || sprite == "Item_Prism") Set(image, S("Item_Prism"), preserveAspect: true);
		else if (sprite == "galaxy" || sprite == "Nav_Sectors") Set(image, S("Nav_Sectors"), preserveAspect: true);
		else if (sprite == "Treasure_ChestICON" || sprite == "Item_RewardCache") Set(image, S("Item_RewardCache"), preserveAspect: true);
		else if (WindowSprites.Contains(sprite) && button == null)
		{
			Set(image, S("Panel_ResearchDialog"));
		}
		else if (BuySprites.Contains(sprite))
		{
			if (button != null) SetButton(button, image, "Claim");
			else Set(image, S("Button_Claim_Normal"));
		}
		else if (button != null && ActionSprites.Contains(sprite))
		{
			bool isViewTab = parent != null && parent.name == "ViewButtons";
			SetButton(button, image, isViewTab ? "Secondary" : "Primary");
			if (isViewTab)
			{
				StyleTab(button);
			}
		}
		else if (IsRow(image) || RowSprites.Contains(sprite))
		{
			if (name == "AmountBG") Set(image, S("Panel_StatusChip"));
			else if (name == "ResourceSellView") Set(image, S("Panel_Cargo"));
			else if (!IsPlanetUpgradeRow(image)) Set(image, S("Panel_FleetRow"));
		}
		else if (sprite == "Background" && image.GetComponent<NavBarView>() == null && image.GetComponent<Mask>() == null)
		{
			// Content areas inside windows: a light shade so rows stand out on the dark panel
			image.color = ContainerColor;
			Record(image);
		}
	}

	private static bool IsRow(Image image)
	{
		foreach (Component component in image.GetComponents<Component>())
		{
			if (component != null && RowComponents.Contains(component.GetType().Name))
			{
				return true;
			}
		}

		return BoostRowNames.Contains(image.gameObject.name) || image.gameObject.name == "ManagerRow"
			|| image.GetComponent<ResourceInfoItem>() != null || image.GetComponent<MiningInfoItem>() != null;
	}

	private static bool IsPlanetUpgradeRow(Image image)
	{
		string name = image.gameObject.name;
		return name == "MiningRateInfo" || name == "ShipSpeedInfo" || name == "ShipCargoInfo";
	}

	private static bool IsItemCard(Image image)
	{
		return image.GetComponent<SmelterItem>() != null
			|| image.GetComponent<CrafterItem>() != null
			|| image.GetComponent<SmeltRecipeItem>() != null;
	}

	// --- Buttons ---

	/// <summary>A kit button with its highlighted and pressed states swapped in by the Button.</summary>
	private static void SetButton(Button button, Image image, string family)
	{
		Set(image, S("Button_" + family + "_Normal"));
		button.transition = Selectable.Transition.SpriteSwap;
		SpriteState state = new SpriteState
		{
			highlightedSprite = S("Button_" + family + "_Highlighted"),
			pressedSprite = S("Button_" + family + "_Pressed"),
			selectedSprite = null,
			disabledSprite = S("Button_Disabled")
		};
		button.spriteState = state;
		Record(button);
	}

	/// <summary>Sub-category tabs show the bright button art while selected.</summary>
	private static void StyleTab(Button button)
	{
		TabButton tab = button.GetComponent<TabButton>();
		if (tab == null)
		{
			tab = button.gameObject.AddComponent<TabButton>();
		}

		tab.SetSprites(S("Button_Secondary_Normal"), FamilyState("Secondary"), S("Button_Primary_Normal"), FamilyState("Primary"));
		Record(tab);
	}

	private static SpriteState FamilyState(string family)
	{
		return new SpriteState
		{
			highlightedSprite = S("Button_" + family + "_Highlighted"),
			pressedSprite = S("Button_" + family + "_Pressed"),
			disabledSprite = S("Button_Disabled")
		};
	}

	private static void StyleButtonLabel(Button button)
	{
		Image image = button.GetComponent<Image>();
		if (image == null || image.sprite == null || !image.sprite.name.StartsWith("Button_") || image.sprite.name == "Button_Close")
		{
			return;
		}

		TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
		if (label != null)
		{
			label.color = Color.white;
			label.fontStyle |= FontStyles.Bold;
			// Keep labels off the button's bevelled ends; long ones shrink instead of touching them
			label.margin = new Vector4(18f, 0f, 18f, 0f);
			if (!label.enableAutoSizing)
			{
				label.fontSizeMax = label.fontSize;
				label.fontSizeMin = Mathf.Round(label.fontSize * 0.65f);
				label.enableAutoSizing = true;
			}

			Record(label);
		}
	}

	// --- Bottom bar ---

	private static readonly Dictionary<string, string> NavIcons = new Dictionary<string, string>
	{
		{ "resourcesButton", "Nav_Inventory" },
		{ "productionButton", "Nav_Upgrades" },
		{ "projectsButton", "Nav_Research" },
		{ "managersButton", "Nav_Fleet" },
		{ "boostsButton", "Nav_Boost" },
		{ "mothershipButton", "Nav_Prestige" }
	};

	/// <summary>The kit's tab bar behind the bottom buttons, each button an icon with its label underneath.</summary>
	private static void StyleNavBar(NavBarView navBar)
	{
		Image bar = navBar.GetComponent<Image>();
		if (bar == null)
		{
			bar = navBar.gameObject.AddComponent<Image>();
		}

		Set(bar, S("Panel_TabBar"));
		SetPixelsPerUnit(bar, 1.2f);

		// Taller than before so each tab has room for its icon above the label
		RectTransform barRect = (RectTransform)navBar.transform;
		barRect.sizeDelta = new Vector2(barRect.sizeDelta.x, 140f);
		Record(barRect);
		HorizontalOrVerticalLayoutGroup layout = navBar.GetComponent<HorizontalOrVerticalLayoutGroup>();
		if (layout != null)
		{
			layout.padding = new RectOffset(84, 84, 18, 12);
			layout.childControlHeight = true;
			layout.childForceExpandHeight = true;
			// Tabs share the width inside the padding instead of keeping a fixed width
			layout.childControlWidth = true;
			layout.childForceExpandWidth = true;
			Record(layout);
		}

		SerializedObject so = new SerializedObject(navBar);
		foreach (KeyValuePair<string, string> entry in NavIcons)
		{
			Button button = so.FindProperty(entry.Key).objectReferenceValue as Button;
			if (button == null)
			{
				continue;
			}

			Image image = button.GetComponent<Image>();
			Set(image, S("Slot_Empty"));
			image.color = new Color(1f, 1f, 1f, 0f);
			button.transition = Selectable.Transition.ColorTint;
			ColorBlock colors = button.colors;
			colors.normalColor = Color.white;
			colors.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
			colors.pressedColor = new Color(0.7f, 0.9f, 1f, 1f);
			colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
			button.colors = colors;
			// The tab graphic is the icon, so locked tabs dim the icon
			Image icon = EnsureChildImage(button.transform, "TabIcon", S(entry.Value));
			button.targetGraphic = icon;
			RectTransform iconRect = icon.rectTransform;
			iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1f);
			iconRect.pivot = new Vector2(0.5f, 1f);
			iconRect.anchoredPosition = new Vector2(0f, -2f);
			iconRect.sizeDelta = new Vector2(52f, 52f);
			Record(iconRect);
			Record(button);

			TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
			if (label != null)
			{
				RectTransform labelRect = label.rectTransform;
				labelRect.anchorMin = new Vector2(0f, 0f);
				labelRect.anchorMax = new Vector2(1f, 0f);
				labelRect.pivot = new Vector2(0.5f, 0f);
				// Clear of the bar's bottom rim
				labelRect.anchoredPosition = new Vector2(0f, 22f);
				labelRect.sizeDelta = new Vector2(0f, 30f);
				label.fontSize = 24;
				label.enableAutoSizing = true;
				label.fontSizeMin = 18;
				label.fontSizeMax = 24;
				label.textWrappingMode = TextWrappingModes.NoWrap;
				label.color = LabelColor;
				label.alignment = TextAlignmentOptions.Center;
				Record(labelRect);
				Record(label);
			}

			LinkNavTab(button, navBar.transform.parent, label);
		}
	}

	private static readonly Dictionary<string, string> NavWindows = new Dictionary<string, string>
	{
		{ "resourcesButton", "ResourcesView/Panel" },
		{ "productionButton", "ProductionView/Panel" },
		{ "projectsButton", "ResearchView/Panel" },
		{ "managersButton", "ManagersView/Panel" },
		{ "boostsButton", "BoostsView/Panel" },
		{ "mothershipButton", "MothershipRoomsView/Panel" }
	};

	/// <summary>The tab of the open window gets a glowing bar under its label.</summary>
	private static void LinkNavTab(Button button, Transform canvas, TMP_Text label)
	{
		string field = null;
		NavBarView navBar = button.GetComponentInParent<NavBarView>(true);
		SerializedObject so = new SerializedObject(navBar);
		foreach (string key in NavWindows.Keys)
		{
			if (so.FindProperty(key).objectReferenceValue == button)
			{
				field = key;
			}
		}

		Transform window = field != null ? canvas.Find(NavWindows[field]) : null;
		if (window == null)
		{
			return;
		}

		Image mark = EnsureChildImage(button.transform, "SelectedMark", S("Fill_Energy"));
		mark.preserveAspect = false;
		mark.type = Image.Type.Sliced;
		RectTransform markRect = mark.rectTransform;
		markRect.anchorMin = markRect.anchorMax = new Vector2(0.5f, 0f);
		markRect.pivot = new Vector2(0.5f, 0f);
		markRect.anchoredPosition = new Vector2(0f, 8f);
		markRect.sizeDelta = new Vector2(96f, 10f);
		LayoutElement markLayout = mark.GetComponent<LayoutElement>();
		if (markLayout == null)
		{
			markLayout = mark.gameObject.AddComponent<LayoutElement>();
		}

		markLayout.ignoreLayout = true;
		mark.gameObject.SetActive(false);
		Record(markRect);
		Record(markLayout);
		Record(mark.gameObject);

		NavTab tab = button.GetComponent<NavTab>();
		if (tab == null)
		{
			tab = button.gameObject.AddComponent<NavTab>();
		}

		tab.Link(window.gameObject, mark.gameObject, label, LabelColor);
		Record(tab);
	}

	// --- HUD ---

	/// <summary>Right edge HUD button: a square kit slot with an icon and the label underneath.</summary>
	private static void StyleHudButton(Transform buttonTransform, string iconName, float y)
	{
		if (buttonTransform == null)
		{
			return;
		}

		Image image = buttonTransform.GetComponent<Image>();
		Set(image, S("Slot_Empty"));
		Button button = buttonTransform.GetComponent<Button>();
		button.transition = Selectable.Transition.ColorTint;
		button.targetGraphic = image;
		Record(button);

		RectTransform rect = (RectTransform)buttonTransform;
		rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
		rect.anchoredPosition = new Vector2(-24f, y);
		rect.sizeDelta = new Vector2(124f, 116f);
		Record(rect);

		Image icon = EnsureChildImage(buttonTransform, "TabIcon", S(iconName));
		Stretch(icon.rectTransform, 22f);

		TMP_Text label = buttonTransform.GetComponentInChildren<TMP_Text>(true);
		if (label != null)
		{
			RectTransform labelRect = label.rectTransform;
			labelRect.anchorMin = new Vector2(0.5f, 0f);
			labelRect.anchorMax = new Vector2(0.5f, 0f);
			labelRect.pivot = new Vector2(0.5f, 1f);
			labelRect.anchoredPosition = new Vector2(0f, -2f);
			labelRect.sizeDelta = new Vector2(170f, 36f);
			label.fontSize = 26;
			label.enableAutoSizing = false;
			label.color = TextColor;
			label.fontStyle |= FontStyles.Bold;
			label.alignment = TextAlignmentOptions.Center;
			Record(labelRect);
			Record(label);
		}

		// Claim badge on the corner
		Transform badge = buttonTransform.Find("ClaimableBadge");
		if (badge != null)
		{
			Image badgeImage = badge.GetComponent<Image>();
			Set(badgeImage, S("FX_LootGlint"), preserveAspect: true);
			RectTransform badgeRect = (RectTransform)badge;
			badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(1f, 1f);
			badgeRect.pivot = new Vector2(0.5f, 0.5f);
			badgeRect.anchoredPosition = new Vector2(-10f, -10f);
			badgeRect.sizeDelta = new Vector2(70f, 70f);
			badge.SetAsLastSibling();
			Record(badgeRect);
		}
	}

	private static void StyleArkButton(Transform ark)
	{
		if (ark == null)
		{
			return;
		}

		Image image = ark.GetComponent<Image>();
		Set(image, S("Slot_Selected"));
		Button button = ark.GetComponent<Button>();
		button.transition = Selectable.Transition.ColorTint;
		button.targetGraphic = image;
		Record(button);

		RectTransform rect = (RectTransform)ark;
		rect.sizeDelta = new Vector2(124f, 116f);
		Record(rect);

		Transform iconTransform = ark.Find("Icon");
		if (iconTransform != null)
		{
			Image icon = iconTransform.GetComponent<Image>();
			Set(icon, S("Item_RewardCache"), preserveAspect: true);
			Stretch(icon.rectTransform, 18f);
		}

		TMP_Text label = ark.GetComponentInChildren<TMP_Text>(true);
		if (label != null)
		{
			RectTransform labelRect = label.rectTransform;
			labelRect.anchorMin = new Vector2(0.5f, 0f);
			labelRect.anchorMax = new Vector2(0.5f, 0f);
			labelRect.pivot = new Vector2(0.5f, 1f);
			labelRect.anchoredPosition = new Vector2(0f, -2f);
			labelRect.offsetMin = new Vector2(labelRect.offsetMin.x, labelRect.offsetMin.y);
			labelRect.sizeDelta = new Vector2(170f, 36f);
			label.fontSize = 26;
			label.color = new Color(1f, 0.85f, 0.4f, 1f);
			label.fontStyle |= FontStyles.Bold;
			label.alignment = TextAlignmentOptions.Center;
			Record(labelRect);
			Record(label);
		}
	}

	/// <summary>One kit frame behind the stacked currency rows.</summary>
	private static void StyleCurrencies(Transform currencies)
	{
		if (currencies == null)
		{
			return;
		}

		const string frameName = "CurrencyFrame";
		Transform existing = currencies.parent.Find(frameName);
		GameObject frame = existing != null ? existing.gameObject : new GameObject(frameName, typeof(RectTransform), typeof(Image));
		frame.layer = currencies.gameObject.layer;
		RectTransform frameRect = (RectTransform)frame.transform;
		frameRect.SetParent(currencies.parent, false);
		// Always drawn behind the rows, also when the frame already exists from an earlier run
		frameRect.SetSiblingIndex(currencies.GetSiblingIndex());
		if (frameRect.GetSiblingIndex() > currencies.GetSiblingIndex())
		{
			currencies.SetSiblingIndex(frameRect.GetSiblingIndex());
		}

		// Covers the four rows at the top left
		frameRect.anchorMin = frameRect.anchorMax = frameRect.pivot = new Vector2(0f, 1f);
		frameRect.anchoredPosition = new Vector2(4f, -84f);
		frameRect.sizeDelta = new Vector2(330f, 268f);
		Image image = frame.GetComponent<Image>();
		Set(image, S("Panel_Cargo"));
		SetPixelsPerUnit(image, 1.6f);
		image.raycastTarget = false;
		Record(frameRect);

		foreach (TMP_Text text in currencies.GetComponentsInChildren<TMP_Text>(true))
		{
			text.color = TextColor;
			text.fontStyle |= FontStyles.Bold;
			Record(text);
		}
	}

	private static void StyleToast(Transform toast)
	{
		if (toast == null)
		{
			return;
		}

		Image image = toast.GetComponent<Image>();
		Set(image, S("Panel_StatusChip"));
		SetPixelsPerUnit(image, 1.1f);
	}

	private static void StyleTutorial(Transform root)
	{
		if (root == null)
		{
			return;
		}

		Transform backing = root.Find("HintPanel/Backing");
		if (backing != null)
		{
			Image image = backing.GetComponent<Image>();
			image.enabled = false;
			Record(image);
		}

		Transform frame = root.Find("HintPanel/Frame");
		if (frame != null)
		{
			Set(frame.GetComponent<Image>(), S("Panel_RewardDialog"));
		}

		// The gold dialog has a tab on top and thick sides, so the hint gets more room and padding
		Transform hint = root.Find("HintPanel");
		if (hint != null)
		{
			RectTransform hintRect = (RectTransform)hint;
			hintRect.sizeDelta = new Vector2(800f, 300f);
			Record(hintRect);
			Transform hintText = hint.Find("HintText");
			if (hintText != null)
			{
				RectTransform textRect = (RectTransform)hintText;
				textRect.offsetMin = new Vector2(64f, 100f);
				textRect.offsetMax = new Vector2(-64f, -66f);
				Record(textRect);
			}

			Transform skip = hint.Find("SkipButton");
			if (skip != null)
			{
				RectTransform skipRect = (RectTransform)skip;
				skipRect.anchoredPosition = new Vector2(64f, 34f);
				Record(skipRect);
			}

			Transform next = hint.Find("ContinueButton");
			if (next != null)
			{
				RectTransform nextRect = (RectTransform)next;
				nextRect.anchoredPosition = new Vector2(-56f, 30f);
				Record(nextRect);
			}
		}

		Transform ring = root.Find("Ring");
		if (ring != null)
		{
			Set(ring.GetComponent<Image>(), S("FX_Scan_02"), preserveAspect: true);
		}

		Transform pointer = root.Find("Pointer");
		if (pointer != null)
		{
			Image image = pointer.GetComponent<Image>();
			image.color = new Color(1f, 0.78f, 0.3f, 1f);
			Record(image);
		}

		foreach (Button button in root.GetComponentsInChildren<Button>(true))
		{
			Image image = button.GetComponent<Image>();
			if (image != null && image.sprite != null && ActionSprites.Contains(image.sprite.name))
			{
				SetButton(button, image, "Claim");
				StyleButtonLabel(button);
			}
		}
	}

	// --- Planet panel ---

	private static readonly Dictionary<string, string> UpgradeModules = new Dictionary<string, string>
	{
		{ "MiningRateInfo", "Module_DrillMotor" },
		{ "ShipSpeedInfo", "Module_WarpDrive" },
		{ "ShipCargoInfo", "Module_CargoPod" }
	};

	/// <summary>Upgrade rows use the kit's row with an icon socket, holding a module for each upgrade.</summary>
	private static void StylePlanetUpgradeRows(Transform updatePanel)
	{
		if (updatePanel == null)
		{
			return;
		}

		foreach (KeyValuePair<string, string> entry in UpgradeModules)
		{
			Transform row = updatePanel.Find(entry.Key);
			if (row == null)
			{
				continue;
			}

			Image image = row.GetComponent<Image>();
			Set(image, S("Panel_UpgradeRow"));
			float height = GetHeight((RectTransform)row, 120f);
			float scale = height / image.sprite.rect.height;
			SetPixelsPerUnit(image, 1f / scale);

			// The socket is the square on the left of the row art
			float socketCenter = 108f * scale;
			float socketSize = 118f * scale;
			Image icon = EnsureChildImage(row, "ModuleIcon", S(entry.Value));
			LayoutElement iconLayout = icon.GetComponent<LayoutElement>();
			if (iconLayout == null)
			{
				iconLayout = icon.gameObject.AddComponent<LayoutElement>();
			}

			iconLayout.ignoreLayout = true;
			RectTransform iconRect = icon.rectTransform;
			iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
			iconRect.pivot = new Vector2(0.5f, 0.5f);
			iconRect.anchoredPosition = new Vector2(socketCenter, 0f);
			iconRect.sizeDelta = new Vector2(socketSize, socketSize);
			Record(iconRect);

			// Texts start right of the socket
			float textLeft = 190f * scale;
			MoveTextX(row.Find("PropertyTitle"), textLeft);
			MoveTextX(row.Find("PropertyLevel"), textLeft);
			MoveTextX(row.Find("PropertyValue"), textLeft + 144f);
		}
	}

	private static void MoveTextX(Transform text, float x)
	{
		if (text == null)
		{
			return;
		}

		RectTransform rect = (RectTransform)text;
		rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
		Record(rect);
	}


	// --- Text fit ---

	private static readonly HashSet<string> ListRows = new HashSet<string> { "TaskItem", "ManagerItem", "RoomItem", "MineBoost", "Ark", "ManagerRow" };

	/// <summary>Keeps texts off the kit frames' bevelled edges and corners.</summary>
	private static void FitTexts(Transform root)
	{
		foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
		{
			Image image = t.GetComponent<Image>();
			if (ListRows.Contains(t.name) && image != null && image.sprite != null && image.sprite.name == "Panel_FleetRow")
			{
				// Slimmer rim, and texts start inside it
				SetPixelsPerUnit(image, 1.5f);
				foreach (Transform child in t)
				{
					InsetRowText(child.GetComponent<TMP_Text>());
				}
			}

			switch (t.name)
			{
				case "ResourceConstantValueText":
					SetX(t, -56f);
					break;
				case "CardTitle":
					SetY(t, -40f);
					break;
				case "PropertyTitle" when IsUpgradeRow(t.parent):
					SetY(t, -12f);
					break;
				case "PropertyLevel" when IsUpgradeRow(t.parent):
				case "PropertyValue" when IsUpgradeRow(t.parent):
					SetY(t, -58f);
					break;
			}
		}
	}

	private static bool IsUpgradeRow(Transform row)
	{
		Image image = row != null ? row.GetComponent<Image>() : null;
		return image != null && image.sprite != null && image.sprite.name == "Panel_UpgradeRow";
	}

	private static void InsetRowText(TMP_Text text)
	{
		if (text == null)
		{
			return;
		}

		RectTransform rect = text.rectTransform;
		Vector2 position = rect.anchoredPosition;
		Vector2 size = rect.sizeDelta;
		if (rect.anchorMin.x == 0f && position.x < 52f)
		{
			// Stretched texts keep their right edge
			if (rect.anchorMax.x == 1f)
			{
				size.x -= 52f - position.x;
			}

			position.x = 52f;
		}

		bool atBottom = rect.anchorMin.y == 0f && rect.anchorMax.y == 0f;
		bool atTop = rect.anchorMin.y == 1f && rect.anchorMax.y == 1f;
		if (atBottom && position.y < 26f)
		{
			position.y = 26f;
		}
		else if (atTop && position.y > -24f)
		{
			position.y = -24f;
		}

		rect.anchoredPosition = position;
		rect.sizeDelta = size;
		Record(rect);
	}

	private static void SetX(Transform t, float x)
	{
		RectTransform rect = (RectTransform)t;
		rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
		Record(rect);
	}

	private static void SetY(Transform t, float y)
	{
		RectTransform rect = (RectTransform)t;
		rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
		Record(rect);
	}

	// --- Helpers ---

	private static Image EnsureChildImage(Transform parent, string childName, Sprite sprite)
	{
		Transform existing = parent.Find(childName);
		GameObject go = existing != null ? existing.gameObject : new GameObject(childName, typeof(RectTransform), typeof(Image));
		go.layer = parent.gameObject.layer;
		go.transform.SetParent(parent, false);
		Image image = go.GetComponent<Image>();
		image.sprite = sprite;
		image.preserveAspect = true;
		image.raycastTarget = false;
		image.color = Color.white;
		image.type = Image.Type.Simple;
		Record(image);
		return image;
	}

	private static void HideChildImages(Image image)
	{
		foreach (Image child in image.GetComponentsInChildren<Image>(true))
		{
			if (child != image)
			{
				child.enabled = false;
				Record(child);
			}
		}
	}

	private static void Stretch(RectTransform rect, float inset)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.offsetMin = new Vector2(inset, inset);
		rect.offsetMax = new Vector2(-inset, -inset);
		Record(rect);
	}

	private static void ScaleRect(Image image, float size)
	{
		RectTransform rect = image.rectTransform;
		rect.sizeDelta = new Vector2(size, size);
		Record(rect);
	}

	private static float GetHeight(RectTransform rect, float fallback)
	{
		if (rect.rect.height > 1f)
		{
			return rect.rect.height;
		}

		LayoutElement layout = rect.GetComponent<LayoutElement>();
		return layout != null && layout.preferredHeight > 0f ? layout.preferredHeight : fallback;
	}

	private static void Set(Image image, Sprite sprite, bool preserveAspect = false)
	{
		image.sprite = sprite;
		image.color = Color.white;
		image.enabled = true;
		image.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
		image.fillCenter = true;
		image.preserveAspect = preserveAspect;

		// Kit frames are drawn for large panels; on smaller elements their borders are scaled down to fit
		if (image.type == Image.Type.Sliced)
		{
			float height = GetHeight(image.rectTransform, sprite.rect.height);
			SetPixelsPerUnit(image, Mathf.Max(1f, sprite.rect.height / Mathf.Max(height, 1f) * 0.85f));
		}

		Record(image);
	}

	private static void SetPixelsPerUnit(Image image, float multiplier)
	{
		image.pixelsPerUnitMultiplier = multiplier;
		Record(image);
	}

	private static void SetBuiltin(Image image, Color color)
	{
		image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
		image.type = Image.Type.Sliced;
		image.color = color;
		Record(image);
	}

	private static void Record(Object obj)
	{
		EditorUtility.SetDirty(obj);
		if (PrefabUtility.IsPartOfPrefabInstance(obj))
		{
			PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
		}
	}
}
