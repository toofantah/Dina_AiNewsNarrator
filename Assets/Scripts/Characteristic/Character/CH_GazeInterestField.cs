using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CH_GazeInterestField : MonoBehaviour
{

    //public List<Transform> GazeTargets { get; private set; } = new List<Transform>();
    public List<Transform> GazeTargets = new List<Transform>();

    private void OnEnable()
    {
        // Starting a coroutine to check for disabled game objects in the interest field every second as they do not call the trigger exit function.
        StartCoroutine(CheckForDisabledTargets());
    }

    private void OnDisable()
    {
        GazeTargets.Clear();
        StopAllCoroutines();
    }

    private void OnTriggerEnter(Collider target)
    {
        // No need to check the target tag, as the probabilistic gaze configurator editor tool set the collision matrix to only enable CH_GazeComponents self collisions.
        GazeTargets.Add(target.transform);
    }

    private void OnTriggerExit(Collider target)
    {
        GazeTargets.Remove(target.transform);
    }

    // Coroutine to check for disabled game objects in the interest field every second as they do not call the trigger exit function.
    private IEnumerator CheckForDisabledTargets()
    {
        if (GazeTargets.Any())
        {
            // Inverted for loop avoiding null reference exceptions when removing a disabled element from the list.
            for (int i = GazeTargets.Count - 1; i > 0; i--)
            {
                if (!GazeTargets[i].gameObject.activeInHierarchy)
                    GazeTargets.Remove(GazeTargets[i]);
            }
        }

        yield return new WaitForSeconds(1f);

        StartCoroutine(CheckForDisabledTargets());
    }
}
