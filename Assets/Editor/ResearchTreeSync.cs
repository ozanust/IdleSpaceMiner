using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Shows only the research tree icons that do something: the research needs settings data,
/// and a telescope needs at least one planet to reveal. Run it after adding research or planets.
/// </summary>
public static class ResearchTreeSync
{
	private const string ScenePath = "Assets/Scenes/GameScene.unity";

	[MenuItem("Tools/Research/Sync Research Tree")]
	public static void SyncInOpenScene()
	{
		Sync();
		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
	}

	/// <summary>Batch mode entry point.</summary>
	public static void SyncAndSave()
	{
		EditorSceneManager.OpenScene(ScenePath);
		Sync();
		EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
	}

	private static void Sync()
	{
		ResourceSettings resources = AssetDatabase.LoadAssetAtPath<ResourceSettings>("Assets/Resources/Settings/ResourceSettings.asset");
		PlanetSettings planets = AssetDatabase.LoadAssetAtPath<PlanetSettings>("Assets/Resources/Settings/PlanetSettings.asset");

		foreach (ResearchItem item in Object.FindObjectsByType<ResearchItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			ResearchType type = (ResearchType)new SerializedObject(item).FindProperty("type").enumValueIndex;
			bool visible = resources.GetResearchSetting(type) != null && RevealsPlanets(type, planets.Settings.Length);

			if (item.gameObject.activeSelf != visible)
			{
				item.gameObject.SetActive(visible);
				PrefabUtility.RecordPrefabInstancePropertyModifications(item.gameObject);
				Debug.Log($"[ResearchTree] {type} {(visible ? "shown" : "hidden")}");
			}
		}
	}

	private static bool RevealsPlanets(ResearchType type, int planetCount)
	{
		if (type < ResearchType.Telescope1 || type > ResearchType.Telescope5)
		{
			return true;
		}

		int firstPlanet = SpaceController.DefaultVisiblePlanets + (type - ResearchType.Telescope1) * SpaceController.PlanetsPerTelescope;
		return firstPlanet < planetCount;
	}
}
