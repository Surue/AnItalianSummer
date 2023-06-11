using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneHelix : MonoBehaviour
{
    [SerializeField] private float _speedModifier = 10;
    
    private Rigidbody _body;
    
    private void Start()
    {
        _body = GetComponentInParent<Rigidbody>();
    }

    private void Update()
    {
        var vel = Mathf.Clamp(_body.velocity.magnitude, 0, 50);
        
        transform.Rotate(Vector3.forward, vel * Time.deltaTime * _speedModifier);
    }
}
