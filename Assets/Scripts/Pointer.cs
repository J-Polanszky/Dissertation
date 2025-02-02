using System;
using UnityEngine;

public class Pointer : MonoBehaviour
{
    public Vector3 playerBase; // Assign the player's base transform in the Inspector
    public RectTransform pointerUI; // Assign the UI element (e.g., an arrow) in the Inspector
    public Camera mainCamera; // Assign the main camera in the Inspector

    private void Start()
    {
        playerBase = GameObject.Find("PlayerDeposit").transform.position;
        playerBase.y += 1.5f;
        pointerUI = GetComponent<RectTransform>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        Vector3 screenPos = mainCamera.WorldToScreenPoint(playerBase);
        Vector3 direction = screenPos - pointerUI.position;
        float angle = Vector3.SignedAngle(Vector3.right, direction, Vector3.forward);

        bool isInFrontOfCamera =
            Vector3.Dot(mainCamera.transform.forward, playerBase - mainCamera.transform.position) > 0;
        bool isOffScreen = screenPos.x <= 0 || screenPos.x >= Screen.width || screenPos.y <= 0 ||
                           screenPos.y >= Screen.height;
        bool isPlayerBaseOffScreen = !isInFrontOfCamera || isOffScreen;

        if (!isInFrontOfCamera)
        {
            // Flip the pointer direction if the base is behind the camera
            angle += 180;
        }

        pointerUI.rotation = Quaternion.Euler(0, 0, angle);

        // Debug.Log(isPlayerBaseOffScreen ? "playerBase is off-screen" : "playerBase is on-screen");
    }
}