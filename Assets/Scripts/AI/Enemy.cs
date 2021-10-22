using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("References")]
    public Vector3 goal;
    public Animator animator;
    public GameObject model;

    [Header("Debug States")]
    public Vector3 moveVector;
    public bool isGrounded;
    public bool isMoving;
    public bool isSprinting;
    public bool isHolding;
    public bool isJumping;

    CharacterController charController;
    CapsuleCollider cc;
    PlayerVoxelCollider voxelCollider;

    float sphereCastRadius;

    private void Awake()
    {
        charController = GetComponent<CharacterController>();
        cc = GetComponent<CapsuleCollider>();
        sphereCastRadius = cc.radius * 0.5f;
        voxelCollider = GetComponent<PlayerVoxelCollider>();
    }

    void Start()
    {
        
    }

    void FixedUpdate()
    {
        if (World.Instance.baseOb == null)
            return;
        else
        {
            goal = World.Instance.baseOb.transform.position;
            isGrounded = CheckGroundedCollider();
            isSprinting = false;
            isJumping = false;

            if (transform.position != goal)
            {
                isMoving = true;
                moveVector = goal - transform.position; // moveVector is the difference of the goal and current position
                moveVector.Normalize();
                moveVector = voxelCollider.CalculateVelocity(moveVector.x, moveVector.z, isSprinting, isJumping);
                if (moveVector != Vector3.zero)
                {
                    if (charController.enabled)
                        charController.Move(moveVector); // move the characterController in direction towards goal
                }

                Vector3 lookVector = moveVector - model.transform.forward;
                model.transform.localEulerAngles = lookVector;
            }
            else
            {
                isMoving = false;
            }

            //AI pathfind if path available( if none, start breaking blocks to get to base, then go forwards, check pathfinding, repeat)
            // if breaking wall
            //isHolding = true;

            animator.SetFloat("Speed", moveVector.magnitude * 3f);
            animator.SetBool("isMoving", isMoving);
            animator.SetBool("isGrounded", isGrounded);
            animator.SetBool("isSprinting", isSprinting);
            animator.SetBool("isHolding", isHolding);
        }
    }

    bool CheckGroundedCollider()
    {
        float rayLength;
        Vector3 rayStart = transform.position;

        // cast a ray starting from within the capsule collider down to just outside the capsule collider.
        rayLength = cc.height * 0.25f + 0.01f;
        

        // Adjust the raycast to be slightly below the collider to allow the collider to climb small slopes
        rayLength += cc.height * 0.6f;

        // Debug tools
        Debug.DrawRay(rayStart, Vector3.down * rayLength, Color.red, 0.02f);

        // check if the char is grounded by casting a ray from rayStart down extending rayLength
        if (Physics.SphereCast(rayStart, sphereCastRadius, Vector3.down, out RaycastHit hit, rayLength))
            return true;
        else
            return false;
    }
}