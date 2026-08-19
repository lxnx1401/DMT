using TMPro;
using UnityEngine;

public class ParticleCountHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text countText;

    void Update()
    {
        if (countText == null || ParticleSimulation.Instance == null)
            return;

        countText.text = ParticleSimulation.Instance.ActiveParticles.ToString("000");
    }
}
