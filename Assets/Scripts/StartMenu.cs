using UnityEngine;

public class StartMenu : MonoBehaviour
{
    [SerializeField] private GameObject startMenuUI;
    [SerializeField] private GameObject road;

    [SerializeField] private GameObject startMenuRoad;
    [SerializeField] private GameObject keyUI;

    [SerializeField] private Camera leftCamera;
    [SerializeField] private GameObject rightCamera;

    private Rect singleScreen = new Rect(0.0f, 0.0f, 1.0f, 1.0f);

    public void SetStartMenuActive(bool value)
    {
        startMenuUI.SetActive(value);

        startMenuRoad.SetActive(value);
        keyUI.SetActive(value);

        // Turn cameras into single screen
        leftCamera.rect = singleScreen;
        rightCamera.SetActive(false);
    }

    public void SetRoadActive(bool value)
    {
        road.SetActive(value);
    }
}
