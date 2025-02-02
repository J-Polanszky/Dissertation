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

        // Calculate the inner radius based on the canvas size
        float innerRadius = Mathf.Min(Screen.width, Screen.height) / 4f; // Adjust as needed

        if (!isInFrontOfCamera)
        {
            // Flip the pointer direction if the base is behind the camera
            angle += 180;
        }
        
        //TODO: When the base is on the edge of the screen, but the transform barely is not, the pointer freaks out. 
        //More polish is necessary, if time allows.

        if (isPlayerBaseOffScreen)
        {
            // Place the pointer at the edge of the canvas based on the angle
            float canvasHalfWidth = Screen.width / 2f;
            float canvasHalfHeight = Screen.height / 2f;
            float radians = angle * Mathf.Deg2Rad;
            float x = canvasHalfWidth * Mathf.Cos(radians);
            float y = canvasHalfHeight * Mathf.Sin(radians);
            pointerUI.position = new Vector3(canvasHalfWidth + x, canvasHalfHeight + y, 0);
        }
        else
        {
            // Move the pointer towards the edge of the screen if outside the inner radius
            if (direction.magnitude > innerRadius)
            {
                direction = direction.normalized * innerRadius;
            }
            pointerUI.position = screenPos - direction;
        }

        pointerUI.rotation = Quaternion.Euler(0, 0, angle);

        // Debug.Log(isPlayerBaseOffScreen ? "playerBase is off-screen" : "playerBase is on-screen");
    }
}