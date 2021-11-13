using UnityEngine;
using UnityEngine.InputSystem;
public class InputHandler : MonoBehaviour
{
    [Header("public variables")]
    public Vector2 move;
    public Vector2 look;
    public Vector2 navigate;
    public Vector2 scrollWheel;
    public bool jump = false;
    public bool sprint = false;
    public bool grab = false;
    public bool shoot = false;
    public bool togglePhotoMode = false;
    public bool toggleControls = false;

    public void OnMove(InputAction.CallbackContext ctx) => move = ctx.ReadValue<Vector2>();
    public void OnLook(InputAction.CallbackContext ctx) => look = ctx.ReadValue<Vector2>();
    public void OnNavigate(InputAction.CallbackContext ctx) => navigate = ctx.ReadValue<Vector2>();
    public void OnScrollWheel(InputAction.CallbackContext ctx) => scrollWheel = ctx.ReadValue<Vector2>();
    public void OnJump(InputAction.CallbackContext ctx) => jump = ctx.performed;
    public void OnSprint(InputAction.CallbackContext ctx) => sprint = ctx.performed;
    public void OnGrab(InputAction.CallbackContext ctx) => grab = ctx.performed;
    public void OnShoot(InputAction.CallbackContext ctx) => shoot = ctx.performed;
}