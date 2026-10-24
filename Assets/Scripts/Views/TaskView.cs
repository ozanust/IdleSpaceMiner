using UnityEngine;
using UnityEngine.UI;
using Zenject;
using TMPro;

/// <summary>Shows the current task from the task chain with its progress and a claim button.</summary>
public class TaskView : MonoBehaviour
{
	[Inject] ITaskController taskController;
	[Inject] SignalBus signalBus;

	[SerializeField] private TMP_Text descriptionText;
	[SerializeField] private TMP_Text progressText;
	[SerializeField] private TMP_Text rewardText;
	[SerializeField] private Button claimButton;

	private void Awake()
	{
		signalBus.Subscribe<TaskUpdatedSignal>(Refresh);
		claimButton.onClick.AddListener(OnClickClaim);
	}

	private void OnDestroy()
	{
		signalBus.Unsubscribe<TaskUpdatedSignal>(Refresh);
	}

	private void Start()
	{
		Refresh();
	}

	private void OnClickClaim()
	{
		taskController.ClaimCurrentTask();
	}

	private void Refresh()
	{
		descriptionText.text = taskController.GetCurrentTaskDescription();
		progressText.text = taskController.GetCurrentTaskProgress();

		bool hasTask = !taskController.AreAllTasksCompleted;
		rewardText.gameObject.SetActive(hasTask);
		rewardText.text = NumberFormatter.FormatMoney(taskController.GetCurrentTaskReward());

		claimButton.gameObject.SetActive(hasTask);
		claimButton.interactable = taskController.IsCurrentTaskCompleted;
	}
}
