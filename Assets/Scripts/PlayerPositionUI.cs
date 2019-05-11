using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerPositionUI : MonoBehaviour {

    public TextMeshProUGUI playerPosLabel;

    private void Awake()
    {
        playerPosLabel.text = "";
    }

    public void SetPlayerPos(int playerPos)
    {
        playerPosLabel.text = playerPos.ToString();
    }
}
