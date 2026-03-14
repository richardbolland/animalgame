using System.Collections.Generic;
using UnityEngine;

public class DuelRigCharacter : MonoBehaviour
{
    public class Joint
    {
        public Transform JointTransform;
    }
    
    public enum ViewMode
    {
        Mix,
        Physics,
        Animated
    }

    private ViewMode _previousShowVisuals;
    public ViewMode ShowVisuals = ViewMode.Mix;

    [Range(0f,1f)]
    [Tooltip("0 = Full anim, 1 = Full ragdoll")]
    public float AnimationToPhysicsAmount;
    
    [Space]
    
    public GameObject PhysicsVisualRoot;
    public GameObject AnimatedVisualRoot;
    public GameObject VisualVisualRoot;

    public Transform PhysicsSkeletonRoot;
    public Transform AnimatedSkeletonRoot;
    public Transform VisualSkeletonRoot;
    
    
    private List<SpriteRenderer> _physicsSpriteRenderers;
    private List<SpriteRenderer> _animatedSpriteRenderers;
    private List<SpriteRenderer> _visualSpriteRenderers;
    
    
    private List<Joint> _physicsJoints;
    private List<Joint> _animatedJoints;
    private List<Joint> _visualJoints;

    private List<SpriteRenderer> PhysicsSpriteRenderers
    {
        get
        {
            if (_physicsSpriteRenderers == null || _animatedSpriteRenderers.Count == 0)
            {
                _physicsSpriteRenderers = new List<SpriteRenderer>(PhysicsVisualRoot.GetComponentsInChildren<SpriteRenderer>());
            }

            return _physicsSpriteRenderers;
        }
    }

    private List<SpriteRenderer> AnimatedSpriteRenderers
    {
        get
        {
            if (_animatedSpriteRenderers == null || _animatedSpriteRenderers.Count == 0)
            {
                _animatedSpriteRenderers = new List<SpriteRenderer>(AnimatedVisualRoot.GetComponentsInChildren<SpriteRenderer>());
            }

            return _animatedSpriteRenderers;
        }
    }

    private List<SpriteRenderer> VisualSpriteRenderers
    {
        get
        {
            if (_visualSpriteRenderers == null || _visualSpriteRenderers.Count == 0)
            {
                _visualSpriteRenderers = new List<SpriteRenderer>(VisualVisualRoot.GetComponentsInChildren<SpriteRenderer>());
            }

            return _visualSpriteRenderers;
        }
    }
    
    
    void Start()
    {

        UpdateVisualState();
        
        //Map physics, animated and visual skeletons so we can interpolate at will!
        _physicsJoints = BuildJointsFromRoot(PhysicsSkeletonRoot, true);
        _animatedJoints = BuildJointsFromRoot(AnimatedSkeletonRoot, true);
        _visualJoints = BuildJointsFromRoot(VisualSkeletonRoot, true);
        
        Debug.Assert(_animatedJoints.Count == _visualJoints.Count, "Animated and visual joint count does not match!!");
        Debug.Assert(_physicsJoints.Count == _visualJoints.Count, "Physics and visual joint count does not match!!");
    }

    private void UpdateVisualState()
    {
        //Enable and disable relevant visuals.
        foreach (SpriteRenderer renderer in PhysicsSpriteRenderers)
        {
            bool showPhysics = ShowVisuals == ViewMode.Physics;
            renderer.enabled = showPhysics;
        }
        
        foreach (SpriteRenderer renderer in AnimatedSpriteRenderers)
        {
            bool showAnimated = ShowVisuals == ViewMode.Animated;
            renderer.enabled = showAnimated;
        }
        
        foreach (SpriteRenderer renderer in VisualSpriteRenderers)
        {
            bool showMix = ShowVisuals == ViewMode.Mix;
            renderer.enabled = showMix;
        }
    }


    private List<Joint> BuildJointsFromRoot(Transform root, bool includeRoot)
    {
        List<Joint> joints = new List<Joint>();

        if (includeRoot)
        {
            joints.Add(new Joint(){JointTransform = root});
        }
        foreach (Transform child in root)
        {
            if (child.GetComponent<NotPartOfSkeleton>() != null)
            {
                continue;
            }
            joints.Add(new Joint(){JointTransform = child});
            joints.AddRange(BuildJointsFromRoot(child, false));
        }

        return joints;
    }
    
    void LateUpdate()
    {
        if (ShowVisuals != _previousShowVisuals)
        {
            UpdateVisualState();
            _previousShowVisuals = ShowVisuals;
        }
        
        Debug.Assert(_animatedJoints.Count == _visualJoints.Count, "Animated and visual joint count does not match!!");
        Debug.Assert(_physicsJoints.Count == _visualJoints.Count, "Physics and visual joint count does not match!!");
        
        //Now we do the sync between animated and physics.
        for (int i = 0; i < _visualJoints.Count; i++)
        {
            Joint visualJoint = _visualJoints[i];
            Joint physicsJoint = _physicsJoints[i];
            Joint animatedJoint = _animatedJoints[i];
            
            visualJoint.JointTransform.position = Vector3.Lerp(animatedJoint.JointTransform.position, physicsJoint.JointTransform.position, AnimationToPhysicsAmount);
            visualJoint.JointTransform.rotation = Quaternion.Slerp(animatedJoint.JointTransform.rotation, physicsJoint.JointTransform.rotation, AnimationToPhysicsAmount);
        }
    }
}
