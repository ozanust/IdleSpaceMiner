public class SmeltRecipeAddSignal
{
	public int SmelterId;
	// Restoring a save: the batch had already taken its ingredients and was this far along
	public bool InProgress;
	public float Progress;
	public AlloyType RecipeType;
	public ResourceType ItemRecipeType;
}
