using UnityEngine;

public enum HookState
{
    None,
    Off,
    Pull,
    Hold,
    Loose
}

// We had vastly improved hook feeling in another project
// This one here is old and feels awful
// See: https://github.com/atil/fpsmapbox/
public class GrapplingHook : MonoBehaviour
{
    // How strong the spring will feel
    [SerializeField]
    private float SpringTightness = 0.5f;

    // The higher this number is, the quicker the spring will come to rest
    [SerializeField]
    private float DampingCoeff = 0.01f;
    
    [SerializeField]
    private float FuelTankCapacity = 3f;

    [SerializeField]
    private float FuelBurnRate = 1f;

    public HookState State { get; private set; }

    [SerializeField]
    private Camera _mainCamera;
    [SerializeField]
    private LayerMask _excludedLayers;
    [SerializeField]
    private Transform _hookSlot;
    [SerializeField]
    private GameObject _hookPrefab;

    private Transform _hookVisual;
    private Vector3 _springEnd;
    private float _hookLength;
    private float _remainingFuel;

    private void Start()
    {
        _hookVisual = Instantiate(_hookPrefab).transform;
        _hookVisual.gameObject.SetActive(false);
        State = HookState.Off;
    }

    public void ExternalUpdate(float dt, Vector3 playerPosition)
    {
        if (State == HookState.Off)
        {
			// Transition: Off -> Pull
            if (Input.GetMouseButtonDown(0))
            {
                if (Physics.Raycast(_mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f)), out RaycastHit hit, float.MaxValue, ~_excludedLayers))
                {
                    _springEnd = hit.point;
                    _hookVisual.gameObject.SetActive(true);
                    State = HookState.Pull;
                }
            }

            AdjustFuel(dt);
        }
        else if (State == HookState.Pull)
        {
			// Transition: Pull -> Hold
            if (Input.GetMouseButtonUp(0))
            {
                State = HookState.Hold;
                _hookLength = Vector3.Distance(playerPosition, _springEnd);
            }

			AdjustFuel(-dt);
        }
        else if (State == HookState.Hold)
        {
			// Transition: Hold -> Pull
            if (Input.GetMouseButtonDown(0))
            {
                State = HookState.Pull;
            }
			
			// Transition: Hold -> Loose
            if (Input.GetMouseButtonDown(1))
            {
                State = HookState.Loose;
            }

			AdjustFuel(dt);
        }
        else if (State == HookState.Loose)
        {
			// Transition: Loose -> Hold
            if (Input.GetMouseButtonUp(1))
            {
                State = HookState.Hold;
                _hookLength = Vector3.Distance(playerPosition, _springEnd);
            }

			AdjustFuel(dt);
        }

		// Pressing F or running out of fuel force breaks the hook
        if (Input.GetKeyDown(KeyCode.F) || _remainingFuel <= 0)
        {
            State = HookState.Off;
            _hookVisual.gameObject.SetActive(false);
        }
    }

	// Apply simple spring physics
    public void ApplyHookAcceleration(ref Vector3 playerVelocity, Vector3 playerPosition)
    {
        if (State != HookState.Pull)
        {
            return;
        }

        Vector3 springDir = (_springEnd - playerPosition).normalized;
        Vector3 damping = playerVelocity * DampingCoeff;

		// The longer the hook, the stronger the pull
        float springLength = Vector3.Distance(playerPosition, _springEnd);
		
		// sqrt(sqrt(x)) feels better than pure linear (which is _the_ spring formula)
        playerVelocity += Mathf.Sqrt(Mathf.Sqrt(springLength)) * SpringTightness * springDir;
        playerVelocity -= damping;
    }

	// Prevent it going further than the length of the hook
    public bool ApplyHookDisplacement(ref Vector3 playerVelocity, ref Vector3 displacement, Vector3 playerPosition)
    {
        if (State != HookState.Hold)
        {
            return false;
        }

        float distance = Vector3.Distance(playerPosition, _springEnd);
        if (distance > _hookLength)
        {
			// The player will have no velocity component in the hook's direction
            Vector3 playerToEndDir = (_springEnd - playerPosition).normalized;
            playerVelocity -= Vector3.Project(playerVelocity, playerToEndDir);

            displacement = playerToEndDir * (distance - _hookLength);
            return true;
        }

        return false;
    }

	// Fuel is burned only when the hook is pulling
	// Otherwise it's always charging
	private void AdjustFuel(float amount)
	{
		_remainingFuel += amount * FuelBurnRate;
		_remainingFuel = Mathf.Clamp(_remainingFuel, 0f, FuelTankCapacity);
	}
	
	// For UI
    public float GetRemainingFuel()
    {
        return _remainingFuel / FuelTankCapacity;
    }

	// Mess with the poor cube's transform to make it look good
    public void Draw()
    {
        _hookVisual.transform.position = (_springEnd + _hookSlot.position) / 2f;
        _hookVisual.transform.rotation = Quaternion.LookRotation(_springEnd - _hookSlot.position);
        _hookVisual.transform.localScale = new Vector3(0.1f, 0.1f, Vector3.Distance(_springEnd, _hookSlot.position));
    }

    public void ResetHook()
    {
        State = HookState.Off;
        _hookVisual.gameObject.SetActive(false);
    }
}
