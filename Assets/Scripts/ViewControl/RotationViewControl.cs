using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationViewControl : ViewControl
{
    const KeyCode upKey = KeyCode.W;
    const KeyCode downKey = KeyCode.S;
    const KeyCode leftKey = KeyCode.A;
    const KeyCode rightKey = KeyCode.D;
    const KeyCode rotateLeft = KeyCode.Q;
    const KeyCode rotateRight = KeyCode.E;

    [SerializeField]
    Transform latitudeRotation, longitudeRotation;

    [SerializeField]
    ZoomViewControl zoomView;

    [SerializeField]
    float automaticRotationVelocityStep = 15f;

    [SerializeField]
    float manualRotationSpeed = 60f;

    [SerializeField]
    bool maxLatitude = true;

    Vector3 longitudeRotationVelocity;
    Vector3 latitudeRotationVelocity;
    Vector3 latitudeAngle;


    void Update()
    {
        if (Input.GetKeyDown(resetKey))
        {
            longitudeRotationVelocity = Vector3.zero;
            latitudeRotationVelocity = Vector3.zero;
            latitudeAngle = Vector3.zero;           
        }
        else if (Input.GetKeyDown(rotateLeft))
        {
            longitudeRotationVelocity.y += automaticRotationVelocityStep;
        }
        else if (Input.GetKeyDown(rotateRight))
        {
            longitudeRotationVelocity.y -= automaticRotationVelocityStep;
        }
        else if (Input.GetKeyDown(upKey))
        {
            latitudeRotationVelocity.x = -manualRotationSpeed;
        }
        else if (Input.GetKeyDown(downKey))
        {
            latitudeRotationVelocity.x = manualRotationSpeed;
        }
        else if (Input.GetKeyDown(leftKey))
        {
            longitudeRotationVelocity.y = -manualRotationSpeed;
        }
        else if (Input.GetKeyDown(rightKey))
        {
            longitudeRotationVelocity.y = manualRotationSpeed;
        }
        else if (Input.GetKeyUp(upKey))
        {
            if (latitudeRotationVelocity.x < 0)
            {
                latitudeRotationVelocity.x = 0;
            }
        }
        else if (Input.GetKeyUp(downKey))
        {
            if (latitudeRotationVelocity.x > 0)
            {
                latitudeRotationVelocity.x = 0;
            }
        }
        else if (Input.GetKeyUp(leftKey))
        {
            if (longitudeRotationVelocity.y < 0)
            {
                longitudeRotationVelocity.y = 0;
            }            
        }
        else if (Input.GetKeyUp(rightKey))
        {
            if (longitudeRotationVelocity.y > 0)
            {
                longitudeRotationVelocity.y = 0;
            }
        }

        float zoomMultiplier = zoomView == null ? 1 : Mathf.Pow(zoomView.ZoomStep, -zoomView.zoomLevel);
        longitudeRotation.rotation *= Quaternion.Euler(longitudeRotationVelocity * Time.deltaTime * zoomMultiplier);

        latitudeAngle += latitudeRotationVelocity * Time.deltaTime * zoomMultiplier;
        if (maxLatitude)
        {
            latitudeAngle.x = Mathf.Clamp(latitudeAngle.x, -90f, 90f);
        }       
        latitudeRotation.rotation = Quaternion.Euler(latitudeAngle);

    }
}
