using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class GazePointerManager2step : MonoBehaviour
{
    [Header("Core Setup")]
    [SerializeField] private GazeProvider gazeProvider;
    [SerializeField] private OVRHand manipulationHand;
    [SerializeField] private OVRHand pointerSpawnHand;
    [SerializeField] private long lookbackMs = 200;
    [SerializeField] private GameObject pointerPrefab;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private float moveSensitivity = 1f;

    [Header("Hover Logic")]
    [SerializeField] private string manipulableTag = "Interactable";
    [SerializeField] private bool useContainmentBounds = false;
    [SerializeField] private string containmentTag = "ColliderBound";
    [SerializeField] private float hoverProximityRadius = 0.2f;

    [Header("Component References")]
    [SerializeField] private IndexPinchManipulator2step indexManipulator;

    private GameObject currentPointer;
    private GameObject currentTarget;
    private Collider clampingCollider;
    private Vector3 grabHandPosition;
    private Vector3 grabPointerPosition;
    private Camera eyeCamera;
    
    private GameObject currentlyHoveredObject;
    private Outline currentlyHoveredOutline;
    private GameObject currentGazeHoverTarget = null;
    private bool wasMiddlePinching = false;
    public GameObject CurrentPointer => currentPointer;
    public GameObject HoveredObject => currentlyHoveredObject;

    public void ClearPointer()
    {
        if (currentPointer != null)
        {
            Destroy(currentPointer);
            currentPointer = null;
            currentTarget = null;
            clampingCollider = null;
        }
        ClearHover();
    }

    // Helper for IndexPinchManipulator to clear selection after delete
    public void ForceClearSelection()
    {
        if (currentlyHoveredOutline != null)
        {
            currentlyHoveredOutline.enabled = false;
            currentlyHoveredOutline = null;
        }
        currentlyHoveredObject = null;
        currentGazeHoverTarget = null;
    }

    private void ClearHover()
    {
        ForceClearSelection();
    }

    void Start()
    {
        eyeCamera = Camera.main;
        if (eyeCamera == null) { Debug.LogError("GazePointerManager2step: No main camera found."); enabled = false; return; }
        if (gazeProvider == null) gazeProvider = FindFirstObjectByType<GazeProvider>();
        if (indexManipulator == null) indexManipulator = FindFirstObjectByType<IndexPinchManipulator2step>();        
        if (manipulationHand == null)
        {
            var hands = FindObjectsByType<OVRHand>(FindObjectsSortMode.None);
            foreach (var h in hands)
            {
                if (h.GetComponent<OVRSkeleton>()?.GetSkeletonType() == OVRSkeleton.SkeletonType.HandRight)
                {
                    manipulationHand = h; break;
                }
            }
        }
    }

    void Update()
    {
        if (manipulationHand == null || gazeProvider == null || eyeCamera == null) return;

        var historicalEntry = gazeProvider.GetGazeHistoryEntry(lookbackMs);
        GameObject gazeTarget = (historicalEntry != null) ? historicalEntry.gazeTarget : null;
        Vector3 gazePoint = (historicalEntry != null) ? historicalEntry.hitInfo.point : Vector3.zero;

        bool isGazeOnInteractable = gazeTarget != null && ((1 << gazeTarget.layer) & interactableLayer) != 0;

        if (isGazeOnInteractable && gazeTarget.CompareTag(manipulableTag))
            currentGazeHoverTarget = gazeTarget;
        else
            currentGazeHoverTarget = null;

        bool isMiddlePinching = pointerSpawnHand.GetFingerIsPinching(OVRHand.HandFinger.Middle);
        
        if (isMiddlePinching && !wasMiddlePinching)
        {
            if (currentPointer == null)
            {
                // Spawn even if NOT looking at interactable (Air Spawn)
                Vector3 spawnPosition;
                if (isGazeOnInteractable)
                {
                    spawnPosition = gazePoint;
                    currentTarget = gazeTarget;
                }
                else
                {
                    spawnPosition = eyeCamera.transform.position + (eyeCamera.transform.forward * 0.5f);
                    currentTarget = null;
                }

                currentPointer = Instantiate(pointerPrefab, spawnPosition, Quaternion.identity);
                
                clampingCollider = null;

                if (useContainmentBounds)
                {
                    GameObject localBound = (currentTarget != null) ? FindBoundObject(currentTarget, containmentTag) : null;
                    if (localBound != null)
                    {
                        clampingCollider = localBound.GetComponent<Collider>();
                    }
                    else
                    {
                        GameObject globalBound = GameObject.FindGameObjectWithTag(containmentTag);
                        if (globalBound != null)
                        {
                            clampingCollider = globalBound.GetComponent<Collider>();
                        }
                        else if (currentTarget != null)
                        {
                            clampingCollider = currentTarget.GetComponent<Collider>();
                        }
                    }
                }
                else if (currentTarget != null)
                {
                    clampingCollider = currentTarget.GetComponent<Collider>();
                }

                grabHandPosition = manipulationHand.transform.position;
                grabPointerPosition = currentPointer.transform.position;
            }
            else
            {
                ClearPointer();
            }
        }
        wasMiddlePinching = isMiddlePinching;

        if (currentPointer != null)
        {
            Vector3 currentHandPos = manipulationHand.transform.position;
            Vector3 handDelta = currentHandPos - grabHandPosition;

            Vector3 eyePos = eyeCamera.transform.position;
            float distFactor = Vector3.Distance(eyePos, currentPointer.transform.position) / 
                               Mathf.Max(Vector3.Distance(eyePos, currentHandPos), 0.01f);

            Vector3 newPos = grabPointerPosition + (handDelta * moveSensitivity * distFactor);

            if (clampingCollider != null)
            {
                newPos = clampingCollider.ClosestPoint(newPos);
            }

            currentPointer.transform.position = newPos;
            grabHandPosition = currentHandPos;
            grabPointerPosition = currentPointer.transform.position;
        }

        ForceUpdateHover();
    }

    private GameObject FindBoundObject(GameObject child, string tag)
    {
        if (child == null) return null;
        if (child.CompareTag(tag)) return child;

        Transform t = child.transform;
        while (t.parent != null)
        {
            if (t.parent.CompareTag(tag)) return t.parent.gameObject;
            t = t.parent;
        }
        return null; 
    }

    public void ForceUpdateHover()
    {
        GameObject newHoveredObject = null;
        
        if (currentPointer != null)
        {   
            Vector3 pointerPos = currentPointer.transform.position;
            Collider[] overlaps = Physics.OverlapSphere(pointerPos, hoverProximityRadius, interactableLayer);
            
            float minDistance = float.MaxValue;
            int maxDepth = -1;
            GameObject bestCandidate = null;

            foreach (Collider col in overlaps)
            {
                if (!col.gameObject.CompareTag(manipulableTag)) continue;

                // 1. Calculate Distance
                float dist = Vector3.Distance(pointerPos, col.ClosestPoint(pointerPos));

                // 2. Calculate Hierarchy Depth
                int depth = 0;
                Transform t = col.transform;
                while (t.parent != null) { depth++; t = t.parent; }

                // A. If this object is significantly closer (> 1cm difference), it wins.
                // B. If distances are roughly the same (both touching pointer), pick the deeper child.
                
                bool isSignificantlyCloser = dist < (minDistance - 0.01f);
                bool isRoughlySameAndDeeper = (Mathf.Abs(dist - minDistance) < 0.01f) && (depth > maxDepth);

                if (isSignificantlyCloser || isRoughlySameAndDeeper)
                {
                    minDistance = dist;
                    maxDepth = depth;
                    bestCandidate = col.gameObject;
                }
            }
            newHoveredObject = bestCandidate;
        }
        else
        {
            newHoveredObject = currentGazeHoverTarget;
        }

        if (newHoveredObject != currentlyHoveredObject)
        {
            if (currentlyHoveredOutline != null) currentlyHoveredOutline.enabled = false;
            
            currentlyHoveredObject = newHoveredObject;
            
            if (currentlyHoveredObject != null)
            {
                currentlyHoveredOutline = currentlyHoveredObject.GetComponent<Outline>();
                if (currentlyHoveredOutline != null) currentlyHoveredOutline.enabled = true;
            }
        }
    }
}