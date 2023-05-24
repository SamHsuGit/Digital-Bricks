using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelBit : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // destroy this object after a while to conserve system resources
        //Destroy(this, 30);
    }

    private void FixedUpdate()
    {
        // hover the voxel bits to be picked up
        //transform.position = new Vector3(transform.position.x, transform.position.y + Mathf.Sin(Time.deltaTime), transform.position.z);
    }
}
