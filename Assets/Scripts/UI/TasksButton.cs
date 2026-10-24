using UnityEngine;
using UnityEngine.UI;
using Zenject;

/// <summary>HUD button that opens a goal list window (tasks or achievements), with a badge when something can be claimed.</summary>
public class TasksButton : MonoBehaviour
{
	[Inject] SignalBus signalBus;
	[Inject] ITaskController taskController;
	[Inject] IAchievementController achievementController;

	[SerializeField] private Button button;
	[SerializeField] private GameObject claimableBadge;
	[SerializeField] private MenuType type = MenuType.Tasks;

	private IClaimableList List => type == MenuType.Achievements ? (IClaimableList)achievementController : taskController;

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
		signalBus.Fire(new MenuOpenSignal() { Type = type });
	}

	private void OnTaskUpdated()
	{
		claimableBadge.SetActive(List.HasClaimableEntry());
	}
}
