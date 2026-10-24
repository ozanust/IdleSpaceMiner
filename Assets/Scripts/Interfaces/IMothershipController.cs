public interface IMothershipController
{
	int GalaxiesSold { get; }

	/// <summary>Money spent on the planets bought in this galaxy and their upgrades.</summary>
	long GetGalaxyValue();

	/// <summary>Dark Matter selling the galaxy now would give.</summary>
	int GetSellReward();

	bool CanSellGalaxy();

	/// <summary>Starts a new galaxy, keeping Dark Matter, rooms, managers and achievements.</summary>
	void SellGalaxy();

	/// <summary>Room levels to save.</summary>
	System.Collections.Generic.List<RoomSaveEntry> GetRoomSaveEntries();

	RoomSetting GetRoom(RoomType type);
	int GetRoomLevel(RoomType type);

	/// <summary>Dark Matter for the next level, or -1 at max level.</summary>
	int GetUpgradeCost(RoomType type);

	bool TryUpgradeRoom(RoomType type);

	/// <summary>Multiplier the room gives at its current level, 1 when not built.</summary>
	float GetMultiplier(RoomType type);
}
