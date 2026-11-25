using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>One row in a goal list window (tasks, achievements): description, reward, progress and a claim button.</summary>
public class TaskItem : MonoBehaviour
{
	[SerializeField] private TMP_Text descriptionText;
	[SerializeField] private TMP_Text rewardText;
	[SerializeField] private TMP_Text progressText;
	[SerializeField] private Slider progressSlider;
	[SerializeField] private Button claimButton;

	private IClaimableList list;
	private int index;
	private TMP_Text claimLabel;

	public int TaskIndex => index;

	public void Init(IClaimableList list, int index)
	{
		this.list = list;
		this.index = index;
		claimLabel = claimButton.GetComponentInChildren<TMP_Text>(true);

		claimButton.onClick.RemoveAllListeners();
		claimButton.onClick.AddListener(OnClickClaim);

		descriptionText.text = list.GetDescription(index);
		rewardText.text = list.GetRewardText(index);
		Refresh();
	}

	public void Refresh()
	{
		bool claimed = list.IsClaimed(index);
		progressText.text = list.GetProgressText(index);
		progressSlider.value = list.GetProgressRatio(index);
		claimButton.interactable = !claimed && list.IsCompleted(index);
		if (claimLabel != null)
		{
			claimLabel.text = claimed ? "Claimed" : "Claim";
		}
	}

	private void OnClickClaim()
	{
		list.Claim(index);
	}
}
