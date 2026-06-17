using BladeMode.BodyParts;
using EzySlice;
using UnityEngine;

namespace BladeMode.Slicing
{
    /// <summary>
    /// Routes cut requests to the correct path:
    ///   • SliceableBody (SkinnedMeshRenderer character) → SkinnedMeshPlaneSplitter
    ///   • SliceableSurface (MeshFilter prop)            → EzySlice
    /// Callers only deal with SliceRequest / SliceResult — EzySlice is never exposed.
    ///
    /// Survival contract:
    ///   Body  — body.BodyRenderer (SMR) is updated in-place with the upper-half mesh.
    ///           The character root, Animator, AI, targeting, and all other scripts are
    ///           completely untouched. lowerGO (physics debris) → [SeveredPieces].
    ///           TODO: The character's original Collider (usually a CapsuleCollider on the root)
    ///           is NOT updated after slicing — it keeps the pre-slice shape. If accurate
    ///           collision on the truncated body is needed, replace it with a MeshCollider
    ///           here using the same approach as SliceProp does for SliceableSurface.
    ///   Props — BOTH hulls become independent Rigidbody GameObjects in [SeveredPieces].
    ///           The original GO is destroyed after spawning both hulls. Scripts on the
    ///           original GO implementing ISliceEventListener receive OnBeforeSliced()
    ///           before destruction — use this to clear targeting references, stop audio, etc.
    ///           BoxCollider (mesh bounds) is used instead of convex MeshCollider to avoid
    ///           PhysX Quickhull failures on EzySlice-generated meshes.
    /// </summary>
    public sealed class SlicerService
    {
        static SlicePhysicsSettings _fallbackSettings;
        static SlicePhysicsSettings FallbackSettings =>
            _fallbackSettings ??= ScriptableObject.CreateInstance<SlicePhysicsSettings>();

        // Lazy scene-level container for all physics debris — keeps the hierarchy clean.
        static Transform _severedContainer;
        static Transform SeveredContainer
        {
            get
            {
                if (_severedContainer != null) return _severedContainer;
                var existing = GameObject.Find("[SeveredPieces]");
                _severedContainer = existing != null
                    ? existing.transform
                    : new GameObject("[SeveredPieces]").transform;
                return _severedContainer;
            }
        }

        // ── Public ────────────────────────────────────────────────────────────────

        public SliceResult Slice(SliceRequest req)
        {
            if (req.Target == null)
                return SliceResult.Failure("Target is null");

            var body = req.Target.GetComponentInParent<SliceableBody>();
            if (body != null && body.CanBeSliced)
                return SliceBody(req, body);

            var surface = req.Target.GetComponentInParent<SliceableSurface>();
            if (surface != null && surface.CanBeSliced)
                return SliceProp(req, surface);

            return SliceResult.Failure("Target has no SliceableBody or SliceableSurface");
        }

        // Returns true when the axis-aligned world bounds of the target straddle the plane.
        public bool WouldIntersect(GameObject target, Vector3 planePoint, Vector3 planeNormal)
        {
            var renderer = target.GetComponentInChildren<Renderer>();
            if (renderer == null) return false;

            Bounds b = renderer.bounds;
            float center = Vector3.Dot(planeNormal, b.center - planePoint);
            float extent = Mathf.Abs(Vector3.Dot(planeNormal, b.extents));
            return center - extent <= 0f && center + extent >= 0f;
        }

        // ── Character path ────────────────────────────────────────────────────────

        SliceResult SliceBody(SliceRequest req, SliceableBody body)
        {
            var cap = body.CrossSectionMaterial ?? req.CrossSectionMaterial;
            if (cap == null) return SliceResult.Failure("No cross-section material assigned");

            int  layer        = body.gameObject.layer;
            var  originalPos  = body.BodyRenderer.transform.position;
            var  originalRot  = body.BodyRenderer.transform.rotation;
            var  originalMats = body.BodyRenderer.sharedMaterials;

            var (upperSMR, lowerMesh, pts) =
                SkinnedMeshPlaneSplitter.SplitAtPlane(body.BodyRenderer,
                                                       req.PlanePoint, req.PlaneNormal, cap);
            if (upperSMR == null)
                return SliceResult.Failure("SkinnedMeshPlaneSplitter returned no upper hull");

            var physics = req.PhysicsSettings ?? FallbackSettings;

            // ── Lower (severed) piece ─────────────────────────────────────────────
            // Parented to the scene-level [SeveredPieces] container so it is NOT
            // a child of the character root (which may continue moving/animating).
            var lowerGO = new GameObject("SeveredPiece");
            lowerGO.transform.SetParent(SeveredContainer, worldPositionStays: false);
            lowerGO.transform.SetPositionAndRotation(originalPos, originalRot);
            lowerGO.layer = layer;

            var mf = lowerGO.AddComponent<MeshFilter>();   mf.sharedMesh = lowerMesh;
            var mr = lowerGO.AddComponent<MeshRenderer>(); mr.sharedMaterials = AppendCap(originalMats, cap);
            lowerGO.AddComponent<MeshCollider>().convex = true;
            var rb = lowerGO.AddComponent<Rigidbody>();
            rb.mass = physics.SeveredPieceMass;
            ApplySeverImpulse(rb, lowerGO.transform.position,
                              req.PlanePoint, req.PlaneNormal, physics, isLower: true);
            // Stamp SliceableSurface so severed limbs can be cut again as props.
            lowerGO.AddComponent<SliceableSurface>().SetCrossSectionMaterial(cap);

            // ── Upper (surviving) body ────────────────────────────────────────────
            // SplitAtPlane updates source (body.BodyRenderer) in-place: same GO, same
            // SMR component, just a new sharedMesh and one extra cap material. The
            // character root, Animator, targeting, AI, and every other script on the
            // hierarchy are completely unaffected.
            body.BodyRenderer = upperSMR; // upperSMR == body.BodyRenderer (in-place result)

            body.GetComponentInParent<BodyPartRegistry>()
                ?.NotifyCut(req.PlanePoint, req.PlaneNormal);

            StartCreep(upperSMR.gameObject, lowerGO, req.PlanePoint, req.PlaneNormal);
            return SliceResult.Ok(upperSMR.gameObject, lowerGO, pts);
        }

        // ── Prop path ─────────────────────────────────────────────────────────────

        SliceResult SliceProp(SliceRequest req, SliceableSurface surface)
        {
            var cap = surface.CrossSectionMaterial ?? req.CrossSectionMaterial;

            // EzySlice (Slicer.cs:103) returns null when MeshRenderer.sharedMaterials.Length !=
            // MeshFilter.sharedMesh.subMeshCount. Pad or trim the array to match before slicing.
            {
                var mf  = surface.GetComponent<MeshFilter>();
                var mr  = surface.GetComponent<MeshRenderer>();
                if (mf?.sharedMesh != null && mr != null)
                {
                    int n   = mf.sharedMesh.subMeshCount;
                    var cur = mr.sharedMaterials;
                    if (cur.Length != n)
                    {
                        var fixedMats = new Material[n];
                        for (int i = 0; i < n; i++)
                            fixedMats[i] = i < cur.Length ? cur[i]
                                         : (cur.Length > 0 ? cur[cur.Length - 1] : cap);
                        mr.sharedMaterials = fixedMats;
                    }
                }
            }

            var hull = surface.gameObject.Slice(req.PlanePoint, req.PlaneNormal, cap);
            if (hull == null)
                return SliceResult.Failure("EzySlice found no intersection on prop mesh");

            var physics   = req.PhysicsSettings ?? FallbackSettings;
            int layer     = surface.gameObject.layer;
            var origMR    = surface.GetComponent<MeshRenderer>();
            var origMats  = origMR != null ? origMR.sharedMaterials : System.Array.Empty<Material>();
            var capsOn    = AppendCap(origMats, cap);
            var origPos   = surface.transform.position;
            var origRot   = surface.transform.rotation;
            var origScale = surface.transform.lossyScale;

            // ── Upper hull → fresh Rigidbody GO ──────────────────────────────────
            GameObject upperGO = null;
            if (hull.upperHull != null && hull.upperHull.vertexCount > 0)
            {
                upperGO = new GameObject(surface.gameObject.name + "_Upper");
                upperGO.layer = layer;
                upperGO.transform.SetPositionAndRotation(origPos, origRot);
                upperGO.transform.localScale = origScale;
                upperGO.transform.SetParent(SeveredContainer, worldPositionStays: true);

                upperGO.AddComponent<MeshFilter>().sharedMesh       = hull.upperHull;
                upperGO.AddComponent<MeshRenderer>().sharedMaterials = capsOn;

                // BoxCollider from mesh bounds avoids PhysX Quickhull failures on
                // EzySlice-generated meshes, which can be topologically degenerate.
                var upperBC = upperGO.AddComponent<BoxCollider>();
                upperBC.center = hull.upperHull.bounds.center;
                upperBC.size   = hull.upperHull.bounds.size;

                var upperRb = upperGO.AddComponent<Rigidbody>();
                upperRb.mass = physics.SeveredPieceMass;
                ApplySeverImpulse(upperRb,
                    upperGO.transform.TransformPoint(hull.upperHull.bounds.center),
                    req.PlanePoint, req.PlaneNormal, physics, isLower: false);

                upperGO.AddComponent<SliceableSurface>().SetCrossSectionMaterial(cap);
            }

            // ── Lower hull → fresh Rigidbody GO ──────────────────────────────────
            GameObject lowerGO = null;
            if (hull.lowerHull != null && hull.lowerHull.vertexCount > 0)
            {
                lowerGO = new GameObject(surface.gameObject.name + "_Lower");
                lowerGO.layer = layer;
                lowerGO.transform.SetPositionAndRotation(origPos, origRot);
                lowerGO.transform.localScale = origScale;
                lowerGO.transform.SetParent(SeveredContainer, worldPositionStays: true);

                lowerGO.AddComponent<MeshFilter>().sharedMesh       = hull.lowerHull;
                lowerGO.AddComponent<MeshRenderer>().sharedMaterials = capsOn;

                var lowerBC = lowerGO.AddComponent<BoxCollider>();
                lowerBC.center = hull.lowerHull.bounds.center;
                lowerBC.size   = hull.lowerHull.bounds.size;

                var lowerRb = lowerGO.AddComponent<Rigidbody>();
                lowerRb.mass = physics.SeveredPieceMass;
                ApplySeverImpulse(lowerRb,
                    lowerGO.transform.TransformPoint(hull.lowerHull.bounds.center),
                    req.PlanePoint, req.PlaneNormal, physics, isLower: true);

                lowerGO.AddComponent<SliceableSurface>().SetCrossSectionMaterial(cap);
            }

            // ── Cleanup original GO ───────────────────────────────────────────────
            // ISliceEventListener callbacks fire before destruction so scripts can clear
            // targeting references, deregister events, stop coroutines, etc.
            foreach (var listener in surface.GetComponents<ISliceEventListener>())
                listener.OnBeforeSliced();
            Object.Destroy(surface.gameObject);

            StartCreep(upperGO, lowerGO, req.PlanePoint, req.PlaneNormal);
            return SliceResult.Ok(upperGO, lowerGO);
        }

        // ── Shader creep ──────────────────────────────────────────────────────────

        static void StartCreep(GameObject upper, GameObject lower,
                               Vector3 planePoint, Vector3 planeNormal)
        {
            AttachCreep(upper, planePoint, planeNormal);
            AttachCreep(lower, planePoint, planeNormal);
        }

        // GetOrAdd — avoids the NullRef that occurs when AddComponent returns null because
        // [DisallowMultipleComponent] is on ShaderCreepAnimator and the GO was already sliced.
        // Re-initialising an existing animator is intentional: a second cut restarts the creep.
        static void AttachCreep(GameObject go, Vector3 planePoint, Vector3 planeNormal)
        {
            if (go == null) return;
            var creep = go.GetComponent<ShaderCreepAnimator>()
                        ?? go.AddComponent<ShaderCreepAnimator>();
            creep?.Init(planePoint, planeNormal);
        }

        // ── Impulse ───────────────────────────────────────────────────────────────

        static void ApplySeverImpulse(Rigidbody rb, Vector3 pieceCenter,
            Vector3 planePoint, Vector3 planeNormal, SlicePhysicsSettings s, bool isLower)
        {
            float force = isLower ? s.LaunchForce : s.LaunchForce * s.UpperHullForceRatio;
            if (force <= 0f) return;

            Vector3 awayFromCut = pieceCenter - planePoint;
            Vector3 baseDir = awayFromCut.sqrMagnitude > 0.001f
                ? Vector3.Lerp(awayFromCut.normalized, planeNormal.normalized, s.NormalBlend)
                : planeNormal.normalized;

            Vector3 launchDir = (baseDir + Vector3.up * s.UpwardBias).normalized;
            rb.linearVelocity  = launchDir * force;
            rb.angularVelocity = Random.insideUnitSphere * s.AngularSpinMultiplier * force;
        }

        // ── Utilities ─────────────────────────────────────────────────────────────

        static Material[] AppendCap(Material[] mats, Material cap)
        {
            var result = new Material[mats.Length + 1];
            mats.CopyTo(result, 0);
            result[result.Length - 1] = cap;
            return result;
        }
    }
}
