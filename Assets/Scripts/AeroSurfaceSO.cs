using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "New Aerodynamic Surface Config", menuName = "Aerodynamic Surface Config")]
public class AeroSurfaceSO : ScriptableObject
{
    public float liftSlope = 6.28f;
    public float skinFriction = 0.02f;
    public float zeroLiftAngleOfAttack = 0.0f;
    public float stallAngleHigh = 15.0f;
    public float stallAngleLow = -15.0f;
    public float chord = 1;
    public float flapFraction = 0.0f;
    public float span = 1.0f;
    public bool autoAspectRation = true;
    public float aspectRatio = 2;

#if UNITY_EDITOR
    private void OnValidate()
    {
        bool hasChanged = false;
        // Flap fraction
        if (flapFraction > 0.4f)
        {
            flapFraction = 0.4f;
            hasChanged = true;
        }

        if (flapFraction < 0)
        {
            flapFraction = 0;
            hasChanged = true;
        }
        
        // Stall angles
        if (stallAngleHigh < 0)
        {
            stallAngleHigh = 0;
            hasChanged = true;
        }

        if (stallAngleLow > 0)
        {
            stallAngleLow = 0;
            hasChanged = true;
        }

        // Auto aspect
        if (autoAspectRation)
        {
            aspectRatio = span / chord;
            hasChanged = true;
        }
        
        // Dirty
        if (hasChanged)
        {
            EditorUtility.SetDirty(this);
        }
    }
#endif
}
