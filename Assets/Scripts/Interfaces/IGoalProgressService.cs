public interface IGoalProgressService
{
	bool IsCompleted(TaskSetting goal);
	float GetProgressRatio(TaskSetting goal);
	string GetProgressText(TaskSetting goal);
	string GetDescription(TaskSetting goal);
}
