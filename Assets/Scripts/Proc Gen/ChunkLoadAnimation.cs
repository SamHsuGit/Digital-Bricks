using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkLoadAnimation : MonoBehaviour
{
    float speed = 20f;
    Vector3 targetPos;

    float waitTimer;
    float timer;

    void Start()
    {
        waitTimer = Random.Range(0f, 0.1f);
        targetPos = transform.position;
        transform.position = new Vector3(transform.position.x, VoxelData.ChunkHeight, transform.position.z);
    }

    void Update()
    {
        if (timer < waitTimer)
        {
            timer += Time.deltaTime;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * speed);
            if (Mathf.Abs(targetPos.y - transform.position.y) < 0.05f)
            {
                transform.position = targetPos;
                Destroy(this);
            }
        }
    }
}
