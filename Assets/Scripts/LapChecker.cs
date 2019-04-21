//This script ensures that the player cannot cheat by "activating" the FinishLine 
//script once the player comes close to completing a full lap

using UnityEngine;
// THIS SCRIPT CHECKS THE LAST CHECKPOINT SO THAT THEY DON'T CHEAT - CAN PROBS CHANGE IT SO THAT WAYPOINTCHECKER KNOWS LAST WAYPOINT AND CHECKS AGAINST THAT
public class LapChecker : MonoBehaviour
{
	public FinishLine finishLine;	//Reference to the FinishLine script


	void OnTriggerEnter(Collider other)
	{
		//If the object passing through this collider is tagged as "PlayerSensor"...
		if (other.gameObject.CompareTag("PlayerSensor"))
		{
			//...set the isReady variable of the FinishLine script
			finishLine.isReady = true;
		}

        if (other.gameObject.CompareTag("AISensor"))
        {
            string aiName = other.gameObject.name;
            int aiNum;
            string[] nameSegments = aiName.Split('_');
            for (int i = 0; i < nameSegments.Length; i++)
            {
                if (int.TryParse(nameSegments[i], out aiNum))
                {
                    GameManager.instance.AIPassedLapChecker(aiNum);
                }
            }
        }
    }
}
