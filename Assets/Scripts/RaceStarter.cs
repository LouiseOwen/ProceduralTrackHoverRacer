using UnityEngine;
using UnityEngine.Playables;

public class RaceStarter : MonoBehaviour
{
    // Checks if cutscene has finished before starting race

    [SerializeField] private PlayableDirector cutscene;

    void OnEnable()
    {
        cutscene.stopped += OnPlayableDirectorStopped;
    }

    void OnPlayableDirectorStopped(PlayableDirector aDirector)
    {
        if (cutscene == aDirector)
        {
            //Debug.Log("PlayableDirector named " + aDirector.name + " is now stopped.");
            GameManager.instance.SetRaceHasBegun();
        }
    }
}
