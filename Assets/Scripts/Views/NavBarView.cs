using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class NavBarView : MonoBehaviour
{
    [Inject] SignalBus signalBus;

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
}
