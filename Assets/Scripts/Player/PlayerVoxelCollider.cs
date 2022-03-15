using System.Collections.Generic;
using UnityEngine;

public class PlayerVoxelCollider : MonoBehaviour
{
    
    public float baseWalkSpeed;
    public float baseSprintSpeed;
    public float baseJumpForce = 15f;
    public bool isMoving = false;
    public bool isGrounded = false;
    public int maxJumps;
    public int currentJumps;

    World world;
    CapsuleCollider cc;
    Controller controller;
    public Vector3 center;
    public float halfColliderHeight;
    public int stepHeight;
    public Vector3 stepUpOffset;
    public float colliderOffset = 1;

    public bool isPlayer = false;
    public bool isCamera = false;

    private float gravity = -9.8f * 3; // multiply to account for scaled geometry
    private float verticalMomentum = 0;
    public float width;
    public float length;
    public float height;
    public bool playerChunkIsActive;

    byte[] adjacentVoxelIDs;

    // Start is called before the first frame update
    void Start()
    {
        if (isPlayer)
        {
            if(GetComponent<Controller>() != null)
            {
                controller = GetComponent<Controller>();
                controller.world.JoinPlayer(gameObject);
            }
                
            //set initial char size
            if (gameObject.GetComponent<CapsuleCollider>() != null)
            {
                cc = gameObject.GetComponent<CapsuleCollider>();
                width = cc.radius * 2;
                height = cc.height;
                length = cc.radius * 2;

                halfColliderHeight = height / 2;
                stepHeight = 1;
                colliderOffset = 1;
                stepUpOffset = new Vector3(0, stepHeight, 0);
                maxJumps = 2;
            }
        }
        else if (isCamera)
        {
            width = 1;
            height = 1;
            length = 1;
            stepHeight = 1;
            colliderOffset = 1;
        }
    }

    public Vector3 CalculateVelocity(float horizontal, float vertical, bool isSprinting, bool jumpRequest)
    {
        if (controller == null)
            return Vector3.zero;

        Vector3 velocityPlayer;
        //playerChunkIsActive = PlayerInActiveChunk();

        if (cc != null)
            center = cc.transform.position + cc.center; // cache current center of collider position

        if (!SettingsStatic.LoadedSettings.flight)
        {
            // reset jumps when grounded
            if (isGrounded || (isPlayer && controller.isGrounded))
                currentJumps = 0;

            // can jump off sides of objects
            if (isPlayer && (front || back || left || right))
                currentJumps = 0;

            // apply jump force
            if (jumpRequest && currentJumps < maxJumps)
            {
                verticalMomentum = baseJumpForce;
                currentJumps++;
            }

            // Affect vertical momentum with gravity if flight not enabled
            if (verticalMomentum > gravity)
                verticalMomentum += Time.fixedDeltaTime * gravity;
        }

        // if we're running on road, increase road multiplier.
        int roadFactor;
        if (PlayerIsTouchingBlockID(3))
            roadFactor = 2;
        else
            roadFactor = 1;

        // if we're sprinting, use the sprint multiplier
        if(SettingsStatic.LoadedSettings.flight)
            velocityPlayer = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * baseSprintSpeed * 0.1f;
        else if (isSprinting)
            velocityPlayer = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * baseSprintSpeed * roadFactor;
        else
            velocityPlayer = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * baseWalkSpeed * roadFactor;

        if (!SettingsStatic.LoadedSettings.flight)
        {
            // Apply vertical momentum (falling/jumping).
            velocityPlayer += Vector3.up * verticalMomentum * Time.fixedDeltaTime;
        }

        return velocityPlayer;
    }

    public Vector3 CalculateVelocityCamera(float horizontal, float vertical, bool isSprinting)
    {
        Vector3 velocityCamera;
        //bool cameraChunkIsActive = PlayerInActiveChunk();

        center = transform.position;

        // if we're sprinting, use the sprint multiplier
        if (isSprinting)
            velocityCamera = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * baseSprintSpeed;
        else
            velocityCamera = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * baseWalkSpeed;

        //if (cameraChunkIsActive) // DISABLED since the camera was getting stuck inside meshes due to character controller?, more important to have a free camera
        {
            isGrounded = CheckGrounded(velocityCamera.y);
            // horizontal collision detection
            if ((velocityCamera.z > 0 && front) || (velocityCamera.z < 0 && back))
                velocityCamera.z = 0;
            if ((velocityCamera.x > 0 && right) || (velocityCamera.x < 0 && left))
                velocityCamera.x = 0;
            // vertical collision detection
            if (velocityCamera.y < 0 && isGrounded)
                velocityCamera.y = 0;
            else if (velocityCamera.y > 0)
                velocityCamera.y = CheckUpSpeed(velocityCamera.y);
        }

        return velocityCamera;
    }

    public bool CheckGrounded(float downSpeed) // checks in cross pattern
    {
        float distToVoxelBelow = center.y - halfColliderHeight + downSpeed;

        if (controller != null && controller.isGrounded) //use physics raycast grounded if true, else check voxels
            return true;

        //if (distToVoxelBelow < 0) // prevent checking voxels below bottom of world
        //    return false; // allow player to move below bottom of world chunks

        if (
            world.CheckForVoxel(new Vector3(center.x - width / 2 + colliderOffset, distToVoxelBelow, center.z)) ||
            world.CheckForVoxel(new Vector3(center.x + width / 2 - colliderOffset, distToVoxelBelow, center.z)) ||
            world.CheckForVoxel(new Vector3(center.x, distToVoxelBelow, center.z - length / 2 + colliderOffset)) ||
            world.CheckForVoxel(new Vector3(center.x, distToVoxelBelow, center.z + length / 2 - colliderOffset))
           )
        {
            //Debug.Log("landed on: " + center.x + ", " + (center.y - yOffset + downSpeed) + ", " + (center.z + width / 2 - colliderOffset));
            return true;
        }
        else
        {
            return false;
        }
    }

    public float CheckUpSpeed(float upSpeed) // checks in cross pattern
    {
        float distToVoxelAbove = center.y + halfColliderHeight + upSpeed;

        if (distToVoxelAbove >= VoxelData.ChunkHeight) // prevent checking voxels above top of world
            return upSpeed; // allow player to move above top of world chunks

        if (
            world.CheckForVoxel(new Vector3(center.x - width / 2 + colliderOffset, distToVoxelAbove, center.z)) ||
            world.CheckForVoxel(new Vector3(center.x + width / 2 - colliderOffset, distToVoxelAbove, center.z)) ||
            world.CheckForVoxel(new Vector3(center.x, distToVoxelAbove, center.z - length / 2 + colliderOffset)) ||
            world.CheckForVoxel(new Vector3(center.x, distToVoxelAbove, center.z + length / 2 - colliderOffset))
           )
        {
            return 0;
        }
        else
        {
            return upSpeed;
        }
    }

    public bool front // checks 2 corners
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(center.x, center.y + halfColliderHeight, center.z + length / 2 + colliderOffset)) ||
                world.CheckForVoxel(new Vector3(center.x, center.y - halfColliderHeight, center.z + length / 2 + colliderOffset))
                )
                return true;
            else
                return false;
        }
    }
    public bool back // checks 2 corners
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(center.x, center.y + halfColliderHeight, center.z - length / 2 - colliderOffset)) ||
                world.CheckForVoxel(new Vector3(center.x, center.y - halfColliderHeight, center.z - length / 2 - colliderOffset))
                )
                return true;
            else
                return false;
        }
    }
    public bool left // checks 2 corners
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(center.x - width / 2 - colliderOffset, center.y + halfColliderHeight, center.z)) ||
                world.CheckForVoxel(new Vector3(center.x - width / 2 - colliderOffset, center.y - halfColliderHeight, center.z))
                )
                return true;
            else
                return false;
        }
    }
    public bool right // checks 2 corners
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(center.x + width / 2 + colliderOffset, center.y + halfColliderHeight, center.z)) ||
                world.CheckForVoxel(new Vector3(center.x + width / 2 + colliderOffset, center.y - halfColliderHeight, center.z))
                )
                return true;
            else
                return false;
        }
    }

    public bool PlayerIsTouchingBlockID(byte _blockID)
    {
        bool isTouching = false;

        if(playerChunkIsActive && adjacentVoxelIDs != null)
        {
            for (int i = 0; i < adjacentVoxelIDs.Length; i++) // for all check positions around player
            {
                // uses getVoxel for most accurate voxelstate
                if (_blockID == adjacentVoxelIDs[i]) // if any of the voxel ids matches the id we are looking for, then mark as touching
                    isTouching = true;
            }
        }
        return isTouching;
    }
}
