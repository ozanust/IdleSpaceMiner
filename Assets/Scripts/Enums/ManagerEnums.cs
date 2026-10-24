public enum ManagerSchool
{
	Public = 0,
	Private = 1,
	Brilliant = 2
}

/// <summary>A manager's main skill, applied to the planet they are assigned to.</summary>
public enum ManagerPrimary
{
	Mining = 0,
	ShipSpeed = 1,
	Cargo = 2
}

/// <summary>Skill of 3+ star managers that applies to everything while they are assigned.</summary>
public enum ManagerSecondary
{
	None = 0,
	AllMining = 1,
	AllShipSpeed = 2,
	AllCargo = 3,
	AllCraftSpeed = 4,
	AllSmeltSpeed = 5
}
