using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FpsController : MonoBehaviour
{
    [SerializeField]
    private Transform _groundCheckRaysParent;

    private const float GroundAccelerationCoeff = 500.0f;
    public float AirAccelCoeff = 5f;
    public float AirDecelCoeff = 5f;
    private const float MaxSpeedAlongOneDimension = 15.0f;
    private const float Friction = 30;
    private const float FrictionSpeedThreshold = 1f; // Just stop if under this speed
    private const float GroundedCheckRayLength = 0.2f;
    private const float MouseSensitivity = 2.0f;
    private const float JumpStrength = 10f;
    private const float Gravity = 25f;

    private Transform _transform;
    private Vector3 _currentVelocity;
    private float _mousePitch;
    private float _mouseYaw;

    void Start()
	{
        _transform = transform;
	}
	
    void Update()
    {
	    var dt = Time.deltaTime;
	    var inputX = Input.GetAxis("Horizontal");
	    var inputZ = Input.GetAxis("Vertical");

        var isGrounded = IsGrounded();

        // GetKey() allows just keeping on Space to jump repeatedly
        // May not be Q3 but even QL does it like this now
        var jumpQuery = Input.GetKey(KeyCode.Space) && isGrounded;

	    var wishDir = _transform.TransformDirection(new Vector3(inputX, 0, inputZ));
        if (isGrounded)
        {
            _currentVelocity = Accelerate(_currentVelocity, wishDir, GroundAccelerationCoeff, dt);
            _currentVelocity = ApplyFriction(_currentVelocity, dt);

            _currentVelocity.y = 0;
            if (jumpQuery)
            {
                _currentVelocity.y = JumpStrength;
            }
        }
        else
        {
            var airAccel = Vector3.Dot(_currentVelocity, wishDir) > 0 ? AirAccelCoeff : AirDecelCoeff;
            _currentVelocity = Accelerate(_currentVelocity, wishDir, airAccel, dt);

            _currentVelocity.y -= Gravity * dt;
        }

        _transform.position += _currentVelocity * dt;

        // TODO: Do pitch only on Camera object
        _mousePitch -= MouseSensitivity * Input.GetAxis("Mouse Y");
        _mouseYaw += MouseSensitivity * Input.GetAxis("Mouse X");
        _transform.rotation = Quaternion.Euler(_mousePitch, _mouseYaw, 0f);
    }

    private bool IsGrounded()
    {
        foreach (Transform t in _groundCheckRaysParent)
        {
            var ray = new Ray(t.position, Vector3.down);
            if (Physics.Raycast(ray, GroundedCheckRayLength))
            {
                return true;
            }
        }
        return false;
    }

    private Vector3 Accelerate(Vector3 playerVelocity, Vector3 accelDir, float accelCoeff, float dt)
    {
        var projSpeed = Vector3.Dot(playerVelocity, accelDir); // Vector projection of Current velocity onto accelDir.
        var accelAmount = accelCoeff * dt; // Accelerated velocity in direction of movment

        // If necessary, truncate the accelerated velocity so the vector projection does not exceed max_velocity
        if (projSpeed + accelAmount > MaxSpeedAlongOneDimension)
        {
            accelAmount = MaxSpeedAlongOneDimension - projSpeed;
        }

        return playerVelocity + accelDir * accelAmount;
    }

    private Vector3 ApplyFriction(Vector3 playerVelocity, float dt)
    {
        var speed = playerVelocity.magnitude;
        if (speed <= 0.00001)
        {
            return playerVelocity;
        }

        var frictionCoeff = Mathf.Max(speed, FrictionSpeedThreshold);
        var dropAmount = speed - (frictionCoeff * Friction * dt);
        if (dropAmount < 0)
        {
            dropAmount = 0;
        }

        var dropRate = dropAmount / speed;

        return new Vector3(playerVelocity.x * dropRate, 0, playerVelocity.z * dropRate);
    }

}
