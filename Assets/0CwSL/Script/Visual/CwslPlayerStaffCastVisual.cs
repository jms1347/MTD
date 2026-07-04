using System.Collections;
using UnityEngine;

public class CwslPlayerStaffCastVisual : MonoBehaviour
{
    private Transform castArmPivot;
    private Transform staffPivot;
    private Transform torsoPivot;
    private Vector3 armBaseRotation;
    private Vector3 staffBaseRotation;
    private Vector3 torsoBaseRotation;
    private Vector3 visualBaseLocalPosition;
    private Coroutine routine;

    private void Awake()
    {
        castArmPivot = transform.Find("CastArmPivot");
        staffPivot = castArmPivot != null
            ? castArmPivot.Find("StaffPivot")
            : transform.Find("StaffPivot");
        torsoPivot = transform.Find("TorsoPivot");

        if (castArmPivot != null)
            armBaseRotation = castArmPivot.localEulerAngles;
        if (staffPivot != null)
            staffBaseRotation = staffPivot.localEulerAngles;
        if (torsoPivot != null)
            torsoBaseRotation = torsoPivot.localEulerAngles;

        visualBaseLocalPosition = transform.localPosition;
    }

    public void PlayCast()
    {
        if (staffPivot == null && castArmPivot == null)
            return;

        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(CastRoutine());
    }

    private IEnumerator CastRoutine()
    {
        const float windupDuration = 0.16f;
        const float swingDuration = 0.28f;
        const float recoverDuration = 0.14f;
        var timer = 0f;

        while (timer < windupDuration)
        {
            timer += Time.deltaTime;
            var t = timer / windupDuration;
            ApplyPose(-18f * t, -28f * t, 12f * t, 0.02f * t);
            yield return null;
        }

        timer = 0f;
        while (timer < swingDuration)
        {
            timer += Time.deltaTime;
            var t = timer / swingDuration;
            var swing = Mathf.Sin(t * Mathf.PI);
            ApplyPose(-18f + 54f * swing, -28f + 96f * swing, 12f - 24f * swing, 0.02f + 0.05f * swing);
            yield return null;
        }

        timer = 0f;
        while (timer < recoverDuration)
        {
            timer += Time.deltaTime;
            var t = timer / recoverDuration;
            ApplyPose(Mathf.Lerp(36f, 0f, t), Mathf.Lerp(68f, 0f, t), Mathf.Lerp(-12f, 0f, t), Mathf.Lerp(0.07f, 0f, t));
            yield return null;
        }

        ResetPose();
        routine = null;
    }

    private void ApplyPose(float armPitch, float staffPitch, float torsoPitch, float bodyLift)
    {
        if (castArmPivot != null)
        {
            castArmPivot.localRotation = Quaternion.Euler(
                armBaseRotation.x + armPitch,
                armBaseRotation.y,
                armBaseRotation.z - armPitch * 0.35f);
        }

        if (staffPivot != null)
        {
            staffPivot.localRotation = Quaternion.Euler(
                staffBaseRotation.x + staffPitch,
                staffBaseRotation.y + staffPitch * 0.25f,
                staffBaseRotation.z - staffPitch * 0.45f);
        }

        if (torsoPivot != null)
            torsoPivot.localRotation = Quaternion.Euler(torsoBaseRotation.x + torsoPitch, torsoBaseRotation.y, torsoBaseRotation.z);

        transform.localPosition = visualBaseLocalPosition + Vector3.up * bodyLift;
    }

    private void ResetPose()
    {
        if (castArmPivot != null)
            castArmPivot.localRotation = Quaternion.Euler(armBaseRotation);
        if (staffPivot != null)
            staffPivot.localRotation = Quaternion.Euler(staffBaseRotation);
        if (torsoPivot != null)
            torsoPivot.localRotation = Quaternion.Euler(torsoBaseRotation);
        transform.localPosition = visualBaseLocalPosition;
    }
}
