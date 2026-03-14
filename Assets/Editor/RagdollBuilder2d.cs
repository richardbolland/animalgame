// RagdollBuilder2D.cs
// Place this file anywhere inside an Editor/ folder.
//
// Usage:
//   1. Select the ROOT bone Transform in the Hierarchy
//   2. Right-click → 2D Ragdoll → Build Physics Skeleton
//   3. Tweak per-bone HingeJoint2D angle limits in the Inspector
//   4. Ctrl+Z undoes the entire operation in one step
//
// Works with plain Transform hierarchies (Unity SpriteSkin / Sprite Editor rigging).
// No Bone2D or any other 2D Animation package component required.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class RagdollBuilder2D
{
    // ─── Tuning constants ─────────────────────────────────────────────────────────
    const float TipBoneFallbackLength  = 0.1f;   // length used when a bone has no children
    const float CapsuleThicknessRatio  = 0.28f;  // capsule width = boneLength * this
    const float DefaultGravityScale    = 1f;
    const float DefaultLinearDamping   = 1.5f;
    const float DefaultAngularDamping  = 2f;
    const float DefaultAngleLimitMin   = -45f;
    const float DefaultAngleLimitMax   =  45f;
    // ─────────────────────────────────────────────────────────────────────────────

    [MenuItem("GameObject/2D Ragdoll/Build Physics Skeleton", true)]
    static bool Validate() => Selection.activeTransform != null;

    [MenuItem("GameObject/2D Ragdoll/Build Physics Skeleton", false, 10)]
    static void Build()
    {
        var root = Selection.activeTransform;

        // Collect every transform in the hierarchy (breadth-first so parents
        // always come before children — important for the joint-wiring pass).
        var bones = CollectHierarchy(root);

        bool confirmed = EditorUtility.DisplayDialog(
            "Build Physics Skeleton",
            $"Add Rigidbody2D, CapsuleCollider2D, and HingeJoint2D to {bones.Count} transform(s) under '{root.name}'?\n\n" +
            "• Root bone  →  Kinematic Rigidbody2D (acts as anchor)\n" +
            "• All others →  Dynamic + HingeJoint2D to parent\n\n" +
            "This is fully undoable (Ctrl+Z).\n\n" +
            "Run this while the character is in the BIND POSE.",
            "Build", "Cancel");

        if (!confirmed) return;

        Undo.SetCurrentGroupName("Build 2D Physics Skeleton");
        int undoGroup = Undo.GetCurrentGroup();

        // Pass 1 — Rigidbody2D + CapsuleCollider2D on every bone
        foreach (var bone in bones)
            SetupBonePhysics(bone, isRoot: bone == root);

        // Pass 2 — HingeJoint2D (needs all Rigidbody2Ds to exist first)
        int jointsCreated = 0;
        foreach (var bone in bones)
        {
            if (bone == root) continue;
            if (WireHingeJoint(bone))
                jointsCreated++;
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[RagdollBuilder2D] {bones.Count} bones processed, {jointsCreated} HingeJoint2Ds created.");
        EditorUtility.DisplayDialog("Build Complete",
            $"Physics skeleton built!\n\n" +
            $"  {bones.Count} bones processed\n" +
            $"  {jointsCreated} HingeJoint2Ds connected\n\n" +
            "Next steps:\n" +
            "• Tweak angle limits per bone in the Inspector\n" +
            "• Set bone masses (heavier torso, lighter extremities)\n" +
            "• Put bones on their own Physics Layer and call\n" +
            "  Physics2D.IgnoreLayerCollision to stop self-clipping",
            "OK");
    }

    // ── Pass 1 ────────────────────────────────────────────────────────────────────

    static void SetupBonePhysics(Transform bone, bool isRoot)
    {
        var go = bone.gameObject;

        // ── Rigidbody2D ──────────────────────────────────────────────────────────
        var rb = go.GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            rb = Undo.AddComponent<Rigidbody2D>(go);
        }

        if (isRoot)
        {
            // Kinematic: stays driven by your animation/transform.
            // Flip to Dynamic when you want to "activate" the ragdoll at runtime.
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true;
        }
        else
        {
            rb.bodyType       = RigidbodyType2D.Dynamic;
            rb.gravityScale   = DefaultGravityScale;
            rb.linearDamping  = DefaultLinearDamping;
            rb.angularDamping = DefaultAngularDamping;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // ── CapsuleCollider2D ─────────────────────────────────────────────────────
        var col = go.GetComponent<CapsuleCollider2D>();

        if (col == null)
        {
            col = Undo.AddComponent<CapsuleCollider2D>(go);
        }

        float boneLength = MeasureBoneLength(bone);
        float thickness  = boneLength * CapsuleThicknessRatio;

        // Point the capsule from the bone's origin toward its first child.
        // We work in local space so it survives any rotation the bone has.
        Vector2 localDir = LocalDirectionToChild(bone);

        col.direction = Mathf.Abs(localDir.x) > Mathf.Abs(localDir.y)
            ? CapsuleDirection2D.Horizontal
            : CapsuleDirection2D.Vertical;

        col.size   = col.direction == CapsuleDirection2D.Vertical
            ? new Vector2(thickness, boneLength)
            : new Vector2(boneLength, thickness);

        // Offset so the capsule starts at the pivot and extends toward the child
        col.offset = localDir * (boneLength * 0.5f);
    }

    // ── Pass 2 ────────────────────────────────────────────────────────────────────

    static bool WireHingeJoint(Transform bone)
    {
        var parentRb = bone.parent != null
            ? bone.parent.GetComponent<Rigidbody2D>()
            : null;

        if (parentRb == null)
        {
            Debug.LogWarning($"[RagdollBuilder2D] '{bone.name}': parent has no Rigidbody2D — joint skipped.");
            return false;
        }

        var hinge = bone.GetComponent<HingeJoint2D>();

        if (hinge == null)
        {
            hinge = Undo.AddComponent<HingeJoint2D>(bone.gameObject);
        }

        // This bone's pivot in its own local space is always the origin
        hinge.anchor = Vector2.zero;

        // The matching point on the parent is this bone's world position
        // expressed in the parent's local space
        hinge.autoConfigureConnectedAnchor = false;
        hinge.connectedAnchor = bone.parent.InverseTransformPoint(bone.position);
        hinge.connectedBody   = parentRb;

        // Default angle limits — tune per bone after generation
        hinge.useLimits = true;
        hinge.limits    = new JointAngleLimits2D
        {
            min = DefaultAngleLimitMin,
            max = DefaultAngleLimitMax
        };

        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    /// Breadth-first traversal — parents always before children.
    static List<Transform> CollectHierarchy(Transform root)
    {
        var result = new List<Transform>();
        var queue  = new Queue<Transform>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var t = queue.Dequeue();
            result.Add(t);
            foreach (Transform child in t)
                queue.Enqueue(child);
        }
        return result;
    }

    /// World-space distance from this bone to its first child.
    /// Falls back to TipBoneFallbackLength for leaf bones.
    static float MeasureBoneLength(Transform bone)
    {
        if (bone.childCount == 0) return TipBoneFallbackLength;
        return Vector3.Distance(bone.position, bone.GetChild(0).position);
    }

    /// Direction from this bone toward its first child, in LOCAL space.
    /// Returns local +Y for leaf bones (standard bone convention).
    static Vector2 LocalDirectionToChild(Transform bone)
    {
        if (bone.childCount == 0) return Vector2.up;

        Vector3 worldDir = (bone.GetChild(0).position - bone.position).normalized;
        Vector3 localDir = bone.InverseTransformDirection(worldDir);
        return new Vector2(localDir.x, localDir.y).normalized;
    }
}
