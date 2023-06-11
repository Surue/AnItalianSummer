using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WingRoll : MonoBehaviour
{
    [SerializeField] private float _maxAngle = 15;
    [SerializeField] private float _factor = 1;
    
    private PlaneController _planeController;
    private float _defaultXRot;
    
    private void Start()
    {
        _planeController = GetComponentInParent<PlaneController>();
        _defaultXRot = transform.localEulerAngles.x;
    }

    private void Update()
    {
        var rot = transform.localEulerAngles;
        transform.localEulerAngles = new Vector3(_defaultXRot + _planeController.Roll * _maxAngle * _factor, rot.y, rot.z);
    }
}
