using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class ARArrowAnchor : MonoBehaviour
{
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARAnchorManager anchorManager;

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private bool arrowPlaced = false;

    void Update()
    {
        if (arrowPlaced) return;
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;

        if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;

            // ✅ New way — create a GameObject with ARAnchor component
            GameObject anchorGO = new GameObject("ArrowAnchor");
            anchorGO.transform.position = hitPose.position;
            anchorGO.transform.rotation = hitPose.rotation;

            ARAnchor anchor = anchorGO.AddComponent<ARAnchor>();

            // Spawn arrow parented to anchor
            GameObject arrow = Instantiate(arrowPrefab, anchor.transform);
            arrow.transform.localPosition = Vector3.zero;
            arrow.transform.localRotation = Quaternion.identity;

            arrowPlaced = true;
            Debug.Log("Arrow anchored at: " + hitPose.position);
        }
    }
}