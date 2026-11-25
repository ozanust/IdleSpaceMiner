using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

/// <summary>
/// Pinch or mouse wheel to zoom, drag with one finger or the mouse to pan.
/// The starting view shows the whole map, so it is the zoom-out limit and panning
/// keeps the camera inside it. Gestures that start on UI are ignored.
/// </summary>
public class CameraView : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [Inject] private IInputService inputService;

    [Tooltip("Field of view when fully zoomed in")]
    [SerializeField] private float minFieldOfView = 50f;
    [Tooltip("Zoom change per mouse wheel step")]
    [SerializeField] private float scrollZoomStep = 0.1f;
    [Tooltip("Space kept around the outermost visible planets when panning")]
    [SerializeField] private float panMargin = 6f;

    private float maxFieldOfView;
    // Camera distance at the start; zooming out further than the starting view pulls the camera back
    private float startDistance;
    private Vector3 startPosition;

    private bool isPointerDown;
    private bool isDragging;
    private bool isGestureOnUI;
    private Vector2 pressPosition;
    private Vector2 lastPointerPosition;

    private bool isPinching;
    private float lastPinchDistance;
    private Vector2 lastPinchCenter;

    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    // Planets decide how far the camera may pan, hidden ones don't count until a telescope reveals them
    private PlanetSpaceView[] planets;
    private Rect panArea;
    private float nextPanAreaUpdate;
    private const float PanAreaUpdateInterval = 0.5f;

    // Distance from the camera to the plane the planets are on (z = 0)
    private float PlaneDistance => Mathf.Abs(cam.transform.position.z);

    void Start()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        maxFieldOfView = cam.fieldOfView;
        startDistance = PlaneDistance;
        startPosition = cam.transform.position;
        planets = FindObjectsByType<PlanetSpaceView>(FindObjectsInactive.Include);
        inputService.OnScroll += OnScroll;
    }

    private void OnDestroy()
    {
        inputService.OnScroll -= OnScroll;
    }

    private void Update()
    {
        if (Input.touchCount >= 2)
        {
            HandlePinch(Input.GetTouch(0), Input.GetTouch(1));
        }
        else if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            // Lifting one finger of a pinch shouldn't turn into a drag
            if (isPinching)
            {
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    isPinching = false;
                }

                return;
            }

            HandlePointer(touch.position, touch.phase == TouchPhase.Began, touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled);
        }
        else if (Input.touchSupported)
        {
            isPinching = false;
            ResetPointer();
        }
        else
        {
            HandlePointer(Input.mousePosition, Input.GetMouseButtonDown(0), Input.GetMouseButtonUp(0));
        }
    }

    /// <summary>
    /// Only called while scrolling the mouse wheel
    /// </summary>
    /// <param name="scrollAxis">Determines the movement axis of mouse wheel, 1 for forward, -1 for backward.</param>
    private void OnScroll(float scrollAxis)
	{
        if (IsOverUI(Input.mousePosition))
        {
            return;
        }

        float factor = scrollAxis > 0 ? 1f - scrollZoomStep : 1f + scrollZoomStep;
        ZoomAround(Input.mousePosition, factor);
    }

    private void HandlePointer(Vector2 position, bool down, bool up)
    {
        if (down)
        {
            isPointerDown = true;
            isDragging = false;
            isGestureOnUI = IsOverUI(position);
            pressPosition = position;
            lastPointerPosition = position;
            return;
        }

        if (!isPointerDown)
        {
            return;
        }

        if (!isGestureOnUI)
        {
            // Small movements are taps, so planets and asteroids stay clickable
            if (!isDragging && (position - pressPosition).magnitude > GetDragThreshold())
            {
                isDragging = true;
            }

            if (isDragging)
            {
                Pan(position - lastPointerPosition);
            }
        }

        lastPointerPosition = position;

        if (up)
        {
            ResetPointer();
        }
    }

    private void HandlePinch(Touch first, Touch second)
    {
        Vector2 center = (first.position + second.position) / 2f;
        float distance = Vector2.Distance(first.position, second.position);

        if (!isPinching || first.phase == TouchPhase.Began || second.phase == TouchPhase.Began)
        {
            isPinching = true;
            ResetPointer();
            isGestureOnUI = IsOverUI(first.position) || IsOverUI(second.position);
            lastPinchDistance = distance;
            lastPinchCenter = center;
            return;
        }

        if (!isGestureOnUI && distance > 0f)
        {
            ZoomAround(center, lastPinchDistance / distance);
            Pan(center - lastPinchCenter);
        }

        lastPinchDistance = distance;
        lastPinchCenter = center;
    }

    private void ResetPointer()
    {
        isPointerDown = false;
        isDragging = false;
        isGestureOnUI = false;
    }

    /// <summary>Moves the camera so the map follows the finger.</summary>
    private void Pan(Vector2 screenDelta)
    {
        Vector3 move = -(Vector3)screenDelta * GetWorldUnitsPerPixel(VisibleHalfHeight);
        cam.transform.position += move;
        ClampPosition();
    }

    /// <summary>Scales the visible area by factor (below 1 zooms in) while keeping the point under the screen position in place.</summary>
    private void ZoomAround(Vector2 screenPosition, float factor)
    {
        float minHalfHeight = startDistance * Mathf.Tan(minFieldOfView * 0.5f * Mathf.Deg2Rad);
        float before = VisibleHalfHeight;
        float after = Mathf.Clamp(before * factor, minHalfHeight, GetMaxHalfHeight());

        Vector2 fromCenter = screenPosition - new Vector2(Screen.width, Screen.height) * 0.5f;
        SetVisibleHalfHeight(after);
        cam.transform.position += (Vector3)(fromCenter * (GetWorldUnitsPerPixel(before) - GetWorldUnitsPerPixel(after)));
        ClampPosition();
    }

    // Half the height of the visible part of the planet plane
    private float VisibleHalfHeight => PlaneDistance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);

    /// <summary>
    /// Up to the starting view this narrows or widens the field of view as before; past it the field of
    /// view stays at the start and the camera moves back, which avoids the stretching of a very wide view.
    /// </summary>
    private void SetVisibleHalfHeight(float halfHeight)
    {
        float startHalfHeight = startDistance * Mathf.Tan(maxFieldOfView * 0.5f * Mathf.Deg2Rad);
        Vector3 position = cam.transform.position;
        if (halfHeight <= startHalfHeight)
        {
            position.z = -startDistance;
            cam.fieldOfView = 2f * Mathf.Atan(halfHeight / startDistance) * Mathf.Rad2Deg;
        }
        else
        {
            cam.fieldOfView = maxFieldOfView;
            position.z = -halfHeight / Mathf.Tan(maxFieldOfView * 0.5f * Mathf.Deg2Rad);
        }

        cam.transform.position = position;
    }

    // Fully zoomed out shows every visible planet
    private float GetMaxHalfHeight()
    {
        UpdatePanArea();
        return Mathf.Max(panArea.height * 0.5f, panArea.width * 0.5f / cam.aspect);
    }

    /// <summary>Keeps the view inside the area around the visible planets, at least the starting view.</summary>
    private void ClampPosition()
    {
        if (Time.unscaledTime >= nextPanAreaUpdate)
        {
            nextPanAreaUpdate = Time.unscaledTime + PanAreaUpdateInterval;
            UpdatePanArea();
        }

        float halfHeight = VisibleHalfHeight;
        float halfWidth = halfHeight * cam.aspect;

        Vector3 position = cam.transform.position;
        position.x = ClampAxis(position.x, panArea.xMin + halfWidth, panArea.xMax - halfWidth);
        position.y = ClampAxis(position.y, panArea.yMin + halfHeight, panArea.yMax - halfHeight);
        cam.transform.position = position;
    }

    // Centres the view when the area is narrower than it
    private static float ClampAxis(float value, float min, float max)
    {
        return min > max ? (min + max) * 0.5f : Mathf.Clamp(value, min, max);
    }

    private void UpdatePanArea()
    {
        // The fully zoomed out starting view is always reachable
        float maxHalfHeight = startDistance * Mathf.Tan(maxFieldOfView * 0.5f * Mathf.Deg2Rad);
        float maxHalfWidth = maxHalfHeight * cam.aspect;
        Rect area = Rect.MinMaxRect(startPosition.x - maxHalfWidth, startPosition.y - maxHalfHeight, startPosition.x + maxHalfWidth, startPosition.y + maxHalfHeight);

        foreach (PlanetSpaceView planet in planets)
        {
            SpriteRenderer sprite = planet != null ? planet.GetComponent<SpriteRenderer>() : null;
            if (sprite == null || !sprite.enabled)
            {
                continue;
            }

            Vector3 p = planet.transform.position;
            float reach = sprite.bounds.extents.x + panMargin;
            area.xMin = Mathf.Min(area.xMin, p.x - reach);
            area.xMax = Mathf.Max(area.xMax, p.x + reach);
            area.yMin = Mathf.Min(area.yMin, p.y - reach);
            area.yMax = Mathf.Max(area.yMax, p.y + reach);
        }

        panArea = area;
    }

    private static float GetWorldUnitsPerPixel(float halfHeight)
    {
        return 2f * halfHeight / Screen.height;
    }

    private float GetDragThreshold()
    {
        return EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 10f;
    }

    /// <summary>True when the position is over a UI element (planets don't count, they're clickable world objects).</summary>
    private bool IsOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = screenPosition };
        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            if (result.module is GraphicRaycaster)
            {
                return true;
            }
        }

        return false;
    }
}
