using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Restyles the game UI with the "Sci-Fi Game UI 8" pack: window frames, title bars, tab and action
/// buttons, list rows, item cards, progress bars, sliders, scroll bars and the tasks HUD button.
/// Run it again after rebuilding UI with the other UI tools.
/// </summary>
public static class SciFiThemeApplier
{
	private const string PackFolder = "Assets/Sci-Fi Game UI 8/MAIN/PNG/";
	private const string CropFolder = "Assets/GFX/Sprites/UI/SciFi";
	private const string ScenePath = "Assets/Scenes/GameScene.unity";
	private const string PrefabFolder = "Assets/Prefabs/UIPrefabs";

	private static readonly Color TextColor = new Color(0.88f, 0.97f, 1f, 1f);
	private static readonly Color ContainerColor = new Color(0.02f, 0.07f, 0.09f, 0.45f);

	private class Theme
	{
		public Sprite Panel, Slot, Tab, TabDefault, Action, Buy, Ring, Track, Progress, Knob, ScrollTrack, ScrollKnob, Chest;
	}

	[MenuItem("Tools/UI/Apply Sci-Fi Theme")]
	public static void ApplyInOpenScene()
	{
		Theme theme = PrepareSprites();
		ApplyToPrefabs(theme);
		ApplyToCanvas(theme);
		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
	}

	/// <summary>Batch mode entry point: prepares sprites, restyles the prefabs and GameScene and saves.</summary>
	public static void ApplyAndSave()
	{
		Theme theme = PrepareSprites();
		ApplyToPrefabs(theme);
		EditorSceneManager.OpenScene(ScenePath);
		ApplyToCanvas(theme);
		EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
		AssetDatabase.SaveAssets();
	}

	// --- Sprites ---

	private static Theme PrepareSprites()
	{
		Directory.CreateDirectory(CropFolder);

		// Pack sprites only need 9-slice borders (left, bottom, right, top) so frames stretch cleanly
		Theme theme = new Theme
		{
			Panel = Sprite("Panel.png", new Vector4(175, 95, 180, 75)),
			Slot = Sprite("Shopping item slott.png", new Vector4(240, 40, 70, 45)),
			Tab = Sprite("Horizontal tab bar/Tab button active.png", new Vector4(20, 20, 130, 30)),
			TabDefault = Sprite("Horizontal tab bar/Tab button default.png", new Vector4(20, 20, 130, 30)),
			Action = Sprite("Apply_Cancel button.png", new Vector4(45, 30, 110, 35)),
			Buy = Sprite("Buy button.png", Vector4.zero),
			Knob = Sprite("slider/Knob.png", Vector4.zero),
			ScrollTrack = Sprite("Scroll bar/groover.png", new Vector4(0, 30, 0, 30)),
			ScrollKnob = Sprite("Scroll bar/knob.png", new Vector4(0, 25, 0, 25)),
			Chest = Sprite("Treasure_ChestICON.png", Vector4.zero),
		};

		// Some pack images hold one element in a larger transparent canvas, so crop those out
		theme.Ring = Crop("Main_menu_button.png", "ui_ring.png", new RectInt(0, 0, 175, 174), Vector4.zero);
		theme.Progress = Crop("slider/Progress.png", "ui_progress.png", null, new Vector4(24, 0, 24, 0));
		theme.Track = Crop("slider/groover.png", "ui_track.png", null, new Vector4(24, 0, 24, 0));
		return theme;
	}

	private static Sprite Sprite(string file, Vector4 border)
	{
		string path = PackFolder + file;
		TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
		importer.textureType = TextureImporterType.Sprite;
		importer.spriteImportMode = SpriteImportMode.Single;
		importer.spriteBorder = border;
		importer.alphaIsTransparency = true;
		importer.mipmapEnabled = false;
		importer.SaveAndReimport();
		return AssetDatabase.LoadAssetAtPath<Sprite>(path);
	}

	/// <summary>Writes the visible part of a pack image (optionally inside a search area) as its own sprite.</summary>
	private static Sprite Crop(string file, string outputName, RectInt? searchArea, Vector4 border)
	{
		Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
		source.LoadImage(File.ReadAllBytes(PackFolder + file));
		RectInt area = searchArea ?? new RectInt(0, 0, source.width, source.height);

		int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
		for (int y = area.yMin; y < area.yMax; y++)
		{
			for (int x = area.xMin; x < area.xMax; x++)
			{
				if (source.GetPixel(x, y).a > 0.06f)
				{
					minX = Mathf.Min(minX, x);
					minY = Mathf.Min(minY, y);
					maxX = Mathf.Max(maxX, x);
					maxY = Mathf.Max(maxY, y);
				}
			}
		}

		int width = maxX - minX + 1, height = maxY - minY + 1;
		Texture2D crop = new Texture2D(width, height, TextureFormat.RGBA32, false);
		crop.SetPixels(source.GetPixels(minX, minY, width, height));
		crop.Apply();

		string path = CropFolder + "/" + outputName;
		File.WriteAllBytes(path, crop.EncodeToPNG());
		AssetDatabase.ImportAsset(path);

		TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
		importer.textureType = TextureImporterType.Sprite;
		importer.spriteImportMode = SpriteImportMode.Single;
		importer.spriteBorder = border;
		importer.alphaIsTransparency = true;
		importer.mipmapEnabled = false;
		importer.SaveAndReimport();
		return AssetDatabase.LoadAssetAtPath<Sprite>(path);
	}

	// --- Applying ---

	private static void ApplyToPrefabs(Theme theme)
	{
		foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			GameObject root = PrefabUtility.LoadPrefabContents(path);
			Restyle(root.transform, theme);
			PrefabUtility.SaveAsPrefabAsset(root, path);
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	private static void ApplyToCanvas(Theme theme)
	{
		Transform canvas = Object.FindFirstObjectByType<NavBarView>(FindObjectsInactive.Include).transform.parent;
		Restyle(canvas, theme);

		TasksButton tasksButton = canvas.GetComponentInChildren<TasksButton>(true);
		if (tasksButton != null)
		{
			StyleTasksButton(tasksButton, theme);
		}

		// The research window was copied from Production and kept its title
		Transform researchTitle = canvas.Find("ResearchView/Panel/TitleBG/ViewTitle");
		if (researchTitle != null)
		{
			TMP_Text title = researchTitle.GetComponent<TMP_Text>();
			title.text = "Projects";
			Record(title);
		}
	}

	private static void Restyle(Transform root, Theme theme)
	{
		foreach (Image image in root.GetComponentsInChildren<Image>(true))
		{
			if (image.GetComponentInParent<TasksButton>(true) != null)
			{
				continue;
			}

			RestyleImage(image, theme);
		}

		// Old buttons were light with dark labels, the new ones are dark
		foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
		{
			Color c = text.color;
			if (Mathf.Max(c.r, Mathf.Max(c.g, c.b)) < 0.45f && c.a > 0.5f)
			{
				text.color = new Color(TextColor.r, TextColor.g, TextColor.b, c.a);
				Record(text);
			}
		}
	}

	// Old sprite names, plus the pack names so running the tool again finds already themed UI
	private static readonly HashSet<string> WindowSprites = new HashSet<string> { "Futuro_WindowBack", "Panel" };
	private static readonly HashSet<string> TitleSprites = new HashSet<string> { "Futuro_ButtonBackDark", "Tab button active" };
	private static readonly HashSet<string> UpgradeSprites = new HashSet<string> { "Futuro_ButtonBack", "Buy button" };
	private static readonly HashSet<string> ButtonSprites = new HashSet<string> { "UISprite", "Futuro_Bar", "Apply_Cancel button", "Tab button active" };
	private static readonly HashSet<string> RowSprites = new HashSet<string> { "Background", "Shopping item slott", "Tab button default" };

	private static void RestyleImage(Image image, Theme theme)
	{
		string sprite = image.sprite != null ? image.sprite.name : "";
		string name = image.gameObject.name;
		if (name == "Backdrop")
		{
			return;
		}

		bool isButton = image.GetComponent<Button>() != null;
		Slider slider = image.GetComponentInParent<Slider>(true);
		bool inSlider = slider != null && slider.gameObject != image.gameObject;

		if (image.GetComponent<Scrollbar>() != null)
		{
			Set(image, theme.ScrollTrack, Color.white);
		}
		else if (image.GetComponentInParent<Scrollbar>(true) != null && name == "Handle")
		{
			Set(image, theme.ScrollKnob, Color.white);
		}
		else if (name == "CloseButton" || name == "RemoveRecipeButton" || sprite == "Futoro_Ring")
		{
			Set(image, theme.Ring, Color.white, preserveAspect: true);
		}
		else if (inSlider && !isButton)
		{
			if (name == "Background") Set(image, theme.Track, Color.white);
			else if (name == "Fill") Set(image, theme.Progress, Color.white);
			else if (name == "Handle") Set(image, theme.Knob, Color.white, preserveAspect: true);
		}
		else if (IsItemCard(image))
		{
			Set(image, theme.Panel, Color.white);
		}
		else if (WindowSprites.Contains(sprite))
		{
			Set(image, theme.Panel, Color.white);
			AddBackdrop(image);
		}
		else if (TitleSprites.Contains(sprite) && !isButton)
		{
			Set(image, theme.Tab, Color.white);
		}
		else if (UpgradeSprites.Contains(sprite))
		{
			Set(image, theme.Buy, Color.white);
		}
		else if (isButton && ButtonSprites.Contains(sprite))
		{
			Transform parent = image.transform.parent;
			bool isNavButton = parent != null && parent.GetComponent<NavBarView>() != null;
			bool isViewTab = parent != null && parent.name == "ViewButtons";
			Set(image, isNavButton || isViewTab ? theme.Tab : theme.Action, Color.white);

			if (isViewTab)
			{
				EnlargeViewTab(image);
			}
		}
		else if (RowSprites.Contains(sprite))
		{
			if (image.GetComponent<ResourceInfoItem>() != null || image.GetComponent<TaskItem>() != null || name == "ResourceSellView")
			{
				Set(image, theme.Slot, Color.white);
			}
			else if (name == "AmountBG")
			{
				Set(image, theme.TabDefault, Color.white);
			}
			else if (image.GetComponent<NavBarView>() == null)
			{
				// Content areas inside windows: a dark tint so rows stand out
				image.color = ContainerColor;
				Record(image);
			}
		}
	}

	/// <summary>The pack's window frame is see-through, so windows get a dark backdrop inside the frame.</summary>
	private static void AddBackdrop(Image window)
	{
		const string backdropName = "Backdrop";
		Transform existing = window.transform.Find(backdropName);
		GameObject backdrop = existing != null ? existing.gameObject : new GameObject(backdropName, typeof(RectTransform), typeof(Image));
		backdrop.layer = window.gameObject.layer;

		RectTransform rect = (RectTransform)backdrop.transform;
		rect.SetParent(window.transform, false);
		rect.SetAsFirstSibling();
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = new Vector2(30, 30);
		rect.offsetMax = new Vector2(-30, -30);

		// Some windows lay out their children (planet info), the backdrop must stay out of that
		LayoutElement layout = backdrop.GetComponent<LayoutElement>();
		if (layout == null)
		{
			layout = backdrop.AddComponent<LayoutElement>();
		}

		layout.ignoreLayout = true;

		Image image = backdrop.GetComponent<Image>();
		image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
		image.type = Image.Type.Sliced;
		image.color = new Color(0.01f, 0.04f, 0.06f, 0.92f);
		image.raycastTarget = false;
	}

	private static void EnlargeViewTab(Image tab)
	{
		RectTransform rect = (RectTransform)tab.transform;
		rect.sizeDelta = new Vector2(240, 72);
		LayoutElement layout = tab.GetComponent<LayoutElement>();
		if (layout == null)
		{
			layout = tab.gameObject.AddComponent<LayoutElement>();
		}

		layout.preferredWidth = 240;
		layout.preferredHeight = 72;

		TMP_Text label = tab.GetComponentInChildren<TMP_Text>(true);
		if (label != null)
		{
			label.fontSize = 30;
			label.enableAutoSizing = false;
		}
	}

	private static bool IsItemCard(Image image)
	{
		return image.GetComponent<SmelterItem>() != null
			|| image.GetComponent<CrafterItem>() != null
			|| image.GetComponent<SmeltRecipeItem>() != null;
	}

	private static void StyleTasksButton(TasksButton tasksButton, Theme theme)
	{
		Image image = tasksButton.GetComponent<Image>();
		Set(image, theme.Chest, Color.white, preserveAspect: true);
		RectTransform rect = (RectTransform)tasksButton.transform;
		rect.sizeDelta = new Vector2(150, 130);
		Record(rect);

		// Label under the icon
		TMP_Text label = tasksButton.GetComponentInChildren<TMP_Text>(true);
		RectTransform labelRect = label.rectTransform;
		labelRect.anchorMin = new Vector2(0.5f, 0);
		labelRect.anchorMax = new Vector2(0.5f, 0);
		labelRect.pivot = new Vector2(0.5f, 1);
		labelRect.anchoredPosition = new Vector2(0, 4);
		labelRect.sizeDelta = new Vector2(180, 40);
		label.fontSize = 30;
		label.color = TextColor;
		Record(labelRect);
		Record(label);
	}

	private static void Set(Image image, Sprite sprite, Color color, bool preserveAspect = false)
	{
		image.sprite = sprite;
		image.color = color;
		image.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
		image.fillCenter = true;
		image.preserveAspect = preserveAspect;
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
