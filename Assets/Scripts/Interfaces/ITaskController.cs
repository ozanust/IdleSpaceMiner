public interface ITaskController : IClaimableList
{
	bool AreAllTasksCompleted { get; }
	int[] GetClaimedTasks();
}
