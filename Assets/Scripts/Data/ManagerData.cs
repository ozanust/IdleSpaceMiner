[System.Serializable]
public class ManagerData
{
	public int Id;
	public int Stars;
	public ManagerPrimary Primary;
	public ManagerSecondary Secondary;
	// -1 while not assigned
	public int PlanetId = -1;
}
