using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class RaceTrigger : MonoBehaviour
{
    public Action Triggered;
    private float _baseY;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("PlayerCollider"))
        {
            if (Triggered != null)
            {
                Triggered();
            }
        }

        _baseY = transform.position.y;
    }

    void Update()
    {
        transform.position += Vector3.up * Mathf.Sin(Time.time * 4) / 80f;
        transform.Rotate(Vector3.up, 40 * Time.deltaTime);
    }
}
