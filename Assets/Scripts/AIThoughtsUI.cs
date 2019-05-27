using UnityEngine;
using UnityEngine.UI;

public class AIThoughtsUI : MonoBehaviour
{
    public GameObject aiThoughtsPanel;
    public Text[] aiThoughts;

    private void Awake()
    {
        aiThoughtsPanel.SetActive(false);

        for (int i = 0; i < aiThoughts.Length; i++)
        {
            aiThoughts[i].text = "";
        }
    }

    public void EnableAIThoughtsList()
    {
        aiThoughtsPanel.SetActive(true);
    }

    public void SetAIThought(int aiNum, string thought)
    {
        aiThoughts[aiNum].text = thought;
    }
}
