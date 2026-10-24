[System.Serializable]
public enum ResourceType : int
{
	// Appended in tiers because saves and settings store the numbers, so ranges don't
	// tell ores, bars and items apart; use ResourceSettings.GetResourceParentType for that
	Copper = 0,
	Iron = 1,
	Lead = 2,
	Silica = 3,
	Aluminum = 4,
	CopperBar = 5,
	IronBar = 6,
	LeadBar = 7,
	SilicaBar = 8,
	AluminumBar = 9,
	CopperWire = 10,
	IronNail = 11,
	Battery = 12,
	Hammer = 13,
	Glass = 14,

	// Telescope 3-4 tier
	Silver = 15,
	Gold = 16,
	SilverBar = 17,
	GoldBar = 18,
	BronzeBar = 19,
	SteelBar = 20,
	Circuit = 21,
	Lens = 22,
	Laser = 23,
	BasicComputer = 24,
	SolarPanel = 25,
	LaserTorch = 26
}
