using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{    
    private Vector3 moveDirection;

    public Transform orientation;

    public float playerHeight = 0.5f;


    [Header("Movement")]
    public float moveForce = 12f;
    public float gravity = -9.81f;
    /// how much air control you have
    /// for example: airMultiplier = 0.5f -> you can only move half as fast will being in the air
    public float airMultiplier = 1f;
    public float groundDrag = 5f;
    public float jumpForce = 13f;
    bool readyToJump;
    public int doubleJumps = 1;
    private int doubleJumpsLeft;
    public float crouchYScale = 0.5f; // how tall your player is while crouching (0.5f -> half as tall as normal)
    private float startYScale;
    //fixes bugs
    public float jumpCooldown = 0.25f;


    [Header("Speed handling")]
    // these variables define how fast your player can move while being in the specific movemt mode
    public float runMaxSpeed = 4f;
    public float crouchMaxSpeed = 2f;
    public float slopeSlideMaxSpeed = 30f;
    public float airMaxSpeed = 4f;
    private float desiredMaxSpeed; // needed to smoothly change between speed limitations
    private float desiredMaxSpeedLastFrame; // the previous desired max speed

    [Header("Movement Modes")] 
    [HideInInspector] public MovementMode mm; // this variable stores the current movement mode of the player
    public enum MovementMode 
    {
        running,
        crouching,
        air
    };



    [Header("Input")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("References")]
    private PlayerCamera cam;
    private Rigidbody rb; // the players rigidbody

    [HideInInspector] public float horizontalInput;
    [HideInInspector] public float verticalInput;
    [HideInInspector] public bool grounded;
    [HideInInspector] public bool crouching;

    [Header("Ground Detection")]
    public LayerMask whatIsGround;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public float maxSlopeAngle = 40f; // how steep the slopes you walk on can be



    // Start is called before the first frame update
    void Start()
    {
        if (whatIsGround.value == 0)
            whatIsGround = LayerMask.GetMask("Default");

        rb = GetComponent<Rigidbody>();
        cam = GetComponent<PlayerCamera>();

        // freeze all rotation on the rigidbody, otherwise the player falls over
        /// (a capsule would fall over)
        rb.freezeRotation = true;

        readyToJump = true;
        
        startYScale = transform.localScale.y;                                                                                                                        //add the speed controller and a banger movementmode function
    }

    // Update is called once per frame
    void Update()
    {   
        MyInput();
        DragHandler();
        SpeedControl();
        StateHandler();

        // shooting a raycast down from the middle of the player and checking if it hits the ground
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        // if you hit the ground again after double jumping, reset your double jumps
        if (grounded && doubleJumpsLeft != doubleJumps)
            ResetDoubleJumps();
    }

    private void DragHandler()
    {
        if(grounded){
            rb.drag = groundDrag;
        } else {
            rb.drag = 0;
        }
        // // if you're walking or sprinting, apply drag to your rigidbody in order to prevent slippery movement
        // if (mm == MovementMode.walking || mm == MovementMode.sprinting)
        //     rb.drag = groundDrag;

        // // in any other case you don't want any drag
        // else
        //     rb.drag = 0;
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        // get your W,A,S,D inputs from your keyboard
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if(Input.GetKey(jumpKey) && grounded && readyToJump)
        {
            readyToJump = false;
            Jump();

            // This will set readyToJump to true again after the cooldown is over
            Invoke(nameof(ResetJump), jumpCooldown);
        }
        // if you press the crouch key while not pressing W,A,S or D -> start crouching
        /// Note: if you are pressing W,A,S or D, the sliding script will start a slide instead
        if (Input.GetKeyDown(crouchKey) && horizontalInput == 0 && verticalInput == 0)
            StartCrouch();

        // uncrouch again when you release the crouch key
        if (Input.GetKeyUp(crouchKey) && crouching)
            StopCrouch();

        // // if you press the jump key while being in the air -> perform a double jump
        // else if(Input.GetKeyDown(jumpKey) && (mm == MovementMode.air || mm == MovementMode.walljumping))
        // {
        //     DoubleJump();
        // }

        // if you press the jump key while being in the air -> perform a double jump
        else if(Input.GetKeyDown(jumpKey) && (!grounded))
        {
            DoubleJump();
        }

    }

    private void MovePlayer()
    {
        // calculate the direction you want to move in
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // movement on ground
        if(grounded)
            rb.AddForce(moveDirection.normalized * moveForce * 10f, ForceMode.Force);

        // movement in air
        else if(!grounded)
            rb.AddForce(moveDirection.normalized * moveForce * 10f * airMultiplier, ForceMode.Force);

    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        //limit velocity
        if(flatVel.magnitude > desiredMaxSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * desiredMaxSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    #region StateMachine

    // Basically it just decides in which movement mode the player is currently in and sets the maxSpeed accordingly
    MovementMode movementModeLastFrame;
    MovementMode previousMovementMode;
    private void StateHandler()
    {   
        // Mode - Crouching
        if (crouching && grounded)
        {
            mm = MovementMode.crouching;
            desiredMaxSpeed = crouchMaxSpeed;
        }
        else if (grounded)
        {
            mm = MovementMode.running;
            desiredMaxSpeed = runMaxSpeed;
            }
        else 
        {
            mm = MovementMode.air;
            desiredMaxSpeed = airMaxSpeed;
        }
        
        bool desiredMaxSpeedHasChanged = desiredMaxSpeed != desiredMaxSpeedLastFrame;
    }

    #endregion

    #region Jump

    public void Jump()
    {
        // reset of y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // add upward force
        rb.AddForce(orientation.up * jumpForce, ForceMode.Impulse);
    }

    public void DoubleJump()
    {
        //if you have no jumps left, stop
        if (doubleJumpsLeft <= 0) return;

        // bugfix for wallrunning
        //if (movementmode == MovementMode.wallrunning) return;

        // reset of y velocity
        // get rb velocity without y axis
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // save this velocity
        float flatVelMag = flatVel.magnitude;

        Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        
        // make midairjump directionalchange possible
        rb.velocity = inputDirection.normalized * flatVelMag;

        rb.AddForce(orientation.up * jumpForce, ForceMode.Impulse);

        doubleJumpsLeft--;
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    public void ResetDoubleJumps()
    {
        doubleJumpsLeft = doubleJumps;
    }

    #endregion
    
    #region Crouching

    /// called when crouchKey is pressed down
    private void StartCrouch()
    {
        Debug.Log("Test");
        // shrink the player down
        transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);

        // after shrinking, you'll be a bit in the air, so add downward force to hit the ground again
        /// not optimal but idk
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

        crouching = true;
    }

    /// called when crouchKey is released
    private void StopCrouch()
    {
        // make sure your players size is the same as before
        transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);

        crouching = false;
    }

    #endregion
    

}
