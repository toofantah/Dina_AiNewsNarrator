using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoRotateAnimation : MonoBehaviour
{
  
    public float speed = 1f;
    public Vector3 rotationAxis = new Vector3(0, 1, 0);

    // Simple object rotation animation.
    void Update()
    {
        transform.Rotate(rotationAxis, speed * Time.deltaTime);
    }
}
