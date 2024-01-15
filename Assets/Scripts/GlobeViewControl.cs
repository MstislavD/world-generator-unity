using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HexGlobe))]
public class GlobeViewControl : ViewControl
{
    const KeyCode decreaseDataLevel = KeyCode.Z;
    const KeyCode increaseDataLevel = KeyCode.C;
    const KeyCode regenKey = KeyCode.R;
    const KeyCode cycleColoring = KeyCode.M;

    HexGlobe globe;

    private void Start()
    {
        globe = GetComponent<HexGlobe>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(resetKey))
        {
            globe.ResetColors();
        }
        else if (Input.GetKeyDown(regenKey))
        {
            globe.Regenerate();
        }
        else if (Input.GetKeyDown(decreaseDataLevel))
        {
            globe.ChangeDataLevel(-1);
        }
        else if (Input.GetKeyDown(increaseDataLevel))
        {
            globe.ChangeDataLevel(1);
        }
        else if (Input.GetKeyDown(cycleColoring))
        {
            globe.CycleColoring();
        }

        for (int i = 0; i < 10; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                globe.ChangeSphereLevel(i);
            }
        }
    }
}
