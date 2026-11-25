public enum MenuType
{
	None,
	Resources,
	Production,
	SmeltRecipes,
	CraftRecipes,
	Research,
	ResarchInfo,
	Managers,
	Boosts,
	Mothership,
	Tasks,
	Achievements,
	Settings,
	// The planet info window, opened by tapping a planet
	Planet
}

public static class MenuTypes
{
	/// <summary>
	/// Main windows replace each other, so only one is open at a time. Recipe lists and research
	/// info open over their own window and don't close it.
	/// </summary>
	public static bool IsMainWindow(MenuType type)
	{
		return type != MenuType.None && type != MenuType.SmeltRecipes && type != MenuType.CraftRecipes && type != MenuType.ResarchInfo;
	}

	/// <summary>Whether a window of the given type closes because another main window is opening.</summary>
	public static bool ClosesFor(MenuType opening, MenuType own)
	{
		return opening != own && IsMainWindow(opening);
	}
}
