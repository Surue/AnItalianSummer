using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ControlInputType
{
    Pitch,
    Yaw,
    Roll,
    Flap
}

public class AeroSurface : MonoBehaviour
{
    [SerializeField] private AeroSurfaceSO _config = null;
    public bool IsControlSurface;
    public ControlInputType InputType;
    public float InputMultiplyer = 1;

    private float flapAngle;

    public void SetFlapAngle(float angle)
    {
        flapAngle = Mathf.Clamp(angle, -Mathf.Deg2Rad * 50.0f, Mathf.Deg2Rad * 50);
    }

    public (Vector3 force, Vector3 torque) CalculateForces(Vector3 worldAirVelocity, float airDensity, Vector3 relativePosition)
    {
        Vector3 force = new Vector3();
        Vector3 torque = new Vector3();

        if (!gameObject.activeInHierarchy || _config == null)
        {
            return new ValueTuple<Vector3, Vector3>(force, torque);
        }

        float correctedLiftSlop = _config.liftSlope * _config.aspectRatio /
                                 (_config.aspectRatio + 2 * (_config.aspectRatio + 4) / (_config.aspectRatio + 2));

        float theta = Mathf.Acos(2 * _config.flapFraction - 1);
        float flapEffectiveness = 1 - (theta - Mathf.Sin(theta)) / Mathf.PI;
        float deltaLift = correctedLiftSlop * flapEffectiveness * FlapEffectivnessCorrection(flapAngle) * flapAngle;

        float zeroLiftAngleOfAttackBase = _config.zeroLiftAngleOfAttack * Mathf.Deg2Rad;
        float zeroLiftAngleOfAttack = zeroLiftAngleOfAttackBase - deltaLift / correctedLiftSlop;

        float stallAngleHighBase = _config.stallAngleHigh * Mathf.Deg2Rad;
        float stallAngleLowBase = _config.stallAngleLow * Mathf.Deg2Rad;

        float clMaxHigh = correctedLiftSlop * (stallAngleHighBase - zeroLiftAngleOfAttackBase) + deltaLift * LiftCoefficientMaxFraction(_config.flapFraction);
        float clMaxLow = correctedLiftSlop * (stallAngleLowBase - zeroLiftAngleOfAttackBase) + deltaLift * LiftCoefficientMaxFraction(_config.flapFraction);

        float stallAngleHigh = zeroLiftAngleOfAttack + clMaxHigh / correctedLiftSlop;
        float stallAngleLow = zeroLiftAngleOfAttack + clMaxLow / correctedLiftSlop;

        Vector3 airVelocity = transform.InverseTransformDirection(worldAirVelocity);
        airVelocity = new Vector3(airVelocity.x, airVelocity.y);
        Vector3 dragDirection = transform.TransformDirection(airVelocity.normalized);
        Vector3 liftDirection = Vector3.Cross(dragDirection, transform.forward);

        float area = _config.chord * _config.span;
        float dynamicPressure = 0.5f * airDensity * airVelocity.sqrMagnitude;
        float angleOfAttack = Mathf.Atan2(airVelocity.y, -airVelocity.x);

        Vector3 aerodynamicCoefficients = CalculateCoefficients(angleOfAttack, correctedLiftSlop, zeroLiftAngleOfAttack, stallAngleHigh, stallAngleLow);

        Vector3 lift = liftDirection * aerodynamicCoefficients.x * dynamicPressure * area;
        Vector3 drag = dragDirection * aerodynamicCoefficients.y * dynamicPressure * area;
        Vector3 tmpTorque = -transform.forward * aerodynamicCoefficients.z * dynamicPressure * area * _config.chord;

        force += lift + drag;
        torque += Vector3.Cross(relativePosition, force);
        torque += tmpTorque;
        
#if UNITY_EDITOR
        IsAtStall = !(angleOfAttack < stallAngleHigh && angleOfAttack > stallAngleLow);
        CurrentLift = lift;
        CurrentDrag = drag;
        CurrentTorque = torque;
#endif
        
        return new ValueTuple<Vector3, Vector3>(force, torque);
    }

    private Vector3 CalculateCoefficients(float angleOfAttack, float correctedLiftSlop, float zeroLiftAngleOfAttack, float stallAngleHigh, float stallAngleLow)
    {
        Vector3 aerodynamicCoefficients;

        float paddingAngleHigh = Mathf.Deg2Rad * Mathf.Lerp(15, 5, (Mathf.Rad2Deg * flapAngle + 50) / 100);
        float paddingAngleLow = Mathf.Deg2Rad * Mathf.Lerp(15, 5, (-Mathf.Rad2Deg * flapAngle + 50) / 100);
        float paddedStallAngleHigh = stallAngleHigh + paddingAngleHigh;
        float paddedStallAngleLow = stallAngleLow - paddingAngleLow;

        if (angleOfAttack < stallAngleHigh && angleOfAttack > stallAngleLow)
        {
            aerodynamicCoefficients = CalculateCoefficientsAtLowAngleOfAttack(angleOfAttack, correctedLiftSlop, zeroLiftAngleOfAttack);
        }
        else
        {
            if (angleOfAttack > paddingAngleHigh || angleOfAttack < paddingAngleLow)
            {
                aerodynamicCoefficients = CalculateCoefficientsAtStall(angleOfAttack, correctedLiftSlop, zeroLiftAngleOfAttack, stallAngleHigh, stallAngleLow);
            }
            else
            {
                Vector3 aerodynamicCoefficientsLow;
                Vector3 aerodynamicCoefficientsStall;
                float lerpParam;

                if (angleOfAttack > stallAngleHigh)
                {
                    aerodynamicCoefficientsLow = CalculateCoefficientsAtLowAngleOfAttack(stallAngleHigh, correctedLiftSlop, zeroLiftAngleOfAttack);
                    aerodynamicCoefficientsStall = CalculateCoefficientsAtStall(paddedStallAngleHigh, correctedLiftSlop, zeroLiftAngleOfAttack, stallAngleHigh, stallAngleLow);
                    lerpParam = (angleOfAttack - stallAngleHigh) / (paddedStallAngleHigh - stallAngleHigh);
                }
                else
                {
                    aerodynamicCoefficientsLow = CalculateCoefficientsAtLowAngleOfAttack(stallAngleLow, correctedLiftSlop, zeroLiftAngleOfAttack);
                    aerodynamicCoefficientsStall = CalculateCoefficientsAtStall(paddedStallAngleLow, correctedLiftSlop, zeroLiftAngleOfAttack, stallAngleHigh, stallAngleLow);
                    lerpParam = (angleOfAttack - stallAngleHigh) / (paddedStallAngleHigh - stallAngleHigh);
                }

                aerodynamicCoefficients = Vector3.Lerp(aerodynamicCoefficientsLow, aerodynamicCoefficientsStall, lerpParam);
            }
        }

        return aerodynamicCoefficients;
    }

    private Vector3 CalculateCoefficientsAtLowAngleOfAttack(float angleOfAttack, float correctedLiftSlop, float zeroLiftAngleOfAttack)
    {
        float liftCoefficient = correctedLiftSlop * (angleOfAttack - zeroLiftAngleOfAttack);
        float inducedAngle = liftCoefficient / (Mathf.PI * _config.aspectRatio);
        float effectiveAngle = angleOfAttack - zeroLiftAngleOfAttack - inducedAngle;

        float tangentialCoefficient = _config.skinFriction * Mathf.Cos(effectiveAngle);

        float normalCoefficient = (liftCoefficient + Mathf.Sin(effectiveAngle) * tangentialCoefficient) / Mathf.Cos(effectiveAngle);
        float dragCoefficient = normalCoefficient * Mathf.Sin(effectiveAngle) + tangentialCoefficient * Mathf.Cos(effectiveAngle);
        float torqueCoefficient = -normalCoefficient * TorqueCoefficientProportion(effectiveAngle);

        return new Vector3(liftCoefficient, dragCoefficient, torqueCoefficient);
    }
    
    private Vector3 CalculateCoefficientsAtStall(float angleOfAttack, float correctedLiftSlop, float zeroLiftAngleOfAttack, float stallAngleHigh, float stallAngleLow)
    {
        float liftCoefficientLowAngleOfAttack;
        if (angleOfAttack > stallAngleHigh)
        {
            liftCoefficientLowAngleOfAttack = correctedLiftSlop * (stallAngleHigh - zeroLiftAngleOfAttack);
        }
        else
        {
            liftCoefficientLowAngleOfAttack = correctedLiftSlop * (stallAngleLow - zeroLiftAngleOfAttack);
        }

        float inducedAngle = liftCoefficientLowAngleOfAttack / (Mathf.PI * _config.aspectRatio);

        float lerpParam;
        if (angleOfAttack > stallAngleHigh)
        {
            lerpParam = (Mathf.PI / 2 - Mathf.Clamp(angleOfAttack, -Mathf.PI / 2, Mathf.PI / 2)) / (Mathf.PI / 2 - stallAngleHigh);
        }
        else
        {
            lerpParam = (-Mathf.PI / 2 - Mathf.Clamp(angleOfAttack, -Mathf.PI / 2, Mathf.PI / 2)) / (-Mathf.PI / 2 - stallAngleLow);
        }

        inducedAngle = Mathf.Lerp(0, inducedAngle, lerpParam);
        float effectiveAngle = angleOfAttack - zeroLiftAngleOfAttack - inducedAngle;

        float normalCoefficient = FrictionAt90Degrees(flapAngle) * Mathf.Sin(effectiveAngle) * (1 / (0.56f + 0.44f * Mathf.Abs(Mathf.Sin(effectiveAngle))) - 0.41f * (1 - Mathf.Exp(-17 / _config.aspectRatio)));
        float tangentialCoefficient = 0.5f * _config.skinFriction * Mathf.Cos(effectiveAngle);

        float liftCoefficient = normalCoefficient * Mathf.Cos(effectiveAngle) - tangentialCoefficient * Mathf.Sin(effectiveAngle);
        float dragCoefficient = normalCoefficient * Mathf.Sin(effectiveAngle) + tangentialCoefficient * Mathf.Cos(effectiveAngle);
        float torqueCoefficient = -normalCoefficient * TorqueCoefficientProportion(effectiveAngle);

        return new Vector3(liftCoefficient, dragCoefficient, torqueCoefficient);
    }

    private float TorqueCoefficientProportion(float effectiveAngle)
    {
        return 0.25f - 0.175f * (1 - 2 * Mathf.Abs(effectiveAngle) / Mathf.PI);
    }
    
    private float FrictionAt90Degrees(float flapAngle)
    {
        return 1.98f - 4.26e-2f * flapAngle * flapAngle + 2.1e-1f * flapAngle;
    }
    
    private float FlapEffectivnessCorrection(float f)
    {
        return Mathf.Lerp(0.8f, 0.4f, (Mathf.Abs(flapAngle) * Mathf.Rad2Deg - 10) / 50);
    }

    private float LiftCoefficientMaxFraction(float flapFraction)
    {
        return Mathf.Clamp01(1 - 0.5f * (flapFraction - 0.1f) / 0.3f);
    }
    
#if UNITY_EDITOR
    public AeroSurfaceSO Config => _config;
    public float GetFlapAngle() => flapAngle;
    public Vector3 CurrentLift { get; private set; }
    public Vector3 CurrentDrag { get; private set; }
    public Vector3 CurrentTorque { get; private set; }
    public bool IsAtStall { get; private set; }
#endif
}
