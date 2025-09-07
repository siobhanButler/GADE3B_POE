using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ObjectUIManager : MonoBehaviour
{
    public Image healthFillImage; // Assign this in the Inspector
    public TextMeshProUGUI nameText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = currentHealth / maxHealth;
        }
    }
}
