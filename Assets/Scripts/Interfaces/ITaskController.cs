using System.Collections.Generic;

public interface ITaskController
{
	bool AreAllTasksCompleted { get; }
	IReadOnlyList<int> GetActiveTasks();
	int[] GetClaimedTasks();
	bool HasClaimableTask();
	bool IsTaskCompleted(int index);
	void ClaimTask(int index);
	long GetTaskReward(int index);
	string GetTaskDescription(int index);
	string GetTaskProgressText(int index);
	float GetTaskProgressRatio(int index);
}
