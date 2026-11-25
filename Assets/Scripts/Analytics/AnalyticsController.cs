using System;
using UnityEngine;
using Zenject;

/// <summary>
/// Game-wide analytics: sessions, play time, return days and user properties for retention, and
/// events for progress, economy and screens from the game's signals. Firebase measures sessions and
/// retention by itself too; these events add play time and progress to cut those reports by.
/// Totals are kept in PlayerPrefs, apart from the save, so they survive galaxy sales and resets.
/// </summary>
public class AnalyticsController : IInitializable, ITickable, IDisposable
{
	// Coming back after this long in the background starts a new session (Firebase uses 30 minutes)
	private const float NewSessionAfterSeconds = 300f;
	private const float SaveInterval = 30f;
	private static readonly int[] PlayMinuteMilestones = { 5, 15, 30, 60, 120, 240, 480, 1000, 2000 };

	private const string InstallKey = "analytics_install_utc";
	private const string SessionsKey = "analytics_sessions";
	private const string PlaySecondsKey = "analytics_play_seconds";
	private const string LastActiveDayKey = "analytics_last_active_day";
	private const string ActiveDaysKey = "analytics_active_days";
	private const string LastSessionEndKey = "analytics_last_session_end_utc";
	private const string MilestoneKey = "analytics_play_milestone";
	private const string GalaxyStartKey = "analytics_galaxy_start_play_seconds";

	// The game scene reloads when a galaxy is sold, the session goes on across it
	private static bool sessionActive;
	private static int sessionNumber;
	private static float sessionSeconds;
	private static int sessionUpgrades;
	private static long sessionUpgradeSpend;
	private static long sessionMoneyFromSales;
	private static int sessionAsteroids;

	private readonly IAnalyticsService analytics;
	private readonly SignalBus signalBus;
	private readonly IPlayerModel playerModel;
	private readonly ISpaceModel spaceModel;
	private readonly IMothershipController mothershipController;
	private readonly ResourceSettings resourceSettings;
	private readonly PlanetSettings planetSettings;

	private double playSeconds;
	private float saveTimer;
	private bool focused = true;
	// Restoring a save fires the same signals as unlocking things; progress counts from the first frame on
	private bool loaded;
	private DateTime? backgroundSinceUtc;

	public AnalyticsController(IAnalyticsService analytics, SignalBus signalBus, IPlayerModel playerModel, ISpaceModel spaceModel,
		IMothershipController mothershipController, ResourceSettings resourceSettings, PlanetSettings planetSettings)
	{
		this.analytics = analytics;
		this.signalBus = signalBus;
		this.playerModel = playerModel;
		this.spaceModel = spaceModel;
		this.mothershipController = mothershipController;
		this.resourceSettings = resourceSettings;
		this.planetSettings = planetSettings;
	}

	private double PlayMinutes => playSeconds / 60.0;

	public void Initialize()
	{
		playSeconds = double.Parse(PlayerPrefs.GetString(PlaySecondsKey, "0"), System.Globalization.CultureInfo.InvariantCulture);
		if (!PlayerPrefs.HasKey(InstallKey))
		{
			PlayerPrefs.SetString(InstallKey, DateTime.UtcNow.Ticks.ToString());
			PlayerPrefs.SetString(GalaxyStartKey, "0");
			analytics.SetUserProperty("install_version", Application.version);
		}

		signalBus.Subscribe<OnApplicationFocusSignal>(OnFocus);
		signalBus.Subscribe<OnApplicationQuitSignal>(OnQuit);
		signalBus.Subscribe<SpaceModelInitializedSignal>(OnGameLoaded);
		signalBus.Subscribe<PlanetUnlockedSignal>(OnPlanetUnlocked);
		signalBus.Subscribe<ResearchCompletedSignal>(OnResearchCompleted);
		signalBus.Subscribe<SmelterUnlockedSignal>(OnSmelterUnlocked);
		signalBus.Subscribe<CrafterUnlockedSignal>(OnCrafterUnlocked);
		signalBus.Subscribe<RecipeUnlockedSignal>(OnRecipeUnlocked);
		signalBus.Subscribe<ResourcesSellSignal>(OnResourcesSold);
		signalBus.Subscribe<PurchaseSignal>(OnPurchase);
		signalBus.Subscribe<AsteroidDestroyedSignal>(OnAsteroidDestroyed);
		signalBus.Subscribe<GalaxySoldSignal>(OnGalaxySold);
		signalBus.Subscribe<MenuOpenSignal>(OnMenuOpen);

		if (!sessionActive)
		{
			BeginSession();
		}
	}

	public void Dispose()
	{
		SaveTotals();
		signalBus.Unsubscribe<OnApplicationFocusSignal>(OnFocus);
		signalBus.Unsubscribe<OnApplicationQuitSignal>(OnQuit);
		signalBus.Unsubscribe<SpaceModelInitializedSignal>(OnGameLoaded);
		signalBus.Unsubscribe<PlanetUnlockedSignal>(OnPlanetUnlocked);
		signalBus.Unsubscribe<ResearchCompletedSignal>(OnResearchCompleted);
		signalBus.Unsubscribe<SmelterUnlockedSignal>(OnSmelterUnlocked);
		signalBus.Unsubscribe<CrafterUnlockedSignal>(OnCrafterUnlocked);
		signalBus.Unsubscribe<RecipeUnlockedSignal>(OnRecipeUnlocked);
		signalBus.Unsubscribe<ResourcesSellSignal>(OnResourcesSold);
		signalBus.Unsubscribe<PurchaseSignal>(OnPurchase);
		signalBus.Unsubscribe<AsteroidDestroyedSignal>(OnAsteroidDestroyed);
		signalBus.Unsubscribe<GalaxySoldSignal>(OnGalaxySold);
		signalBus.Unsubscribe<MenuOpenSignal>(OnMenuOpen);
	}

	public void Tick()
	{
		loaded = true;
		if (!focused)
		{
			return;
		}

		float delta = Time.unscaledDeltaTime;
		playSeconds += delta;
		sessionSeconds += delta;
		saveTimer += delta;
		if (saveTimer >= SaveInterval)
		{
			saveTimer = 0f;
			SaveTotals();
			CheckPlayMilestones();
		}
	}

	// --- Sessions and retention ---

	private void BeginSession()
	{
		sessionActive = true;
		sessionSeconds = 0f;
		sessionUpgrades = 0;
		sessionUpgradeSpend = 0;
		sessionMoneyFromSales = 0;
		sessionAsteroids = 0;
		sessionNumber = PlayerPrefs.GetInt(SessionsKey, 0) + 1;
		PlayerPrefs.SetInt(SessionsKey, sessionNumber);

		int day = DaysSinceInstall();
		double hoursAway = -1;
		if (long.TryParse(PlayerPrefs.GetString(LastSessionEndKey, ""), out long lastEnd))
		{
			hoursAway = (DateTime.UtcNow - new DateTime(lastEnd, DateTimeKind.Utc)).TotalHours;
		}

		// First session of a calendar day since install: day 1, day 7 ... returns for retention charts
		int activeDays = PlayerPrefs.GetInt(ActiveDaysKey, 0);
		if (PlayerPrefs.GetInt(LastActiveDayKey, -1) != day)
		{
			activeDays++;
			PlayerPrefs.SetInt(LastActiveDayKey, day);
			PlayerPrefs.SetInt(ActiveDaysKey, activeDays);
			analytics.LogEvent("day_active", new AnalyticsParams()
				.Add("day", day)
				.Add("active_days", activeDays)
				.Add("play_minutes", Math.Round(PlayMinutes, 1)));
		}

		analytics.LogEvent("session_begin", new AnalyticsParams()
			.Add("session_number", sessionNumber)
			.Add("day", day)
			.Add("active_days", activeDays)
			.Add("hours_away", Math.Round(hoursAway, 2))
			.Add("play_minutes", Math.Round(PlayMinutes, 1)));

		analytics.SetUserProperty("sessions", Bucket(sessionNumber, 1, 2, 3, 5, 10, 20, 50, 100));
		analytics.SetUserProperty("days_since_install", Bucket(day, 0, 1, 2, 3, 7, 14, 30, 60));
		PlayerPrefs.Save();
	}

	private void EndSession(string reason)
	{
		if (!sessionActive)
		{
			return;
		}

		sessionActive = false;
		SaveTotals();
		PlayerPrefs.SetString(LastSessionEndKey, DateTime.UtcNow.Ticks.ToString());
		analytics.LogEvent("session_end", new AnalyticsParams()
			.Add("reason", reason)
			.Add("session_number", sessionNumber)
			.Add("session_seconds", (long)sessionSeconds)
			.Add("play_minutes", Math.Round(PlayMinutes, 1))
			.Add("upgrades", sessionUpgrades)
			.Add("upgrade_spend", sessionUpgradeSpend)
			.Add("sales_income", sessionMoneyFromSales)
			.Add("asteroids", sessionAsteroids)
			.Add("planets", OwnedPlanets())
			.Add("galaxy", mothershipController.GalaxiesSold + 1));
		PlayerPrefs.Save();
	}

	private void OnFocus(OnApplicationFocusSignal signal)
	{
		focused = signal.Focus;
		if (!signal.Focus)
		{
			backgroundSinceUtc = DateTime.UtcNow;
			SaveTotals();
			PlayerPrefs.SetString(LastSessionEndKey, DateTime.UtcNow.Ticks.ToString());
			PlayerPrefs.Save();
			return;
		}

		if (backgroundSinceUtc.HasValue && (DateTime.UtcNow - backgroundSinceUtc.Value).TotalSeconds >= NewSessionAfterSeconds)
		{
			EndSession("background");
			BeginSession();
		}

		backgroundSinceUtc = null;
	}

	private void OnQuit()
	{
		EndSession("quit");
	}

	private void SaveTotals()
	{
		PlayerPrefs.SetString(PlaySecondsKey, playSeconds.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture));
	}

	private void CheckPlayMilestones()
	{
		int reached = PlayerPrefs.GetInt(MilestoneKey, 0);
		foreach (int minutes in PlayMinuteMilestones)
		{
			if (minutes > reached && PlayMinutes >= minutes)
			{
				reached = minutes;
				PlayerPrefs.SetInt(MilestoneKey, reached);
				analytics.LogEvent("play_time_reached", new AnalyticsParams()
					.Add("minutes", minutes)
					.Add("day", DaysSinceInstall())
					.Add("planets", OwnedPlanets())
					.Add("galaxy", mothershipController.GalaxiesSold + 1));
				analytics.SetUserProperty("play_minutes", Bucket(minutes, PlayMinuteMilestones));
			}
		}
	}

	private int DaysSinceInstall()
	{
		if (!long.TryParse(PlayerPrefs.GetString(InstallKey, ""), out long ticks))
		{
			return 0;
		}

		DateTime installDay = new DateTime(ticks, DateTimeKind.Utc).ToLocalTime().Date;
		return Math.Max(0, (int)(DateTime.Now.Date - installDay).TotalDays);
	}

	// --- Progress ---

	private void OnGameLoaded()
	{
		analytics.SetUserProperty("galaxies_sold", Bucket(mothershipController.GalaxiesSold, 0, 1, 2, 3, 5, 10, 20));
		analytics.SetUserProperty("planets_owned", OwnedPlanets().ToString());
		analytics.SetUserProperty("research_count", playerModel.GetUnlockedResearchs().Length.ToString());
	}

	private void OnPlanetUnlocked(PlanetUnlockedSignal signal)
	{
		if (!loaded)
		{
			return;
		}

		int owned = OwnedPlanets();
		PlanetDataSetting setting;
		string name = planetSettings.TryGetPlanetSetting(signal.PlanetId, out setting) ? setting.Name : signal.PlanetId.ToString();
		analytics.LogEvent("planet_unlocked", Progress()
			.Add("planet_id", signal.PlanetId)
			.Add("planet_name", name)
			.Add("planets", owned));
		// Firebase's level_up report: planets owned stand in for the player's level
		analytics.LogEvent("level_up", new AnalyticsParams().Add("level", owned).Add("character", "galaxy_" + (mothershipController.GalaxiesSold + 1)));
		analytics.SetUserProperty("planets_owned", owned.ToString());
	}

	private void OnResearchCompleted(ResearchCompletedSignal signal)
	{
		if (!loaded)
		{
			return;
		}

		int count = playerModel.GetUnlockedResearchs().Length;
		analytics.LogEvent("research_completed", Progress()
			.Add("research", signal.ResearchType.ToString())
			.Add("research_count", count));
		analytics.LogEvent("unlock_achievement", AnalyticsParams.With("achievement_id", "research_" + signal.ResearchType));
		analytics.SetUserProperty("research_count", count.ToString());
	}

	private void OnSmelterUnlocked(SmelterUnlockedSignal signal)
	{
		if (!loaded)
		{
			return;
		}

		analytics.LogEvent("machine_unlocked", Progress().Add("machine", "smelter").Add("number", signal.SmelterId + 1));
	}

	private void OnCrafterUnlocked(CrafterUnlockedSignal signal)
	{
		if (!loaded)
		{
			return;
		}

		analytics.LogEvent("machine_unlocked", Progress().Add("machine", "crafter").Add("number", signal.CrafterId + 1));
	}

	private void OnRecipeUnlocked(RecipeUnlockedSignal signal)
	{
		if (!loaded)
		{
			return;
		}

		analytics.LogEvent("recipe_unlocked", Progress().Add("recipe", signal.Type.ToString()));
	}

	private void OnAsteroidDestroyed()
	{
		sessionAsteroids++;
		analytics.LogEvent("asteroid_mined", AnalyticsParams.With("session_count", sessionAsteroids));
	}

	private void OnGalaxySold(GalaxySoldSignal signal)
	{
		double galaxyStart = double.Parse(PlayerPrefs.GetString(GalaxyStartKey, "0"), System.Globalization.CultureInfo.InvariantCulture);
		analytics.LogEvent("galaxy_sold", Progress()
			.Add("reward_dark_matter", signal.Reward)
			.Add("galaxy_value", mothershipController.GetGalaxyValue())
			.Add("galaxies_sold", mothershipController.GalaxiesSold)
			.Add("galaxy_play_minutes", Math.Round((playSeconds - galaxyStart) / 60.0, 1))
			.Add("planets", OwnedPlanets())
			.Add("research_count", playerModel.GetUnlockedResearchs().Length));
		analytics.EarnDarkMatter(signal.Reward, "galaxy_sale");
		PlayerPrefs.SetString(GalaxyStartKey, playSeconds.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture));
		analytics.SetUserProperty("galaxies_sold", Bucket(mothershipController.GalaxiesSold, 0, 1, 2, 3, 5, 10, 20));
		SaveTotals();
		PlayerPrefs.Save();
	}

	// --- Economy ---

	private void OnResourcesSold(ResourcesSellSignal signal)
	{
		long value = signal.Amount * resourceSettings.GetResourceValue(signal.Type);
		sessionMoneyFromSales += value;
		analytics.LogEvent("resources_sold", new AnalyticsParams()
			.Add("resource", signal.Type.ToString())
			.Add("category", resourceSettings.GetResourceParentType(signal.Type).ToString())
			.Add("amount", signal.Amount)
			.Add("value", value));
	}

	private void OnPurchase(PurchaseSignal signal)
	{
		if (!signal.Success)
		{
			// Where players run short: tapping something they can't afford yet
			if (signal.Kind != PurchaseKind.Ad)
			{
				analytics.LogEvent("purchase_failed", AnalyticsParams.With("kind", signal.Kind.ToString()));
			}

			return;
		}

		if (signal.Kind == PurchaseKind.Upgrade)
		{
			// Upgrades are tapped often, they are counted and sent with the session end
			sessionUpgrades++;
			sessionUpgradeSpend += signal.Price;
		}
		else if (signal.Kind == PurchaseKind.Planet || signal.Kind == PurchaseKind.Production)
		{
			analytics.LogEvent("spend_virtual_currency", new AnalyticsParams()
				.Add("virtual_currency_name", "money")
				.Add("value", signal.Price)
				.Add("item_name", signal.Kind.ToString().ToLowerInvariant()));
		}
	}

	// --- Screens ---

	private void OnMenuOpen(MenuOpenSignal signal)
	{
		if (signal.Type == MenuType.None)
		{
			return;
		}

		analytics.LogEvent("screen_view", new AnalyticsParams()
			.Add("screen_name", signal.Type.ToString())
			.Add("screen_class", "Window"));
	}

	// --- Helpers ---

	/// <summary>Where the player is: shared by the progress events.</summary>
	private AnalyticsParams Progress()
	{
		return new AnalyticsParams()
			.Add("galaxy", mothershipController.GalaxiesSold + 1)
			.Add("play_minutes", Math.Round(PlayMinutes, 1))
			.Add("session_minutes", Math.Round(sessionSeconds / 60.0, 1))
			.Add("day", DaysSinceInstall());
	}

	private int OwnedPlanets()
	{
		PlanetData[] planets = spaceModel.GetPlanetsData();
		int count = 0;
		if (planets != null)
		{
			foreach (PlanetData planet in planets)
			{
				if (planet.IsUnlocked)
				{
					count++;
				}
			}
		}

		return count;
	}

	/// <summary>Groups a number for a user property, such as "3-4" or "100+".</summary>
	private static string Bucket(int value, params int[] edges)
	{
		for (int i = edges.Length - 1; i >= 0; i--)
		{
			if (value >= edges[i])
			{
				if (i == edges.Length - 1)
				{
					return edges[i] + "+";
				}

				int upper = edges[i + 1] - 1;
				return upper == edges[i] ? edges[i].ToString() : $"{edges[i]}-{upper}";
			}
		}

		return "0";
	}
}
