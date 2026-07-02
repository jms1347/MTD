using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 슬라임 Walk 애니메이션의 뼈 전진을 막아 실제 이동과 어긋나는 순간이동 현상을 방지합니다.
/// </summary>
public class CoopSlimeRootMotionSuppressor : MonoBehaviour
{
    private struct BonePose
    {
        public Transform transform;
        public Vector3 localPosition;
        public Quaternion localRotation;
    }

    private Transform visualRoot;
    private Vector3 visualDefaultLocalPosition;
    private readonly List<BonePose> suppressedBones = new();
    private bool cached;

    private void LateUpdate()
    {
        if (!cached)
            CacheAnimationRootDefaults();

        SuppressAnimatedRootMotion();
    }

    private void CacheAnimationRootDefaults()
    {
        cached = true;
        suppressedBones.Clear();

        var animator = GetComponentInChildren<Animator>();
        visualRoot = animator != null ? animator.transform : transform;
        visualDefaultLocalPosition = visualRoot.localPosition;

        if (animator != null && animator.isHuman)
            TryAddSuppressedBone(animator.GetBoneTransform(HumanBodyBones.Hips));

        TryAddSuppressedBone(FindChildRecursive(visualRoot, "Bone"));
        TryAddSuppressedBone(FindChildRecursive(visualRoot, "Rig"));
    }

    private void SuppressAnimatedRootMotion()
    {
        for (var i = 0; i < suppressedBones.Count; i++)
        {
            var pose = suppressedBones[i];
            if (pose.transform == null)
                continue;

            pose.transform.localPosition = pose.localPosition;
            pose.transform.localRotation = pose.localRotation;
        }

        if (visualRoot != null)
            visualRoot.localPosition = visualDefaultLocalPosition;
    }

    private void TryAddSuppressedBone(Transform bone)
    {
        if (bone == null)
            return;

        for (var i = 0; i < suppressedBones.Count; i++)
        {
            if (suppressedBones[i].transform == bone)
                return;
        }

        suppressedBones.Add(new BonePose
        {
            transform = bone,
            localPosition = bone.localPosition,
            localRotation = bone.localRotation
        });
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            var nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
