using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BehaviourTreeRunner : MonoBehaviour
{
    BehaviourTree tree;
    // Start is called before the first frame update
    void Start()
    {
        tree = ScriptableObject.CreateInstance<BehaviourTree>();

       var log1 = ScriptableObject.CreateInstance<DebogLogNode>();
        log1.message = "Hello EveryOne! 111";

        var Pause1 = ScriptableObject.CreateInstance<WaitNode>();

        var log2 = ScriptableObject.CreateInstance<DebogLogNode>();
        log2.message = "Hello EveryOne! 222";

        var Pause2 = ScriptableObject.CreateInstance<WaitNode>();

        var log3 = ScriptableObject.CreateInstance<DebogLogNode>();
        log3.message = "Hello EveryOne! 333";

        var Pause3 = ScriptableObject.CreateInstance<WaitNode>();


        var sequence = ScriptableObject.CreateInstance<SequencerNode>();
        sequence.children.Add(log1);
        sequence.children.Add(Pause1);
        sequence.children.Add(log2);
        sequence.children.Add(Pause2);
        sequence.children.Add(log3);
        sequence.children.Add(Pause3);
        var loop = ScriptableObject.CreateInstance<RepeatNode>();
        loop.child = sequence;

        tree.rootNode = loop;
    }

    // Update is called once per frame
    void Update()
    {
        tree.Update();
    }
}
