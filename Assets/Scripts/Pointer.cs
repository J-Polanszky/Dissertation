using System;
using UnityEngine;

public class Pointer : MonoBehaviour
{
    public Vector3 playerBase; // Assign the player's base transform in the Inspector
    public RectTransform pointerUI; // Assign the UI element (e.g., an arrow) in the Inspector
    public Camera mainCamera;

    [HideInInspector] public bool isVisible = false;

    private void Start()
    {
        playerBase = GameObject.Find("PlayerDeposit").transform.position;
        playerBase.y += 1.5f;
        pointerUI = GetComponent<RectTransform>();
        mainCamera = Camera.main;
    }

    public void BecameVisible()
    {
        isVisible = true;
    }

    public void BecameInvisible()
    {
        isVisible = false;
    }

    void FixedUpdate()
    {
        Vector3 screenPos = mainCamera.WorldToScreenPoint(playerBase);
        Vector3 direction = screenPos - pointerUI.position;
        float angle = Vector3.SignedAngle(Vector3.right, direction, Vector3.forward);

        bool isInFrontOfCamera =
            Vector3.Dot(mainCamera.transform.forward, playerBase - mainCamera.transform.position) > 0;

        // The building is off-screen if either:
        // 1. It's behind the camera OR
        // 2. It's not visible (as reported by OnBecameInvisible)
        bool isPlayerBaseOffScreen = !isInFrontOfCamera || !isVisible;

        float innerRadius = Mathf.Min(Screen.width, Screen.height) / 4f;

        if (!isInFrontOfCamera)
        {
            angle += 180;
        }

        if (isPlayerBaseOffScreen)
        {
            // Position the pointer at the edge of the screen pointing toward the building
            float canvasHalfWidth = Screen.width / 2f;
            float canvasHalfHeight = Screen.height / 2f;
            float radians = angle * Mathf.Deg2Rad;
            float x = canvasHalfWidth * Mathf.Cos(radians);
            float y = canvasHalfHeight * Mathf.Sin(radians);
            pointerUI.position = new Vector3(canvasHalfWidth + x, canvasHalfHeight + y, 0);
        }
        else
        {
            // Position the pointer within the inner radius
            if (direction.magnitude > innerRadius)
            {
                direction = direction.normalized * innerRadius;
            }

            pointerUI.position = screenPos - direction;
        }

        pointerUI.rotation = Quaternion.Euler(0, 0, angle);
    }
}