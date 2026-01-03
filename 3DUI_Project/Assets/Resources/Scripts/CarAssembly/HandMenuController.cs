using UnityEngine;

public class HandMenuController : MonoBehaviour
{
    [SerializeField] private OVRHand leftHand; // Assign LEFT OVRHand
    [SerializeField] private GameObject menuCanvas; // Drag your World Space Canvas
    [SerializeField] private Vector3 offset = new Vector3(0, 0.15f, 0.05f); // Offset above palm

    private bool isMenuVisible = false;
    private bool wasPinching = false;

    void Update()
    {
        if (leftHand == null || menuCanvas == null) return;

        // Toggle on Left Index Pinch
        bool isPinching = leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        if (isPinching && !wasPinching)
        {
            isMenuVisible = !isMenuVisible;
            menuCanvas.SetActive(isMenuVisible);
        }
        wasPinching = isPinching;

        if (isMenuVisible)
        {
            // Follow palm
            Vector3 palmPos = leftHand.PointerPose.position;
            Quaternion palmRot = leftHand.PointerPose.rotation;

            transform.position = palmPos + (palmRot * offset);
            
            // Face user
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0); // Flip UI to face camera
        }
    }
}