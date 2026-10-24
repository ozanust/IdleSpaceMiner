public interface ITutorialController
{
	/// <summary>The step to show now, or null while the tutorial waits or is finished.</summary>
	TutorialStepInfo CurrentStep { get; }

	/// <summary>Index of the next unfinished step, equal to the step count once finished.</summary>
	int StepIndex { get; }

	/// <summary>Confirms a step that only asks the player to read and continue.</summary>
	void Continue();

	void Skip();
}

/// <summary>What the tutorial overlay shows for a step.</summary>
public class TutorialStepInfo
{
	public string Text;

	// Pointed at in order: the first one on screen wins, so later entries lead the way to earlier ones
	public TutorialTargetId[] Targets = new TutorialTargetId[0];

	public bool HasContinue;
}
