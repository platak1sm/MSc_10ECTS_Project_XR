// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// [RequireComponent(typeof(LineRenderer))]
// public class GazePointerManager2step : MonoBehaviour
// {
//     [Header("Core Setup")]
//     [SerializeField] private GazeProvider gazeProvider;
//     [SerializeField] private OVRHand manipulationHand;
//     [SerializeField] private OVRHand pointerSpawnHand;
//     [SerializeField] private long lookbackMs = 200;
//     [SerializeField] private GameObject pointerPrefab;
//     [SerializeField] private LayerMask interactableLayer;
//     [SerializeField] private float moveSensitivity = 1f;

//     [Header("Hover Logic")]
//     [SerializeField]
//     private string manipulableTag = "Interactable";
//     [SerializeField]
//     [Tooltip("If checked, the pointer will be trapped inside the bounds of the object with the 'Containment Bounds Tag'.")]
//     private bool useContainmentBounds = false;
//     [SerializeField]
//     private string containmentTag = "ColliderBound"; 
//     [SerializeField]
//     [Tooltip("The radius around the pointer to find the *closest* object to highlight.")]
//     private float hoverProximityRadius = 0.2f;

//     [Header("Component References")]
//     [SerializeField]
//     private TransparencyManager transparencyManager;
//     [SerializeField]
//     private IndexPinchManipulator2step indexManipulator; 

//     private GameObject currentPointer;
//     private GameObject currentTarget; 
//     private Collider clampingCollider; 
//     private GameObject rootInteractable; 
//     private Vector3 grabHandPosition;
//     private Vector3 grabPointerPosition;
//     private Camera eyeCamera;

//     private GameObject currentlyHoveredObject;
//     private Outline currentlyHoveredOutline; 

//     private GameObject currentGazeHoverTarget = null;

//     private bool wasMiddlePinching = false;

//     private LineRenderer tetherRenderer;

//     public GameObject CurrentPointer => currentPointer;
//     public GameObject HoveredObject => currentlyHoveredObject;

//     public void ClearPointer()
//     {
//         if (currentPointer != null)
//         {
//             Destroy(currentPointer);
//             currentPointer = null;
//             currentTarget = null;
//             rootInteractable = null;
//             clampingCollider = null;
//         }

//         if (tetherRenderer != null)
//         {
//             tetherRenderer.enabled = false;
//         }

//         ClearHover();
//     }

// private void ClearHover()
// {
//     if (currentlyHoveredOutline != null)
//     {
//         currentlyHoveredOutline.enabled = false;
//         currentlyHoveredOutline = null;
//     }
//     currentlyHoveredObject = null;
// }

//     void Start()
//     {
//         eyeCamera = Camera.main;
//         if (eyeCamera == null) { Debug.LogError("GazePointerManager2step: No main camera found."); enabled = false; return; }

//         tetherRenderer = GetComponent<LineRenderer>();
//         if (tetherRenderer.sharedMaterial == null)
//         {
//              tetherRenderer.startWidth = 0.005f;
//              tetherRenderer.endWidth = 0.001f;
//              tetherRenderer.material = new Material(Shader.Find("Unlit/Color")) { color = new Color(1, 1, 1, 0.5f) };
//         }
//         tetherRenderer.enabled = false;

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

//         if (pointerPrefab == null) Debug.LogError("GazePointerManager2step: No pointer prefab assigned.");

//         if (indexManipulator == null) indexManipulator = FindFirstObjectByType<IndexPinchManipulator2step>(); 
//         if (transparencyManager == null) transparencyManager = FindFirstObjectByType<TransparencyManager>();

//         if (indexManipulator == null) Debug.LogError("GazePointerManager2step: No IndexPinchManipulator2step found.");
//         if (transparencyManager == null) Debug.LogError("GazePointerManager2step: No TransparencyManager found.");
//         if (manipulationHand == null) Debug.LogError("GazePointerManager2step: No OVRHand found.");
//         if (gazeProvider == null) Debug.LogError("GazePointerManager2step: No GazeProvider found.");
//     }

//     void Update()
//     {
//         if (manipulationHand == null || gazeProvider == null || eyeCamera == null || indexManipulator == null || transparencyManager == null) return;

//         var historicalEntry = gazeProvider.GetGazeHistoryEntry(lookbackMs);
//         GameObject gazeTarget = (historicalEntry != null) ? historicalEntry.gazeTarget : null;
//         Vector3 gazePoint = (historicalEntry != null) ? historicalEntry.hitInfo.point : Vector3.zero;

//         bool isGazeOnInteractable = gazeTarget != null && ((1 << gazeTarget.layer) & interactableLayer) != 0;

//         if (isGazeOnInteractable)
//         {
//             if (gazeTarget.CompareTag(manipulableTag))
//             {
//                 currentGazeHoverTarget = gazeTarget;
//             }
//             else
//             {
//                 currentGazeHoverTarget = null;
//             }
//         }
//         else
//         {
//             currentGazeHoverTarget = null;
//         }

//         bool isMiddlePinching = pointerSpawnHand.GetFingerIsPinching(OVRHand.HandFinger.Middle);
//         if (isMiddlePinching && !wasMiddlePinching)
//         {
//             if (currentPointer == null)
//             {
//                 if (isGazeOnInteractable)
//                 {
//                     currentPointer = Instantiate(pointerPrefab, gazePoint, Quaternion.identity);

//                     currentTarget = gazeTarget; 
//                     rootInteractable = gazeTarget.transform.root.gameObject; 
//                     clampingCollider = null; 

//                     if (useContainmentBounds)
//                     {
//                         GameObject containmentObject = FindParentWithTag(gazeTarget, containmentTag);
//                         if (containmentObject != null)
//                         {
//                             clampingCollider = containmentObject.GetComponent<Collider>();
//                         }
//                         else
//                         {
//                             clampingCollider = rootInteractable.GetComponent<Collider>();
//                         }
//                     }
//                     else
//                     {
//                         clampingCollider = rootInteractable.GetComponent<Collider>();
//                     }

//                     grabHandPosition = manipulationHand.transform.position;
//                     grabPointerPosition = currentPointer.transform.position;

//                     tetherRenderer.enabled = true;
//                 }
//             }
//             else
//             {
//                 ClearPointer();
//             }
//         }
//         wasMiddlePinching = isMiddlePinching;


//         if (isGazeOnInteractable)
//         {
//             transparencyManager.MakeTransparent();

//             if (currentPointer != null)
//             {
//                 Vector3 currentHandPos = manipulationHand.transform.position;
//                 Vector3 handDeltaThisFrame = currentHandPos - grabHandPosition;

//                 Vector3 eyePoint = eyeCamera.transform.position;
//                 float eyeHandDist = Vector3.Distance(eyePoint, currentHandPos);
//                 float eyeObjectDist = Vector3.Distance(eyePoint, currentPointer.transform.position);
//                 float visualAngleGain = eyeObjectDist / Mathf.Max(eyeHandDist, 0.01f); 

//                 Vector3 scaledDelta = handDeltaThisFrame * moveSensitivity * visualAngleGain;
//                 Vector3 newPos = grabPointerPosition + scaledDelta;

//                 if (clampingCollider != null)
//                 {
//                     newPos = clampingCollider.ClosestPoint(newPos);
//                 }

//                 currentPointer.transform.position = newPos;
//                 grabHandPosition = currentHandPos;
//                 grabPointerPosition = currentPointer.transform.position;

//                 tetherRenderer.SetPosition(0, manipulationHand.transform.position);
//                 tetherRenderer.SetPosition(1, currentPointer.transform.position);
//             }
//         }
//         else 
//         {
//             transparencyManager.MakeOpaque();

//             if (currentPointer != null)
//             {
//                 ClearPointer(); 
//             }
//         }

//         ForceUpdateHover();
//     } 

// public void ForceUpdateHover()
// {
//     GameObject newHoveredObject = null;

//     if (currentPointer != null)
//     {
//         Vector3 pointerPos = currentPointer.transform.position;
//         Collider[] overlaps = Physics.OverlapSphere(pointerPos, hoverProximityRadius, interactableLayer);
//         float minDistance = float.MaxValue;
//         GameObject closestObject = null; 

//         foreach (Collider col in overlaps)
//         {
//             if (!col.gameObject.CompareTag(manipulableTag)) continue; 

//             Vector3 closestPoint = col.ClosestPoint(pointerPos);
//             float distance = Vector3.Distance(pointerPos, closestPoint);
//             if (distance < minDistance)
//             {
//                 minDistance = distance;
//                 closestObject = col.gameObject; 
//             }
//         }
//         newHoveredObject = closestObject;
//     }
//     else 
//     {
//         newHoveredObject = currentGazeHoverTarget;
//     }

//     if (newHoveredObject != currentlyHoveredObject)
//     {
//         if (currentlyHoveredOutline != null)
//         {
//             currentlyHoveredOutline.enabled = false;
//         }

//         if (newHoveredObject != null)
//         {
//             currentlyHoveredOutline = newHoveredObject.GetComponent<Outline>();
//             if (currentlyHoveredOutline != null)
//             {
//                 currentlyHoveredOutline.enabled = true;
//             }
//         }
//         else
//         {
//             currentlyHoveredOutline = null;
//         }

//         currentlyHoveredObject = newHoveredObject;
//     }
// }

//     private GameObject FindParentWithTag(GameObject child, string tag)
//     {
//         Transform t = child.transform;
//         while (t.parent != null)
//         {
//             if (t.parent.CompareTag(tag))
//             {
//                 return t.parent.gameObject;
//             }
//             t = t.parent;
//         }
//         return null; 
//     }
// }

// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// [RequireComponent(typeof(LineRenderer))]
// public class GazePointerManager2step : MonoBehaviour
// {
//     [Header("Core Setup")]
//     [SerializeField] private GazeProvider gazeProvider;
//     [SerializeField] private OVRHand manipulationHand;
//     [SerializeField] private OVRHand pointerSpawnHand;
//     [SerializeField] private long lookbackMs = 200;
//     [SerializeField] private GameObject pointerPrefab;
//     [SerializeField] private LayerMask interactableLayer;
//     [SerializeField] private float moveSensitivity = 1f;

//     [Header("Hover Logic")]
//     [SerializeField]
//     private string manipulableTag = "Interactable";
//     [SerializeField]
//     [Tooltip("If checked, the pointer will be trapped inside the bounds of the object with the 'Containment Bounds Tag'.")]
//     private bool useContainmentBounds = false;
//     [SerializeField]
//     private string containmentTag = "ColliderBound";
//     [SerializeField]
//     [Tooltip("The radius around the pointer to find the *closest* object to highlight.")]
//     private float hoverProximityRadius = 0.2f;

//     [Header("Component References")]
//     [SerializeField]
//     private TransparencyManager transparencyManager;
//     [SerializeField]
//     private IndexPinchManipulator2step indexManipulator;

//     private GameObject currentPointer;
//     private GameObject currentTarget;
//     private Collider clampingCollider;
//     private GameObject rootInteractable;
//     private Vector3 grabHandPosition;
//     private Vector3 grabPointerPosition;
//     private Camera eyeCamera;

//     private GameObject currentlyHoveredObject;
//     private Outline currentlyHoveredOutline;

//     private GameObject currentGazeHoverTarget = null;

//     private bool wasMiddlePinching = false;

//     //private LineRenderer tetherRenderer;

//     public GameObject CurrentPointer => currentPointer;
//     public GameObject HoveredObject => currentlyHoveredObject;

//     public void ClearPointer()
//     {
//         if (currentPointer != null)
//         {
//             Destroy(currentPointer);
//             currentPointer = null;
//             currentTarget = null;
//             rootInteractable = null;
//             clampingCollider = null;
//         }

//         // if (tetherRenderer != null)
//         // {
//         //     tetherRenderer.enabled = false;
//         // }

//         ClearHover();
//     }

//     private void ClearHover()
//     {
//         if (currentlyHoveredOutline != null)
//         {
//             currentlyHoveredOutline.enabled = false;
//             currentlyHoveredOutline = null;
//         }
//         currentlyHoveredObject = null;
//     }

//     void Start()
//     {
//         eyeCamera = Camera.main;
//         if (eyeCamera == null) { Debug.LogError("GazePointerManager2step: No main camera found."); enabled = false; return; }

//         // rRenderer = GetComponent<LineRenderer>();
//         // if (tetherRenderer.sharedMaterial == null)
//         // {
//         //     tetherRenderer.startWidth = 0.005f;
//         //     tetherRenderer.endWidth = 0.001f;
//         //     tetherRenderer.material = new Material(Shader.Find("Unlit/Color")) { color = new Color(1, 1, 1, 0.5f) };
//         // }tethe
//         // tetherRenderer.enabled = false;

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

//         if (pointerPrefab == null) Debug.LogError("GazePointerManager2step: No pointer prefab assigned.");

//         if (indexManipulator == null) indexManipulator = FindFirstObjectByType<IndexPinchManipulator2step>();
//         if (transparencyManager == null) transparencyManager = FindFirstObjectByType<TransparencyManager>();

//         if (indexManipulator == null) Debug.LogError("GazePointerManager2step: No IndexPinchManipulator2step found.");
//         if (transparencyManager == null) Debug.LogError("GazePointerManager2step: No TransparencyManager found.");
//         if (manipulationHand == null) Debug.LogError("GazePointerManager2step: No OVRHand found.");
//         if (gazeProvider == null) Debug.LogError("GazePointerManager2step: No GazeProvider found.");
//     }

//     void Update()
//     {
//         if (manipulationHand == null || gazeProvider == null || eyeCamera == null || indexManipulator == null || transparencyManager == null) return;

//         var historicalEntry = gazeProvider.GetGazeHistoryEntry(lookbackMs);
//         GameObject gazeTarget = (historicalEntry != null) ? historicalEntry.gazeTarget : null;
//         Vector3 gazePoint = (historicalEntry != null) ? historicalEntry.hitInfo.point : Vector3.zero;

//         bool isGazeOnInteractable = gazeTarget != null && ((1 << gazeTarget.layer) & interactableLayer) != 0;

//         if (isGazeOnInteractable)
//         {
//             if (gazeTarget.CompareTag(manipulableTag))
//             {
//                 currentGazeHoverTarget = gazeTarget;
//             }
//             else
//             {
//                 currentGazeHoverTarget = null;
//             }
//         }
//         else
//         {
//             currentGazeHoverTarget = null;
//         }

//         bool isMiddlePinching = pointerSpawnHand.GetFingerIsPinching(OVRHand.HandFinger.Middle);
//         if (isMiddlePinching && !wasMiddlePinching)
//         {
//             if (currentPointer == null)
//             {
//                 if (isGazeOnInteractable)
//                 {
//                     currentPointer = Instantiate(pointerPrefab, gazePoint, Quaternion.identity);
//                     currentTarget = gazeTarget;

//                     rootInteractable = GameObject.Find("Body"); //collider for car body 

//                     clampingCollider = null;

//                     if (useContainmentBounds)
//                     {
//                         // A. Try to find a specific bound on the object or its parents (e.g. Car parts)
//                         GameObject localBound = FindBoundObject(gazeTarget, containmentTag);

//                         if (localBound != null)
//                         {
//                             clampingCollider = localBound.GetComponent<Collider>();
//                         }
//                         else
//                         {
//                             // B. FALLBACK: Find the GLOBAL object with the "ColliderBound" tag
//                             // This covers your Room case where the bound is a separate object
//                             GameObject globalBound = GameObject.FindGameObjectWithTag(containmentTag);
//                             if (globalBound != null)
//                             {
//                                 clampingCollider = globalBound.GetComponent<Collider>();
//                             }
//                             else
//                             {
//                                 // C. Absolute fallback: restrict to the collider of the object itself
//                                 clampingCollider = gazeTarget.GetComponent<Collider>();
//                             }
//                         }
//                     }
//                     else
//                     {
//                         // If bounds are off, just clamp to the object we hit
//                         clampingCollider = gazeTarget.GetComponent<Collider>();
//                     }

//                     grabHandPosition = manipulationHand.transform.position;
//                     grabPointerPosition = currentPointer.transform.position;

//                     //tetherRenderer.enabled = true;
//                 }
//             }
//             else
//             {
//                 ClearPointer();
//             }
//         }
//         wasMiddlePinching = isMiddlePinching;


//         if (isGazeOnInteractable)
//         {
//             transparencyManager.MakeTransparent();

//             if (currentPointer != null)
//             {
//                 Vector3 currentHandPos = manipulationHand.transform.position;
//                 Vector3 handDeltaThisFrame = currentHandPos - grabHandPosition;

//                 Vector3 eyePoint = eyeCamera.transform.position;
//                 float eyeHandDist = Vector3.Distance(eyePoint, currentHandPos);
//                 float eyeObjectDist = Vector3.Distance(eyePoint, currentPointer.transform.position);
//                 float visualAngleGain = eyeObjectDist / Mathf.Max(eyeHandDist, 0.01f);

//                 Vector3 scaledDelta = handDeltaThisFrame * moveSensitivity * visualAngleGain;
//                 Vector3 newPos = grabPointerPosition + scaledDelta;

//                 if (clampingCollider != null)
//                 {
//                     newPos = clampingCollider.ClosestPoint(newPos);
//                 }

//                 currentPointer.transform.position = newPos;
//                 grabHandPosition = currentHandPos;
//                 grabPointerPosition = currentPointer.transform.position;

//                 // tetherRenderer.SetPosition(0, manipulationHand.transform.position);
//                 // tetherRenderer.SetPosition(1, currentPointer.transform.position);
//             }
//         }
//         else
//         {
//             transparencyManager.MakeOpaque();

//             if (currentPointer != null)
//             {
//                 ClearPointer();
//             }
//         }

//         ForceUpdateHover();
//     }

//     // Helper that checks the object ITSELF first, then parents
//     private GameObject FindBoundObject(GameObject child, string tag)
//     {
//         if (child == null) return null;
//         if (child.CompareTag(tag)) return child;

//         Transform t = child.transform;
//         while (t.parent != null)
//         {
//             if (t.parent.CompareTag(tag)) return t.parent.gameObject;
//             t = t.parent;
//         }
//         return null; 
//     }

//     public void ForceUpdateHover()
//     {
//         GameObject newHoveredObject = null;

//         if (currentPointer != null)
//         {
//             Vector3 pointerPos = currentPointer.transform.position;
//             Collider[] overlaps = Physics.OverlapSphere(pointerPos, hoverProximityRadius, interactableLayer);
//             float minDistance = float.MaxValue;
//             GameObject closestObject = null;

//             foreach (Collider col in overlaps)
//             {
//                 if (!col.gameObject.CompareTag(manipulableTag)) continue;

//                 Vector3 closestPoint = col.ClosestPoint(pointerPos);
//                 float distance = Vector3.Distance(pointerPos, closestPoint);
//                 if (distance < minDistance)
//                 {
//                     minDistance = distance;
//                     closestObject = col.gameObject;
//                 }
//             }
//             newHoveredObject = closestObject;
//         }
//         else
//         {
//             newHoveredObject = currentGazeHoverTarget;
//         }

//         if (newHoveredObject != currentlyHoveredObject)
//         {
//             if (currentlyHoveredOutline != null)
//             {
//                 currentlyHoveredOutline.enabled = false;
//             }

//             if (newHoveredObject != null)
//             {
//                 currentlyHoveredOutline = newHoveredObject.GetComponent<Outline>();
//                 if (currentlyHoveredOutline != null)
//                 {
//                     currentlyHoveredOutline.enabled = true;
//                 }
//             }
//             else
//             {
//                 currentlyHoveredOutline = null;
//             }

//             currentlyHoveredObject = newHoveredObject;
//         }
//     }

//     private GameObject FindParentWithTag(GameObject child, string tag)
//     {
//         if (child == null) return null;
//         Transform t = child.transform;
//         while (t.parent != null)
//         {
//             if (t.parent.CompareTag(tag))
//             {
//                 return t.parent.gameObject;
//             }
//             t = t.parent;
//         }
//         return null;
//     }
// }

// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// [RequireComponent(typeof(LineRenderer))]
// public class GazePointerManager2step : MonoBehaviour
// {
//     [Header("Core Setup")]
//     [SerializeField] private GazeProvider gazeProvider;
//     [SerializeField] private OVRHand manipulationHand;
//     [SerializeField] private OVRHand pointerSpawnHand;
//     [SerializeField] private long lookbackMs = 200;
//     [SerializeField] private GameObject pointerPrefab;
//     [SerializeField] private LayerMask interactableLayer;
//     [SerializeField] private float moveSensitivity = 1f;

//     [Header("Hover Logic")]
//     [SerializeField] private string manipulableTag = "Interactable";
//     [SerializeField] private bool useContainmentBounds = false;
//     [SerializeField] private string containmentTag = "ColliderBound";
//     [SerializeField] private float hoverProximityRadius = 0.2f;

//     [Header("Component References")]
//     [SerializeField] private TransparencyManager transparencyManager;
//     [SerializeField] private IndexPinchManipulator2step indexManipulator;

//     private GameObject currentPointer;
//     private GameObject currentTarget;
//     private Collider clampingCollider;
//     private Vector3 grabHandPosition;
//     private Vector3 grabPointerPosition;
//     private Camera eyeCamera;
    
//     // Hover State
//     private GameObject currentlyHoveredObject;
//     private Outline currentlyHoveredOutline;
//     private GameObject currentGazeHoverTarget = null;
//     private bool wasMiddlePinching = false;


//     public GameObject CurrentPointer => currentPointer;
//     public GameObject HoveredObject => currentlyHoveredObject;

//     public void ClearPointer()
//     {
//         if (currentPointer != null)
//         {
//             Destroy(currentPointer);
//             currentPointer = null;
//             currentTarget = null;

//         ClearHover();
//         }
//     }

//     private void ClearHover()
//     {
//         if (currentlyHoveredOutline != null)
//         {
//             currentlyHoveredOutline.enabled = false;
//             currentlyHoveredOutline = null;
//         }
//         currentlyHoveredObject = null;
//     }

//     void Start()
//     {
//         eyeCamera = Camera.main;
//         if (eyeCamera == null) { Debug.LogError("GazePointerManager2step: No main camera found."); enabled = false; return; }

//         if (gazeProvider == null) gazeProvider = FindFirstObjectByType<GazeProvider>();
//         if (indexManipulator == null) indexManipulator = FindFirstObjectByType<IndexPinchManipulator2step>();
//         if (transparencyManager == null) transparencyManager = FindFirstObjectByType<TransparencyManager>();
        
//         if (manipulationHand == null)
//         {
//             var hands = FindObjectsByType<OVRHand>(FindObjectsSortMode.None);
//             foreach (var h in hands)
//             {
//                 if (h.GetComponent<OVRSkeleton>()?.GetSkeletonType() == OVRSkeleton.SkeletonType.HandRight)
//                 {
//                     manipulationHand = h; break;
//                 }
//             }
//         }
//     }

//     void Update()
//     {
//         if (manipulationHand == null || gazeProvider == null || eyeCamera == null) return;

//         var historicalEntry = gazeProvider.GetGazeHistoryEntry(lookbackMs);
//         GameObject gazeTarget = (historicalEntry != null) ? historicalEntry.gazeTarget : null;
//         Vector3 gazePoint = (historicalEntry != null) ? historicalEntry.hitInfo.point : Vector3.zero;

//         bool isGazeOnInteractable = gazeTarget != null && ((1 << gazeTarget.layer) & interactableLayer) != 0;

//         if (isGazeOnInteractable && gazeTarget.CompareTag(manipulableTag))
//             currentGazeHoverTarget = gazeTarget;
//         else
//             currentGazeHoverTarget = null;

//         bool isMiddlePinching = pointerSpawnHand.GetFingerIsPinching(OVRHand.HandFinger.Middle);
        
//         // --- POINTER SPAWN / TOGGLE LOGIC ---
//         if (isMiddlePinching && !wasMiddlePinching)
//         {
//             if (currentPointer == null)
//             {
//                 // FIX 1: Spawn even if NOT looking at interactable
//                 Vector3 spawnPosition;
                
//                 if (isGazeOnInteractable)
//                 {
//                     spawnPosition = gazePoint;
//                     currentTarget = gazeTarget;
//                 }
//                 else
//                 {
//                     // Default to 0.5m in front of user if looking at air
//                     spawnPosition = eyeCamera.transform.position + (eyeCamera.transform.forward * 0.5f);
//                     currentTarget = null;
//                 }

//                 currentPointer = Instantiate(pointerPrefab, spawnPosition, Quaternion.identity);
                
//                 // --- CLAMPING LOGIC ---
//                 clampingCollider = null;

//                 if (useContainmentBounds)
//                 {
//                     // 1. Try local bounds if we hit a target
//                     GameObject localBound = (currentTarget != null) ? FindBoundObject(currentTarget, containmentTag) : null;
                    
//                     if (localBound != null)
//                     {
//                         clampingCollider = localBound.GetComponent<Collider>();
//                     }
//                     else
//                     {
//                         // 2. Fallback to Global Bounds (The Room) - Works even for Air Spawning
//                         GameObject globalBound = GameObject.FindGameObjectWithTag(containmentTag);
//                         if (globalBound != null)
//                         {
//                             clampingCollider = globalBound.GetComponent<Collider>();
//                         }
//                         else if (currentTarget != null)
//                         {
//                             // 3. Absolute Fallback (The object itself, if we hit one)
//                             clampingCollider = currentTarget.GetComponent<Collider>();
//                         }
//                     }
//                 }
//                 else if (currentTarget != null)
//                 {
//                     clampingCollider = currentTarget.GetComponent<Collider>();
//                 }

//                 grabHandPosition = manipulationHand.transform.position;
//                 grabPointerPosition = currentPointer.transform.position;
//             }
//             else
//             {
//                 // Toggle OFF
//                 ClearPointer();
//             }
//         }
//         wasMiddlePinching = isMiddlePinching;

//         // --- POINTER MOVEMENT LOGIC ---
//         // FIX 2: Check 'currentPointer != null' instead of 'isGazeOnInteractable'
//         // This keeps the pointer alive even when looking at nothing.
//         if (currentPointer != null)
//         {
//             Vector3 currentHandPos = manipulationHand.transform.position;
//             Vector3 handDelta = currentHandPos - grabHandPosition;

//             Vector3 eyePos = eyeCamera.transform.position;
//             float distFactor = Vector3.Distance(eyePos, currentPointer.transform.position) / 
//                                Mathf.Max(Vector3.Distance(eyePos, currentHandPos), 0.01f);

//             Vector3 newPos = grabPointerPosition + (handDelta * moveSensitivity * distFactor);

//             if (clampingCollider != null)
//             {
//                 newPos = clampingCollider.ClosestPoint(newPos);
//             }

//             currentPointer.transform.position = newPos;
//             grabHandPosition = currentHandPos;
//             grabPointerPosition = currentPointer.transform.position;
//         }

//         ForceUpdateHover();
//     }

//     private GameObject FindBoundObject(GameObject child, string tag)
//     {
//         if (child == null) return null;
//         if (child.CompareTag(tag)) return child;

//         Transform t = child.transform;
//         while (t.parent != null)
//         {
//             if (t.parent.CompareTag(tag)) return t.parent.gameObject;
//             t = t.parent;
//         }
//         return null; 
//     }

//     public void ForceUpdateHover()
//     {
//         GameObject newHoveredObject = null;
        
//         // Prioritize the 3D Pointer's proximity
//         if (currentPointer != null)
//         {
//             Vector3 pointerPos = currentPointer.transform.position;
//             Collider[] overlaps = Physics.OverlapSphere(pointerPos, hoverProximityRadius, interactableLayer);
//             float minDist = float.MaxValue;

//             foreach (Collider col in overlaps)
//             {
//                 if (!col.gameObject.CompareTag(manipulableTag)) continue;

//                 float dist = Vector3.Distance(pointerPos, col.ClosestPoint(pointerPos));
//                 if (dist < minDist)
//                 {
//                     minDist = dist;
//                     newHoveredObject = col.gameObject;
//                 }
//             }
//         }
//         else
//         {
//             // Fallback to gaze
//             newHoveredObject = currentGazeHoverTarget;
//         }

//         if (newHoveredObject != currentlyHoveredObject)
//         {
//             if (currentlyHoveredOutline != null) currentlyHoveredOutline.enabled = false;
//             currentlyHoveredObject = newHoveredObject;
//             if (currentlyHoveredObject != null)
//             {
//                 currentlyHoveredOutline = currentlyHoveredObject.GetComponent<Outline>();
//                 if (currentlyHoveredOutline != null) currentlyHoveredOutline.enabled = true;
//             }
//         }
//     }
//     public void ForceClearSelection()
//     {
//         // 1. Disable outline
//         if (currentlyHoveredOutline != null)
//         {
//             currentlyHoveredOutline.enabled = false;
//             currentlyHoveredOutline = null;
//         }
        
//         // 2. Forget the object
//         currentlyHoveredObject = null;
        
//         // 3. Reset gaze targets
//         currentGazeHoverTarget = null;
        
//         // 4. (Optional) If you want the pointer to disappear on delete:
//         // ClearPointer(); 
//     }
// } 

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
    [SerializeField] private TransparencyManager transparencyManager;
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
        if (transparencyManager == null) transparencyManager = FindFirstObjectByType<TransparencyManager>();
        
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
        
        // --- POINTER SPAWN / TOGGLE LOGIC ---
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
                
                // --- CLAMPING LOGIC ---
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

        // --- POINTER MOVEMENT ---
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

        // // --- TRANSPARENCY ---
        // if (isGazeOnInteractable)
        // {
        //     if (transparencyManager) transparencyManager.MakeTransparent();
        // }
        // else
        // {
        //     if (transparencyManager) transparencyManager.MakeOpaque();
        // }

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
            // --- FIX FOR INNER COLLIDERS ---
            // Instead of just taking the closest distance, we weigh depth (hierarchy) heavily.
            
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

                // LOGIC: 
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