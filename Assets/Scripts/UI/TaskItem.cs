using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>One row in the tasks window: description, reward, progress and a claim button.</summary>
public class TaskItem : MonoBehaviour
{
	[SerializeField] private TMP_Text descriptionText;
	[SerializeField] private TMP_Text rewardText;
	[SerializeField] private TMP_Text progressText;
	[SerializeField] private Slider progressSlider;
	[SerializeField] private Button claimButton;

	private ITaskController taskController;
	private int taskIndex;

	public int TaskIndex => taskIndex;

	public void Init(ITaskController taskController, int taskIndex)
	{
		this.taskController = taskController;
		this.taskIndex = taskIndex;

		claimButton.onClick.RemoveAllListeners();
		claimButton.onClick.AddListener(OnClickClaim);

		descriptionText.text = taskController.GetTaskDescription(taskIndex);
		rewardText.text = "Reward: " + NumberFormatter.FormatMoney(taskController.GetTaskReward(taskIndex));
		Refresh();
	}

	public void Refresh()
	{
		progressText.text = taskController.GetTaskProgressText(taskIndex);
		progressSlider.value = taskController.GetTaskProgressRatio(taskIndex);
		claimButton.interactable = taskController.IsTaskCompleted(taskIndex);
	}

	private void OnClickClaim()
	{
		taskController.ClaimTask(taskIndex);
	}
}
