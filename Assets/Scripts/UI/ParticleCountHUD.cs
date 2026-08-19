using TMPro;
using UnityEngine;

public class ParticleCountHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text countText;

    void Update()
    {
        if (countText == null)
            return;

        bool isEndlessMode = LevelManager.Instance != null && LevelManager.Instance.IsEndlessMode;

        if (isEndlessMode)
        {
            if (GameManager.Instance == null)
                return;

            countText.text = GameManager.Instance.TotalCrownsThisRun.ToString("000");
        }
        else
        {
            if (ParticleSimulation.Instance == null)
                return;

            countText.text = ParticleSimulation.Instance.ActiveParticles.ToString("000");
        }
    }
}
