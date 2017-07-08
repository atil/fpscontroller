using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Video;

public class FpsController : MonoBehaviour
{
    [SerializeField]
    private float _height;

    [SerializeField]
    private List<Collider> _collisionElements;

    [SerializeField]
    private LayerMask _excludedLayers;

    [Header("Movement")]
    private const float GroundAccelerationCoeff = 500.0f;
    private const float AirAccelCoeff = 0.8f;
    private const float AirDecelCoeff = 1.5f;
    private const float MaxSpeedAlongOneDimension = 8f;
    private const float Friction = 15;
    private const float FrictionSpeedThreshold = 0.5f; // Just stop if under this speed
    private const float JumpStrength = 10f;
    private const float Gravity = 25f;
    private const float AirControlPrecision = 8f;

    private MouseLook _mouseLook;
    private Transform _transform;
    [SerializeField]
    private Vector3 _velocity;
    private Vector3 _moveInput;
    private Transform _camTransform;

    [SerializeField]
    private bool _isGroundedInThisFrame;
    private bool _isGroundedInPrevFrame;
    private bool _isGonnaJump;

    private readonly Collider[] _overlappingColliders = new Collider[10]; // Hope no more is needed

    private Vector3 _wishDirDebug;

    void Start()
	{
        _transform = transform;
        Application.targetFrameRate = 60; // Don't know why I did that
        _camTransform = Camera.main.transform;
        _mouseLook = new MouseLook(_camTransform);
	}

    // Only for debug drawing
    void OnGUI()
    {
        // Print current horizontal speed
        var ups = _velocity;
        ups.y = 0;
        GUI.Box(new Rect(Screen.width / 2f - 50, Screen.height / 2f + 50, 100, 40),
            (Mathf.Round(ups.magnitude * 100) / 100).ToString());

        // Draw horizontal speed as a line
        // Should remain integer division, otherwise GUI drawing gets screwed up
        // ... was a pain in the ass figuring this out
        var mid = new Vector2(Screen.width / 2, Screen.height / 2); 
        var v = _camTransform.InverseTransformDirectionHorizontal(_velocity) * _velocity.WithY(0).magnitude * 10f;
        if (v.WithY(0).magnitude > 0.0001)
        {
            Drawing.DrawLine(mid, mid + Vector2.up * -v.z + Vector2.right * v.x, Color.red, 3f);
        }

        // Draw input direction
        var w = _camTransform.InverseTransformDirectionHorizontal(_wishDirDebug) * 100;
        if (w.magnitude > 0.001)
        {
            Drawing.DrawLine(mid, mid + Vector2.up * -w.z + Vector2.right * w.x, Color.blue, 2f);
        }
    }

    // All logic, including player displacement, happens here
    void FixedUpdate()
    {
	    var dt = Time.fixedDeltaTime;
        var justLanded = !_isGroundedInPrevFrame && _isGroundedInThisFrame;

	    var wishDir = _camTransform.TransformDirectionHorizontal(_moveInput); // We want to go in this direction
        _wishDirDebug = new Vector3(wishDir.x, 0, wishDir.z);

        if (_isGroundedInThisFrame) // Ground move
        {
            if (!justLanded && !_isGonnaJump) // Don't apply friction if just landed
            {
                ApplyFriction(ref _velocity, Friction, dt);
            }

            Accelerate(ref _velocity, wishDir, MaxSpeedAlongOneDimension, GroundAccelerationCoeff, dt);

            _velocity.y = 0;
            if (_isGonnaJump)
            {
                _isGonnaJump = false;
                _velocity.y = JumpStrength;
            }
        }
        else // Air move
        {
            // If the input is not in alignment with the current velocity
            // then slow down instead of speeding up
            var coeff = Vector3.Dot(_velocity, wishDir) > 0 ? AirAccelCoeff : AirDecelCoeff;

            Accelerate(ref _velocity, wishDir, MaxSpeedAlongOneDimension, coeff, dt);

            if (Mathf.Abs(_moveInput.z) > 0.0001)
            {
                ApplyAirControl(ref _velocity, wishDir, dt);
            }

            _velocity.y -= Gravity * dt;
        }

        _transform.position += _velocity * dt; // Actual displacement

        var collisionDisplacement = ResolveCollisions(ref _velocity);
        _transform.position += collisionDisplacement; // Pushing out of environment

        _isGroundedInPrevFrame = _isGroundedInThisFrame;
    }

    // Input taking happens here
    // We also bring the camera (which is a separate entity) to the controller position here
    void Update()
    {
        Cursor.lockState = CursorLockMode.Locked; // Keep doing this. We don't want cursor anywher just yet

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

        var mouseLookForward = _mouseLook.Update();
        _transform.rotation = Quaternion.LookRotation(mouseLookForward.WithY(0), Vector3.up); // 

        // Reset player
        if (Input.GetKeyDown(KeyCode.R))
        {
            _transform.position = Vector3.zero + Vector3.up * 2f;
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
        // maxSpeed * dt => the real deal. a = v * (1 / t)
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

    private void ApplyAirControl(ref Vector3 playerVelocity, Vector3 accelDir, float dt)
    {
        var playerDirHorz = playerVelocity.WithY(0).normalized;
        var playerSpeedHorz = playerVelocity.WithY(0).magnitude;

        var dot = Vector3.Dot(playerDirHorz, accelDir);
        if (dot > 0)
        {
            var k = AirControlPrecision * dot * dot * dt;
            
            // A little bit closer to accelDir
            playerDirHorz = playerDirHorz * playerSpeedHorz + accelDir * k;
            playerDirHorz.Normalize();

            // Assign new direction, without touching the vertical speed
            playerVelocity = (playerDirHorz * playerSpeedHorz).WithY(playerVelocity.y);
        }

    }

    private Vector3 ResolveCollisions(ref Vector3 playerVelocity)
    {
        _isGroundedInThisFrame = false;
        
        // Get nearby colliders
        Physics.OverlapSphereNonAlloc(_transform.position, _height + 0.1f,
            _overlappingColliders, ~_excludedLayers);

        var totalDisplacement = Vector3.zero;
        var checkedColliderIndices = new HashSet<int>();
        foreach (var playerColl in _collisionElements)
        {
            // If the player is intersecting with that environment collider, separate them
            for (var i = 0; i < _overlappingColliders.Length; i++)
            {
                // Two player colliders shouldn't resolve collision with the same environment collider
                if (checkedColliderIndices.Contains(i))
                {
                    continue;
                }

                var envColl = _overlappingColliders[i];
                
                // Skip empty slots
                if (envColl == null)
                {
                    continue;
                }

                Vector3 collisionNormal;
                float collisionDistance;

                if (Physics.ComputePenetration(
                    playerColl, playerColl.transform.position, playerColl.transform.rotation,
                    envColl, envColl.transform.position, envColl.transform.rotation,
                    out collisionNormal, out collisionDistance))
                {
                    // If the collision pointing up to some degree
                    // then it means we're standing on something
                    if (Vector3.Dot(collisionNormal, Vector3.up) > 0.5)
                    {
                        _isGroundedInThisFrame = true;
                    }

                    // Ignore very small penetrations
                    // Required for standing still on slopes
                    // ... still far from perfect though
                    if (collisionDistance < 0.01)
                    {
                        continue;
                    }

                    checkedColliderIndices.Add(i);

                    // Get outta that collider!
                    totalDisplacement += collisionNormal * collisionDistance;

                    // Crop down the velocity component which is in the direction of penetration
                    playerVelocity -= Vector3.Project(playerVelocity, collisionNormal);
                }
            }
        }


        for (var i = 0; i < _overlappingColliders.Length; i++)
        {
            _overlappingColliders[i] = null;
        }

        return totalDisplacement;
    }

    public void ResetAt(Transform t)
    {
        // Teleport this guy to the said spot
        _transform.position = t.position + Vector3.up * 0.5f;
        _camTransform.position = _transform.position;
        _mouseLook.LookAt(t.position + t.forward);
        _velocity = t.TransformDirection(Vector3.forward);
    }
}
