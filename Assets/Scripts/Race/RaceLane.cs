using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class RaceLane : MonoBehaviour
{
    public static RaceLane CurrentLane;

    public RaceTrigger StartTrigger;
    public RaceTrigger EndTrigger;
    public GameObject RestartPoint;

    private float _elapsedTime;
    private bool _isRunning;
    private Ui _ui;

    private Transform _origin;

    void Start()
    {
        _ui = FindObjectOfType<Ui>(); // Sorry for not following good software engineering practices
        _origin = GameObject.Find("Origin").transform;

        StartTrigger.Triggered += () =>
        {
            if (!_isRunning && CurrentLane == null)
            {
                _isRunning = true;
                CurrentLane = this;
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
        CurrentLane = null;
        if (forced)
        {
            _ui.HideTimer();
        }
        else
        {
            _ui.ShowResultWith(_elapsedTime);
        }
        _elapsedTime = 0f;
    }

    void Update()
    {
        if (_isRunning)
        {
            _elapsedTime += Time.deltaTime;
            _ui.UpdateTimerWith(_elapsedTime);

            // Reset player
            if (Input.GetKeyDown(KeyCode.Q))
            {
                FindObjectOfType<FpsController>().ResetAt(RestartPoint.transform);
                EndRace(true);
            }
        }

        // Fps controller reset ends race
        if (Input.GetKeyDown(KeyCode.R))
        {
            EndRace(true);
        }

    }

}
