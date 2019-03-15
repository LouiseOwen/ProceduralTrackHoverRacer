using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoints : MonoBehaviour {

    public Color lineColour; // for visualisation

    private List<Transform> waypoints = new List<Transform>();

    void OnDrawGizmosSelected()
    {
        Gizmos.color = lineColour;

        Transform[] waypointTransforms = GetComponentsInChildren<Transform>();
        waypoints = new List<Transform>();

        for (int i = 0; i < waypointTransforms.Length; i++)
        {
            if (waypointTransforms[i] != transform)
            {
                waypoints.Add(waypointTransforms[i]);

            }

        }

        for (int i = 0; i < waypoints.Count; i++)
        {
            Vector3 currentNode = waypoints[i].position;
            Vector3 previousNode = Vector3.zero;

            if (i > 0)
            {
                previousNode = waypoints[i - 1].position;

            }
            else if (i == 0 && waypoints.Count > 1)
            {
                previousNode = waypoints[waypoints.Count - 1].position;

            }

            Gizmos.DrawLine(previousNode, currentNode);
            Gizmos.DrawWireSphere(currentNode, 0.3f);

        }

    } // end of OnDrawGizmo()

} // end of class
