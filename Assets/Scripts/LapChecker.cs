using UnityEngine;

public class LapChecker : MonoBehaviour
{
    // Lets FinishLine know when a vehicle crosses the LapChecker (tries to reduce cheating)
    // i.e. player must pass through LapChecker to be able to register the lap

    [SerializeField] private FinishLine finishLine;	// reference to FinishLine script

	void OnTriggerEnter(Collider other)
	{
		// if the Player passes through the LapChecker
		if (other.gameObject.CompareTag("PlayerSensor"))
		{
			finishLine.isReady = true; // they are allowed to use the FinishLine
		}

        // if the AI has passed through the LapChecker
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
                    GameManager.instance.AIPassedLapChecker(aiNum); // allow them to use the FinishLine
                }
            }
        }
    }
}
