using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class RaceStarter : MonoBehaviour {

    //[SerializeField] private GameObject cutsceneObject;
    [SerializeField] private PlayableDirector cutscene;

	// Use this for initialization
	void Start ()
    {
        //cutscene = cutsceneObject.GetComponent<PlayableDirector>();
	}
	
	// Update is called once per frame
	void Update ()
    {

	}

    void OnEnable()
    {
        cutscene.stopped += OnPlayableDirectorStopped;
    }

    void OnPlayableDirectorStopped(PlayableDirector aDirector)
    {
        if (cutscene == aDirector)
            Debug.Log("PlayableDirector named " + aDirector.name + " is now stopped.");
        GameManager.instance.SetRaceHasBegun();
    }
}
