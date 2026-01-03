using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GazeSelectable : MonoBehaviour
{
    [SerializeField] private GazeProvider gazeProvider;

    private bool isGazedUpon;

    public UnityEvent OnGazeEnter;
    public UnityEvent OnGazeExit;

    void Start()
    {
        if (gazeProvider == null)
        {
            gazeProvider = FindFirstObjectByType<GazeProvider>();
            if (gazeProvider == null)
            {
                Debug.LogError("GazeSelectable: No GazeProvider found in scene.");
                enabled = false;
                return;
            }
        }
    }

    void Update()
    {
        if (gazeProvider == null) return;

        bool isGazed = gazeProvider.GazeTarget == gameObject;

        if (!isGazedUpon && isGazed)
        {
            isGazedUpon = true;
            OnGazeEnter.Invoke();
        }
        else if (isGazedUpon && !isGazed)
        {
            isGazedUpon = false;
            OnGazeExit.Invoke();
        }
    }
}