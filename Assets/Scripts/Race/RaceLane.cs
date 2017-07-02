using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class RaceLane : MonoBehaviour
{
    public RaceTrigger StartTrigger;
    public RaceTrigger EndTrigger;
    public GameObject RestartPoint;

    private float _elapsedTime;
    private bool _isRunning;
    private Ui _ui; 

    void Start()
    {
        _ui = FindObjectOfType<Ui>(); // Sorry for not following good software engineering practices

        StartTrigger.Triggered += () =>
        {
            if (!_isRunning)
            {
                _isRunning = true;
            }
        };

        EndTrigger.Triggered += () =>
        {
            if (_isRunning)
            {
                EndRace(false);
            }
        };
    }

    public void EndRace(bool forced)
    {
        _isRunning = false;
        _elapsedTime = 0f;
        if (forced)
        {
            _ui.HideTimer();
        }
        else
        {
            _ui.ShowResultWith(_elapsedTime);
        }
    }

    void Update()
    {
        if (_isRunning)
        {
            _elapsedTime += Time.deltaTime;
            _ui.UpdateTimerWith(_elapsedTime);

            // Reset player
            if (Input.GetKeyDown(KeyCode.R))
            {
                FindObjectOfType<FpsController>().ResetAt(RestartPoint.transform);
                EndRace(true);
            }
        }

    }

}
