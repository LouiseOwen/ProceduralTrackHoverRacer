using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShowSliderVal : MonoBehaviour
{
    private TextMeshProUGUI valueText;
    [SerializeField] private Slider slider;

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
