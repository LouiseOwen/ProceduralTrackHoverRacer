using UnityEngine;

public class WaypointChecker : MonoBehaviour
{
    int waypointNum;

    void Start()
    {
        // Code to extract waypoint index from gameobject name
        string[] nameSegments = gameObject.name.Split(' ');
        for (int i = 0; i < nameSegments.Length; i++)
        {
            if (int.TryParse(nameSegments[i], out waypointNum));
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // if player passes through waypoint
        if (other.CompareTag("PlayerSensor"))
        {
            GameManager.instance.PlayerPassedWaypoint(waypointNum); // set their waypoint index to this waypoint
        }

        // if AI passes through waypoint
        if (other.CompareTag("AISensor"))
        {
            // Code to extract AI number from gameobject name
            string aiName = other.gameObject.name;
            int aiNum;
            string[] nameSegments = aiName.Split('_');
            for (int i = 0; i < nameSegments.Length; i++)
            {
                if (int.TryParse(nameSegments[i], out aiNum))
                {
                    GameManager.instance.AIPassedWaypoint(aiNum, waypointNum); // set their waypoint index to this waypoint
                }
            }
        }
    }

}
