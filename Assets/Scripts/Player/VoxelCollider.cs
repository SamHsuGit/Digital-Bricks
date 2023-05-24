using UnityEngine;

public class VoxelCollider : MonoBehaviour
{
    public float baseWalkSpeed;
    public float baseSprintSpeed;
    public float baseJumpForce = 15f;
    public bool isMoving = false;
    public bool isGrounded = false;
    public int maxJumps;
    public int currentJumps;

    public World world;
    public Controller controller;
    public Vector3 center;
    public float halfColliderHeight;
    public int stepHeight;
    public Vector3 stepUpOffset;
    public float colliderOffset = 1;

    public bool isPlayer = false;
    public bool isCamera = false;

    CapsuleCollider cc;
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

        if (cc != null)
            center = cc.transform.position + cc.center; // cache current center of collider position

        if (!SettingsStatic.LoadedSettings.creativeMode)
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
            if (world.worldLoaded &&  verticalMomentum > gravity)
                verticalMomentum += Time.fixedDeltaTime * gravity;
        }

        // if we're running on road, increase road multiplier.
        int roadFactor;
        if (PlayerIsTouchingBlockID(3))
            roadFactor = 2;
        else
            roadFactor = 1;

        // if we're sprinting, use the sprint multiplier
        if(SettingsStatic.LoadedSettings.creativeMode)
            velocityPlayer = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * baseSprintSpeed * 0.1f;
        else if (isSprinting)
            velocityPlayer = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * baseSprintSpeed * roadFactor;
        else
            velocityPlayer = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * baseWalkSpeed * roadFactor;

        if (!SettingsStatic.LoadedSettings.creativeMode)
        {
            // Apply vertical momentum (falling/jumping).
            velocityPlayer += Vector3.up * verticalMomentum * Time.fixedDeltaTime;
        }

        isGrounded = CheckGrounded(velocityPlayer.y);

        // voxel based collision detection (no chunk meshes due to performance cost of updating chunk meshes)
        if (!SettingsStatic.LoadedSettings.creativeMode)
        {
            if ((velocityPlayer.z > 0 && front) || (velocityPlayer.z < 0 && back))
                velocityPlayer.z = 0;
            if ((velocityPlayer.x > 0 && right) || (velocityPlayer.x < 0 && left))
                velocityPlayer.x = 0;
            if (velocityPlayer.y < 0)
                velocityPlayer.y = CheckDownSpeed(velocityPlayer.y);
            if (velocityPlayer.y > 0)
                velocityPlayer.y = CheckUpSpeed(velocityPlayer.y);
        }

        return velocityPlayer;
    }

    public Vector3 CalculateVelocityCamera(float horizontal, float vertical, bool isSprinting)
    {
        Vector3 velocityCamera;

        center = transform.position;

        // if we're sprinting, use the sprint multiplier
        if (isSprinting)
            velocityCamera = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * baseSprintSpeed;
        else
            velocityCamera = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * baseWalkSpeed;

        {
            isGrounded = CheckGrounded(velocityCamera.y);
            
            // horizontal collision detection
            if ((velocityCamera.z > 0 && front) || (velocityCamera.z < 0 && back)) // (Global East/West)
                velocityCamera.z = 0;
            if ((velocityCamera.x > 0 && right) || (velocityCamera.x < 0 && left)) // (Global North/South)
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
            World.Instance.CheckForVoxel(new Vector3(center.x - width / 2 - colliderOffset, distToVoxelBelow, center.z)) || // left bottom center
            World.Instance.CheckForVoxel(new Vector3(center.x + width / 2 + colliderOffset, distToVoxelBelow, center.z)) || // right bottom center
            World.Instance.CheckForVoxel(new Vector3(center.x, distToVoxelBelow, center.z - length / 2 - colliderOffset)) || // center bottom back
            World.Instance.CheckForVoxel(new Vector3(center.x, distToVoxelBelow, center.z + length / 2 + colliderOffset)) // center bottom front
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

    public float CheckDownSpeed(float downSpeed) // checks in cross pattern
    {
        float distToVoxelBelow = center.y - halfColliderHeight + downSpeed;

        //if (distToVoxelBelow < 0) // prevent checking voxels below bottom of world
        //    return false; // allow player to move below bottom of world chunks

        if (
            //World.Instance.CheckForVoxel(new Vector3(center.x - width / 2 + colliderOffset, distToVoxelBelow, center.z)) || // left bottom center
            //World.Instance.CheckForVoxel(new Vector3(center.x + width / 2 - colliderOffset, distToVoxelBelow, center.z)) || // right bottom center
            //World.Instance.CheckForVoxel(new Vector3(center.x, distToVoxelBelow, center.z - length / 2 - colliderOffset)) || // center bottom back
            //World.Instance.CheckForVoxel(new Vector3(center.x, distToVoxelBelow, center.z + length / 2 + colliderOffset)) // center bottom front

            World.Instance.CheckForVoxel(new Vector3(center.x, distToVoxelBelow, center.z)) // directly below player
           )
        {
            //Debug.Log("landed on: " + center.x + ", " + (center.y - yOffset + downSpeed) + ", " + (center.z + width / 2 - colliderOffset));
            isGrounded = true;
            return 0;
        }
        else
        {
            isGrounded = false;
            return downSpeed;
        }
    }

    public float CheckUpSpeed(float upSpeed) // checks in cross pattern
    {
        float distToVoxelAbove = center.y + halfColliderHeight + upSpeed;

        if (distToVoxelAbove >= VoxelData.ChunkHeight) // prevent checking voxels above top of world
            return upSpeed; // allow player to move above top of world chunks

        if (
            //World.Instance.CheckForVoxel(new Vector3(center.x - width / 2 + colliderOffset, distToVoxelAbove, center.z)) || // left top center
            //World.Instance.CheckForVoxel(new Vector3(center.x + width / 2 - colliderOffset, distToVoxelAbove, center.z)) || // right top center
            //World.Instance.CheckForVoxel(new Vector3(center.x, distToVoxelAbove, center.z - length / 2 - colliderOffset)) || // center top back
            //World.Instance.CheckForVoxel(new Vector3(center.x, distToVoxelAbove, center.z + length / 2 + colliderOffset)) // center top front

            World.Instance.CheckForVoxel(new Vector3(center.x, distToVoxelAbove, center.z)) // directly above player
           )
        {
            return 0;
        }
        else
        {
            return upSpeed;
        }
    }

    public bool front // checks front 2 points
    {
        get
        {
            if (
                World.Instance.CheckForVoxel(new Vector3(center.x, center.y + halfColliderHeight, center.z + length / 2 + colliderOffset)) || // center top front
                World.Instance.CheckForVoxel(new Vector3(center.x, center.y - halfColliderHeight, center.z + length / 2 + colliderOffset)) // center bottom front
                )
                return true;
            else
                return false;
        }
    }
    public bool back // checks back 2 points
    {
        get
        {
            if (
                World.Instance.CheckForVoxel(new Vector3(center.x, center.y + halfColliderHeight, center.z - length / 2 - colliderOffset)) || // center top back
                World.Instance.CheckForVoxel(new Vector3(center.x, center.y - halfColliderHeight, center.z - length / 2 - colliderOffset)) // center bottom back
                )
                return true;
            else
                return false;
        }
    }
    public bool left // checks left 2 points
    {
        get
        {
            if (
                World.Instance.CheckForVoxel(new Vector3(center.x - width / 2 - colliderOffset, center.y + halfColliderHeight, center.z)) || // left top center
                World.Instance.CheckForVoxel(new Vector3(center.x - width / 2 - colliderOffset, center.y - halfColliderHeight, center.z)) // left bottom center
                )
                return true;
            else
                return false;
        }
    }
    public bool right // checks right 2 points
    {
        get
        {
            if (
                World.Instance.CheckForVoxel(new Vector3(center.x + width / 2 + colliderOffset, center.y + halfColliderHeight, center.z)) || // right top center
                World.Instance.CheckForVoxel(new Vector3(center.x + width / 2 + colliderOffset, center.y - halfColliderHeight, center.z)) // right bottom center
                )
                return true;
            else
                return false;
        }
    }

    public bool PlayerIsTouchingBlockID(byte blockID)
    {
        bool isTouching = false;

        if(playerChunkIsActive && adjacentVoxelIDs != null)
        {
            for (int i = 0; i < adjacentVoxelIDs.Length; i++) // for all check positions around player
            {
                // uses getVoxel for most accurate voxelstate
                if (blockID == adjacentVoxelIDs[i]) // if any of the voxel ids matches the id we are looking for, then mark as touching
                    isTouching = true;
            }
        }
        return isTouching;
    }
}
