using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RankingUI : MonoBehaviour {

    public GameObject[] rankingNumbers;
    public TextMeshProUGUI[] rankingLabels;

    private void Awake()
    {
        for (int i = 0; i < rankingNumbers.Length; i++)
        {
            rankingNumbers[i].SetActive(false);
        }

        for (int i = 0; i < rankingLabels.Length; i++)
        {
            rankingLabels[i].text = "";
        }
    }

    public void EnableRankingList()
    {
        for (int i = 0; i < rankingNumbers.Length; i++)
        {
            rankingNumbers[i].SetActive(true);
        }
    }

    public void SetRankingPosition(int rankingPos, string driverName)
    {
        rankingLabels[rankingPos].text = driverName;
    }
}
