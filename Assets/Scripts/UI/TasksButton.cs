using UnityEngine;
using UnityEngine.UI;
using Zenject;

/// <summary>HUD button that opens the tasks window, with a badge when a task can be claimed.</summary>
public class TasksButton : MonoBehaviour
{
	[Inject] SignalBus signalBus;
	[Inject] ITaskController taskController;

	[SerializeField] private Button button;
	[SerializeField] private GameObject claimableBadge;

	private void Awake()
	{
		button.onClick.AddListener(OnClick);
		signalBus.Subscribe<TaskUpdatedSignal>(OnTaskUpdated);
	}

	private void OnDestroy()
	{
		signalBus.Unsubscribe<TaskUpdatedSignal>(OnTaskUpdated);
	}

	private void Start()
	{
		OnTaskUpdated();
	}

	private void OnClick()
	{
		signalBus.Fire(new MenuOpenSignal() { Type = MenuType.Tasks });
	}

	private void OnTaskUpdated()
	{
		claimableBadge.SetActive(taskController.HasClaimableTask());
	}
}
