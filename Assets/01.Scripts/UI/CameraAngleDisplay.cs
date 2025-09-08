using UnityEngine;
using TMPro;

public class CameraAngleDisplay : MonoBehaviour
{
    [SerializeField] private Transform cameraTarget; // Assign the camera target whose Y rotation we care about
    [SerializeField] private TextMeshProUGUI angleText; // Assign the TMP text field that should display the angle

    void Update()
    {
        if (cameraTarget == null || angleText == null) return;

        float angle = Mathf.DeltaAngle(0f, cameraTarget.eulerAngles.y);
        angleText.text = $"Rotate View: {Mathf.RoundToInt(angle)}°";
    }
}