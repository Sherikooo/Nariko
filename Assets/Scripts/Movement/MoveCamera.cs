using System.Collections;
using System.Collections.Generic;
using UnityEngine;



// Moves the cameraHolder to the position of the player


public class MoveCamera_MLab : MonoBehaviour
{
    public Transform cameraPos; // an empty gameObject inside the player, that indicates where the camera should be

    private void Update()
    {
        // move the cameraHolder to the intendet position
        transform.position = cameraPos.position;
    }
}
