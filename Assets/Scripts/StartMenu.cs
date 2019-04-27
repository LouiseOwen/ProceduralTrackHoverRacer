using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartMenu : MonoBehaviour {

    [SerializeField] private GameObject startMenuUI;
    [SerializeField] private GameObject road;

    public void SetStartMenuActive(bool value)
    {
        startMenuUI.SetActive(value);
    }

    public void SetRoadActive(bool value)
    {
        road.SetActive(value);
    }
}
