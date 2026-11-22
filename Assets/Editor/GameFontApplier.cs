using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Creates the Rajdhani TextMeshPro font assets (SemiBold for normal text, Bold for bold), makes Rajdhani
/// the TMP default and switches every text in the game's scenes and prefabs to it.
/// </summary>
public static class GameFontApplier
{
	private const string FontFolder = "Assets/Fonts/Rajdhani";
	private const string RegularFontPath = FontFolder + "/Rajdhani-SemiBold.ttf";
	private const string BoldFontPath = FontFolder + "/Rajdhani-Bold.ttf";
	private const string RegularAssetPath = FontFolder + "/Rajdhani-SemiBold SDF.asset";
	private const string BoldAssetPath = FontFolder + "/Rajdhani-Bold SDF.asset";

	private static readonly string[] Scenes =
	{
		"Assets/Scenes/GameScene.unity",
		"Assets/Scenes/MenuScene.unity",
		"Assets/Scenes/InitScene.unity",
	};

	// Only the game's own prefabs, not third party examples
	private static readonly string[] PrefabFolders = { "Assets/Prefabs", "Assets/Resources" };

	[MenuItem("Tools/UI/Apply Game Font")]
	public static void Apply()
	{
		TMP_FontAsset regular = GetOrCreateFontAsset(RegularFontPath, RegularAssetPath);
		TMP_FontAsset bold = GetOrCreateFontAsset(BoldFontPath, BoldAssetPath);

		// Bold text uses the Bold font instead of faux bold
		regular.fontWeightTable[7].regularTypeface = bold;
		EditorUtility.SetDirty(regular);

		SetDefaultFont(regular);

		int count = 0;
		foreach (string folder in PrefabFolders)
		{
			foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				GameObject root = PrefabUtility.LoadPrefabContents(path);
				int changed = ApplyTo(root.GetComponentsInChildren<TMP_Text>(true), regular);
				if (changed > 0)
				{
					PrefabUtility.SaveAsPrefabAsset(root, path);
					count += changed;
				}

				PrefabUtility.UnloadPrefabContents(root);
			}
		}

		foreach (string scenePath in Scenes)
		{
			var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
			List<TMP_Text> texts = new List<TMP_Text>();
			foreach (GameObject root in scene.GetRootGameObjects())
			{
				texts.AddRange(root.GetComponentsInChildren<TMP_Text>(true));
			}

			int changed = ApplyTo(texts, regular);
			if (changed > 0)
			{
				EditorSceneManager.SaveScene(scene);
				count += changed;
			}
		}

		AssetDatabase.SaveAssets();
		Debug.Log($"[GameFont] Switched {count} texts to {regular.name}");
	}

	private static int ApplyTo(IEnumerable<TMP_Text> texts, TMP_FontAsset font)
	{
		int changed = 0;
		foreach (TMP_Text text in texts)
		{
			if (text.font == font)
			{
				continue;
			}

			// Planet labels and other texts inside prefab instances become scene overrides
			text.font = font;
			text.fontSharedMaterial = font.material;
			EditorUtility.SetDirty(text);
			if (PrefabUtility.IsPartOfPrefabInstance(text))
			{
				PrefabUtility.RecordPrefabInstancePropertyModifications(text);
			}

			changed++;
		}

		return changed;
	}

	private static TMP_FontAsset GetOrCreateFontAsset(string fontPath, string assetPath)
	{
		TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
		if (existing != null)
		{
			return existing;
		}

		Font font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);

		// Dynamic atlas: glyphs are added as texts need them, so any language/symbol works
		TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
		asset.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
		AssetDatabase.CreateAsset(asset, assetPath);

		// The atlas texture and material have to live inside the font asset file
		asset.atlasTextures[0].name = asset.name + " Atlas";
		asset.material.name = asset.name + " Material";
		AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
		AssetDatabase.AddObjectToAsset(asset.material, asset);

		// Prefill the common characters so the first frames don't build the atlas
		asset.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;!?'\"()[]/\\%$+-*=<>#&@_");

		EditorUtility.SetDirty(asset);
		AssetDatabase.SaveAssets();
		return asset;
	}

	private static void SetDefaultFont(TMP_FontAsset font)
	{
		TMP_Settings settings = Resources.Load<TMP_Settings>("TMP Settings");
		SerializedObject so = new SerializedObject(settings);
		so.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
		so.ApplyModifiedPropertiesWithoutUndo();
	}
}
