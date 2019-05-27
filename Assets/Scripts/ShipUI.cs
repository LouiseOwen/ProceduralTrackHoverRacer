using UnityEngine;
using TMPro;

public class ShipUI : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI currentSpeedText;
	[SerializeField] private TextMeshProUGUI currentLapText;

	public void SetLapDisplay(int currentLap, int numberOfLaps)
	{
		if (currentLap > numberOfLaps) // don't set lap number if its higher than the total number of laps
			return;

		currentLapText.text = currentLap + "/" + numberOfLaps;
	}

	public void SetSpeedDisplay(float currentSpeed)
	{
		int speed = (int)currentSpeed;
		currentSpeedText.text = speed.ToString();
	}
}