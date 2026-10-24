using UnityEngine;
using UnityEngine.UI;
using Zenject;

/// <summary>Button that opens or closes a menu window.</summary>
public class MenuOpenButton : MonoBehaviour
{
	[Inject] SignalBus signalBus;

	[SerializeField] private Button button;
	[SerializeField] private MenuType type;

	private void Awake()
	{
		button.onClick.AddListener(() => signalBus.Fire(new MenuOpenSignal() { Type = type }));
	}
}
