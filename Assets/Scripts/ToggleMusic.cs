using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleMusic : MonoBehaviour
{
    public void ChangeSound(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            GameManager.Instance.ChangeSoundState();
        }
    }
}
