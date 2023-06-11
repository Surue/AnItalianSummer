using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Floater : MonoBehaviour
{
    [SerializeField] private Rigidbody _body;   
    
    public float _depthBeforeSubmerged = 1f;
    public float _cubeVolume = 3f;
    public int _floaterCount = 1;
    public float _waterDrag = 0.99f;
    public float _waterAngularDrag = 0.5f;
    
    private void FixedUpdate() 
    {
        // _body.AddForceAtPosition(Physics.gravity / _floaterCount, transform.position, ForceMode.Acceleration);

        float waveHeight = WaterController.current.GetHeightAtPosition(transform.position);
        if (transform.position.y < waveHeight)
        {
            float displacementMultiplier = Mathf.Clamp01((waveHeight - transform.position.y) / _depthBeforeSubmerged) * _cubeVolume;
            _body.AddForceAtPosition(new Vector3(0f, Mathf.Abs(Physics.gravity.y) * displacementMultiplier, 0f), transform.position, ForceMode.Acceleration);
            _body.AddForce(displacementMultiplier * -_body.velocity * _waterDrag * Time.fixedDeltaTime, ForceMode.VelocityChange);
            _body.AddTorque(displacementMultiplier * -_body.angularVelocity * _waterAngularDrag * Time.fixedDeltaTime, ForceMode.VelocityChange);
            
            Debug.Log("Add force");
        }
    }
}
