using UnityEngine;

namespace BladeMode.Animation
{
    public static class TwoBoneIkSolver
    {
        /// <summary>
        /// Solves a two-bone chain (root → mid → tip) to reach targetWS.
        /// Uses delta-rotation (FromToRotation) so it works regardless of each bone's
        /// local axis orientation — no assumption about which axis the bone "points along".
        /// poleWS biases the mid-joint (elbow) direction.
        /// weight blends the IK solve on top of the bone's current animation rotation.
        /// </summary>
        public static void Solve(Transform root, Transform mid, Transform tip,
                                 Vector3 targetWS, Vector3 poleWS, float weight = 1f)
        {
            if (weight <= 0f || root == null || mid == null || tip == null) return;

            float a = Vector3.Distance(root.position, mid.position);  // upper bone length
            float b = Vector3.Distance(mid.position,  tip.position);  // lower bone length
            // Clamp reach so law-of-cosines stays valid
            float c = Mathf.Clamp(Vector3.Distance(root.position, targetWS),
                                  Mathf.Abs(a - b) + 0.0001f,
                                  a + b - 0.0001f);

            // Law of cosines: angle at root between (root→mid) and (root→target)
            float cosA  = (a * a + c * c - b * b) / (2f * a * c);
            float angleA = Mathf.Acos(Mathf.Clamp(cosA, -1f, 1f));

            Vector3 toTarget = (targetWS - root.position).normalized;
            Vector3 toPole   = (poleWS   - root.position).normalized;

            // Bend axis: cross of toTarget and toPole defines the elbow-bend plane
            Vector3 bendAxis = Vector3.Cross(toTarget, toPole);
            if (bendAxis.sqrMagnitude < 0.0001f)
                bendAxis = Vector3.Cross(toTarget, Vector3.up);
            if (bendAxis.sqrMagnitude < 0.0001f)
                bendAxis = Vector3.Cross(toTarget, Vector3.right);
            bendAxis = bendAxis.normalized;

            // Direction we want (root → mid) to point: rotate toTarget by angleA around bendAxis
            Vector3 desiredMidDir = Quaternion.AngleAxis(angleA * Mathf.Rad2Deg, bendAxis) * toTarget;

            // Root: rotate by the delta from current (root→mid) to desiredMidDir
            Vector3    currentMidDir = (mid.position - root.position).normalized;
            Quaternion rootDelta     = Quaternion.FromToRotation(currentMidDir, desiredMidDir);
            root.rotation = Quaternion.Slerp(root.rotation, rootDelta * root.rotation, weight);

            // After root rotation Unity immediately updates child world positions.
            // Rotate mid: delta from current (mid→tip) direction to (mid→target)
            Vector3    currentTipDir = (tip.position - mid.position).normalized;
            Vector3    desiredTipDir = (targetWS - mid.position).normalized;
            Quaternion midDelta      = Quaternion.FromToRotation(currentTipDir, desiredTipDir);
            mid.rotation = Quaternion.Slerp(mid.rotation, midDelta * mid.rotation, weight);
        }
    }
}
