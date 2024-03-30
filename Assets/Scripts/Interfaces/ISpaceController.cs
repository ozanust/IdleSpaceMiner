using UnityEngine;
public interface ISpaceController
{
    void UnravelPlanet(int planetIndex);
    void UnlockPlanet(int planetIndex);
    void OpenPlanet(int planetIndex);
    void ClickPlanet(int planetIndex);
}
