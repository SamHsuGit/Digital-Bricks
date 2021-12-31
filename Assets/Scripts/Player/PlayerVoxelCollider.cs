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
    CharacterController charController;
    public Vector3 center;
    public float halfColliderHeight;
    public int stepHeight;
    public Vector3 stepUpOffset;
    public float colliderOffset = 1;

    public bool isPlayer = false;
    public bool isCamera = false;
    public bool isAI = false;

    private float gravity = -9.8f * 3; // multiply to account for scaled geometry
    private float verticalMomentum = 0;
    public float width;
    public float length;
    public float height;
    public bool playerChunkIsActive;

    List<Vector3> checkPositions = new List<Vector3>();

    byte[] adjacentVoxelIDs;

    private void Awake()
    {
        world = World.Instance;
    }

    // Start is called before the first frame update
    void Start()
    {
        // called when this gameObject is created
        if (isPlayer)
            world.PlayerJoined(gameObject);

        if (isPlayer)
        {
            controller = GetComponent<Controller>();
            charController = GetComponent<CharacterController>();
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
        else if (isAI)
        {
            charController = GetComponent<CharacterController>();
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
    }

    public Vector3 CalculateVelocity(float horizontal, float vertical, bool isSprinting, bool jumpRequest)
    {
        if (controller == null)
            return Vector3.zero;

        Vector3 velocityPlayer;
        //playerChunkIsActive = PlayerInActiveChunk();

        //if (isGrounded && transform.position.y % 1 != 0)
        //    transform.Translate(new Vector3(0, 1 - transform.position.y % 1, 0)); // ensures elevation is a whole number (no longer needed as we are using colliders)

        if (cc != null)
            center = cc.transform.position + cc.center; // cache current center of collider position

        // updated once per frame update, while PlayerIsTouchingBlockID can be called by other scripts (multiple times per frame update. This reduces calls to GetVoxel).
        adjacentVoxelIDs = GetAdjacentVoxelIDs();

        // reset jumps when grounded
        //if (playerChunkIsActive)
        {
            if (isGrounded || (isPlayer && controller.isGrounded))
                currentJumps = 0;
        }

        // can jump off sides of objects
        if (isPlayer && (front || back || left || right))
            currentJumps = 0;

        // apply jump force
        if (jumpRequest && currentJumps < maxJumps)
        {
            verticalMomentum = baseJumpForce;
            currentJumps++;
        }

        // Affect vertical momentum with gravity.
        if (verticalMomentum > gravity)
            verticalMomentum += Time.fixedDeltaTime * gravity;

        // if we're running on road, increase road multiplier.
        int roadFactor;
        if (PlayerIsTouchingBlockID(3))
            roadFactor = 2;
        else
            roadFactor = 1;

        // if we're sprinting, use the sprint multiplier
        if (isSprinting)
            velocityPlayer = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * baseSprintSpeed * roadFactor;
        else
            velocityPlayer = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * baseWalkSpeed * roadFactor;

        // Apply vertical momentum (falling/jumping).
        velocityPlayer += Vector3.up * verticalMomentum * Time.fixedDeltaTime;

        //isGrounded = CheckGrounded(velocityPlayer.y);

        //Vector3 oldVelocity = velocityPlayer;
        //// horizontal collision detection
        //if (isMoving && velocityPlayer.z > 0 && front || velocityPlayer.z < 0 && back)
        //{
        //    velocityPlayer.z = 0;
        //}
        //if (isMoving && velocityPlayer.x > 0 && right || velocityPlayer.x < 0 && left)
        //{
        //    velocityPlayer.x = 0;
        //}
        // vertical collision detection
        //if (velocityPlayer.y < 0 && controller.isGrounded)
        //{
        //    velocityPlayer.y = 0;
        //}
        //else if (velocityPlayer.y > 0)
        //{
        //    velocityPlayer.y = CheckUpSpeed(velocityPlayer.y);
        //}
        //// step collision detection
        //if (isMoving && isGrounded && isSprinting && stepDetected && CheckIfPlayerCanStepUp())
        //{
        //    // move this gameobject up slightly to get up steps
        //    charController.enabled = false;
        //    transform.position += stepUpOffset;
        //    charController.enabled = true;
        //    velocityPlayer = oldVelocity;
        //}

        checkPositions = CalculateCheckPositions(velocityPlayer.y);

        return velocityPlayer;
    }

    public bool PlayerInActiveChunk()
    {
        if (World.Instance.worldLoaded)
        {
            ChunkCoord currentPlayerChunkCoord = World.Instance.GetChunkFromVector3(transform.position).coord;
            if (World.Instance.chunks[currentPlayerChunkCoord].isActive && World.Instance.chunks.ContainsKey(currentPlayerChunkCoord)) // only do voxel collision detection if player is in an active chunk
            {
                //Debug.Log(currentPlayerChunkCoord.x + ", " + currentPlayerChunkCoord.z + " is active");
                return true;
            }
            else
                return false;
        }
        return false;
    }

    public bool CheckIfPlayerCanStepUp()
    {
        if (!BlockDetected(center.x, center.y - halfColliderHeight + stepHeight, center.z + length / 2) && // check bottom cross
            !BlockDetected(center.x, center.y - halfColliderHeight + stepHeight, center.z - length / 2) &&
            !BlockDetected(center.x + width / 2, center.y - halfColliderHeight + stepHeight, center.z) &&
            !BlockDetected(center.x - width / 2, center.y - halfColliderHeight + stepHeight, center.z) &&
            !BlockDetected(center.x + width / 2, center.y - halfColliderHeight + stepHeight, center.z + length / 2) && // check bottom corners
            !BlockDetected(center.x - width / 2, center.y - halfColliderHeight + stepHeight, center.z - length / 2) &&
            !BlockDetected(center.x - width / 2, center.y - halfColliderHeight + stepHeight, center.z + length / 2) &&
            !BlockDetected(center.x + width / 2, center.y - halfColliderHeight + stepHeight, center.z - length / 2) &&
            !BlockDetected(center.x, center.y, center.z + length / 2) && // check middle cross
            !BlockDetected(center.x, center.y, center.z - length / 2) &&
            !BlockDetected(center.x - width / 2, center.y, center.z) &&
            !BlockDetected(center.x - width / 2, center.y, center.z) &&
            !BlockDetected(center.x + width / 2, center.y, center.z + length / 2) && // check middle corners
            !BlockDetected(center.x - width / 2, center.y, center.z - length / 2) &&
            !BlockDetected(center.x - width / 2, center.y, center.z + length / 2) &&
            !BlockDetected(center.x + width / 2, center.y, center.z - length / 2) &&
            !BlockDetected(center.x, center.y + halfColliderHeight + 1, center.z + length / 2) && // check top cross
            !BlockDetected(center.x, center.y + halfColliderHeight + 1, center.z - length / 2) &&
            !BlockDetected(center.x + width / 2, center.y + halfColliderHeight + 1, center.z) &&
            !BlockDetected(center.x - width / 2, center.y + halfColliderHeight + 1, center.z) &&
            !BlockDetected(center.x + width / 2, center.y + halfColliderHeight + 1, center.z + length / 2) && // check top corners
            !BlockDetected(center.x - width / 2, center.y + halfColliderHeight + 1, center.z - length / 2) &&
            !BlockDetected(center.x - width / 2, center.y + halfColliderHeight + 1, center.z + length / 2) &&
            !BlockDetected(center.x + width / 2, center.y + halfColliderHeight + 1, center.z - length / 2)
            )
            return true;
        else
            return false;
    }

    List<Vector3> CalculateCheckPositions(float yVelocity)
    {
        checkPositions = new List<Vector3>();

        // adds top check position
        checkPositions.Add(new Vector3(center.x, center.y + Mathf.FloorToInt(halfColliderHeight) + colliderOffset + yVelocity, center.z)); // top
        
        // adds front, back, left, right for multiple levels in the character model (works bottom to top).
        for (int y = -Mathf.FloorToInt(halfColliderHeight); y < Mathf.FloorToInt(halfColliderHeight); y++)
        {
            checkPositions.Add(new Vector3(center.x, center.y + y + yVelocity, center.z + length / 2 + colliderOffset)); // front
            checkPositions.Add(new Vector3(center.x, center.y + y + yVelocity, center.z - length / 2 - colliderOffset)); // back
            checkPositions.Add(new Vector3(center.x - width / 2 - colliderOffset, center.y + y + yVelocity, center.z)); // left
            checkPositions.Add(new Vector3(center.x + width / 2 + colliderOffset, center.y + y + yVelocity, center.z)); // right
        }

        // adds bottom check position
        checkPositions.Add(new Vector3(center.x, center.y - Mathf.FloorToInt(halfColliderHeight) - colliderOffset + yVelocity, center.z)); // bottom

        return checkPositions;
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

    // helps traverse blocky terrain
    public bool BlockDetected(float xCheck, float yCheck, float zCheck)
    {
        if (world.CheckForVoxel(new Vector3(xCheck, yCheck, zCheck)))
            return true;
        else
            return false;
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

    public bool stepDetected // checks in cross pattern
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(center.x, center.y - halfColliderHeight, center.z + length / 2 + colliderOffset)) || // front bottom
                world.CheckForVoxel(new Vector3(center.x, center.y - halfColliderHeight, center.z - length / 2 - colliderOffset)) || // back bottom
                world.CheckForVoxel(new Vector3(center.x - width / 2 - colliderOffset, center.y - halfColliderHeight, center.z)) || // left bottom
                world.CheckForVoxel(new Vector3(center.x + width / 2 + colliderOffset, center.y - halfColliderHeight, center.z))    // right bottom
                )
                return true;
            else
                return false;
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

    public byte[] GetAdjacentVoxelIDs() // only called once per frame update while PlayerIsTouchingBlockID can be called multiple times per frame update to minimize the calls to GetVoxel.
    {
        byte[] adjacentVoxelIDs = new byte[checkPositions.Count];

        for (int i = 0; i < adjacentVoxelIDs.Length; i++) // for all adjacent voxels
        {
            if (!World.Instance.CheckForVoxel(checkPositions[i])) // if voxel to check is not solid (e.g. air) or there is no voxel, then skip this position
            {
                adjacentVoxelIDs[i] = 0;
                continue;
            }

            // uses getVoxel for most accurate voxelstate
            adjacentVoxelIDs[i] = World.Instance.GetVoxelState(new Vector3(Mathf.FloorToInt(checkPositions[i].x), Mathf.FloorToInt(checkPositions[i].y), Mathf.FloorToInt(checkPositions[i].z))).id;
        }
        return adjacentVoxelIDs;
    }
}
