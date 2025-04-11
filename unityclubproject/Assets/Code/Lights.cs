using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightController2D : MonoBehaviour
{
    [Header("Lights To Control")]
    public List<Light2D> controlledLights;

    public void SetLights(bool state)
    {
        foreach (Light2D light in controlledLights)
        {
            if (light != null)
                light.enabled = state;
        }
    }
}
