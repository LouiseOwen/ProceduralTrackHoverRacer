using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShowSliderVal : MonoBehaviour {

    private TextMeshProUGUI valueText;
    [SerializeField] private Slider slider;

	// Use this for initialization
	void Start ()
    {
        valueText = GetComponent<TextMeshProUGUI>();
        valueText.text = slider.value.ToString();
	}
	
	public void TextUpdate(float value)
    {
        valueText.text = value.ToString();
    }
}
