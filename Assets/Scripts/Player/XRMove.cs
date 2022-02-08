using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class XRMove : MonoBehaviour
{
    Rigidbody rb;
    public bool touched = false;
    public GameObject playerCamera;
    public float moveForce = 0.1f;

    private PlayerInputActions touchControls;

    private void Awake()
    {
        touchControls = new PlayerInputActions();
    }

    private void OnEnable()
    {
        touchControls.Enable();
    }

    private void OnDisable()
    {
        touchControls.Disable();
    }

    void Start()
    {
        touchControls.Actions.Touched.started += ctx => StartTouch(ctx);
        touchControls.Actions.Touched.canceled += ctx => EndTouch(ctx);
        rb = GetComponent<Rigidbody>();
    }

    private void StartTouch(InputAction.CallbackContext context)
    {
        touched = true;
    }

    private void EndTouch(InputAction.CallbackContext context)
    {
        touched = false;
    }

    void Update()
    {
        if (touched)
            transform.Translate(playerCamera.transform.forward * moveForce);
        //rb.AddForce(playerCamera.transform.forward * moveForce, ForceMode.Impulse);
    }
}
