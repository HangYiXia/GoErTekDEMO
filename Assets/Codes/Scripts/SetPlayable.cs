using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class SetPlayable : MonoBehaviour
{
    // public PlayableDirector director;

    // Start is called before the first frame update
    void Start()
    {
        PlayableDirector director = GetComponent<PlayableDirector>();
        var graph = director.playableGraph;
        Debug.Log(graph.GetRootPlayable(0));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
