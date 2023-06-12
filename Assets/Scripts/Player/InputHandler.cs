using UnityEngine;
using UnityEngine.InputSystem;
public class InputHandler : MonoBehaviour
{
    [Header("public variables")]
    public Vector2 move;
    public Vector2 look;
    public bool next;
    public bool previous;
    public bool navUp;
    public bool navDown;
    public bool navLeft;
    public bool navRight;
    public Vector2 scrollWheel;
    public bool jump = false;
    public bool sprint = false;
    public bool grab = false;
    public bool shoot = false;

    public void OnMove(InputAction.CallbackContext ctx) => move = ctx.ReadValue<Vector2>();
    public void OnLook(InputAction.CallbackContext ctx) => look = ctx.ReadValue<Vector2>();
    public void OnNext(InputAction.CallbackContext ctx) => next = ctx.performed;
    public void OnPrevious(InputAction.CallbackContext ctx) => previous = ctx.performed;
    public void OnNavigateUp(InputAction.CallbackContext ctx) => navUp = ctx.performed;
    public void OnNavigateDown(InputAction.CallbackContext ctx) => navDown = ctx.performed;
    public void OnNavigateLeft(InputAction.CallbackContext ctx) => navLeft = ctx.performed;
    public void OnNavigateRight(InputAction.CallbackContext ctx) => navRight = ctx.performed;
    public void OnScrollWheel(InputAction.CallbackContext ctx) => scrollWheel = ctx.ReadValue<Vector2>();
    public void OnJump(InputAction.CallbackContext ctx) => jump = ctx.performed;
    public void OnSprint(InputAction.CallbackContext ctx) => sprint = ctx.performed;
    public void OnGrab(InputAction.CallbackContext ctx) => grab = ctx.performed;
    public void OnShoot(InputAction.CallbackContext ctx) => shoot = ctx.performed;
}