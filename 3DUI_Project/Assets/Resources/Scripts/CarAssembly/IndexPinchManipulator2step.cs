// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class IndexPinchManipulator2step : MonoBehaviour
// {
//     [SerializeField] private GazeProvider gazeProvider;
//     [SerializeField] private OVRHand manipulationHand;
//     [SerializeField] private GazePointerManager2step pointerManager;
    
//     [SerializeField] private long lookbackMs = 200;
//     [SerializeField] private string manipulableTag = "Interactable";
//     [SerializeField] private float moveSensitivity = 1f;

//     private bool isPinching;
//     private GameObject selectedObject;
//     private Vector3 grabHandPosition;
//     private Quaternion grabHandRotation;
//     private Vector3 grabObjectPosition;
//     private Quaternion grabObjectRotation;
//     private Vector3 pivotPoint;
//     private Camera eyeCamera;

//     public bool IsPinching => isPinching;

//     void Start()
//     {
//         eyeCamera = Camera.main;
//         if (eyeCamera == null) { Debug.LogError("IndexPinchManipulator2step: No main camera found."); enabled = false; return; }
        
//         if (InteractionToolManager.Instance == null) Debug.LogWarning("No InteractionToolManager found in scene! Please create one.");

//         if (gazeProvider == null) gazeProvider = FindFirstObjectByType<GazeProvider>();
//         if (manipulationHand == null)
//         {
//             OVRHand[] hands = FindObjectsByType<OVRHand>(FindObjectsSortMode.None);
//             foreach (var hand in hands)
//             {
//                 var skeleton = hand.GetComponent<OVRSkeleton>();
//                 if (skeleton != null && skeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.HandRight)
//                 {
//                     manipulationHand = hand;
//                     break;
//                 }
//             }
//         }
//         if (pointerManager == null) pointerManager = FindFirstObjectByType<GazePointerManager2step>();
//     }

    
//     void LateUpdate()
//     {
//         if (manipulationHand == null || gazeProvider == null || pointerManager == null || eyeCamera == null) return;

//         bool pinching = manipulationHand.GetFingerIsPinching(OVRHand.HandFinger.Index);

//         // --- PINCH START ---
//         if (!isPinching && pinching)
//         {
//             isPinching = true;

//             // 1. Check for UI Interaction (VR Buttons)
//             GameObject hoverTarget = pointerManager.HoveredObject;
//             if (hoverTarget != null)
//             {
//                 VRButton btn = hoverTarget.GetComponent<VRButton>();
//                 if (btn != null)
//                 {
//                     btn.Click();
//                     isPinching = false; // Release immediately so we don't drag the button
//                     return;
//                 }
//             }

//             // 2. Handle Tools (Spawn, Delete, etc.)
//             InteractionTool tool = InteractionToolManager.Instance != null ? InteractionToolManager.Instance.CurrentTool : InteractionTool.MoveRotate;
            
//             if (tool == InteractionTool.Spawn)
//             {
//                 HandleSpawn();
//                 isPinching = false; // Single click action
//                 return;
//             }

//             // 3. Select Object for Manipulation (Move/Delete/Color)
//             GameObject targetToManipulate = GetTarget();
            
//             if (targetToManipulate != null)
//             {
//                 // Execute tool logic
//                 HandleToolOnObject(targetToManipulate, tool);
//             }
//             else
//             {
//                 isPinching = false;
//             }
//         } 
//         // --- PINCH RELEASE ---
//         else if (isPinching && !pinching)
//         {
//             isPinching = false;
//             selectedObject = null;
//         }

//         // --- MANIPULATION UPDATE (Only for Move Tool) ---
//         if (selectedObject != null)
//         {
//             PerformMoveRotate();
//         }
//     }

//     private GameObject GetTarget()
//     {
//         // Priority 1: Hovered Object (from Pointer or Gaze)
//         GameObject target = pointerManager.HoveredObject;
        
//         // Priority 2: Historical Gaze (Fallback)
//         if (target == null)
//         {
//             var historicalEntry = gazeProvider.GetGazeHistoryEntry(lookbackMs);
//             if (historicalEntry != null && historicalEntry.gazeTarget != null && historicalEntry.gazeTarget.CompareTag(manipulableTag))
//             {
//                 target = historicalEntry.gazeTarget;
//             }
//         }
//         return target;
//     }

//     private void HandleSpawn()
//     {
//         // Spawning requires the 3D pointer to be active (Middle Pinching) to know WHERE to spawn
//         if (pointerManager.CurrentPointer != null && InteractionToolManager.Instance.PrefabToSpawn != null)
//         {
//             Instantiate(InteractionToolManager.Instance.PrefabToSpawn, pointerManager.CurrentPointer.transform.position, Quaternion.identity);
            
//             // Optional: Switch back to Move tool after spawning?
//             // InteractionToolManager.Instance.SetTool(InteractionTool.MoveRotate); 
//         }
//         else
//         {
//             Debug.Log("Cannot Spawn: Ensure Middle Pinch (Pointer) is active.");
//         }
//     }

//     private void HandleToolOnObject(GameObject target, InteractionTool tool)
//     {
//         switch (tool)
//         {
//             case InteractionTool.MoveRotate:
//                 selectedObject = target;
                
//                 // Set Pivot: Use Pointer tip if active, else Object center
//                 if (pointerManager.CurrentPointer != null)
//                 {
//                     pivotPoint = pointerManager.CurrentPointer.transform.position;
//                     // Note: We do NOT clear the pointer here for 2-step, as you might want to adjust your grip
//                 }
//                 else
//                 {
//                     pivotPoint = selectedObject.transform.position;
//                 }

//                 grabHandPosition = manipulationHand.transform.position;
//                 grabHandRotation = manipulationHand.transform.rotation;
//                 grabObjectPosition = selectedObject.transform.position;
//                 grabObjectRotation = selectedObject.transform.rotation;
//                 break;

//             case InteractionTool.Delete:
//                 Destroy(target);
//                 isPinching = false;
//                 break;

//             case InteractionTool.Duplicate:
//                 GameObject clone = Instantiate(target, target.transform.position, target.transform.rotation);
//                 selectedObject = clone; // Automatically grab the clone
//                 pivotPoint = clone.transform.position;
//                 grabHandPosition = manipulationHand.transform.position;
//                 grabHandRotation = manipulationHand.transform.rotation;
//                 break;

//             case InteractionTool.ColorPicker:
//                 Renderer r = target.GetComponent<Renderer>();
//                 if (r != null && InteractionToolManager.Instance != null)
//                 {
//                     r.material.color = InteractionToolManager.Instance.paintColor;
//                 }
//                 isPinching = false;
//                 break;
//         }
//     }

//     private void PerformMoveRotate()
//     {
//         Vector3 currentHandPos = manipulationHand.transform.position;
//         Quaternion currentHandRot = manipulationHand.transform.rotation;

//         Vector3 handDeltaThisFrame = currentHandPos - grabHandPosition;

//         Vector3 eyePoint = eyeCamera.transform.position;
//         float eyeHandDist = Vector3.Distance(eyePoint, currentHandPos);
//         float eyeObjectDist = Vector3.Distance(eyePoint, selectedObject.transform.position);
//         float visualAngleGain = eyeObjectDist / Mathf.Max(eyeHandDist, 0.01f);

//         // 1. Apply Translation
//         Vector3 scaledDelta = handDeltaThisFrame * moveSensitivity * visualAngleGain;
//         selectedObject.transform.position += scaledDelta;

//         // 2. Apply Rotation (around pivot)
//         Quaternion handDeltaRotation = currentHandRot * Quaternion.Inverse(grabHandRotation);
//         selectedObject.transform.position -= pivotPoint;
//         selectedObject.transform.rotation = handDeltaRotation * selectedObject.transform.rotation;
//         selectedObject.transform.position += pivotPoint;

//         grabHandPosition = currentHandPos;
//         grabHandRotation = currentHandRot;
//         grabObjectPosition = selectedObject.transform.position;
//         grabObjectRotation = selectedObject.transform.rotation;
//     }
// }

// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class IndexPinchManipulator2step : MonoBehaviour
// {
//     [SerializeField] private GazeProvider gazeProvider;
//     [SerializeField] private OVRHand manipulationHand; // RIGHT HAND
//     [SerializeField] private GazePointerManager2step pointerManager;
    
//     [SerializeField] private long lookbackMs = 200;
//     [SerializeField] private string manipulableTag = "Interactable";
//     [SerializeField] private float moveSensitivity = 1f;

//     private bool isPinching;
//     private GameObject selectedObject;
//     private Vector3 grabHandPosition;
//     private Quaternion grabHandRotation;
//     private Vector3 pivotPoint;
//     private Camera eyeCamera;

//     void Start()
//     {
//         eyeCamera = Camera.main;
//         if (InteractionToolManager.Instance == null) Debug.LogError("Missing InteractionToolManager in scene!");
//         if (gazeProvider == null) gazeProvider = FindFirstObjectByType<GazeProvider>();
//         if (pointerManager == null) pointerManager = FindFirstObjectByType<GazePointerManager2step>();
        
//         // Auto-find Right Hand if not assigned
//         if (manipulationHand == null) {
//             var hands = FindObjectsByType<OVRHand>(FindObjectsSortMode.None);
//             foreach (var h in hands) {
//                 if (h.GetComponent<OVRSkeleton>()?.GetSkeletonType() == OVRSkeleton.SkeletonType.HandRight) {
//                     manipulationHand = h; break;
//                 }
//             }
//         }
//     }

//     void LateUpdate()
//     {
//         if (manipulationHand == null || gazeProvider == null || pointerManager == null) return;

//         bool pinching = manipulationHand.GetFingerIsPinching(OVRHand.HandFinger.Index);

//         // --- 1. PINCH START ---
//         if (!isPinching && pinching)
//         {
//             isPinching = true;

//             // STEP 1: Find the target immediately (Check Pointer Hover FIRST, then Gaze Fallback)
//             GameObject target = GetTargetObject();

//             if (target != null)
//             {
//                 // STEP 2: Check if the target is a BUTTON
//                 // We do this BEFORE checking for tools like Move or Spawn.
//                 VRButton btn = target.GetComponent<VRButton>();
//                 if (btn != null)
//                 {
//                     btn.Click();
//                     isPinching = false; // Release pinch immediately
//                     return; // STOP. Do not run any movement or spawn logic.
//                 }

//                 // STEP 3: Check Tool Logic (Spawn, Move, etc.)
//                 InteractionTool tool = InteractionToolManager.Instance.CurrentTool;

//                 if (tool == InteractionTool.Spawn)
//                 {
//                     // SPAWN MODE: Requires Middle Pinch (Pointer) + Index Pinch
//                     if (pointerManager.CurrentPointer != null)
//                     {
//                         SpawnObjectAtPointer();
//                     }
//                     isPinching = false; // Single click action
//                     return;
//                 }

//                 // STEP 4: Object Interaction (Move, Delete, Dup, Color)
//                 HandleObjectAction(target, tool);
//             }
//             else
//             {
//                 isPinching = false; // Nothing hit
//             }
//         } 
//         // --- 2. PINCH RELEASE ---
//         else if (isPinching && !pinching)
//         {
//             isPinching = false;
//             selectedObject = null;
//         }

//         // --- 3. MANIPULATION (Move/Rotate Mode Only) ---
//         if (selectedObject != null)
//         {
//             ApplyMoveRotate();
//         }
//     }

//     private GameObject GetTargetObject()
//     {
//         // Use Hover if available (Pointer)
//         if (pointerManager.HoveredObject != null) return pointerManager.HoveredObject;
        
//         // Fallback to Gaze History
//         var history = gazeProvider.GetGazeHistoryEntry(lookbackMs);
//         if (history != null && history.gazeTarget != null && history.gazeTarget.CompareTag(manipulableTag))
//         {
//             return history.gazeTarget;
//         }
//         return null;
//     }

//     private void SpawnObjectAtPointer()
//     {
//         GameObject prefab = InteractionToolManager.Instance.PrefabToSpawn;
//         if (prefab != null && pointerManager.CurrentPointer != null)
//         {
//             Vector3 spawnPos = pointerManager.CurrentPointer.transform.position;
//             Instantiate(prefab, spawnPos, Quaternion.identity);
//         }
//     }

//     private void HandleObjectAction(GameObject target, InteractionTool tool)
//     {
//         switch (tool)
//         {
//             case InteractionTool.MoveRotate:
//                 selectedObject = target;
//                 // If using pointer, pivot is pointer tip. If gaze, pivot is object center.
//                 pivotPoint = (pointerManager.CurrentPointer != null) ? pointerManager.CurrentPointer.transform.position : target.transform.position;
//                 grabHandPosition = manipulationHand.transform.position;
//                 grabHandRotation = manipulationHand.transform.rotation;
//                 break;

//             case InteractionTool.Delete:
//                 Destroy(target);
//                 isPinching = false;
//                 break;

//             case InteractionTool.Duplicate:
//                 // 1. Create the clone at the exact same position/rotation
//                 GameObject clone = Instantiate(target, target.transform.position, target.transform.rotation);
                
//                 // 2. IMPORTANT: Switch our selection to the *clone* immediately
//                 selectedObject = clone;

//                 // 3. Set the pivot. Since we are grabbing it instantly, use the object's center 
//                 pivotPoint = clone.transform.position;

//                 // 4. Update the grab offsets so we can move it immediately
//                 grabHandPosition = manipulationHand.transform.position;
//                 grabHandRotation = manipulationHand.transform.rotation;
                
//                 // 5. DO NOT set isPinching = false! 
//                 // We want to keep holding the clone so we can pull it away from the original.
//                 break;

//             case InteractionTool.ColorPicker:
//                 Renderer r = target.GetComponent<Renderer>();
//                 if (r != null)
//                 {
//                     r.material.color = InteractionToolManager.Instance.ActivePaintColor;
//                 }
//                 isPinching = false;
//                 break;
//         }
//     }

//     private void ApplyMoveRotate()
//     {
//         Vector3 currentHandPos = manipulationHand.transform.position;
//         Quaternion currentHandRot = manipulationHand.transform.rotation;

//         Vector3 handDelta = currentHandPos - grabHandPosition;
        
//         // Visual angle gain logic
//         float gain = Vector3.Distance(eyeCamera.transform.position, selectedObject.transform.position) / 
//                      Mathf.Max(Vector3.Distance(eyeCamera.transform.position, currentHandPos), 0.01f);
        
//         selectedObject.transform.position += handDelta * moveSensitivity * gain;

//         Quaternion rotDelta = currentHandRot * Quaternion.Inverse(grabHandRotation);
//         selectedObject.transform.position -= pivotPoint;
//         selectedObject.transform.rotation = rotDelta * selectedObject.transform.rotation;
//         selectedObject.transform.position += pivotPoint;

//         grabHandPosition = currentHandPos;
//         grabHandRotation = currentHandRot;
//     }
// }

// 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndexPinchManipulator2step : MonoBehaviour
{
    [SerializeField] private GazeProvider gazeProvider;
    [SerializeField] private OVRHand manipulationHand; // RIGHT HAND
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

        // --- 1. PINCH START ---
        if (!isPinching && pinching)
        {
            isPinching = true; // Lock pinch state

            // Get the target
            GameObject target = GetTargetObject();

            // A. Check for UI Buttons
            if (target != null)
            {
                VRButton btn = target.GetComponent<VRButton>();
                if (btn != null)
                {
                    btn.Click();
                    return; // Wait for release
                }
            }

            // B. Check Spawn Mode
            InteractionTool tool = InteractionToolManager.Instance.CurrentTool;
            if (tool == InteractionTool.Spawn)
            {
                if (pointerManager.CurrentPointer != null)
                {
                    SpawnObjectAtPointer();
                }
                return; // Wait for release
            }

            // C. Handle Object Interaction
            if (target != null)
            {
                HandleObjectAction(target, tool);
            }
            else
            {
                // Nothing hit, reset immediately so we don't get stuck
                isPinching = false;
            }
        } 
        // --- 2. PINCH RELEASE ---
        else if (isPinching && !pinching)
        {
            isPinching = false;
            selectedObject = null;
        }

        // --- 3. MANIPULATION ---
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
                // FIX 1: CLEANUP BEFORE DESTROY (Prevents Pink Ghost)
                // Disable the outline immediately so it doesn't get orphaned
                var outline = target.GetComponent<Outline>();
                if (outline != null) 
                {
                    outline.enabled = false;
                    Destroy(outline); // Destroy the component first
                }

                // Force the PointerManager to forget this object so it doesn't try to update a dead object
                pointerManager.ForceClearSelection(); 

                // FIX 2: Destroy the object
                Destroy(target);
                
                // FIX 3: DO NOT reset isPinching!
                // We return and wait for the user to physically release their finger.
                // This prevents deleting the object behind it.
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
        // Safety check: If object was deleted externally, stop manipulating
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