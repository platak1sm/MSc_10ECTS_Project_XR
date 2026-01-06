using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndexPinchManipulator2step : MonoBehaviour
{
    [SerializeField] private GazeProvider gazeProvider;
    [SerializeField] private OVRHand manipulationHand; 
    [SerializeField] private GazePointerManager2step pointerManager;
    
    [SerializeField] private long lookbackMs = 200;
    [SerializeField] private string manipulableTag = "Interactable";
    [SerializeField] private float moveSensitivity = 1f;

    private bool isPinching;
    private GameObject selectedObject;
    private Vector3 grabHandPosition;
    private Quaternion grabHandRotation;
    private Vector3 pivotPoint;
    private Camera eyeCamera;

    void Start()
    {
        eyeCamera = Camera.main;
        if (InteractionToolManager.Instance == null) Debug.LogError("Missing InteractionToolManager in scene!");
        if (gazeProvider == null) gazeProvider = FindFirstObjectByType<GazeProvider>();
        if (pointerManager == null) pointerManager = FindFirstObjectByType<GazePointerManager2step>();
        
        if (manipulationHand == null) {
            var hands = FindObjectsByType<OVRHand>(FindObjectsSortMode.None);
            foreach (var h in hands) {
                if (h.GetComponent<OVRSkeleton>()?.GetSkeletonType() == OVRSkeleton.SkeletonType.HandRight) {
                    manipulationHand = h; break;
                }
            }
        }
    }

    void LateUpdate()
    {
        if (manipulationHand == null || gazeProvider == null || pointerManager == null) return;

        bool pinching = manipulationHand.GetFingerIsPinching(OVRHand.HandFinger.Index);

        if (!isPinching && pinching)
        {
            isPinching = true; // Lock pinch state

            // Get the target
            GameObject target = GetTargetObject();

            // Check for UI Buttons
            if (target != null)
            {
                VRButton btn = target.GetComponent<VRButton>();
                if (btn != null)
                {
                    btn.Click();
                    return; // Wait for release
                }
            }

            // Check Spawn Mode
            InteractionTool tool = InteractionToolManager.Instance.CurrentTool;
            if (tool == InteractionTool.Spawn)
            {
                if (pointerManager.CurrentPointer != null)
                {
                    SpawnObjectAtPointer();
                }
                return; // Wait for release
            }

            // Handle Object Interaction
            if (target != null)
            {
                HandleObjectAction(target, tool);
            }
            else
            {
                isPinching = false;
            }
        } 
        else if (isPinching && !pinching)
        {
            isPinching = false;
            selectedObject = null;
        }

        if (selectedObject != null)
        {
            ApplyMoveRotate();
        }
    }

    private GameObject GetTargetObject()
    {
        if (pointerManager.HoveredObject != null) return pointerManager.HoveredObject;
        
        var history = gazeProvider.GetGazeHistoryEntry(lookbackMs);
        if (history != null && history.gazeTarget != null && history.gazeTarget.CompareTag(manipulableTag))
        {
            return history.gazeTarget;
        }
        return null;
    }

    private void SpawnObjectAtPointer()
    {
        GameObject prefab = InteractionToolManager.Instance.PrefabToSpawn;
        if (prefab != null && pointerManager.CurrentPointer != null)
        {
            Vector3 spawnPos = pointerManager.CurrentPointer.transform.position;
            Instantiate(prefab, spawnPos, Quaternion.identity);
        }
    }

    private void HandleObjectAction(GameObject target, InteractionTool tool)
    {
        switch (tool)
        {
            case InteractionTool.MoveRotate:
                selectedObject = target;
                pivotPoint = (pointerManager.CurrentPointer != null) ? pointerManager.CurrentPointer.transform.position : target.transform.position;
                grabHandPosition = manipulationHand.transform.position;
                grabHandRotation = manipulationHand.transform.rotation;
                break;

            case InteractionTool.Delete:
                var outline = target.GetComponent<Outline>();
                if (outline != null) 
                {
                    outline.enabled = false;
                    Destroy(outline); 
                }

                pointerManager.ForceClearSelection(); 
                Destroy(target);
                break;

            case InteractionTool.Duplicate:
                GameObject clone = Instantiate(target, target.transform.position, target.transform.rotation);
                selectedObject = clone;
                pivotPoint = clone.transform.position;
                grabHandPosition = manipulationHand.transform.position;
                grabHandRotation = manipulationHand.transform.rotation;
                break;

            case InteractionTool.ColorPicker:
                Renderer r = target.GetComponent<Renderer>();
                if (r != null) r.material.color = InteractionToolManager.Instance.ActivePaintColor;
                break;
        }
    }

    private void ApplyMoveRotate()
    {
        if (selectedObject == null) return;

        Vector3 currentHandPos = manipulationHand.transform.position;
        Quaternion currentHandRot = manipulationHand.transform.rotation;

        Vector3 handDelta = currentHandPos - grabHandPosition;
        float gain = Vector3.Distance(eyeCamera.transform.position, selectedObject.transform.position) / 
                     Mathf.Max(Vector3.Distance(eyeCamera.transform.position, currentHandPos), 0.01f);
        
        selectedObject.transform.position += handDelta * moveSensitivity * gain;

        Quaternion rotDelta = currentHandRot * Quaternion.Inverse(grabHandRotation);
        selectedObject.transform.position -= pivotPoint;
        selectedObject.transform.rotation = rotDelta * selectedObject.transform.rotation;
        selectedObject.transform.position += pivotPoint;

        grabHandPosition = currentHandPos;
        grabHandRotation = currentHandRot;
    }
}