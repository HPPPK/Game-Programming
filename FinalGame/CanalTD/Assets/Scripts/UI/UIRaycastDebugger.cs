using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIRaycastDebugger : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("UIRaycastDebugger AWAKE");
    }

    private void OnEnable()
    {
        Debug.Log("UIRaycastDebugger ENABLED");
    }

    private void Start()
    {
        Debug.Log("UIRaycastDebugger START");
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Debug.Log("Mouse clicked: " + Input.mousePosition);

        if (EventSystem.current == null)
        {
            Debug.LogWarning("UIRaycastDebugger: EventSystem.current is null");
            return;
        }

        PointerEventData data = new PointerEventData(EventSystem.current);
        data.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, results);

        Debug.Log("UI Raycast hit count = " + results.Count);

        foreach (var r in results)
        {
            Debug.Log("UI hit: " + r.gameObject.name);
        }
    }
}