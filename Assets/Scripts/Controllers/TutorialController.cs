using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

/// <summary>
/// First time tutorial: a chain of steps that point at what to tap until the basics are covered.
/// Steps finish from game state where possible, so actions taken early or before an update
/// skip them. Only the reached step is saved.
/// </summary>
public class TutorialController : ITutorialController, IInitializable, ITickable, IDisposable
{
	private const float CheckInterval = 0.25f;

	private readonly SignalBus signalBus;
	private readonly IPlayerModel playerModel;
	private readonly ISpaceModel spaceModel;
	private readonly ISaveService saveService;
	private readonly ResourceSettings resourceSettings;
	private readonly ITaskController taskController;

	private readonly List<Step> steps = new List<Step>();
	private int stepIndex;
	private float nextCheck;

	// Actions that leave no trace in game state, reset when the step changes
	private bool planetOpened;
	private bool sold;
	private bool tasksOpened;
	private bool recipeAdded;
	private bool continued;

	public int StepIndex => stepIndex;

	public TutorialStepInfo CurrentStep { get; private set; }

	private class Step
	{
		public TutorialStepInfo Info;
		public Func<bool> IsDone;
		// The step waits hidden until this is true
		public Func<bool> IsReady = () => true;
	}

	public TutorialController(SignalBus signalBus, IPlayerModel playerModel, ISpaceModel spaceModel, ISaveService saveService,
		ResourceSettings resourceSettings, ITaskController taskController)
	{
		this.signalBus = signalBus;
		this.playerModel = playerModel;
		this.spaceModel = spaceModel;
		this.saveService = saveService;
		this.resourceSettings = resourceSettings;
		this.taskController = taskController;

		BuildSteps();
	}

	public void Initialize()
	{
		SaveData save = saveService.Load();
		if (save != null)
		{
			stepIndex = Mathf.Clamp(save.TutorialStep, 0, steps.Count);
		}

		signalBus.Subscribe<PlanetOpenSignal>(OnPlanetOpen);
		signalBus.Subscribe<ResourcesSellSignal>(OnSell);
		signalBus.Subscribe<MenuOpenSignal>(OnMenuOpen);
		signalBus.Subscribe<SmeltRecipeAddSignal>(OnRecipeAdded);
	}

	public void Dispose()
	{
		signalBus.Unsubscribe<PlanetOpenSignal>(OnPlanetOpen);
		signalBus.Unsubscribe<ResourcesSellSignal>(OnSell);
		signalBus.Unsubscribe<MenuOpenSignal>(OnMenuOpen);
		signalBus.Unsubscribe<SmeltRecipeAddSignal>(OnRecipeAdded);
	}

	private void BuildSteps()
	{
		Add("Welcome, Commander! Tap Balor to buy your first planet.",
			() => IsUnlocked(0), TutorialTargetId.FirstPlanet);

		Add("Balor is yours. Tap it to see what it mines.",
			() => planetOpened || GetMiningLevel(0) >= 2, TutorialTargetId.FirstPlanet);

		Add("Upgrade the mining rate so Balor mines faster.",
			() => GetMiningLevel(0) >= 2, TutorialTargetId.MiningUpgradeButton, TutorialTargetId.FirstPlanet);

		Add("Your cargo ship flies the ore to your mothership. Wait for the first delivery.",
			() => HasAny(MainResourceType.Ore) || IsUnlocked(1));

		Add("Open Resources, pick your ore and sell it for money.",
			() => sold || IsUnlocked(1),
			TutorialTargetId.SellButton, TutorialTargetId.CopperResource, TutorialTargetId.ResourcesNavButton);

		Add("Save up $200, then tap Drasta to buy your next planet.",
			() => IsUnlocked(1), TutorialTargetId.ResourcesCloseButton, TutorialTargetId.SecondPlanet);

		Add("Tasks give you rewards. Tap to see them.",
			() => tasksOpened || taskController.GetClaimedTasks().Length > 0, TutorialTargetId.TasksButton);

		// Waits until the player can afford the Smelter, mining enough ore takes a while
		Step smelter = Add("You have enough ore to research the Smelter. Open Projects!",
			() => playerModel.IsResearchUnlocked(ResearchType.Smelter),
			TutorialTargetId.ResearchButton, TutorialTargetId.SmelterResearch, TutorialTargetId.ProjectsNavButton);
		smelter.IsReady = () => playerModel.HasResources(resourceSettings.GetResearchSetting(ResearchType.Smelter).NeededResources);

		Add("Build a smelter in Production and smelt copper ore into bars. Bars sell for more!",
			() => recipeAdded || HasAny(MainResourceType.Alloy),
			TutorialTargetId.CopperBarRecipe, TutorialTargetId.SmelterRecipeButton, TutorialTargetId.SmelterUnlockButton, TutorialTargetId.ProductionNavButton);

		Step finish = Add("That's the basics! Keep upgrading planets, unlock new ones and research projects to grow your empire.",
			() => continued);
		finish.Info.HasContinue = true;
	}

	private Step Add(string text, Func<bool> isDone, params TutorialTargetId[] targets)
	{
		Step step = new Step
		{
			Info = new TutorialStepInfo { Text = text, Targets = targets },
			IsDone = isDone
		};
		steps.Add(step);
		return step;
	}

	public void Tick()
	{
		if (Time.unscaledTime < nextCheck)
		{
			return;
		}

		nextCheck = Time.unscaledTime + CheckInterval;
		Evaluate();
	}

	private void Evaluate()
	{
		// The space model is empty until the game has loaded or started
		if (spaceModel.GetPlanetsData() == null)
		{
			CurrentStep = null;
			return;
		}

		while (stepIndex < steps.Count && steps[stepIndex].IsDone())
		{
			Advance();
		}

		CurrentStep = stepIndex < steps.Count && steps[stepIndex].IsReady() ? steps[stepIndex].Info : null;
	}

	private void Advance()
	{
		stepIndex++;
		planetOpened = false;
		sold = false;
		tasksOpened = false;
		recipeAdded = false;
		continued = false;
	}

	public void Continue()
	{
		continued = true;
		Evaluate();
	}

	public void Skip()
	{
		stepIndex = steps.Count;
		CurrentStep = null;
	}

	private bool IsUnlocked(int planetId)
	{
		PlanetData data;
		return spaceModel.TryGetPlanetData(planetId, out data) && data.IsUnlocked;
	}

	private int GetMiningLevel(int planetId)
	{
		PlanetData data;
		return spaceModel.TryGetPlanetData(planetId, out data) && data.IsUnlocked ? data.MiningRateLevel : 0;
	}

	private bool HasAny(MainResourceType kind)
	{
		foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
		{
			if (resourceSettings.GetResourceParentType(type) == kind && playerModel.GetResource(type) > 0)
			{
				return true;
			}
		}

		return false;
	}

	private void OnPlanetOpen(PlanetOpenSignal signal)
	{
		planetOpened |= signal.PlanetId == 0;
	}

	private void OnSell()
	{
		sold = true;
	}

	private void OnMenuOpen(MenuOpenSignal signal)
	{
		tasksOpened |= signal.Type == MenuType.Tasks;
	}

	private void OnRecipeAdded()
	{
		recipeAdded = true;
	}
}
