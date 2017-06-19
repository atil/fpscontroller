using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FpsController : MonoBehaviour
{
    [SerializeField]
    private Transform _groundCheckRaysParent;

    public float GroundAccelerationCoeff = 500.0f;
    public float AirAccelCoeff = 1f;
    public float AirDecelCoeff = 1f;
    public float MaxSpeedAlongOneDimension = 15.0f;
    public float Friction = 30;
    public float FrictionSpeedThreshold = 1f; // Just stop if under this speed
    public float GroundedCheckRayLength = 0.2f;
    public float MouseSensitivity = 2.0f;
    public float JumpStrength = 10f;
    public float Gravity = 25f;

    private Transform _transform;
    private Vector3 _currentVelocity;
    private float _mousePitch;
    private float _mouseYaw;

    private bool _isGroundedInPrevFrame;
    private bool _isGonnaJump;

    void Start()
	{
        _transform = transform;
        Cursor.lockState = CursorLockMode.Locked;
	}

    void OnGUI()
    {
        var ups = _currentVelocity;
        ups.y = 0;
        GUI.Box(new Rect(Screen.width/2f-50, Screen.height / 2f + 50, 100, 40),  (Mathf.Round(ups.magnitude * 100) / 100).ToString());
    }

    void Update()
    {
	    var dt = Time.deltaTime;
        var moveInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        var isGroundedInThisFrame = IsGrounded();
        var justLanded = !_isGroundedInPrevFrame && isGroundedInThisFrame;
        _isGroundedInPrevFrame = isGroundedInThisFrame; 

        if (Input.GetKeyDown(KeyCode.Space) && !_isGonnaJump)
        {
            _isGonnaJump = true;
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            _isGonnaJump = false;
        }

	    var wishDir = _transform.TransformDirection(moveInput);
        if (isGroundedInThisFrame) // Ground move
        {
            _currentVelocity = Accelerate(_currentVelocity, wishDir, MaxSpeedAlongOneDimension, GroundAccelerationCoeff, dt);

            if (!justLanded)
            {
                var frictionCoeff = Friction;
                if (_isGonnaJump)
                {
                    frictionCoeff *= 0.5f;
                }
                _currentVelocity = ApplyFriction(_currentVelocity, frictionCoeff, dt);
            }

            _currentVelocity.y = 0;
            if (_isGonnaJump)
            {
                _isGonnaJump = false;
                _currentVelocity.y = JumpStrength;
            }
        }
        else // Air move
        {
            var airAccel = Vector3.Dot(_currentVelocity, wishDir) > 0 ? AirAccelCoeff : AirDecelCoeff;
            _currentVelocity = Accelerate(_currentVelocity, wishDir, MaxSpeedAlongOneDimension, airAccel, dt);
            _currentVelocity.y -= Gravity * dt;
        }

        _transform.position += _currentVelocity * dt;

        // TODO: Do pitch only on Camera object
        _mousePitch -= MouseSensitivity * Input.GetAxis("Mouse Y");
        _mouseYaw += MouseSensitivity * Input.GetAxis("Mouse X");
        _transform.rotation = Quaternion.Euler(_mousePitch, _mouseYaw, 0f);

        // Reset player
        if (Input.GetKeyDown(KeyCode.R))
        {
            _transform.position = Vector3.up * 1.5f;
            _currentVelocity = Vector3.forward;
        }
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

    private Vector3 Accelerate(Vector3 playerVelocity, Vector3 accelDir, float maxSpeedAlongOneDimension, float accelCoeff, float dt)
    {
        var projSpeed = Vector3.Dot(playerVelocity, accelDir);
        var accelAmount = accelCoeff * dt;

        if (projSpeed + accelAmount > maxSpeedAlongOneDimension)
        {
            accelAmount = maxSpeedAlongOneDimension - projSpeed;
        }

        return playerVelocity + accelDir * accelAmount;
    }

    private Vector3 ApplyFriction(Vector3 playerVelocity, float frictionCoeff, float dt)
    {
        var speed = playerVelocity.magnitude;
        if (speed <= 0.00001)
        {
            return playerVelocity;
        }

        var downLimit = Mathf.Max(speed, FrictionSpeedThreshold);
        var dropAmount = speed - (downLimit * frictionCoeff * dt);
        if (dropAmount < 0)
        {
            dropAmount = 0;
        }

        var dropRate = dropAmount / speed;
        return new Vector3(playerVelocity.x * dropRate, 0, playerVelocity.z * dropRate);
    }

}
