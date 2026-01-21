using System;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;

    [Header("Input Config")]
    public PlayerInputConfig playerInput;

    [Header("Animation")]
    public Animator animator;
    public string idleTrigger = "Idle";
    public string walkTrigger = "Walk";

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundRadius = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody2D _rb;
    private bool _isGrounded;
    private float _moveInput;
    private bool _jumpQueued;
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private StateMachine<PlayerState> _stateMachine;
    private IdleState _idleState;
    private WalkState _walkState;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _stateMachine = new StateMachine<PlayerState>();
        _idleState = new IdleState(this);
        _walkState = new WalkState(this);
        _idleState.Awake();
        _walkState.Awake();
        ChangeState(_idleState);
    }

    private void OnEnable()
    {
        if (playerInput == null || playerInput.inputActions == null)
        {
            return;
        }

        InputActionMap map = playerInput.inputActions.FindActionMap(playerInput.actionMapName, true);
        _moveAction = map.FindAction(playerInput.moveActionName, true);
        _jumpAction = map.FindAction(playerInput.jumpActionName, true);

        _moveAction.Enable();
        _jumpAction.Enable();
        _jumpAction.performed += OnJump;
    }

    private void OnDisable()
    {
        if (_stateMachine != null && _stateMachine.CurrentState != null)
        {
            _stateMachine.CurrentState.Exit();
        }

        if (_jumpAction != null)
        {
            _jumpAction.performed -= OnJump;
            _jumpAction.Disable();
        }

        if (_moveAction != null)
        {
            _moveAction.Disable();
        }
    }

    private void Update()
    {
        if (_moveAction != null)
        {
            _moveInput = _moveAction.ReadValue<Vector2>().x;
        }
        else
        {
            _moveInput = 0f;
        }

        if (_moveInput != 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Sign(_moveInput) * Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        if (_stateMachine != null && _stateMachine.CurrentState != null)
        {
            _stateMachine.CurrentState.Update();
        }
    }

    private void FixedUpdate()
    {
        _isGrounded = groundCheck != null &&
                      Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

        if (_jumpQueued && _isGrounded)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
            _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        _jumpQueued = false;

        Vector2 velocity = _rb.linearVelocity;
        velocity.x = _moveInput * moveSpeed;
        if (_isGrounded)
        {
            _rb.linearVelocity = velocity;
        }
        

        if (_stateMachine != null && _stateMachine.CurrentState != null)
        {
            _stateMachine.CurrentState.FixedUpdate();
        }
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        _jumpQueued = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }

    public float MoveInput => _moveInput;

    private void ChangeState(PlayerState nextState)
    {
        if (_stateMachine == null)
        {
            return;
        }

        _stateMachine.ChangeState(nextState);
    }

    private void SetAnimatorTrigger(string triggerToSet, string triggerToReset)
    {
        Debug.Log(animator.name);
        if (animator == null || string.IsNullOrEmpty(triggerToSet))
        {
            return;
        }

        if (!string.IsNullOrEmpty(triggerToReset))
        {
            animator.ResetTrigger(triggerToReset);
        }

        animator.SetTrigger(triggerToSet);
    }

    private sealed class IdleState : PlayerState
    {
        public IdleState(PlayerController controller) : base(controller)
        {
        }

        public override void Enter()
        {
            Controller.SetAnimatorTrigger(Controller.idleTrigger, Controller.walkTrigger);
        }

        public override void Update()
        {
            if (Mathf.Abs(Controller.MoveInput) > 0.01f)
            {
                Controller.ChangeState(Controller._walkState);
            }
        }
    }

    private sealed class WalkState : PlayerState
    {
        public WalkState(PlayerController controller) : base(controller)
        {
        }

        public override void Enter()
        {
            Controller.SetAnimatorTrigger(Controller.walkTrigger, Controller.idleTrigger);
        }

        public override void Update()
        {
            if (Mathf.Abs(Controller.MoveInput) <= 0.01f)
            {
                Controller.ChangeState(Controller._idleState);
            }
        }
    }
}
