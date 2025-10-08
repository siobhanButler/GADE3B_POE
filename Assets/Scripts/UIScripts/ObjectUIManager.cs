using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ObjectUIManager : MonoBehaviour
{
    public Slider healthFillImage; // Assign this in the Inspector
    public TextMeshProUGUI nameText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        healthFillImage.value = 1;
    }

    // Update method removed - UI updates handled by external calls

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthFillImage != null)
        {
            healthFillImage.value = currentHealth / maxHealth;
        }
    }
}
