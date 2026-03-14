using System;
using UnityEngine;

public class CamelCharacterController : MonoBehaviour
{
    private static readonly int walk = Animator.StringToHash("Walk");
    private static readonly int idle = Animator.StringToHash("Idle");
    private static readonly int jump = Animator.StringToHash("Jump");
    public float MoveForce = 100;
    public float JumpForce = 100;
    public Transform _forcePosition;
    public Rigidbody2D _rigidbody;
    private CamelInput _input;


    public Animator CamelAnimator;
    
    private void Awake()
    {
        _input = new CamelInput();
    }

    private void OnEnable()  => _input.Enable();
    private void OnDisable() => _input.Disable();
    
    private void Update()
    {
      

        // Jump - check if held this frame
        bool jumpHeld = _input.Player.Jump.IsPressed();

        // Jump - triggered once on press
        bool jumpPressed = _input.Player.Jump.WasPressedThisFrame();
        
        
    }

    public void FixedUpdate()
    {
        // Movement (assuming it's a Vector2 action)
        Vector2 move = _input.Player.Move.ReadValue<Vector2>();

        if (move.x > 0)
        {
            _rigidbody.AddForceAtPosition(transform.right * MoveForce * Time.fixedDeltaTime, _forcePosition.position, ForceMode2D.Impulse);
            
            CamelAnimator.ResetTrigger(idle);
            CamelAnimator.ResetTrigger(jump);
            CamelAnimator.SetTrigger(walk);
        }
        else
        {
            CamelAnimator.ResetTrigger(jump);
            CamelAnimator.ResetTrigger(walk);
            CamelAnimator.SetTrigger(idle);
        }
        
        bool jumpPressed = _input.Player.Jump.WasPressedThisFrame();
        if (jumpPressed)
        {
            CamelAnimator.ResetTrigger(idle);
            CamelAnimator.ResetTrigger(walk);
            CamelAnimator.SetTrigger(jump);
            _rigidbody.AddForceAtPosition(transform.forward * MoveForce * Time.fixedDeltaTime, _forcePosition.position, ForceMode2D.Impulse);
        }
    }
}
