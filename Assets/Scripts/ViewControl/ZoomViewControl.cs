using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoomViewControl : ViewControl
{
    const KeyCode zoomOutKey = KeyCode.Minus;
    const KeyCode zoomInKey = KeyCode.Equals;

    [SerializeField]
    Camera mainCamera;

    [SerializeField]
    protected float zoomStep = 1.5f;

    public int zoomLevel { get; private set; }
    float defaultCameraSize;

    public float ZoomStep => zoomStep;

    private void Start()
    {
        defaultCameraSize = mainCamera.orthographicSize;
    }

    void Update()
    {
        if (Input.GetKeyDown(resetKey))
        {
            mainCamera.orthographicSize = defaultCameraSize;
            zoomLevel = 0;
        }
        else if (Input.GetKeyDown(zoomOutKey))
        {
            mainCamera.orthographicSize *= zoomStep;
            zoomLevel -= 1;
        }
        else if (Input.GetKeyDown(zoomInKey))
        {
            mainCamera.orthographicSize /= zoomStep;
            zoomLevel += 1;
        }
    }
}
