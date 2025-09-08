using UnityEngine;
using UnityEngine.UI;
using TMPro; // Add this for TextMeshPro

public class ButtonInputValidator : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField; // Changed to TMP_InputField
    [SerializeField] private Button targetButton;
    
    void Update()
    {
        targetButton.interactable = !string.IsNullOrEmpty(inputField.text.Trim());
    }
}