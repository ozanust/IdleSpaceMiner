public interface ITaskController
{
	int CurrentTaskIndex { get; }
	int TaskCount { get; }
	bool AreAllTasksCompleted { get; }
	bool IsCurrentTaskCompleted { get; }
	string GetCurrentTaskDescription();
	string GetCurrentTaskProgress();
	float GetCurrentTaskProgressRatio();
	long GetCurrentTaskReward();
	void ClaimCurrentTask();
}
