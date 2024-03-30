using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class CameraView : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [Inject] private IInputService inputService;

    public float zoomSpeed = 10.0f;
    public float minFOV = 10.0f;
    public float maxFOV = 60.0f;

    // Start is called before the first frame update
    void Start()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        inputService.OnScroll += OnScroll;
    }

    /// <summary>
    /// Only called while scrolling the mouse wheel
    /// </summary>
    /// <param name="scrollAxis">Determines the movement axis of mouse wheel, 1 for forward, -1 for backward.</param>
    private void OnScroll(float scrollAxis)
	{
        float newFOV = cam.fieldOfView - (scrollAxis * zoomSpeed);

        // Clamp the field of view to stay within the defined min and max values
        newFOV = Mathf.Clamp(newFOV, minFOV, maxFOV);

        LeanTween.value(gameObject, cam.fieldOfView, newFOV, 0.3f)
            .setOnUpdate((float value) =>
            {
                cam.fieldOfView = value;
            }).setEase(LeanTweenType.easeOutSine);
    }
}
