public interface ITaskController
{
	int CurrentTaskIndex { get; }
	bool AreAllTasksCompleted { get; }
	bool IsCurrentTaskCompleted { get; }
	string GetCurrentTaskDescription();
	string GetCurrentTaskProgress();
	long GetCurrentTaskReward();
	void ClaimCurrentTask();
}
