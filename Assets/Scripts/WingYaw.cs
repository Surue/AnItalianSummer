using UnityEngine;

public class WingYaw : MonoBehaviour
{
    [SerializeField] private float _maxAngle = 15;
    
    private PlaneController _planeController;
    private float _defaultYRot;
    
    private void Start()
    {
        _planeController = GetComponentInParent<PlaneController>();
        _defaultYRot = transform.localEulerAngles.y;
    }

    private void Update()
    {
        var rot = transform.localEulerAngles;
        transform.localEulerAngles = new Vector3(rot.x, _defaultYRot + _planeController.Yaw * _maxAngle, rot.z);
    }
}

