using UnityEngine;
using UnityEngine.Playables;

public class PlayController : MonoBehaviour
{
    private PlayableDirector timeline;

    
    void Start()
    {
        timeline = this.transform.GetChild(0).GetComponent<PlayableDirector>();
    }

    public void StartPlay()
    {
        timeline.Play();
    }
}
