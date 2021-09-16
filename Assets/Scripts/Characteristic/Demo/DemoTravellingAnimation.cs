using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoTravellingAnimation : MonoBehaviour
{
    public float speed = 1f;
    public float maxRotation = 45f;

    private void Update()
    {
        // Travelling rotation animation alternating from left to right.
        transform.rotation = Quaternion.Euler(0f, maxRotation * Mathf.Sin(Time.time * speed), 0f);
    }
}
