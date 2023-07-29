using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Q3-based first person controller
/// </summary>
public class FpsController : MonoBehaviour
{
    #region Drag Drop
    [Header("Components")]
    [SerializeField]
    private Transform _camTransform = default;

    // Collision resolving is done with respect to this volume
    [SerializeField]
    private CapsuleCollider _collisionVolume = default;

    // Collision will not happend with these layers
    // One of them has to be this controller's own layer
    [SerializeField]
    private LayerMask _excludedLayers = default;

    [SerializeField]
    private bool _debugInfo = default;

    [SerializeField]
    private List<Transform> _groundedRayPositions = default;
    #endregion

    #region Movement Parameters
    [Header("Movement parameters")]
    // The controller can collide with colliders within this radius
    [SerializeField]
    private float _radius = 2f;

    // Ad-hoc approach to make the controller accelerate faster
    [SerializeField]
    private float _groundAccelerationCoeff = 500.0f;

    // How fast the controller accelerates while it's not grounded
    [SerializeField]
    private float _airAccelCoeff = 1f;

    // Air deceleration occurs when the player gives an input that's not aligned with the current velocity
    [SerializeField]
    private float _airDecelCoeff = 1.5f;

    // Along a dimension, we can't go faster than this
    // This dimension is relative to the controller, not global
    // Meaning that "max speend along X" means "max speed along 'right side' of the controller"
    [SerializeField]
    private float _maxSpeedAlongOneDimension = 8f;

    // How fast the controller decelerates on the grounded
    [SerializeField]
    private float _friction = 15;

    // Stop if under this speed
    [SerializeField]
    private float _frictionSpeedThreshold = 0.5f;

    // Push force given when jumping
    [SerializeField]
    private float _jumpStrength = 8f;

    // yeah...
    [SerializeField]
    private float _gravityAmount = 24f;

    // How precise the controller can change direction while not grounded 
    [SerializeField]
    private float _airControlPrecision = 16f;

    // When moving only forward, increase air control dramatically
    [SerializeField]
    private float _airControlAdditionForward = 8f;

    // Keyboard and mouse input are enabled
    [SerializeField]
    private bool _canControl = true;

    // Maybe you wanna make a Wolf3D clone?
    [SerializeField]
    private bool _verticalLookEnabled = true;

    [SerializeField]
    private bool _jumpEnabled = true;

    [Header("Footsteps")]
    [SerializeField]
    private float _distancePerFootstep = 3f;

    [SerializeField]
    private AudioSource _audioSource = default;

    [SerializeField]
    private AudioClip[] _footstepClips = default;

    [SerializeField]
    private AudioClip _jumpClip = default;

    [SerializeField]
    private AudioClip _landClip = default;

    private List<AudioClip> _shuffledFootstepClips;
    private Vector3 _prevPos;
    private float _distanceCovered;
    #endregion

    #region Fields
    // The real velocity of this controller
    private Vector3 _velocity;

    // Raw input taken with GetAxisRaw()
    private Vector3 _moveInput;

    // Vertical look
    private float _pitch = 0; // We keep track of this value since we want to clamp it
    private const float Sensitivity = 150;

    // Caching...
    private readonly Collider[] _overlappingColliders = new Collider[10]; // Hope no more is needed
    private Transform _ghostJumpRayPosition;

    // Some information to persist
    private bool _isGroundedInPrevFrame;
    private bool _isGonnaJump;
    private Vector3 _wishDirDebug;
    #endregion

    private void Start()
    {
        Application.targetFrameRate = 60; // My laptop is shitty and burn itself to death if not for this
        _ghostJumpRayPosition = _groundedRayPositions[_groundedRayPositions.Count - 1];

        _shuffledFootstepClips = new List<AudioClip>(_footstepClips);
    }

    // Only for debug drawing
    private void OnGUI()
    {
        if (!_debugInfo)
        {
            return;
        }

        // Print current horizontal speed
        Vector3 ups = _velocity;
        ups.y = 0;
        GUI.Box(new Rect(Screen.width / 2f - 50, Screen.height / 2f + 50, 100, 40),
            (Mathf.Round(ups.magnitude * 100) / 100).ToString());

        // Draw horizontal speed as a line
        Vector2 mid = new(Screen.width / 2, Screen.height / 2); // Should remain integer division, otherwise GUI drawing gets screwed up
        Vector3 v = _camTransform.InverseTransformDirectionHorizontal(_velocity) * (_velocity.ToHorizontal().magnitude * 10f);
        if (v.ToHorizontal().magnitude > 0.0001)
        {
            GuiDraw.DrawLine(mid, mid + Vector2.up * -v.z + Vector2.right * v.x, Color.red, 3f);
        }

        // Draw input direction
        Vector3 w = _camTransform.InverseTransformDirectionHorizontal(_wishDirDebug) * 100;
        if (w.magnitude > 0.001)
        {
            GuiDraw.DrawLine(mid, mid + Vector2.up * -w.z + Vector2.right * w.x, Color.blue, 2f);
        }
    }

    private void Update()
    {
        Cursor.lockState = CursorLockMode.Locked; // Keep doing this. We don't want cursor anywhere just yet

        float dt = Time.deltaTime;

        if (_canControl)
        {
            // We use GetAxisRaw, since we need it to feel as responsive as possible
            _moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

            if (_jumpEnabled && Input.GetKeyDown(KeyCode.Space) && !_isGonnaJump)
            {
                _isGonnaJump = true;
            }
            else if (Input.GetKeyUp(KeyCode.Space))
            {
                _isGonnaJump = false;
            }

            // Mouse look
            if (_verticalLookEnabled)
            {
                _pitch += Input.GetAxis("Mouse Y") * -Sensitivity * dt;
                _pitch = Mathf.Clamp(_pitch, -89, 89);
                _camTransform.localRotation = Quaternion.Euler(Vector3.right * _pitch);
            }
            transform.rotation *= Quaternion.Euler(Input.GetAxis("Mouse X") * Sensitivity * dt * Vector3.up);
        }

        // MOVEMENT
        Vector3 wishDir = _camTransform.TransformDirectionHorizontal(_moveInput); // We want to go in this direction
        _wishDirDebug = wishDir.ToHorizontal();

        bool isGrounded = IsGrounded(out Vector3 groundNormal);

        UpdateFootsteps(_isGonnaJump, isGrounded, isGrounded && !_isGroundedInPrevFrame);

        if (isGrounded) // Ground move
        {
            // Don't apply friction if just landed or about to jump
            if (_isGroundedInPrevFrame && !_isGonnaJump)
            {
                ApplyFriction(ref _velocity, dt);
            }

            Accelerate(ref _velocity, wishDir, _groundAccelerationCoeff, dt);

            // Crop up horizontal velocity component
            _velocity = Vector3.ProjectOnPlane(_velocity, groundNormal);
            if (_isGonnaJump)
            {
                // Jump away
                _velocity += -Gravity.Down * _jumpStrength;
            }
        }
        else // Air move
        {
            // If the input doesn't have the same facing with the current velocity
            // then slow down instead of speeding up
            float coeff = Vector3.Dot(_velocity, wishDir) > 0 ? _airAccelCoeff : _airDecelCoeff;

            Accelerate(ref _velocity, wishDir, coeff, dt);

            if (Mathf.Abs(_moveInput.z) > 0.0001) // Pure side velocity doesn't allow air control
            {
                ApplyAirControl(ref _velocity, wishDir, dt);
            }

            _velocity += Gravity.Down * (_gravityAmount * dt);
        }

        Vector3 displacement = _velocity * dt;

        // If we're moving too fast, make sure we don't hollow through any collider
        if (displacement.magnitude > _collisionVolume.radius)
        {
            ClampDisplacement(ref _velocity, ref displacement, transform.position);
        }

        transform.position += displacement;

        Vector3 collisionDisplacement = ResolveCollisions(ref _velocity);

        transform.position += collisionDisplacement;
        _isGroundedInPrevFrame = isGrounded;


        // Testing
        //if (Input.GetKeyDown(KeyCode.G))
        //{
        //    Gravity.Set(Vector3.right);
        //    _transform.rotation = Quaternion.LookRotation(Gravity.Forward, -Gravity.Down);
        //}
    }

    private void Accelerate(ref Vector3 playerVelocity, Vector3 accelDir, float accelCoeff, float dt)
    {
        // How much speed we already have in the direction we want to speed up
        float projSpeed = Vector3.Dot(playerVelocity, accelDir);

        // How much speed we need to add (in that direction) to reach max speed
        float addSpeed = _maxSpeedAlongOneDimension - projSpeed;
        if (addSpeed <= 0)
        {
            return;
        }

        // How much we are gonna increase our speed
        // maxSpeed * dt => the real deal. a = v / t
        // accelCoeff => ad hoc approach to make it feel better
        float accelAmount = accelCoeff * _maxSpeedAlongOneDimension * dt;

        // If we are accelerating more than in a way that we exceed maxSpeedInOneDimension, crop it to max
        if (accelAmount > addSpeed)
        {
            accelAmount = addSpeed;
        }

        playerVelocity += accelDir * accelAmount; // Magic happens here
    }

    private void ApplyFriction(ref Vector3 playerVelocity, float dt)
    {
        float speed = playerVelocity.magnitude;
        if (speed <= 0.00001f)
        {
            return;
        }

        float downLimit = Mathf.Max(speed, _frictionSpeedThreshold); // Don't drop below treshold
        float dropAmount = speed - (downLimit * _friction * dt);
        if (dropAmount < 0)
        {
            dropAmount = 0;
        }

        playerVelocity *= dropAmount / speed; // Reduce the velocity by a certain percent
    }

    private void ApplyAirControl(ref Vector3 playerVelocity, Vector3 accelDir, float dt)
    {
        // This only happens in the horizontal plane
        // TODO: Verify that these work with various gravity values
        Vector3 playerDirHorz = playerVelocity.ToHorizontal().normalized;
        float playerSpeedHorz = playerVelocity.ToHorizontal().magnitude;

        float dot = Vector3.Dot(playerDirHorz, accelDir);
        if (dot > 0)
        {
            float k = _airControlPrecision * dot * dot * dt;

            // CPMA thingy:
            // If we want pure forward movement, we have much more air control
            bool isPureForward = Mathf.Abs(_moveInput.x) < 0.0001f && Mathf.Abs(_moveInput.z) > 0;
            if (isPureForward)
            {
                k *= _airControlAdditionForward;
            }

            // A little bit closer to accelDir
            playerDirHorz = playerDirHorz * playerSpeedHorz + accelDir * k;
            playerDirHorz.Normalize();

            // Assign new direction, without touching the vertical speed
            playerVelocity = (playerDirHorz * playerSpeedHorz).ToHorizontal() + Gravity.Up * playerVelocity.VerticalComponent();
        }

    }

    // Calculates the displacement required in order not to be in a world collider
    private Vector3 ResolveCollisions(ref Vector3 playerVelocity)
    {
        // Get nearby colliders
        Physics.OverlapSphereNonAlloc(transform.position, _radius + 0.1f,
            _overlappingColliders, ~_excludedLayers);

        Vector3 totalDisplacement = Vector3.zero;
        HashSet<int> checkedColliderIndices = new();

        // If the player is intersecting with that environment collider, separate them
        for (int i = 0; i < _overlappingColliders.Length; i++)
        {
            // Two player colliders shouldn't resolve collision with the same environment collider
            if (checkedColliderIndices.Contains(i))
            {
                continue;
            }

            Collider envColl = _overlappingColliders[i];

            // Skip empty slots
            if (envColl == null)
            {
                continue;
            }

            if (Physics.ComputePenetration(
                _collisionVolume, _collisionVolume.transform.position, _collisionVolume.transform.rotation,
                envColl, envColl.transform.position, envColl.transform.rotation,
                out Vector3 collisionNormal, out float collisionDistance))
            {
                // Ignore very small penetrations
                // Required for standing still on slopes
                // ... still far from perfect though
                if (collisionDistance < 0.015f)
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

        // It's better to be in a clean state in the next resolve call
        for (int i = 0; i < _overlappingColliders.Length; i++)
        {
            _overlappingColliders[i] = null;
        }

        return totalDisplacement;
    }

    // If one of the rays hit, we're considered to be grounded
    private bool IsGrounded(out Vector3 groundNormal)
    {
        groundNormal = -Gravity.Down;

        bool isGrounded = false;
        foreach (Transform t in _groundedRayPositions)
        {
            // The last one is reserved for ghost jumps
            // Don't check that one if already on the ground
            if (t == _ghostJumpRayPosition && isGrounded)
            {
                continue;
            }

            if (Physics.Raycast(t.position, Gravity.Down, out RaycastHit hit, 0.51f, ~_excludedLayers))
            {
                groundNormal = hit.normal;
                isGrounded = true;
            }
        }

        return isGrounded;
    }

    // If there's something between the current position and the next, clamp displacement
    private void ClampDisplacement(ref Vector3 playerVelocity, ref Vector3 displacement, Vector3 playerPosition)
    {
        if (Physics.Raycast(playerPosition, playerVelocity.normalized, out RaycastHit hit, displacement.magnitude, ~_excludedLayers))
        {
            displacement = hit.point - playerPosition;
        }
    }

    private void UpdateFootsteps(bool isGonnaJump, bool isGrounded, bool isLandedThisFrame)
    {
        if (_distanceCovered > _distancePerFootstep)
        {
            _distanceCovered = 0;

            _audioSource.PlayOneShot(_shuffledFootstepClips[0]);
            _shuffledFootstepClips.Shuffle();
        }

        if (isGonnaJump && isGrounded)
        {
            _audioSource.PlayOneShot(_jumpClip);
        }

        if (isLandedThisFrame && !isGonnaJump)
        {
            _audioSource.PlayOneShot(_landClip);
        }

        if (isGrounded)
        {
            _distanceCovered += Vector3.Distance(_prevPos.ToHorizontal(), transform.position.ToHorizontal());
        }
        _prevPos = transform.position;
    }

    // Handy when testing
    public void ResetAt(Transform t)
    {
        transform.position = t.position + Vector3.up * 0.5f;
        _camTransform.position = transform.position;
        _velocity = t.TransformDirection(Vector3.forward);
    }
}

public static class Gravity
{
    public static Vector3 Down { get; private set; }
    public static Vector3 Forward { get; private set; }
    public static Vector3 Up { get { return -Down; } }

    static Gravity()
    {
        Down = Vector3.down;
        Forward = Vector3.forward;
    }

    public static void Set(Vector3 down)
    {
        // Gravity will rotate around this axis with this amount
        Vector3 axis = Vector3.Cross(Down, down);
        float angle = Vector3.Angle(Down, down);

        Down = down;
        Forward = Quaternion.AngleAxis(angle, axis) * Forward;
    }
}

public static class FpsControllerExtensions
{

    public static Vector3 ToHorizontal(this Vector3 v)
    {
        return Vector3.ProjectOnPlane(v, Gravity.Down);
    }

    public static float VerticalComponent(this Vector3 v)
    {
        return Vector3.Dot(v, Gravity.Up);
    }

    public static Vector3 TransformDirectionHorizontal(this Transform t, Vector3 v)
    {
        return t.TransformDirection(v).ToHorizontal().normalized;
    }

    public static Vector3 InverseTransformDirectionHorizontal(this Transform t, Vector3 v)
    {
        return t.InverseTransformDirection(v).ToHorizontal().normalized;
    }

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            (list[n], list[k]) = (list[k], list[n]); // Swap
        }
    }

}

