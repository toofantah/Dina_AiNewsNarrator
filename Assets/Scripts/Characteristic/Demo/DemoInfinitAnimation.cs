using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoInfinitAnimation : MonoBehaviour
{
  
    void Update()
    {
        // Lemniscate of Bernoulli position animation.
        float scale = 4 / (3 - Mathf.Cos(2 * Time.time));
        transform.localPosition = new Vector3(scale * Mathf.Cos(Time.time), scale * Mathf.Sin(2 * Time.time) / 2, 0);
    }
}
