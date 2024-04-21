using UnityEngine;
public interface ISpaceModel
{
    void InitializePlanetData(PlanetData[] planetData);
    PlanetData[] GetPlanetsData();
    void UnravelPlanet(int planetId);
    void UnlockPlanet(int planetId);
    bool TryGetPlanetTransform(int planetId, out Transform transform);
    bool TryGetPlanetData(int planetId, out PlanetData data);
    public void UpdatePlanetMiningRate(int planetId);
    public void UpdatePlanetShipSpeed(int planetId);
    public void UpdatePlanetCargo(int planetId);
}
