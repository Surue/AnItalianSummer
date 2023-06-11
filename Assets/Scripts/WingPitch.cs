using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WingPitch : MonoBehaviour
{
    [SerializeField] private float _maxAngle = 15;
    
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
        transform.localEulerAngles = new Vector3(_defaultXRot + _planeController.Pitch * _maxAngle, rot.y, rot.z);
    }
}
