using System.Collections.Generic;

/// <summary>A list of goals with rewards, shown in a list window (tasks, achievements).</summary>
public interface IClaimableList
{
	IReadOnlyList<int> GetVisibleEntries();
	bool HasClaimableEntry();
	bool IsCompleted(int index);
	bool IsClaimed(int index);
	void Claim(int index);
	string GetDescription(int index);
	string GetRewardText(int index);
	string GetProgressText(int index);
	float GetProgressRatio(int index);
}
