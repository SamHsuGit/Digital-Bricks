using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    CharacterController charController;
    // Start is called before the first frame update
    void Start()
    {
        charController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        // look at at base
        // move character controller forwards

        //AI pathfind if path available( if none, start breaking blocks to get to base, then go forwards, check pathfinding, repeat)
    }
}
