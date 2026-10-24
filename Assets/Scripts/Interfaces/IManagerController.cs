using System.Collections.Generic;

public interface IManagerController
{
	IReadOnlyList<ManagerData> Managers { get; }
	int Slots { get; }
	int AssignedCount { get; }
	int TotalHired { get; }
	bool FreeManagerGranted { get; }

	int GetNextSlotCost();
	bool TryBuySlot();
	bool TryHire(ManagerSchool school, out ManagerData hired);
	ManagerSchoolSetting GetSchool(ManagerSchool school);

	/// <summary>Puts the manager on the planet, replacing that planet's manager. Fails without a free slot.</summary>
	bool TryAssign(int managerId, int planetId);
	void Unassign(int managerId);
	ManagerData GetPlanetManager(int planetId);

	bool CanPromote(int managerId);
	bool TryPromote(int managerId);

	// Combined bonuses of assigned managers
	float GetMiningMultiplier(int planetId);
	float GetShipSpeedMultiplier(int planetId);
	float GetCargoMultiplier(int planetId);
	float SmeltSpeedMultiplier { get; }
	float CraftSpeedMultiplier { get; }

	string GetPrimaryText(ManagerData manager);
	string GetSecondaryText(ManagerData manager);
}
