using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointChecker : MonoBehaviour
{
    int waypointNum;

    void Start()
    {
        string[] nameSegments = gameObject.name.Split(' ');
        for (int i = 0; i < nameSegments.Length; i++)
        {
            if (int.TryParse(nameSegments[i], out waypointNum));
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerSensor"))
        {
            GameManager.instance.PlayerPassedWaypoint(waypointNum);
        }

        if (other.CompareTag("AISensor"))
        {
            string aiName = other.gameObject.name;
            int aiNum;
            string[] nameSegments = aiName.Split('_');
            for (int i = 0; i < nameSegments.Length; i++)
            {
                if (int.TryParse(nameSegments[i], out aiNum))
                {
                    GameManager.instance.AIPassedWaypoint(aiNum, waypointNum);
                }
            }
        }
    }

}
