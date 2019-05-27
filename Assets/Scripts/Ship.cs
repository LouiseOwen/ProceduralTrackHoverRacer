using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AIType { Advanced, Middle, Back, Close, Not }

public class Ship
{
    private int aiNumber;
    private string aiName; // for display purposes
    private AIType aiType;
    private int currentLap;
    private int currentWaypoint;
    private Vector3 currentPosition;
    private float counter;
    private PlayerInput aiControl;
    private bool lapReady = false;

    public Ship(int aiNum, string name, AIType type, int currLap, int currWaypoint, Vector3 currPos)
    {
        aiNumber = aiNum;
        aiName = name;
        aiType = type;
        currentLap = currLap;
        currentWaypoint = currWaypoint;
        currentPosition = currPos;
    }

    public Ship(int aiNum, string name, AIType type, int currLap, int currWaypoint, Vector3 currPos, GameObject shipObject)
    {
        aiNumber = aiNum;
        aiName = name;
        aiType = type;
        currentLap = currLap;
        currentWaypoint = currWaypoint;
        currentPosition = currPos;
        aiControl = shipObject.GetComponent<PlayerInput>();
    }

    // Getters
    public int GetAINumber()
    {
        return aiNumber;
    }

    public string GetAIName()
    {
        return aiName;
    }

    public AIType GetAIType()
    {
        return aiType;
    }

    public int GetCurrLap()
    {
        return currentLap;
    }

    public int GetCurrWaypoint()
    {
        return currentWaypoint;
    }

    public Vector3 GetCurrPos()
    {
        return currentPosition;
    }

    public float GetCounter()
    {
        return counter;
    }

    public bool LapReady()
    {
        return lapReady;
    }

    // Setters
    public void SetCurrPos(Vector3 currPos)
    {
        currentPosition = currPos;
    }

    public void SetCounter(float counterCalc)
    {
        counter = counterCalc;
    }

    public void IncrementCurrLap()
    {
        currentLap++;
    }

    public void SetCurrWaypoint(int currWaypoint)
    {
        currentWaypoint = currWaypoint;
    }

    public void SetAIType(AIType type)
    {
        aiType = type;
    }

    public void SetShipObject(GameObject shipObject)
    {
        aiControl = shipObject.GetComponent<PlayerInput>();
    }

    public void SetAIBestSkill()
    {
        aiControl.BestSkill();
    }

    public void SetAIMidSkill()
    {
        aiControl.MidSkill();
    }

    public void SetAIWorstSkill()
    {
        aiControl.WorstSkill();
    }

    public void SetLapReady(bool value)
    {
        lapReady = value;
    }
}
