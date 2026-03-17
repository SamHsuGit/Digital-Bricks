using UnityEngine;
using UnityEngine.InputSystem;
public class InputHandler : MonoBehaviour
{
    [Header("public variables")]
    public Vector2 move;
    public Vector2 look;
    public bool inventory;
    public bool drop;
    public bool navUp;
    public bool navDown;
    public bool navLeft;
    public bool navRight;
    public Vector2 scrollWheel;
    public bool jump = false;
    public bool crouch = false;
    public bool use = false;
    public bool mine = false;

    public void OnMove(InputAction.CallbackContext ctx) => move = ctx.ReadValue<Vector2>();
    public void OnLook(InputAction.CallbackContext ctx) => look = ctx.ReadValue<Vector2>();
    public void OnInventory(InputAction.CallbackContext ctx) => inventory = ctx.performed;
    public void OnDrop(InputAction.CallbackContext ctx) => drop = ctx.performed;
    public void OnNavigateUp(InputAction.CallbackContext ctx) => navUp = ctx.performed;
    public void OnNavigateDown(InputAction.CallbackContext ctx) => navDown = ctx.performed;
    public void OnNavigateLeft(InputAction.CallbackContext ctx) => navLeft = ctx.performed;
    public void OnNavigateRight(InputAction.CallbackContext ctx) => navRight = ctx.performed;
    public void OnScrollWheel(InputAction.CallbackContext ctx) => scrollWheel = ctx.ReadValue<Vector2>();
    public void OnJump(InputAction.CallbackContext ctx) => jump = ctx.performed;
    public void OnCrouch(InputAction.CallbackContext ctx) => crouch = ctx.performed;
    public void OnUse(InputAction.CallbackContext ctx) => use = ctx.performed;
    public void OnMine(InputAction.CallbackContext ctx) => mine = ctx.performed;
}