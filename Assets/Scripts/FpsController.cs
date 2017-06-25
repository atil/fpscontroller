using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Video;

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
    public float JumpStrength = 10f;
    public float Gravity = 25f;
    public float AirControlPrecision = 8f;

    private MouseLook _mouseLook;
    private Transform _transform;
    private Vector3 _currentVelocity;

    private bool _isGroundedInPrevFrame;
    private bool _isGonnaJump;
    private readonly Collider[] _overlappingColliders = new Collider[] {};

    void Start()
	{
        _transform = transform;
        Cursor.lockState = CursorLockMode.Locked;
        Application.targetFrameRate = 60;

        _mouseLook = new MouseLook(_transform, Camera.main.transform);
	}

    void OnGUI()
    {
        var ups = _currentVelocity;
        ups.y = 0;
        GUI.Box(new Rect(Screen.width/2f-50, Screen.height / 2f + 50, 100, 40),  (Mathf.Round(ups.magnitude * 100) / 100).ToString());

        var mid = new Vector2(Screen.width / 2f, Screen.height / 2f);
        var v = _transform.InverseTransformVector(_currentVelocity * 10);
        if (v.WithY(0).magnitude > 0)
        {
            Drawing.DrawLine(mid, mid + Vector2.up * -v.z + Vector2.right * v.x, Color.red, 3f);
        }
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
            Accelerate(ref _currentVelocity, wishDir, MaxSpeedAlongOneDimension, GroundAccelerationCoeff, dt);

            if (!justLanded) // Don't apply friction if just landed
            {
                var frictionCoeff = Friction;
                if (_isGonnaJump) // Apply half friction just before the jump
                {
                    frictionCoeff *= 0.5f;
                }
                ApplyFriction(ref _currentVelocity, frictionCoeff, dt);
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
            Accelerate(ref _currentVelocity, wishDir, MaxSpeedAlongOneDimension, airAccel, dt);

            if (Mathf.Abs(moveInput.z) > 0.0001)
            {
                ApplyAirControl(ref _currentVelocity, wishDir);
            }

            _currentVelocity.y -= Gravity * dt;
        }

        _transform.position += _currentVelocity * dt;

        ResolveCollisions(ref _currentVelocity);

        _mouseLook.Update();

        // Reset player
        if (Input.GetKeyDown(KeyCode.R))
        {
            _transform.position = Vector3.up * 1.5f;
            _currentVelocity = Vector3.forward;
        }

        //Debug.Log(_currentVelocity + " == " + _currentVelocity.magnitude);

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

    private void Accelerate(ref Vector3 playerVelocity, Vector3 accelDir, float maxSpeedAlongOneDimension, float accelCoeff, float dt)
    {
        var projSpeed = Vector3.Dot(playerVelocity, accelDir);
        var addSpeed = maxSpeedAlongOneDimension - projSpeed;
        if (addSpeed <= 0)
        {
            return;
        }

        var accelAmount = accelCoeff * maxSpeedAlongOneDimension * dt;
        if (accelAmount > addSpeed)
        {
            accelAmount = addSpeed;
        }

        playerVelocity += accelDir * accelAmount;
    }

    private void ApplyFriction(ref Vector3 playerVelocity, float frictionCoeff, float dt)
    {
        var speed = playerVelocity.magnitude;
        if (speed <= 0.00001)
        {
            return;
        }

        var downLimit = Mathf.Max(speed, FrictionSpeedThreshold);
        var dropAmount = speed - (downLimit * frictionCoeff * dt);
        if (dropAmount < 0)
        {
            dropAmount = 0;
        }

        var dropRate = dropAmount / speed;
        playerVelocity *= dropRate;
    }

    private void ApplyAirControl(ref Vector3 playerVelocity, Vector3 accelDir)
    {
        var playerDirHorz = playerVelocity.WithY(0).normalized;
        var playerSpeedHorz = playerVelocity.WithY(0).magnitude;

        var dot = Vector3.Dot(playerDirHorz, accelDir);
        if (dot > 0)
        {
            var k = AirControlPrecision * dot * dot * Time.deltaTime;
            playerDirHorz = playerDirHorz * playerSpeedHorz + accelDir * k;
            playerDirHorz.Normalize();

            playerVelocity = (playerDirHorz * playerSpeedHorz).WithY(playerVelocity.y);
        }

    }

    private void ResolveCollisions(ref Vector3 playerVelocity)
    {
        var cap = GetComponent<CapsuleCollider>();
        var p0 = cap.transform.position + cap.center + (cap.height / 2f) * Vector3.down + cap.radius * Vector3.up;
        var p1 = cap.transform.position + cap.center + (cap.height / 2f) * Vector3.up + cap.radius * Vector3.down;

        Physics.OverlapCapsuleNonAlloc(p0, p1, cap.radius, _overlappingColliders);

        foreach (var overlappingCollider in _overlappingColliders)
        {
            Vector3 collisionNormal;
            float collisionDistance;

            if (Physics.ComputePenetration(cap, cap.transform.position, cap.transform.rotation,
                overlappingCollider, overlappingCollider.transform.position, overlappingCollider.transform.rotation,
                out collisionNormal, out collisionDistance))
            {
                _transform.position += collisionNormal * collisionDistance;
                playerVelocity -= Vector3.Project(playerVelocity, collisionNormal);
            }
        }

    }
}
