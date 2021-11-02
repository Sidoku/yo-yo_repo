using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController_Xueqing : MonoBehaviour
{
    [Header("Player in the room")]
    // public GameObject Bag;
    public static PlayerController_Xueqing Instance;

    public float speed, jumpForce;
    public int playerGravityScale;
    // [Header("Player State")] 
    // public float health;
    // public bool isDead;

    [Header("States Check")] public bool isGround;
    public bool isJump;
    public bool canJump;

    // public bool isPlatform;
    [Header("Ground Check")] public Transform groundCheck;
    public LayerMask groundLayer, slopeLayer;
    public float checkRadius;
    [Header("FX Check")] public GameObject jumpFX;
    public GameObject fallFX;
    public GameObject leftRunFX;

    [Header("Slope Function")] public float slopeCheckDistance;

    private Vector2 colliderSize;
    private Vector2 newVelocity; //new move & jump method
    private Vector2 newForce;
    private float slopeDownAngle;
    private float slopeDownAngleOld;
    private float slopeSideAngle;
    private Vector2 slopeNormalPerpendicular;

    [SerializeField] private bool isOnSlope;
    private bool isJumping;
    private CapsuleCollider2D bc2D;

    public PhysicsMaterial2D noFriction;
    public PhysicsMaterial2D fullFriction;

    #region Sid's movement

    InputMaster controls;

    Vector2 move;

    //bool jump;
    //bool boost;
    public int boost;

    public float boostForce;
    private float moveInput;

    public int jumps;
    public int maxJumps = 2;
    public int maxBoost = 1;

    #endregion

    #region private members

    private MessageTest _messageTest;
    private Rigidbody2D rb;
    private Animator _animator;
    public float horizontalInput;

    private int animationCount;

    #endregion

    //买了几个技能的判断
    // private int _purchasedSkill;

    private void OnEnable()
    {
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
    }

    private void Awake()
    {
        Instance = this;
        controls = new InputMaster();

        //Horizontal movement controls
        controls.Player.Movement.performed += ctx => move = ctx.ReadValue<Vector2>();
        controls.Player.Movement.canceled += ctx => move = Vector2.zero;

        //Jump
        controls.Player.Jump.performed += ctx => this.Jump();
        //controls.Player.Jump.canceled += ctx => jump = false;

        controls.Player.Boost.performed += ctx => this.Boost();
        //controls.Player.Boost.canceled += ctx => boost = false; //removed ability to cancel boost, was doing it too often
    }

    void Start()
    {
        _messageTest = FindObjectOfType<MessageTest>();
        // Bag.SetActive(false);
        bc2D = GetComponent<CapsuleCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        colliderSize = bc2D.size;
    }

    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        rb.AddForce(new Vector2(move.x * speed * Time.fixedDeltaTime, move.y), ForceMode2D.Impulse);
        PhysicsCheck();
        Movement();
        SlopeCheck();
    }
    
    private void Movement()
    {
        if (move.x == 0)
        {
            leftRunFX.SetActive(false);
        }

        if (move.x != 0 && isGround)
        {
            leftRunFX.SetActive(true);
            transform.localScale = new Vector3(move.x, 1, 1);
        }
        else if (move.x != 0 && isJump)
        {
            transform.localScale = new Vector3(move.x, 1, 1);
            leftRunFX.SetActive(false);
        }

        if (isOnSlope && !canJump)
        {
            rb.AddForce(new Vector2(-move.x * speed * slopeNormalPerpendicular.x * Time.fixedDeltaTime, -move.x * speed * slopeNormalPerpendicular.y * Time.fixedDeltaTime), ForceMode2D.Impulse);
            // newVelocity.Set(-move.x * speed * slopeNormalPerpendicular.x, -move.x * speed * slopeNormalPerpendicular.y);
            // // newVelocity.Set(-move.x * speed * slopeNormalPerpendicular.x * Time.fixedDeltaTime, -move.x * speed * slopeNormalPerpendicular.y * Time.fixedDeltaTime);
            // rb.velocity = newVelocity;
            Debug.Log("22");
        }
    }

    void Jump()
    {
        if (jumps == 0)
        {
            return;
        }

        if (jumps > 0)
        {
            //xue qing's version
            isJump = true;
            canJump = true;
            jumpFX.SetActive(true);
            jumpFX.transform.position = transform.position + new Vector3(0, -0.45f, 0);
            Vector2 v2Velocity = rb.velocity;
            rb.velocity = new Vector2(move.x, 0); // stops player from falling down, allowing for a verticle jump when falling
            rb.velocity = new Vector2(move.x + v2Velocity.x, jumpForce * 100 * Time.fixedDeltaTime);
            rb.gravityScale = playerGravityScale;
            jumps--;
        }
    }
    
    public void FallFXFinish() //animation event
    {
        fallFX.SetActive(true);
        fallFX.transform.position = transform.position + new Vector3(0, -0.75f, 0);
    }

    private void PhysicsCheck()
    {
        var position = groundCheck.position;
        // var position1 = platformCheck.transform.position;
        isGround = Physics2D.OverlapCircle(position, checkRadius, groundLayer)
                   || Physics2D.OverlapCircle(position, checkRadius, slopeLayer);
        // isPlatform = Physics2D.OverlapCircle(position1, 0.02f, platformLayer);
        if (isGround)
        {
            rb.gravityScale = 1;
            isJump = false;
            // jumpTwice = false;
        }
        else if (!isJump && !isGround) //上平台之后修复重力变成1
        {
            rb.gravityScale = playerGravityScale;
        }

        if (rb.velocity.y <= 0.0f)
        {
            canJump = false;
        }

        // else if (isOnSlope && !isGround)
        // {
        //     rb.gravityScale = 100;
        // }
        // if (hit.collider.CompareTag("Wall"))
        // {
        //     Debug.DrawRay(transform.position,grabDir);
        // }
    }

    #region Slope Logic

    private void SlopeCheck()
    {
        Vector2 checkPos = transform.position - new Vector3(0, colliderSize.y / 2);
        SlopeCheckVertical(checkPos);
        SlopeCheckHorizontal(checkPos);
    }

    private void SlopeCheckHorizontal(Vector2 checkPos)
    {
        RaycastHit2D slopeHitFront = Physics2D.Raycast(checkPos, transform.right, slopeCheckDistance, slopeLayer);
        RaycastHit2D slopeHitBack = Physics2D.Raycast(checkPos, -transform.right, slopeCheckDistance, slopeLayer);
        if (slopeHitFront)
        {
            isOnSlope = true;
            slopeSideAngle = Vector2.Angle(slopeHitFront.normal, Vector2.up);
        }
        else if (slopeHitBack)
        {
            isOnSlope = true;
            slopeSideAngle = Vector2.Angle(slopeHitBack.normal, Vector2.up);
        }
        else
        {
            slopeSideAngle = 0f;
            isOnSlope = false;
        }
    }

    private void SlopeCheckVertical(Vector2 checkPos)
    {
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, slopeCheckDistance, slopeLayer);
        if (hit)
        {
            slopeNormalPerpendicular = Vector2.Perpendicular(hit.normal).normalized;

            slopeDownAngle = Vector2.Angle(hit.normal, Vector2.up); //angle between x-axis and the slope y-axis

            if (slopeDownAngle != slopeDownAngleOld)
            {
                isOnSlope = true;
            }

            slopeDownAngleOld = slopeDownAngle;

            Debug.DrawRay(hit.point, slopeNormalPerpendicular, Color.red);
            Debug.DrawRay(hit.point, hit.normal, Color.blue);
        }

        if (isOnSlope && move.x == 0f)
        {
            rb.sharedMaterial = fullFriction;
        }
        else
        {
            rb.sharedMaterial = noFriction;
        }
    }

    #endregion

    #region Sid's logic

    private void Boost() //Changed boost to be a int rather than bool. We can maybe make a level with multiple boosts
    {
        if (boost > 0)
        {
            Vector2 v2Velocity01 = rb.velocity;
            if (move.x < 0) //move.x is positive when moving right, move.x is negative when moving left
            {
                rb.velocity = Vector2.left * boostForce + v2Velocity01;
            }
            else
            {
                rb.velocity = Vector2.right * boostForce + v2Velocity01;
            }

            boost--;
        }
    }

    void OnCollisionEnter2D(Collision2D collider)
    {
        if (collider.gameObject.tag == "Ground" || collider.gameObject.tag == "Slope" ||
            collider.gameObject.tag == "SpeedZone")
        {
            jumps = maxJumps;
            boost = maxBoost;
        }

        // if (collider.gameObject.tag == "Ground")
        // {
        //     speed = 12;
        // }
        //
        // if (collider.gameObject.tag == "Slope")
        // {
        //     speed = slopeSpeed;
        // }
        //
        // if (collider.gameObject.CompareTag("SpeedZone"))
        // {
        //     speed = speedZoneSpeed;
        // }
    }

    #endregion

    // private void OnCollisionExit2D(Collision2D other)
    // {
    //     if (other.gameObject.CompareTag("Ground"))
    //     {
    //         speed = 8;
    //     }
    // }
}