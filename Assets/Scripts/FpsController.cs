using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Video;

public class FpsController : MonoBehaviour
{
    [SerializeField]
    private CapsuleCollider _collisionElement;


    public float GroundAccelerationCoeff = 500.0f;
    public float AirAccelCoeff = 0.3f;
    public float AirDecelCoeff = 1f;
    public float MaxSpeedAlongOneDimension = 10f;
    public float Friction = 20;
    public float FrictionSpeedThreshold = 0.5f; // Just stop if under this speed
    public float JumpStrength = 10f;
    public float Gravity = 25f;
    public float AirControlPrecision = 8f;

    private MouseLook _mouseLook;
    private Transform _transform;
    [SerializeField]
    private Vector3 _velocity;
    [SerializeField]
    private Vector3 _position;
    private Vector3 _moveInput;
    private Transform _camTransform;

    [SerializeField]
    private bool _isGroundedInThisFrame;
    private bool _isGroundedInPrevFrame;
    private bool _isGonnaJump;
    private readonly Collider[] _overlappingColliders = new Collider[10]; // Hope no more is needed
    private int _playerLayer;

    private Vector3 _wishDirDebug;

    void Start()
	{
        _transform = transform;
        Cursor.lockState = CursorLockMode.Locked;
        Application.targetFrameRate = 60; // Need to work around this
        _mouseLook = new MouseLook(_transform, Camera.main.transform);
        _playerLayer = LayerMask.NameToLayer("PlayerCollider");
        _position = _transform.position;
        _camTransform = Camera.main.transform;
	}

    void OnGUI()
    {
        var ups = _velocity;
        ups.y = 0;
        GUI.Box(new Rect(Screen.width/2f-50, Screen.height / 2f + 50, 100, 40),  
            (Mathf.Round(ups.magnitude * 100) / 100).ToString());

        var mid = new Vector2(Screen.width / 2, Screen.height / 2);
        var v = _camTransform.InverseTransformVector(_velocity * 10);
        if (v.WithY(0).magnitude > 0.0001)
        {
            Drawing.DrawLine(mid, mid + Vector2.up * -v.z + Vector2.right * v.x, Color.red, 3f);
        }

        var w = _camTransform.InverseTransformDirection(_wishDirDebug) * 100;
        if (w.magnitude > 0.001)
        {
            Drawing.DrawLine(mid, mid + Vector2.up * -w.z + Vector2.right * w.x, Color.blue, 2f);
        }
    }

    void FixedUpdate()
    {
	    var dt = Time.fixedDeltaTime;
        var justLanded = !_isGroundedInPrevFrame && _isGroundedInThisFrame;

	    var wishDir = _camTransform.TransformDirection(_moveInput);
        _wishDirDebug = new Vector3(wishDir.x, 0, wishDir.z);

        if (_isGroundedInThisFrame) // Ground move
        {
            Accelerate(ref _velocity, wishDir, MaxSpeedAlongOneDimension, GroundAccelerationCoeff, dt);

            if (!justLanded) // Don't apply friction if just landed
            {
                var frictionCoeff = Friction;
                if (_isGonnaJump) // Apply half friction just before the jump
                {
                    frictionCoeff *= 0f;
                }

                ApplyFriction(ref _velocity, frictionCoeff, dt);
            }


            _velocity.y = 0;
            if (_isGonnaJump)
            {
                _isGonnaJump = false;
                _velocity.y = JumpStrength;
            }
        }
        else // Air move
        {
            var airAccel = Vector3.Dot(_velocity, wishDir) > 0 ? AirAccelCoeff : AirDecelCoeff;
            Accelerate(ref _velocity, wishDir, MaxSpeedAlongOneDimension, airAccel, dt);

            if (Mathf.Abs(_moveInput.z) > 0.0001)
            {
                ApplyAirControl(ref _velocity, wishDir);
            }

            _velocity.y -= Gravity * dt;
        }

        _transform.position += _velocity * dt;

        ResolveCollisions(ref _velocity);

        _isGroundedInPrevFrame = _isGroundedInThisFrame;
    }

    void Update()
    {
	    var dt = Time.deltaTime;
        _moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        if (Input.GetKeyDown(KeyCode.Space) && !_isGonnaJump)
        {
            _isGonnaJump = true;
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            _isGonnaJump = false;
        }

        _camTransform.position = Vector3.Lerp(_camTransform.position, _transform.position, dt * 200f);

        _mouseLook.Update();

        // Reset player
        if (Input.GetKeyDown(KeyCode.R))
        {
            _transform.position = Vector3.up * 1.5f;
            _velocity = Vector3.forward;
        }
    }

    private void Accelerate(ref Vector3 playerVelocity, Vector3 accelDir, float maxSpeedAlongOneDimension, float accelCoeff, float dt)
    {
        // How much speed we already have in the direction we want to speed up
        var projSpeed = Vector3.Dot(playerVelocity, accelDir);

        // How much speed we need to add (in that direction) to reach max speed
        var addSpeed = maxSpeedAlongOneDimension - projSpeed;
        if (addSpeed <= 0)
        {
            return;
        }

        // How much we are gonna increase our speed
        // maxSpeed * dt => real amount
        // accelCoeff => ad hoc approach to make it feel better
        var accelAmount = accelCoeff * maxSpeedAlongOneDimension * dt;

        // If we are accelerating more than in a way that we exceed maxSpeedInOneDimension, crop it to max
        if (accelAmount > addSpeed)
        {
            accelAmount = addSpeed;
        }

        playerVelocity += accelDir * accelAmount; // Magic happens here
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

        playerVelocity *= dropAmount / speed; // Reduce the velocity by a certain percent
    }

    private void ApplyAirControl(ref Vector3 playerVelocity, Vector3 accelDir)
    {
        var playerDirHorz = playerVelocity.WithY(0).normalized;
        var playerSpeedHorz = playerVelocity.WithY(0).magnitude;

        var dot = Vector3.Dot(playerDirHorz, accelDir);
        if (dot > 0)
        {
            var k = AirControlPrecision * dot * dot * Time.deltaTime;
            
            // A little bit closer to accelDir
            playerDirHorz = playerDirHorz * playerSpeedHorz + accelDir * k;
            playerDirHorz.Normalize();

            // Assign new direction, without touching the vertical speed
            playerVelocity = (playerDirHorz * playerSpeedHorz).WithY(playerVelocity.y);
        }

    }

    private void ResolveCollisions(ref Vector3 playerVelocity)
    {
        _isGroundedInThisFrame = false;
        
        // Get nearby colliders
        Physics.OverlapSphereNonAlloc(_collisionElement.transform.position, _collisionElement.height / 2f,
            _overlappingColliders, ~(1 << _playerLayer));

        foreach (var overlappingCollider in _overlappingColliders)
        {
            // Skip empty slots
            if (overlappingCollider == null)
            {
                continue;
            }

            Vector3 collisionNormal;
            float collisionDistance;

            if (Physics.ComputePenetration(
                _collisionElement, _collisionElement.transform.position, _collisionElement.transform.rotation,
                overlappingCollider, overlappingCollider.transform.position, overlappingCollider.transform.rotation,
                out collisionNormal, out collisionDistance))
            {
                if (Vector3.Dot(collisionNormal, Vector3.up) > 0.5)
                {
                    _isGroundedInThisFrame = true;

                    // If dealing with the ground, don't resolve collision that much
                    // Because if we fully resolve ground penetration, _isGrounded will be false
                    // Which will result in AirMove and gravity being applied
                    //collisionDistance *= 0.9f;
                }

                // Ignore very small penetrations
                if (collisionDistance < 0.001)
                {
                    continue;
                }

                // Get outta that collider!
                _transform.position += collisionNormal * collisionDistance;

                // Crop down the velocity component which is in the direction of penetration
                playerVelocity -= Vector3.Project(playerVelocity, collisionNormal);
            }
        }

    }

}
