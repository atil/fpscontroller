using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MouseLook
{
    private readonly Vector2 _pitchLimits;

    private const float Sensitivity = 2f;
    private readonly Transform _cam;
    private float _pitch;

    public MouseLook(Transform camTransform)
    {
        _cam = camTransform;
        _pitchLimits = new Vector2(-89f, 89f);
    }

    public Vector3 Update(float dt)
    {
        _pitch -= Sensitivity * Input.GetAxis("Mouse Y");
        _pitch = Mathf.Clamp(_pitch, _pitchLimits.x, _pitchLimits.y);
        
        var targetRot = Quaternion.Euler(_pitch, Sensitivity * Input.GetAxis("Mouse X"), 0f);
        _cam.localRotation = Quaternion.Slerp(_cam.localRotation, targetRot, dt * 200);

        return _cam.forward;
    }

    // Look at straight ahead to the given world position
    // TODO: Doesn't look straight ahead
    public void LookAt(Vector3 worldPos)
    {
        _pitch = 0; 
    }
}
