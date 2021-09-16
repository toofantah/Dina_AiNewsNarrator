using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class CH_Manager : MonoBehaviour
{
    [Header("Preset settings:")]
    [Tooltip("Blend shapes preset matching the character's template. Use Window -> DINA Project -> Blend Shapes Mapper Editor to create a new preset.")]
    public BlendShapesMapper blendShapesMapperPreset;

    public int TotalCharacterBlendShapes { get; private set; } = 0;

    private List<SkinnedMeshRenderer> m_skinnedMeshRenderersWithBlendShapes = new List<SkinnedMeshRenderer>();

    private CH_Emotions m_CHEmotions;
    private CH_Gaze m_CHGaze;
    private CH_LipSync m_CHLipSync;

    private float[] m_emotionBlendShapeValues;
    private float[] m_gazeBlendShapeValues;
    private float[] m_lipBlendShapeValues;

    private float[] m_prioritizedBlendShapeValues;
    private float[] m_previousPrioritizedBlendShapeValues;

    private void Awake()
    {
        // Checking that a blenshapes mapper preset is added to the VHP manager.
        if (!blendShapesMapperPreset)
        {
            Debug.LogWarning("No blend shapes mapper preset. Please assign a mapper to enable procedural facial animations.");
            return;
        }

        // Getting the skinned mesh renderers with blend shapes of the character to use procedural facial animations.
        // Has to be executed in the Awake function as other CH_ scripts require the total blend shape number to set their respective blenshapes values lists.
        GetSkinnedMeshRenderersWithBlendShapes(gameObject);
    }

    private void OnEnable()
    {
        GetCHComponents();

        m_emotionBlendShapeValues = new float[TotalCharacterBlendShapes];
        m_gazeBlendShapeValues = new float[TotalCharacterBlendShapes];
        m_lipBlendShapeValues = new float[TotalCharacterBlendShapes];

        m_prioritizedBlendShapeValues = new float[TotalCharacterBlendShapes];
        m_previousPrioritizedBlendShapeValues = new float[TotalCharacterBlendShapes];

        SubscribeToBlendShapesEvent();
    }

    private void Update()
    {
        PrioritizeBlendShapeValues();
    }

    private void OnDisable()
    {
        UnsubscribeToBlendShapesEvent();
        ResetBlendShapeValues();
    }

    // Function to detect and add to a list the skinned mesh renderers with blend shapes of the character.
    private void GetSkinnedMeshRenderersWithBlendShapes(GameObject character)
    {
        // Getting all the child objects with a skinned mesh renderer.
        SkinnedMeshRenderer[] skinnedMeshRenderers = character.GetComponentsInChildren<SkinnedMeshRenderer>();

        // Each skinned mesh renderer containing blend shapes is added to the list of skinned mesh renderers with blend shapes.
        foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
        {
            if (skinnedMeshRenderer.sharedMesh.blendShapeCount > 0)
            {
                m_skinnedMeshRenderersWithBlendShapes.Add(skinnedMeshRenderer);
                TotalCharacterBlendShapes += skinnedMeshRenderer.sharedMesh.blendShapeCount;
            }
        }

        // A warning message is displayed if the character does not contain any skinned mesh renderer with blendshapes.
        if (!m_skinnedMeshRenderersWithBlendShapes.Any())
            Debug.LogWarning("No skinned mesh renderer with blend shapes detected on the character.");
    }

    // Function to get all the CH_ components.
    private void GetCHComponents()
    {
        if (gameObject.GetComponent<CH_Emotions>())
            m_CHEmotions = gameObject.GetComponent<CH_Emotions>();

        if (gameObject.GetComponent<CH_Gaze>())
            m_CHGaze = gameObject.GetComponent<CH_Gaze>();

        if (gameObject.GetComponent<CH_LipSync>())
            m_CHLipSync = gameObject.GetComponent<CH_LipSync>();
    }

    // Function to subscribe to all the events updating the blend shape values.
    private void SubscribeToBlendShapesEvent()
    {
        if (m_CHEmotions)
            m_CHEmotions.OnEmotionsChange += CollectEmotionBlendShapeValues;

        if (m_CHGaze)
            m_CHGaze.OnGazeChange += CollectGazeBlendShapeValues;

        if (m_CHLipSync)
            m_CHLipSync.OnLipChange += CollectLipBlendShapeValues;
    }

    // Function to unsubscribe to all the events updating the blendshapes values.
    private void UnsubscribeToBlendShapesEvent()
    {
        if (m_CHEmotions)
            m_CHEmotions.OnEmotionsChange -= CollectEmotionBlendShapeValues;

        if (m_CHGaze)
            m_CHGaze.OnGazeChange -= CollectGazeBlendShapeValues;

        if (m_CHLipSync)
            m_CHLipSync.OnLipChange -= CollectLipBlendShapeValues;
    }

    private void CollectEmotionBlendShapeValues(float[] blendShapeValues)
    {
        m_emotionBlendShapeValues = blendShapeValues;
    }

    private void CollectGazeBlendShapeValues(float[] blendShapeValues)
    {
        m_gazeBlendShapeValues = blendShapeValues;
    }

    private void CollectLipBlendShapeValues(float[] blendShapeValues)
    {
        m_lipBlendShapeValues = blendShapeValues;
    }

    // Function to prioritize the collected blend shape values (emotion, gaze and lip sync blend shapes).
    private void PrioritizeBlendShapeValues()
    {
        if (m_skinnedMeshRenderersWithBlendShapes.Any())
        {
            // Lip sync blend shapes are considered first. Then, emotions blend shapes are applied if they do not override the lip sync ones.
            // The same process is repeated with the gaze blend shapes. Finally 0 is added if no lip sync/emotion/gaze blend shape is applied.
            for (int i = 0; i < TotalCharacterBlendShapes; i++)
            {
                if (m_lipBlendShapeValues[i] != 0)
                    m_prioritizedBlendShapeValues[i] = m_lipBlendShapeValues[i];

                else if (m_emotionBlendShapeValues[i] != 0)
                    m_prioritizedBlendShapeValues[i] = m_emotionBlendShapeValues[i];

                else if (m_gazeBlendShapeValues[i] != 0)
                    m_prioritizedBlendShapeValues[i] = m_gazeBlendShapeValues[i];

                else
                    m_prioritizedBlendShapeValues[i] = 0;
            }

            // Starting the coroutine to update the character's blend shapes if the values are different from the previous ones.
            if (m_prioritizedBlendShapeValues != m_previousPrioritizedBlendShapeValues)
            {
                StopAllCoroutines();
                StartCoroutine(LerpBlendShapeValues(m_prioritizedBlendShapeValues));

                System.Array.Copy(m_prioritizedBlendShapeValues, m_previousPrioritizedBlendShapeValues, TotalCharacterBlendShapes);
            }
        }

        else
            Debug.LogWarning("No skinned mesh renderers with blend shapes");
    }

    public static int counter = 0;
    // Coroutine to interpolate the blend shape values to create a progressive transition between their intial and their targeted values.
    private IEnumerator LerpBlendShapeValues(float[] blendShapeValues)
    {
        List<float> initialBlenshapeValues = new List<float>();
        float elapsedTime = 0;
        float lerpDuration = 0.05f;
        float currentBlendShapeValue;

        // Storing the initial blend shape values.
        foreach (SkinnedMeshRenderer skinnedMeshRenderer in m_skinnedMeshRenderersWithBlendShapes)
            for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
                initialBlenshapeValues.Add(skinnedMeshRenderer.GetBlendShapeWeight(i));

        // Updating smoothly the blend shape values from their initial values to the desired ones.
        while (elapsedTime < lerpDuration)
        {
            elapsedTime += Time.deltaTime;

            int blendShapeIndex = 0;

            foreach (SkinnedMeshRenderer skinnedMeshRenderer in m_skinnedMeshRenderersWithBlendShapes)
            {
                for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
                {
                    if (initialBlenshapeValues[blendShapeIndex] != blendShapeValues[blendShapeIndex])
                    {
                        currentBlendShapeValue = Mathf.Lerp(initialBlenshapeValues[blendShapeIndex], blendShapeValues[blendShapeIndex], (elapsedTime / lerpDuration));
                        skinnedMeshRenderer.SetBlendShapeWeight(i, currentBlendShapeValue);
                    }

                    blendShapeIndex++;
                }
            }

            yield return new WaitForEndOfFrame();
        }
    }

    // Function to reset the character blend shape values.
    private void ResetBlendShapeValues()
    {
        if (m_skinnedMeshRenderersWithBlendShapes.Any())
        {
            int blendshapeIndex = 0;

            // For each skinned mesh renderer of the character all the blend shapes are updated according to list of values sent to the CH_manager.
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in m_skinnedMeshRenderersWithBlendShapes)
            {
                for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
                {
                    skinnedMeshRenderer.SetBlendShapeWeight(i, 0);

                    blendshapeIndex++;
                }
            }
        }

        else
            Debug.LogWarning("No skinned mesh renderers with blend shapes");
    }
}
