using System;
using UnityEngine;

public class Pointer : MonoBehaviour
{
    public Vector3 playerBase; // Assign the player's base transform in the Inspector
    public RectTransform pointerUI; // Assign the UI element (e.g., an arrow) in the Inspector
    public Camera mainCamera;
    private bool wasOffScreen = false;
    private float edgeMargin = 20f; // pixels

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

        // Hysteresis: no margin for going off-screen, margin for coming back on-screen
        bool isOffScreenNow = 
            screenPos.x < 0 || screenPos.x > Screen.width ||
            screenPos.y < 0 || screenPos.y > Screen.height;

        bool isOnScreenWithMargin = 
            screenPos.x > edgeMargin && screenPos.x < Screen.width - edgeMargin &&
            screenPos.y > edgeMargin && screenPos.y < Screen.height - edgeMargin;

        if (wasOffScreen)
        {
            // Only switch to on-screen if well inside the screen
            wasOffScreen = !isOnScreenWithMargin;
        }
        else
        {
            // Switch to off-screen as soon as it's outside
            wasOffScreen = isOffScreenNow;
        }

        bool isPlayerBaseOffScreen = !isInFrontOfCamera || wasOffScreen;

        // ...rest of your code remains unchanged...
        float innerRadius = Mathf.Min(Screen.width, Screen.height) / 4f;

        if (!isInFrontOfCamera)
        {
            angle += 180;
        }

        if (isPlayerBaseOffScreen)
        {
            float canvasHalfWidth = Screen.width / 2f;
            float canvasHalfHeight = Screen.height / 2f;
            float radians = angle * Mathf.Deg2Rad;
            float x = canvasHalfWidth * Mathf.Cos(radians);
            float y = canvasHalfHeight * Mathf.Sin(radians);
            pointerUI.position = new Vector3(canvasHalfWidth + x, canvasHalfHeight + y, 0);
        }
        else
        {
            if (direction.magnitude > innerRadius)
            {
                direction = direction.normalized * innerRadius;
            }
            pointerUI.position = screenPos - direction;
        }

        pointerUI.rotation = Quaternion.Euler(0, 0, angle);
    }
}