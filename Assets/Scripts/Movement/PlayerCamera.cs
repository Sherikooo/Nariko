using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Sensitivity")]
    public float sensX = 10f;
    public float sensY = 10f;


    [Header("Assignables")]
    public Transform camT; // the camera holder
    public Camera cam; // the camera (inside the camera holder)
    public Transform orientation; // orientation of the player

    private float xRotation;
    private float yRotation;

    // Start is called before the first frame update
    void Start()
    {
        // lock the mouse cursor in the middle of the screen
        Cursor.lockState = CursorLockMode.Locked;
        // make the mouse coursor invisible
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
       RotateCamera(); 
    }

    public void RotateCamera()
    {
        // first get the mouse X and Y Input
        float mouseX = Input.GetAxisRaw("Mouse X") * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensY;

        // then calculate the x and y rotation of your camera using this formula:
        /// yRotation + mouseX
        /// xRotation - mouseY
        yRotation += mouseX * sensX;
        xRotation -= mouseY * sensY;

        // make sure that you can't look up or down more than 90* degrees
        xRotation = Mathf.Clamp(xRotation, -89f, 89f);

        // rotate the camera holder along the x and y axis
        camT.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        // rotate the players orientation, but only along the y (horizontal) axis
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }
}
