using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AircraftPhysics : MonoBehaviour
{
    private const float PREDICTION_TIMESTEP_FRACTION = 0.5f;

    [SerializeField] private float _thrust = 0;
    [SerializeField] private List<AeroSurface> _aerodynamicSurfaces;

    private Rigidbody _body;
    private float _thrustPercent;
    private Vector3 _currentForce;
    private Vector3 _currentTorque;

    public void SetThrustPercent(float percent)
    {
        _thrustPercent = percent;
    }

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Vector3 frameForce;
        Vector3 frameTorque;

        var tmp = CalculateAerodynamicForces(_body.velocity, _body.angularVelocity, Vector3.zero, 1.2f, _body.worldCenterOfMass);

        frameForce = tmp.force;
        frameTorque = tmp.torque;

        Vector3 velocityPrediction = PredictVelocity(frameForce + transform.forward * _thrust * _thrustPercent + Physics.gravity * _body.mass);
        Vector3 angularVelocityPrediction = PredictAngularVelocity(frameTorque);
    }

    private (Vector3 force, Vector3 torque) CalculateAerodynamicForces(Vector3 velocity, Vector3 angularVelocity,
        Vector3 wind, float airDensity, Vector3 centerOfMass)
    {
        Vector3 force = new Vector3();
        Vector3 torque = new Vector3();

        foreach (var aerodynamicSurface in _aerodynamicSurfaces)
        {
            Vector3 relativePosition = aerodynamicSurface.transform.position - centerOfMass;
            var tmp = aerodynamicSurface.CalculateForces(-velocity + wind - Vector3.Cross(angularVelocity, relativePosition), airDensity, relativePosition);

            force += tmp.force;
            torque += tmp.torque;
        }
        
        return new ValueTuple<Vector3, Vector3>(force, torque);
    }
    
    private Vector3 PredictVelocity(Vector3 force)
    {
        return _body.velocity + Time.fixedDeltaTime * PREDICTION_TIMESTEP_FRACTION * force / _body.mass;
    }
    
    private Vector3 PredictAngularVelocity(Vector3 torque)
    {
        Quaternion inertiaTensorWorldRotation = _body.rotation * _body.inertiaTensorRotation;
        Vector3 torqueInDiagonalSpace = Quaternion.Inverse(inertiaTensorWorldRotation) * torque;
        Vector3 angularVelocityChangeInDiagonalSpace;
        angularVelocityChangeInDiagonalSpace.x = torqueInDiagonalSpace.x / _body.inertiaTensor.x;
        angularVelocityChangeInDiagonalSpace.y = torqueInDiagonalSpace.y / _body.inertiaTensor.y;
        angularVelocityChangeInDiagonalSpace.z = torqueInDiagonalSpace.z / _body.inertiaTensor.z;

        return _body.angularVelocity + Time.fixedDeltaTime * PREDICTION_TIMESTEP_FRACTION * (inertiaTensorWorldRotation * angularVelocityChangeInDiagonalSpace);
    }
    
#if UNITY_EDITOR
    // For gizmos drawing.
    public void CalculateCenterOfLift(out Vector3 center, out Vector3 force, Vector3 displayAirVelocity, float displayAirDensity)
    {
        Vector3 com;
        Vector3 tmpForce;
        Vector3 tmpTorque;
        if (_aerodynamicSurfaces == null)
        {
            center = Vector3.zero;
            force = Vector3.zero;
            return;
        }

        if (_body == null)
        {
            com = GetComponent<Rigidbody>().worldCenterOfMass;
            var tmp = CalculateAerodynamicForces(-displayAirVelocity, Vector3.zero, Vector3.zero, displayAirDensity, com);
            tmpForce = tmp.force;
            tmpTorque = tmp.torque;
        }
        else
        {
            com = _body.worldCenterOfMass;
            tmpForce = _currentForce;
            tmpTorque = _currentTorque;
        }

        force = tmpForce;
        center = com + Vector3.Cross(tmpForce, tmpTorque) / tmpForce.sqrMagnitude;
    }
#endif
}
