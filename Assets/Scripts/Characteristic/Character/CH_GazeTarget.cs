using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CH_GazeTarget : MonoBehaviour
{
    public CH_Gaze CHGaze { get; set; }
    public CH_GazeInterestField CH_GazeInterestField { get; set; }

    private CH_Gaze.GazeBehavior m_currentGazeBehavior = CH_Gaze.GazeBehavior.NONE;

    private Transform m_initialParentTransform;

    private List<Transform> m_gazeTargets = new List<Transform>();
    private int m_targetListSize = 0;
    private int m_targetIndex = 0;

    private List<Vector4> m_targetsPonderedPositions = new List<Vector4>();
    private int m_targetPositionsListSize = 0;
    private List<Vector4> m_targetsPonderedPositionsCopy = new List<Vector4>();
    private float m_targetPositionsCopyUpdateWaitingDuration = 0f;

    private Dictionary<int, AudioSource> audioSources = new Dictionary<int, AudioSource>();

    private float m_targetSwitchingWaitingDuration = 0f;
    private bool m_transitionInitialized = false;
    private Vector3 m_finalTargetPosition;
    private float m_transitionDuration;
    private float m_xVelocity = 0;
    private float m_yVelocity = 0;
    private float m_zVelocity = 0;

    private Vector3 m_scriptedTargetPosition = Vector3.zero;

    // Property calling the function to set the target position when its value is changed in scripted gaze behavior.
    public Vector3 ScriptedTargetPosition
    {
        get { return m_scriptedTargetPosition; }

        set
        {
            m_scriptedTargetPosition = value;

            if (m_currentGazeBehavior == CH_Gaze.GazeBehavior.SCRIPTED)
                ScriptedGazeBehavior(m_scriptedTargetPosition);
        }
    }

    private void Start()
    {
        m_initialParentTransform = transform.parent;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void Update()
    {
        // Setting the gaze behavior and initializing the corresponding functions. Only updated when the behavior is changed.
        if (m_currentGazeBehavior != CHGaze.gazeBehavior)
        {
            StopAllCoroutines();

            switch (CHGaze.gazeBehavior)
            {
                case CH_Gaze.GazeBehavior.PROBABILISTIC:
                    transform.parent = CHGaze.transform;
                    StartCoroutine(ProbabilisticGazeBehavior());
                    break;
                case CH_Gaze.GazeBehavior.RANDOM:
                    transform.parent = m_initialParentTransform;
                    StartCoroutine(RandomGazeBehavior(3f));
                    break;
                case CH_Gaze.GazeBehavior.STATIC:
                    transform.parent = m_initialParentTransform;
                    StaticGazeBehavior();
                    break;
                case CH_Gaze.GazeBehavior.SCRIPTED:
                    transform.parent = m_initialParentTransform;
                    ScriptedGazeBehavior(CHGaze.NeutralTargetPosition);
                    break;
                case CH_Gaze.GazeBehavior.NONE:
                    Debug.LogWarning("No gaze behavior selected in the gaze settings.");
                    break;
                default:
                    break;
            }

            m_currentGazeBehavior = CHGaze.gazeBehavior;
        }
    }

    #region Probabilistic gaze behavior

    // Recursive coroutine to handle the probabilistic gaze behavior.
    private IEnumerator ProbabilisticGazeBehavior()
    {
        Vector3 aimedTargetPosition;

        // Calling the target selection and the targets ponderation fuction when the interest field contains any target.
        if (CH_GazeInterestField && CH_GazeInterestField.GazeTargets.Any())
        {
            if (transform.parent != CHGaze.transform)
                transform.parent = CHGaze.transform;

            aimedTargetPosition = TargetSelection(TargetsPonderation());
        }

        // Setting a default gaze target position when the interest field does not contain any target. 
        else
        {
            if (transform.parent != m_initialParentTransform)
                transform.parent = m_initialParentTransform;

            aimedTargetPosition = CHGaze.NeutralTargetPosition;
        }

        // Target position has to be override each frame when if parent gameObjects are moving (head rotation, walk, etc.) to stay at the required location.
        if (transform.position != aimedTargetPosition)
            SmoothTargetTransition(aimedTargetPosition);

        yield return null;

        StartCoroutine(ProbabilisticGazeBehavior());
    }

    // Function returning a pondered list of the targets contained in the interest field of the character.
    private List<Vector4> TargetsPonderation()
    {
        // Clearing the list of positions to update the moving targets' position every frame.
        m_targetsPonderedPositions.Clear();

        // When a target enter or exit the interest field, a copy of the list of targets is stored.
        if (m_targetListSize != CH_GazeInterestField.GazeTargets.Count)
        {
            m_gazeTargets = new List<Transform>(CH_GazeInterestField.GazeTargets);
            m_targetsPonderedPositionsCopy.Clear();

            audioSources.Clear();
            AudioSource targetAudioSource;

            // Audio sources are stored in a dictionary to avoid getting them each frame. The dictionary key corresponds to the target's position in the list.
            for (int i = 0; i < m_gazeTargets.Count; i++)
            {
                if (m_gazeTargets[i].parent && m_gazeTargets[i].parent.GetComponent<AudioSource>())
                {
                    targetAudioSource = m_gazeTargets[i].parent.GetComponent<AudioSource>();
                    audioSources.Add(i, targetAudioSource);
                }
            }

            m_targetListSize = m_gazeTargets.Count;
        }

        // Applying ponderations based on distances, movements and sounds for each target in the interest field.
        for (int i = 0; i < m_gazeTargets.Count; i++)
        {
            Vector3 targetPosition = m_gazeTargets[i].position;
            float targetPonderedValue = 0f;

            // Calling a function to apply a distance based ponderation.
            targetPonderedValue = DistancePonderation(targetPosition, targetPonderedValue);

            // Calling a function to apply a sound based ponderation if the dictionary contains at least an active audio source from the targets.
            if (audioSources.Any())
            {
                AudioSource targetAudioSource;

                if (audioSources.TryGetValue(i, out targetAudioSource))
                    targetPonderedValue = SoundPonderation(targetPosition, targetAudioSource, targetPonderedValue);
            }

            // Calling a function to apply a movement based ponderation.
            if (m_targetsPonderedPositionsCopy.Any())
                targetPonderedValue = MovementPonderation(targetPosition, m_targetsPonderedPositionsCopy[i], targetPonderedValue);

            m_targetsPonderedPositions.Add(new Vector4(targetPosition.x, targetPosition.y, targetPosition.z, targetPonderedValue));
        }

        // Updating the targets' positions list copy to enable position comparisions to detect any movements.
        if (m_targetPositionsCopyUpdateWaitingDuration >= 0.25f)
        {
            m_targetsPonderedPositionsCopy = new List<Vector4>(m_targetsPonderedPositions);
            m_targetPositionsCopyUpdateWaitingDuration = 0;
        }

        m_targetPositionsCopyUpdateWaitingDuration += Time.deltaTime;

        return m_targetsPonderedPositions;
    }

    #region Target ponderators

    // Function returning a distance based pondered value.
    private float DistancePonderation(Vector3 targetPosition, float targetPonderedValue)
    {
        float targetDistance = Vector3.Distance(CHGaze.EyesAveragePosition, targetPosition);
        targetPonderedValue = Mathf.Clamp(Mathf.RoundToInt(targetPonderedValue + (1 / targetDistance * 100f)), 0f, 100f);

        return targetPonderedValue;
    }

    // Function returing a sound based pondered value considering the audio source distance and volume.
    private float SoundPonderation(Vector3 targetPosition, AudioSource audioSource, float targetPonderedValue)
    {
        if (audioSource.isPlaying)
        {
            float distanceWithAudioSource = Vector3.Distance(targetPosition, CHGaze.EyesAveragePosition);
            float ponderationFactor = Mathf.Clamp((1 / distanceWithAudioSource) * audioSource.volume * 10, 3f, 10f);
            targetPonderedValue = Mathf.RoundToInt(targetPonderedValue * ponderationFactor);
        }

        return targetPonderedValue;
    }

    // Function returing a movement based pondered value considering the object velocity.
    private float MovementPonderation(Vector3 targetPosition, Vector3 targetPreviousPosition, float targetPonderedValue)
    {
        if (targetPosition != targetPreviousPosition)
        {
            float distanceWithPreviousPosition = Vector3.Distance(targetPosition, targetPreviousPosition);
            float ponderationFactor = Mathf.Clamp(distanceWithPreviousPosition * 100, 3f, 10f);
            targetPonderedValue = Mathf.RoundToInt(targetPonderedValue * ponderationFactor);
        }

        return targetPonderedValue;
    }

    #endregion

    // Function probabilistically returning a target position based on the list of pondered potential targets.
    private Vector3 TargetSelection(List<Vector4> orderedTargetPositions)
    {
        // Changing of target at a random frequency or if a target is added/removed from the list.
        if (m_targetSwitchingWaitingDuration >= Random.Range(2f, 4f) || m_targetPositionsListSize != orderedTargetPositions.Count)
        {
            m_targetPositionsListSize = orderedTargetPositions.Count;

            float totalTargetsWeight = 0;

            // Calculating the total ponderation weight.
            for (int i = 0; i < orderedTargetPositions.Count; i++)
            {
                float targetWeight = orderedTargetPositions[i].w;
                totalTargetsWeight += targetWeight;
            }

            float targetProb = 0f;
            float targetCumulatedProb = targetProb;
            List<float> targetsProbs = new List<float>();

            // Calculating the percentage of selection probability for each target based on its pondered score.
            for (int i = 0; i < orderedTargetPositions.Count; i++)
            {
                targetProb = orderedTargetPositions[i].w * 100 / totalTargetsWeight;
                // Adding the target selection probability to a cumulated score to create value ranges between 1 and 100.
                // Each target selection probability now occupies a range equal to its selection probability between 0 and 100 without overlapping other targets' ranges.
                targetCumulatedProb += targetProb;
                targetsProbs.Add(targetCumulatedProb);
            }

            // Generating a random number between 1 and 100.
            int randomProbScore = Random.Range(1, 101);

            // Checking in which target's range the random value is contained and updating the index to change the gaze target location.
            for (int i = 0; i < targetsProbs.Count(); i++)
            {
                if (targetsProbs[i] >= randomProbScore)
                {
                    m_targetIndex = i;
                    break;
                }
            }

            m_targetSwitchingWaitingDuration = 0;
        }

        else
            m_targetSwitchingWaitingDuration += Time.deltaTime;

        return orderedTargetPositions[m_targetIndex];
    }

    // Function to smooth the transition between the target positions to allow for realistic avatar IK animation.
    private void SmoothTargetTransition(Vector3 aimedTargetPosition)
    {
        // Setting the variables to initialize the smooth transition.
        if (!m_transitionInitialized)
        {
            m_finalTargetPosition = aimedTargetPosition;
            // Clamped transition duration proportional to the distance between the initial and the final positions.
            m_transitionDuration = Mathf.Clamp(Vector3.Distance(transform.position, m_finalTargetPosition) * Random.Range(0.2f, 0.3f), 0.05f, 1f);
            m_transitionInitialized = true;
        }

        // Smooth non linear transition toward the target position.
        float xPosition = Mathf.SmoothDamp(transform.position.x, m_finalTargetPosition.x, ref m_xVelocity, m_transitionDuration);
        float yPosition = Mathf.SmoothDamp(transform.position.y, m_finalTargetPosition.y, ref m_yVelocity, m_transitionDuration);
        float zPosition = Mathf.SmoothDamp(transform.position.z, m_finalTargetPosition.z, ref m_zVelocity, m_transitionDuration);

        transform.position = new Vector3(xPosition, yPosition, zPosition);

        // Allowing variables reinitialization if the transition is over or if the target changed during the transition process.
        if (transform.position == aimedTargetPosition || aimedTargetPosition != m_finalTargetPosition)
            m_transitionInitialized = false;
    }

    #endregion

    #region Randome gaze behavior

    // Recursive coroutine to handle the random gaze behavior.
    private IEnumerator RandomGazeBehavior(float updateDelay)
    {
        float randomVariation = Random.Range(-0.2f, 0.2f);
        float randomRecursiveDelay = Random.Range(3f, 5f);

        transform.position = CHGaze.NeutralTargetPosition + new Vector3(randomVariation, randomVariation, 0);

        yield return new WaitForSeconds(updateDelay);

        StartCoroutine(RandomGazeBehavior(randomRecursiveDelay));
    }

    #endregion

    #region Static gaze behavior

    // Function to set the static gaze behavior target position.
    private void StaticGazeBehavior()
    {
        transform.position = CHGaze.NeutralTargetPosition;
    }

    #endregion

    #region Scripted gaze behavior

    // Function to set the scripted gaze behavior target position.
    private void ScriptedGazeBehavior(Vector3 targetPosition)
    {
        transform.position = targetPosition;
    }

    #endregion
}
