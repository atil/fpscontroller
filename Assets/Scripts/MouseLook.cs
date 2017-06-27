using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MouseLook
{
    private readonly Vector2 _pitchLimits;

    private const float Sensitivity = 2f;
    private readonly Transform _body;
    private readonly Transform _cam;
    private float _pitch;
    private float _yaw;

    public MouseLook(Transform bodyTransform, Transform camTransform)
    {
        _body = bodyTransform;
        _cam = camTransform;
        _pitchLimits = new Vector2(-89f, 89f);
    }

    public void Update()
    {
        _pitch -= Sensitivity * Input.GetAxis("Mouse Y");
        _yaw += Sensitivity * Input.GetAxis("Mouse X");

        _pitch = Mathf.Clamp(_pitch, _pitchLimits.x, _pitchLimits.y);

        //_body.rotation = Quaternion.Euler(0f, _yaw, 0f);
        _cam.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }
}
