using UnityEngine;

public class FinishLine : MonoBehaviour
{
    // Lets GameManager know when a vehicle crosses the finish line, uses LapChecker to enable finish line (tries to reduce cheating)
    // i.e. player must pass through LapChecker to be able to register the lap

	[HideInInspector] public bool isReady;	// is the player passed the LapChecker? 

	public bool debugMode; // stops the need for LapChecker

	void OnTriggerEnter(Collider other)
	{
        // If the player has passed through the FinishLine (also checks if the player has passed the LapChecker or is in debug)
		if ((isReady || debugMode) && other.gameObject.CompareTag("PlayerSensor"))
		{
			GameManager.instance.PlayerCompletedLap(); // increment the player's lap count
			isReady = false; // so that the player must cross the LapChecker before they can use the FinishLine again
		}

        // If the AI has passed through the FinishLine
        if (other.gameObject.CompareTag("AISensor"))
        {
            // Code to extract the AI number from the gameobject name
            string aiName = other.gameObject.name;
            int aiNum;
            string[] nameSegments = aiName.Split('_');
            for (int i = 0; i < nameSegments.Length; i++)
            {
                if (int.TryParse(nameSegments[i], out aiNum))
                {
                    GameManager.instance.AICompletedLap(aiNum); // increment the AI's lap count
                }
            }
        }
	}
}
