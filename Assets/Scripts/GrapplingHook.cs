using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class GrapplingHook
{
    // How strong the spring will feel
    private const float SpringTightness = 0.05f;

    // The higher this number is, the quicker the spring will come to rest
    private const float DampingCoeff = 0.01f;

    private readonly Transform _hookVisual;
    private readonly Vector3 _screenMidPoint;

    private Vector3 _springEnd;
    private bool _isHookActive;

    public GrapplingHook(GameObject hookPrefab, Vector3 screenMidPoint)
    {
        _screenMidPoint = screenMidPoint;
        _hookVisual = GameObject.Instantiate(hookPrefab).transform;
        _hookVisual.gameObject.SetActive(false);
    }

    public void CastHook()
    {
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(_screenMidPoint), out hit))
        {
            _springEnd = hit.point;
            _hookVisual.gameObject.SetActive(true);
            _isHookActive = true;
        }
    }

    public void UncastHook()
    {
        _hookVisual.gameObject.SetActive(false);
        _isHookActive = false;
    }

    public void ApplyHookAcceleration(ref Vector3 playerVelocity, Vector3 playerPosition)
    {
        if (!_isHookActive)
        {
            return;
        }

        var springLength = Vector3.Distance(playerPosition, _springEnd);
        var springDir = (_springEnd - playerPosition).normalized;
        var damping = playerVelocity * DampingCoeff;

        playerVelocity += springLength * SpringTightness * springDir;
        playerVelocity -= damping;
    }

    public void Draw(Vector3 playerPosition)
    {
        _hookVisual.transform.position = (_springEnd + playerPosition) / 2f;
        _hookVisual.transform.rotation = Quaternion.LookRotation(_springEnd - playerPosition);
        _hookVisual.transform.localScale = new Vector3(0.1f, 0.1f, Vector3.Distance(_springEnd, playerPosition));
    }

}
