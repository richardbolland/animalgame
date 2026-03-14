using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CamelCharacterController : MonoBehaviour
{
    private static readonly int walk = Animator.StringToHash("Walk");
    private static readonly int idle = Animator.StringToHash("Idle");
    private static readonly int jump = Animator.StringToHash("Jump");
    public float MoveForce = 100;
    public float RpmForceMultiplier = 5;
    public float JumpForce = 100;
    public Transform _forcePosition;
    public Rigidbody2D _rigidbody;
    private CamelInput _input;

    public float SampleWindowTime = 2;

    private bool wasRpmMagnetDown = false;


    public Animator CamelAnimator;
    
    
    // RPM
    
    private bool _wasRpmMagnetDown = false;
    private float _lastMagnetTime = 0f;
    private float _currentRpm = 0f;
    
    private readonly Queue<(float,float)> _rpmSamples = new Queue<(float,float)>();

    private const float RPM_TIMEOUT = 2f; // seconds before RPM drops to 0
    private float _timeSinceLastMagnet = 0f;
    
    // --- 

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Killbox"))
        {
            SceneManager.LoadScene(1);
        }
    }

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
        
        
        bool crouch = _input.Player.Crouch.WasPressedThisFrame();

        if (crouch)
        {
            SceneManager.LoadScene(1);
        }
        
        //Calculate RPM
        //Remove old samples
        if (_rpmSamples.Count > 0)
        {
            (float sample, float time) = _rpmSamples.Peek();
            if (Time.time - time > SampleWindowTime)
            {
                _rpmSamples.Dequeue();
            }
        }
        
        bool rpmMagnetDown = _input.Player.Attack.WasPressedThisFrame();
        if (rpmMagnetDown && wasRpmMagnetDown)
        {
            //Detected magnet stuck as always on. This is an exploit so we ignore it.
        }
        else
        {
            if (rpmMagnetDown)
            {
                //Add to moving window RPM
                
                float now = Time.time;
                float period = now - _lastMagnetTime; // seconds per revolution

                if (_lastMagnetTime > 0f && period > 0.05f) // sanity check (< 1200 RPM max)
                {
                    float instantRpm = 60f / period;

                    // Rolling average
                    _rpmSamples.Enqueue((instantRpm, Time.time));
                }

                _lastMagnetTime = now;
                _timeSinceLastMagnet = 0f;
            }
            
            wasRpmMagnetDown = rpmMagnetDown;
        }
        
        //Recalculate current RPM
        _currentRpm = 0f;

        //Should only be valid samples now
        foreach ((float sample, float sampleTime) in _rpmSamples)
        {
            _currentRpm += sample;
        }

        if (_rpmSamples.Count == 0)
        {
            _currentRpm = 0;
        }
        else
        {
            _currentRpm /= _rpmSamples.Count;
        }
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
            _rigidbody.AddForceAtPosition(transform.up * MoveForce * Time.fixedDeltaTime, _forcePosition.position, ForceMode2D.Impulse);
        }
       
        Debug.Log("RPM: " + _currentRpm + "Force " + _currentRpm * RpmForceMultiplier * Time.fixedDeltaTime);

        if (_currentRpm > 0)
        {
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

        Vector3 force = transform.right * _currentRpm * RpmForceMultiplier * Time.fixedDeltaTime;
        _rigidbody.AddForceAtPosition(force, _forcePosition.position, ForceMode2D.Impulse);
    }
}
