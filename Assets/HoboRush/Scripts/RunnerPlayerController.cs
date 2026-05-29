using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class RunnerPlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private BoxCollider2D bodyCollider;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Sprite[] runFrames;
    [SerializeField] private Sprite[] duckFrames;
    [SerializeField] private Sprite jumpSprite;
    [SerializeField] private Sprite deadSprite;
    [SerializeField] private float jumpVelocity = 12f;
    [SerializeField] private float jumpBufferTime = 0.14f;
    [SerializeField] private float jumpCutMultiplier = 0.52f;
    [SerializeField] private float coyoteTime = 0.08f;
    [SerializeField] private float groundCheckRadius = 0.16f;
    [SerializeField] private float groundProbeDistance = 0.08f;
    [SerializeField] private float frameRate = 13f;
    [SerializeField] private Vector2 standingColliderSize = new Vector2(0.82f, 1.28f);
    [SerializeField] private Vector2 standingColliderOffset = new Vector2(0f, 0f);
    [SerializeField] private Vector2 duckColliderSize = new Vector2(0.98f, 0.74f);
    [SerializeField] private Vector2 duckColliderOffset = new Vector2(0f, -0.27f);
    [SerializeField] private float footstepInterval = 0.18f;

    private float groundedTimer;
    private float jumpBufferTimer;
    private float animationTimer;
    private float footstepTimer;
    private int frameIndex;
    private bool isDucking;
    private bool wasDucking;
    private bool wasGrounded;
    private bool isDead;
    private readonly Collider2D[] groundHits = new Collider2D[8];
    private readonly RaycastHit2D[] groundCasts = new RaycastHit2D[4];

    public bool IsDucking => isDucking;
    public bool IsGrounded => groundedTimer > 0f;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<BoxCollider2D>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        ApplyStandingCollider();
        wasGrounded = true;
    }

    private void Update()
    {
        if (RunnerGameManager.Instance != null && RunnerGameManager.Instance.IsGameOver)
        {
            return;
        }

        UpdateGroundedState();
        HandleInput();
        UpdateSpriteAnimation();
    }

    private void HandleInput()
    {
        bool jumpPressed = IsJumpPressed();
        bool jumpReleased = IsJumpReleased();
        bool duckHeld = IsDuckHeld();

        if (jumpPressed)
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        if (jumpBufferTimer > 0f && IsGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
            groundedTimer = 0f;
            jumpBufferTimer = 0f;
            RunnerAudioManager.Instance?.PlayJump();
        }

        if (jumpReleased && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }

        isDucking = duckHeld && IsGrounded;
        if (isDucking)
        {
            if (!wasDucking)
            {
                RunnerAudioManager.Instance?.PlaySlide();
            }

            ApplyDuckCollider();
        }
        else
        {
            ApplyStandingCollider();
        }

        if (duckHeld && !IsGrounded && rb.linearVelocity.y > -jumpVelocity)
        {
            rb.linearVelocity += Vector2.down * (jumpVelocity * 1.35f * Time.deltaTime);
        }
    }

    private void UpdateGroundedState()
    {
        bool grounded = IsTouchingGround();
        if (!grounded && groundCheck != null)
        {
            int mask = groundLayer.value == 0 ? Physics2D.DefaultRaycastLayers : groundLayer.value;
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = false;
            filter.SetLayerMask(mask);

            int hitCount = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, filter, groundHits);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = groundHits[i];
                if (hit != null && hit != bodyCollider && !hit.isTrigger)
                {
                    grounded = true;
                    break;
                }
            }
        }

        if (grounded)
        {
            groundedTimer = coyoteTime;
        }
        else
        {
            groundedTimer -= Time.deltaTime;
        }

        if (grounded && !wasGrounded && rb != null && rb.linearVelocity.y <= 0.05f)
        {
            RunnerAudioManager.Instance?.PlayLand();
        }

        wasGrounded = grounded;
    }

    private bool IsTouchingGround()
    {
        if (bodyCollider == null)
        {
            return false;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;
        filter.SetLayerMask(groundLayer.value == 0 ? Physics2D.DefaultRaycastLayers : groundLayer.value);

        int castCount = bodyCollider.Cast(Vector2.down, filter, groundCasts, groundProbeDistance);
        for (int i = 0; i < castCount; i++)
        {
            Collider2D hit = groundCasts[i].collider;
            if (hit != null && hit != bodyCollider && !hit.isTrigger)
            {
                return true;
            }
        }

        return bodyCollider.IsTouchingLayers(groundLayer.value == 0 ? Physics2D.DefaultRaycastLayers : groundLayer.value);
    }

    private bool IsJumpPressed()
    {
        bool pressed = false;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            pressed |= keyboard.spaceKey.wasPressedThisFrame ||
                keyboard.wKey.wasPressedThisFrame ||
                keyboard.upArrowKey.wasPressedThisFrame;
        }
        return pressed;
    }

    private bool IsJumpReleased()
    {
        bool released = false;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            released |= keyboard.spaceKey.wasReleasedThisFrame ||
                keyboard.wKey.wasReleasedThisFrame ||
                keyboard.upArrowKey.wasReleasedThisFrame;
        }
        return released;
    }

    private bool IsDuckHeld()
    {
        bool held = false;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            held |= keyboard.sKey.isPressed ||
                keyboard.downArrowKey.isPressed;
        }
        return held;
    }

    private void UpdateSpriteAnimation()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (!IsGrounded)
        {
            if (jumpSprite != null)
            {
                spriteRenderer.sprite = jumpSprite;
            }

            return;
        }

        if (isDucking)
        {
            Sprite duckSprite = GetDuckSprite();
            if (duckSprite != null)
            {
                spriteRenderer.sprite = duckSprite;
            }

            wasDucking = isDucking;
            return;
        }

        Sprite[] frames = runFrames;
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        animationTimer += Time.deltaTime;
        footstepTimer += Time.deltaTime;
        if (animationTimer >= 1f / Mathf.Max(1f, frameRate))
        {
            animationTimer = 0f;
            frameIndex = (frameIndex + 1) % frames.Length;
            spriteRenderer.sprite = frames[frameIndex];
        }

        if (!isDucking && footstepTimer >= footstepInterval)
        {
            footstepTimer = 0f;
            RunnerAudioManager.Instance?.PlayFootstep();
        }

        wasDucking = isDucking;
    }

    private Sprite GetDuckSprite()
    {
        if (duckFrames != null && duckFrames.Length > 0 && duckFrames[0] != null)
        {
            return duckFrames[0];
        }

        if (deadSprite != null)
        {
            return deadSprite;
        }

        if (runFrames != null && runFrames.Length > 0)
        {
            return runFrames[0];
        }

        return null;
    }

    public void SetDead()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (deadSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = deadSprite;
        }
    }

    private void ApplyStandingCollider()
    {
        if (bodyCollider == null || isDead)
        {
            return;
        }

        bodyCollider.size = standingColliderSize;
        bodyCollider.offset = standingColliderOffset;
    }

    private void ApplyDuckCollider()
    {
        if (bodyCollider == null || isDead)
        {
            return;
        }

        bodyCollider.size = duckColliderSize;
        bodyCollider.offset = duckColliderOffset;
    }
}
