using UnityEngine;

namespace BladeMode.Slicing
{
    public sealed class SliceResult
    {
        public bool       Success           { get; private set; }
        public string     FailureReason     { get; private set; }
        public GameObject UpperHull         { get; private set; }
        public GameObject LowerHull         { get; private set; }
        public Vector3[]  IntersectionPoints { get; private set; }

        public static SliceResult Failure(string reason) =>
            new SliceResult { Success = false, FailureReason = reason };

        public static SliceResult Ok(GameObject upper, GameObject lower, Vector3[] pts = null) =>
            new SliceResult { Success = true, UpperHull = upper, LowerHull = lower, IntersectionPoints = pts };
    }
}
