using UnityEngine;
using TMPro;

public class LapTimeUI : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI[] lapTimeLabels;
	[SerializeField] private TextMeshProUGUI finalTimeLabel;

	void Awake()
	{
        // loop through UI elements and clear their text
        for (int i = 0; i < lapTimeLabels.Length; i++)
        {
            lapTimeLabels[i].text = "";
        }

		finalTimeLabel.text = "";
	}

	public void SetLapTime(int lapNumber, float lapTime)
	{
		if (lapNumber >= lapTimeLabels.Length) // if there aren't enough UI labels for the number of laps, don't try set any
			return;

		lapTimeLabels[lapNumber].text = ConvertTimeToString(lapTime);
	}

	public void SetFinalTime(float lapTime)
	{
		finalTimeLabel.text = ConvertTimeToString(lapTime);
	}

	string ConvertTimeToString(float time)
	{
        // convert time (seconds) into minutes and seconds
		int minutes = (int)(time / 60);
		float seconds = time % 60f;

		// format text
		string output = minutes.ToString("00") + ":" + seconds.ToString("00.000");
		return output;
	}
}
