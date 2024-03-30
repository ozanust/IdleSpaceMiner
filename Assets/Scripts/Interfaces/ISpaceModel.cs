using UnityEngine;
public interface ISpaceModel
{
    void InitializePlanetData(PlanetData[] planetData);
    PlanetData[] GetPlanetsData();
    void UnravelPlanet(int planetId);
    void UnlockPlanet(int planetId);
    bool TryGetPlanetTransform(int planetId, out Transform transform);
    bool TryGetPlanetData(int planetId, out PlanetData data);
}
