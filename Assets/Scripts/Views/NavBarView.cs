using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class NavBarView : MonoBehaviour
{
    [Inject] SignalBus signalBus;
    [Inject] IPlayerModel playerModel;
    [Inject] IManagerController managerController;

    // TODO: Make those custom button and use their menu type property
    [SerializeField] private Button resourcesButton;
    [SerializeField] private Button productionButton;
    [SerializeField] private Button projectsButton;
    [SerializeField] private Button managersButton;
    [SerializeField] private Button boostsButton;
    [SerializeField] private Button mothershipButton;

    // Start is called before the first frame update
    void Start()
    {
        resourcesButton.onClick.AddListener(OnClickResources);
        productionButton.onClick.AddListener(OnClickProduction);
        projectsButton.onClick.AddListener(OnClickResearch);
        managersButton.onClick.AddListener(OnClickManagers);
        boostsButton.onClick.AddListener(OnClickBoosts);
        mothershipButton.onClick.AddListener(OnClickMothership);
        TutorialTarget.Mark(resourcesButton.gameObject, TutorialTargetId.ResourcesNavButton);
        TutorialTarget.Mark(productionButton.gameObject, TutorialTargetId.ProductionNavButton);
        TutorialTarget.Mark(projectsButton.gameObject, TutorialTargetId.ProjectsNavButton);

        // Managers open after the Management research, and stay open in later galaxies once hired
        managersButton.interactable = playerModel.IsResearchUnlocked(ResearchType.Management) || managerController.Managers.Count > 0;

        // The scene keeps it locked like the other upcoming menus, selling the galaxy is always available
        mothershipButton.interactable = true;

        // Locked in the scene like the other upcoming menus, ad boosts are always available
        boostsButton.interactable = true;

        signalBus.Subscribe<ResearchCompletedSignal>(OnResearchUnlocked);
    }

    private void OnDestroy()
    {
        signalBus.Unsubscribe<ResearchCompletedSignal>(OnResearchUnlocked);
    }

    private void OnClickResources()
	{
        signalBus.Fire(new MenuOpenSignal() { Type = MenuType.Resources });
	}

    private void OnClickProduction()
    {
        signalBus.Fire(new MenuOpenSignal() { Type = MenuType.Production });
    }

    private void OnClickResearch()
    {
        signalBus.Fire(new MenuOpenSignal() { Type = MenuType.Research });
    }

    private void OnClickManagers()
    {
        signalBus.Fire(new MenuOpenSignal() { Type = MenuType.Managers });
    }

    private void OnClickBoosts()
    {
        signalBus.Fire(new MenuOpenSignal() { Type = MenuType.Boosts });
    }

    private void OnClickMothership()
    {
        signalBus.Fire(new MenuOpenSignal() { Type = MenuType.Mothership });
    }

    private void OnResearchUnlocked(ResearchCompletedSignal signal)
    {
        if (signal.ResearchType == ResearchType.Smelter)
        {
            productionButton.interactable = true;
        }

        if (signal.ResearchType == ResearchType.Management)
        {
            managersButton.interactable = true;
        }
    }
}
