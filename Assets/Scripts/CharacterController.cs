using System;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Config")]
    public CharacterConfig config;

    [Header("Input Config")]
    public PlayerInputConfig playerInput;

    [Header("Animation")]
    public Animator animator;
    public string idleTrigger = "Idle";
    public string walkTrigger = "Walk";

    [Header("Ground Check")]
    public Collider groundCheck;
    public float groundRayDistance = 1f;
    public LayerMask groundRayLayer;
    public float groundRotateSpeed = 360f;
    public float jumpAngleLimit = 30f;

    private Rigidbody _rb;
    private bool _isGrounded;
    private int _groundContacts;
    private float _moveInput;
    private float _speedMultiplier = 1f;
    private bool _jumpQueued;
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private StateMachine<PlayerState> _stateMachine;
    private IdleState _idleState;
    private WalkState _walkState;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
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
        _isGrounded = _groundContacts > 0;

        float targetAngle = 0f;
        if (TryGetGroundNormal(out Vector3 groundNormal))
        {
            targetAngle = -Mathf.Atan2(groundNormal.x, groundNormal.y) * Mathf.Rad2Deg;
        }

        Vector3 rotation = transform.eulerAngles;
        rotation.z = targetAngle;
        Quaternion targetRotation = Quaternion.Euler(rotation);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            groundRotateSpeed * Time.fixedDeltaTime);

        float currentZ = Mathf.DeltaAngle(0f, transform.eulerAngles.z);
        bool canJump = Mathf.Abs(currentZ) <= jumpAngleLimit;
        if (_jumpQueued && _isGrounded && canJump)
        {
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            _rb.AddForce(Vector3.up * config.jumpForce, ForceMode.Impulse);
        }

        _jumpQueued = false;

        Vector3 velocity = _rb.linearVelocity;
        velocity.x = _moveInput * config.moveSpeed * _speedMultiplier;
        if (!_isGrounded)
        {
            if (velocity.x != 0)
            {
                _rb.linearVelocity = new Vector3(velocity.x, _rb.linearVelocity.y, velocity.z);
            }
        } else
        {
            _rb.linearVelocity = velocity;
        }
        

        if (_stateMachine != null && _stateMachine.CurrentState != null)
        {
            _stateMachine.CurrentState.FixedUpdate();
        }
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = Mathf.Max(0f, multiplier);
    }

    public void ResetSpeedMultiplier()
    {
        _speedMultiplier = 1f;
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
        Gizmos.DrawWireCube(groundCheck.bounds.center, groundCheck.bounds.size);
    }

    private bool TryGetGroundNormal(out Vector3 normal)
    {
        normal = Vector3.up;

        RaycastHit[] hits = Physics.RaycastAll(
            transform.position,
            Vector3.down,
            groundRayDistance,
            groundRayLayer,
            QueryTriggerInteraction.Ignore);

        if (hits.Length == 0)
        {
            return false;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Collider hitCollider = hit.collider;
            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider.attachedRigidbody == _rb || hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            normal = hit.normal;
            return true;
        }

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (groundCheck == null || other == null)
        {
            return;
        }

        if (other.attachedRigidbody == _rb || other.transform.IsChildOf(transform))
        {
            return;
        }

        _groundContacts++;
    }

    private void OnTriggerExit(Collider other)
    {
        if (groundCheck == null || other == null)
        {
            return;
        }

        if (other.attachedRigidbody == _rb || other.transform.IsChildOf(transform))
        {
            return;
        }

        _groundContacts = Mathf.Max(0, _groundContacts - 1);
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
