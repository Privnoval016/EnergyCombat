using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BladeMode.BodyParts
{
    /// <summary>
    /// Splits a SkinnedMeshRenderer at a world-space plane at cut time (not Awake).
    /// The returned upper SMR shares the original skeleton so it continues animating.
    /// The lower baked Mesh is world-space and ready for Rigidbody physics.
    /// </summary>
    // TODO: After slicing, the character's original Collider (typically a CapsuleCollider on
    // the root) is not updated — it retains the pre-slice shape. If accurate physics collision
    // on the truncated body is needed post-slice, replace it here with a MeshCollider using
    // the baked upper mesh, the same way SlicerService.SliceProp handles SliceableSurface props.
    public static class SkinnedMeshPlaneSplitter
    {
        // ── Internal vertex data ──────────────────────────────────────────────────

        struct VData
        {
            public Vector3 Position;   // bind-pose local
            public Vector3 Normal;
            public Vector4 Tangent;
            public Vector2 UV;
            public BoneWeight BW;
        }

        class Accumulator
        {
            public readonly List<VData> Verts = new();
            readonly List<List<int>> _submeshes = new();
            List<int> _cur;

            public void BeginSubMesh() { _cur = new List<int>(); _submeshes.Add(_cur); }
            public int Add(VData v)    { Verts.Add(v); return Verts.Count - 1; }
            public void Tri(int a, int b, int c) { _cur.Add(a); _cur.Add(b); _cur.Add(c); }

            public Mesh Build()
            {
                int n = Verts.Count;
                var pos = new Vector3[n]; var nor = new Vector3[n];
                var tan = new Vector4[n]; var uv  = new Vector2[n];
                var bw  = new BoneWeight[n];
                for (int i = 0; i < n; i++)
                {
                    pos[i] = Verts[i].Position; nor[i] = Verts[i].Normal;
                    tan[i] = Verts[i].Tangent;  uv[i]  = Verts[i].UV;
                    bw[i]  = Verts[i].BW;
                }
                var m = new Mesh { indexFormat = IndexFormat.UInt32 };
                m.SetVertices(pos); m.SetNormals(nor); m.SetTangents(tan);
                m.SetUVs(0, uv); m.boneWeights = bw;
                m.subMeshCount = _submeshes.Count;
                for (int s = 0; s < _submeshes.Count; s++)
                    m.SetTriangles(_submeshes[s], s);
                m.RecalculateBounds();
                return m;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public static (SkinnedMeshRenderer upperSMR, Mesh lowerBakedMesh, Vector3[] intersectionPoints)
            SplitAtPlane(SkinnedMeshRenderer source, Vector3 planePoint, Vector3 planeNormal, Material capMaterial)
        {
            var mesh        = source.sharedMesh;
            var bones       = source.bones;
            var bindPoses   = mesh.bindposes;
            var bindVerts   = mesh.vertices;
            var bindNormals = mesh.normals;
            var bindTangents = mesh.tangents;
            var uvs         = mesh.uv;
            var bws         = mesh.boneWeights;
            int vc          = bindVerts.Length;

            // World-space positions for plane-side classification only
            var worldPos = new Vector3[vc];
            for (int v = 0; v < vc; v++)
                worldPos[v] = SkinVertex(bindVerts[v], bws[v], bones, bindPoses);

            // Signed distance to plane (positive = upper / body side)
            var d = new float[vc];
            for (int v = 0; v < vc; v++)
                d[v] = Vector3.Dot(worldPos[v] - planePoint, planeNormal);

            var upper = new Accumulator();
            var lower = new Accumulator();

            // Vertex dedup maps: original-index → new index in each accumulator
            var uMap = new Dictionary<int, int>();
            var lMap = new Dictionary<int, int>();
            // Edge dedup maps: edge key → intersection vertex index
            var uEdge = new Dictionary<long, int>();
            var lEdge = new Dictionary<long, int>();

            // World-space intersection ring (for VFX + cap generation)
            var capWorld   = new List<Vector3>();
            // Matching upper-accumulator vertex indices (for cap SMR binding)
            var capUpperVI = new List<int>();

            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                upper.BeginSubMesh();
                lower.BeginSubMesh();
                var tris = mesh.GetTriangles(sub);

                for (int t = 0; t < tris.Length; t += 3)
                {
                    int a = tris[t], b = tris[t+1], c = tris[t+2];
                    bool ua = d[a] >= 0f, ub = d[b] >= 0f, uc = d[c] >= 0f;

                    if (ua == ub && ub == uc)
                    {
                        // All on same side
                        if (ua) AddWholeTri(a, b, c, bindVerts, bindNormals, bindTangents, uvs, bws, upper, uMap);
                        else    AddWholeTri(a, b, c, bindVerts, bindNormals, bindTangents, uvs, bws, lower, lMap);
                        continue;
                    }

                    SplitTri(a, b, c, ua, ub, uc, d, worldPos,
                             bindVerts, bindNormals, bindTangents, uvs, bws,
                             upper, lower, uMap, lMap, uEdge, lEdge,
                             capWorld, capUpperVI);
                }
            }

            // Cap submesh
            if (capWorld.Count >= 3)
                BuildCap(upper, lower, capWorld, capUpperVI, planeNormal);

            // Upper SMR — update source in-place so the character GO and every system
            // on it (targeting, AI, health, ragdoll, etc.) are completely unaffected.
            // No new GO is created; source just gets a new mesh and an extra cap material.
            Mesh upMesh = upper.Build();
            if (upMesh.vertexCount == 0)
                return (null, null, capWorld.ToArray()); // nothing above the plane

            upMesh.bindposes = bindPoses;
            source.sharedMesh = upMesh;
            // bones / rootBone are already correct on source — no change needed.

            var upMats = new Material[source.sharedMaterials.Length + 1];
            source.sharedMaterials.CopyTo(upMats, 0);
            upMats[upMats.Length - 1] = capMaterial;
            source.sharedMaterials = upMats;
            // source stays enabled — it now renders only the surviving upper half.

            // Lower baked mesh — world-space static mesh for a Rigidbody GO.
            Mesh lowMesh = lower.Build();
            lowMesh.bindposes = bindPoses;

            var tmpGO  = new GameObject("__TmpBake__");
            tmpGO.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            var tmpSMR = tmpGO.AddComponent<SkinnedMeshRenderer>();
            tmpSMR.sharedMesh = lowMesh;
            tmpSMR.bones      = bones;
            tmpSMR.rootBone   = source.rootBone;
            var bakedMesh = new Mesh();
            tmpSMR.BakeMesh(bakedMesh);
            Object.DestroyImmediate(tmpGO);

            return (source, bakedMesh, capWorld.ToArray());
        }

        // ── Triangle helpers ──────────────────────────────────────────────────────

        static void AddWholeTri(int a, int b, int c,
            Vector3[] verts, Vector3[] normals, Vector4[] tangents, Vector2[] uvs, BoneWeight[] bws,
            Accumulator acc, Dictionary<int, int> map)
        {
            acc.Tri(OrigVert(a, verts, normals, tangents, uvs, bws, acc, map),
                    OrigVert(b, verts, normals, tangents, uvs, bws, acc, map),
                    OrigVert(c, verts, normals, tangents, uvs, bws, acc, map));
        }

        static void SplitTri(
            int a, int b, int c,
            bool ua, bool ub, bool uc,
            float[] d, Vector3[] worldPos,
            Vector3[] verts, Vector3[] normals, Vector4[] tangents, Vector2[] uvs, BoneWeight[] bws,
            Accumulator upper, Accumulator lower,
            Dictionary<int, int> uMap, Dictionary<int, int> lMap,
            Dictionary<long, int> uEdge, Dictionary<long, int> lEdge,
            List<Vector3> capWorld, List<int> capUpperVI)
        {
            // Identify the lone vertex (the one on the opposite side from the other two)
            int lone, s0, s1;
            bool loneSide;
            if (ua == ub) { lone = c; s0 = a; s1 = b; loneSide = uc; }
            else if (ua == uc) { lone = b; s0 = a; s1 = c; loneSide = ub; }
            else { lone = a; s0 = b; s1 = c; loneSide = ua; }

            // Intersection t along lone→s0 and lone→s1
            float tLS0 = d[lone] / (d[lone] - d[s0]);
            float tLS1 = d[lone] / (d[lone] - d[s1]);

            // Upper intersection vertices (cached per edge to avoid seam cracks)
            int uiLS0 = IsectVert(lone, s0, tLS0, verts, normals, tangents, uvs, bws, worldPos,
                                  upper, uEdge, capWorld, capUpperVI, isUpper: true);
            int uiLS1 = IsectVert(lone, s1, tLS1, verts, normals, tangents, uvs, bws, worldPos,
                                  upper, uEdge, capWorld, capUpperVI, isUpper: true);
            // Lower intersection vertices
            int liLS0 = IsectVert(lone, s0, tLS0, verts, normals, tangents, uvs, bws, worldPos,
                                  lower, lEdge, capWorld, capUpperVI, isUpper: false);
            int liLS1 = IsectVert(lone, s1, tLS1, verts, normals, tangents, uvs, bws, worldPos,
                                  lower, lEdge, capWorld, capUpperVI, isUpper: false);

            if (loneSide)
            {
                // Lone vertex is UPPER; the two same-side verts are LOWER
                int uLone = OrigVert(lone, verts, normals, tangents, uvs, bws, upper, uMap);
                int lS0   = OrigVert(s0,   verts, normals, tangents, uvs, bws, lower, lMap);
                int lS1   = OrigVert(s1,   verts, normals, tangents, uvs, bws, lower, lMap);
                upper.Tri(uLone, uiLS0, uiLS1);
                lower.Tri(liLS0, lS0, lS1);
                lower.Tri(liLS0, lS1, liLS1);
            }
            else
            {
                // Lone vertex is LOWER; the two same-side verts are UPPER
                int lLone = OrigVert(lone, verts, normals, tangents, uvs, bws, lower, lMap);
                int uS0   = OrigVert(s0,   verts, normals, tangents, uvs, bws, upper, uMap);
                int uS1   = OrigVert(s1,   verts, normals, tangents, uvs, bws, upper, uMap);
                lower.Tri(lLone, liLS0, liLS1);
                upper.Tri(uiLS0, uS0, uS1);
                upper.Tri(uiLS0, uS1, uiLS1);
            }
        }

        // ── Cap generation ─────────────────────────────────────────────────────────

        static void BuildCap(Accumulator upper, Accumulator lower,
            List<Vector3> capWorld, List<int> capUpperVI, Vector3 planeNormal)
        {
            // Sort intersection ring by angle around centroid (works for convex cross-sections)
            Vector3 centWS = Vector3.zero;
            foreach (var p in capWorld) centWS += p;
            centWS /= capWorld.Count;

            Vector3 rx = Vector3.Cross(planeNormal, Vector3.up);
            if (rx.sqrMagnitude < 0.01f) rx = Vector3.Cross(planeNormal, Vector3.forward);
            rx = rx.normalized;
            Vector3 ry = Vector3.Cross(planeNormal, rx);

            int n = capWorld.Count;
            var order = new int[n];
            for (int i = 0; i < n; i++) order[i] = i;
            System.Array.Sort(order, (ia, ib) =>
            {
                Vector3 da = capWorld[ia] - centWS, db = capWorld[ib] - centWS;
                return Mathf.Atan2(Vector3.Dot(da, ry), Vector3.Dot(da, rx))
                    .CompareTo(Mathf.Atan2(Vector3.Dot(db, ry), Vector3.Dot(db, rx)));
            });

            // Sorted upper-accumulator vertex indices
            var ring = new int[n];
            for (int i = 0; i < n; i++) ring[i] = capUpperVI[order[i]];

            // Centroid vertex for upper cap: average of ring bind-pose positions + weights
            Vector3 avgPos = Vector3.zero; Vector3 avgNor = Vector3.zero;
            BoneWeight avgBW = default;
            foreach (int ri in ring)
            {
                var vd = upper.Verts[ri];
                avgPos += vd.Position; avgNor += vd.Normal;
                avgBW.weight0 += vd.BW.weight0; avgBW.weight1 += vd.BW.weight1;
                avgBW.weight2 += vd.BW.weight2; avgBW.weight3 += vd.BW.weight3;
            }
            avgPos /= n; avgNor = (avgNor / n).normalized;
            avgBW.weight0 /= n; avgBW.weight1 /= n; avgBW.weight2 /= n; avgBW.weight3 /= n;
            var refBW = upper.Verts[ring[0]].BW;
            avgBW.boneIndex0 = refBW.boneIndex0; avgBW.boneIndex1 = refBW.boneIndex1;
            avgBW.boneIndex2 = refBW.boneIndex2; avgBW.boneIndex3 = refBW.boneIndex3;

            var capTan = new Vector4(rx.x, rx.y, rx.z, 1f);

            // Upper cap submesh
            upper.BeginSubMesh();
            int uCent = upper.Add(new VData { Position = avgPos, Normal = planeNormal,
                                              Tangent = capTan, UV = new Vector2(0.5f, 0.5f), BW = avgBW });
            for (int i = 0; i < n; i++)
                upper.Tri(uCent, ring[i], ring[(i + 1) % n]);

            // Lower cap submesh (using same bind-pose positions; baked at export anyway)
            lower.BeginSubMesh();
            var lRing = new int[n];
            for (int i = 0; i < n; i++)
            {
                var upVd = upper.Verts[ring[i]];
                lRing[i] = lower.Add(new VData { Position = upVd.Position,
                                                  Normal = -planeNormal, Tangent = capTan,
                                                  UV = new Vector2(0.5f, 0.5f), BW = upVd.BW });
            }
            int lCent = lower.Add(new VData { Position = avgPos, Normal = -planeNormal,
                                               Tangent = capTan, UV = new Vector2(0.5f, 0.5f), BW = avgBW });
            for (int i = 0; i < n; i++)
                lower.Tri(lCent, lRing[(i + 1) % n], lRing[i]);  // reversed winding
        }

        // ── Vertex utilities ──────────────────────────────────────────────────────

        static int OrigVert(int orig,
            Vector3[] verts, Vector3[] normals, Vector4[] tangents, Vector2[] uvs, BoneWeight[] bws,
            Accumulator acc, Dictionary<int, int> map)
        {
            if (map.TryGetValue(orig, out int existing)) return existing;
            int idx = acc.Add(new VData
            {
                Position = verts[orig],
                Normal   = orig < normals.Length  ? normals[orig]  : Vector3.up,
                Tangent  = orig < tangents.Length ? tangents[orig] : Vector4.zero,
                UV       = orig < uvs.Length      ? uvs[orig]      : Vector2.zero,
                BW       = bws[orig]
            });
            map[orig] = idx;
            return idx;
        }

        // Creates (or retrieves cached) intersection vertex on edge origA→origB at parameter t.
        // capWorld/capUpperVI are only appended when isUpper=true (once per edge, not twice).
        static int IsectVert(int origA, int origB, float t,
            Vector3[] verts, Vector3[] normals, Vector4[] tangents, Vector2[] uvs, BoneWeight[] bws,
            Vector3[] worldPos,
            Accumulator acc, Dictionary<long, int> edgeMap,
            List<Vector3> capWorld, List<int> capUpperVI, bool isUpper)
        {
            long key = EdgeKey(origA, origB);
            if (edgeMap.TryGetValue(key, out int existing)) return existing;

            var va = new VData
            {
                Position = verts[origA],
                Normal   = origA < normals.Length  ? normals[origA]  : Vector3.up,
                Tangent  = origA < tangents.Length ? tangents[origA] : Vector4.zero,
                UV       = origA < uvs.Length      ? uvs[origA]      : Vector2.zero,
                BW       = bws[origA]
            };
            var vb = new VData
            {
                Position = verts[origB],
                Normal   = origB < normals.Length  ? normals[origB]  : Vector3.up,
                Tangent  = origB < tangents.Length ? tangents[origB] : Vector4.zero,
                UV       = origB < uvs.Length      ? uvs[origB]      : Vector2.zero,
                BW       = bws[origB]
            };

            int idx = acc.Add(LerpVData(va, vb, t));
            edgeMap[key] = idx;

            if (isUpper)
            {
                capWorld.Add(Vector3.Lerp(worldPos[origA], worldPos[origB], t));
                capUpperVI.Add(idx);
            }

            return idx;
        }

        static VData LerpVData(VData a, VData b, float t) => new VData
        {
            Position = Vector3.Lerp(a.Position, b.Position, t),
            Normal   = Vector3.Slerp(a.Normal, b.Normal, t).normalized,
            Tangent  = Vector4.Lerp(a.Tangent, b.Tangent, t),
            UV       = Vector2.Lerp(a.UV, b.UV, t),
            BW       = LerpBW(a.BW, b.BW, t)
        };

        // Linear-blend bone weights. Keeps bone indices from vertex A; blends the weights.
        // Acceptable for intersection verts: the blend is over a very short edge segment.
        static BoneWeight LerpBW(BoneWeight a, BoneWeight b, float t)
        {
            float w0 = Mathf.Lerp(a.weight0, b.weight0, t);
            float w1 = Mathf.Lerp(a.weight1, b.weight1, t);
            float w2 = Mathf.Lerp(a.weight2, b.weight2, t);
            float w3 = Mathf.Lerp(a.weight3, b.weight3, t);
            float s  = w0 + w1 + w2 + w3;
            if (s < 0.001f) s = 1f;
            return new BoneWeight
            {
                boneIndex0 = a.boneIndex0, weight0 = w0 / s,
                boneIndex1 = a.boneIndex1, weight1 = w1 / s,
                boneIndex2 = a.boneIndex2, weight2 = w2 / s,
                boneIndex3 = a.boneIndex3, weight3 = w3 / s,
            };
        }

        // Compute skinned world position for a single vertex.
        static Vector3 SkinVertex(Vector3 bindVert, BoneWeight bw, Transform[] bones, Matrix4x4[] bindPoses)
        {
            var m = new Matrix4x4();
            if (bw.weight0 > 0f) AddWeighted(ref m, bones[bw.boneIndex0].localToWorldMatrix * bindPoses[bw.boneIndex0], bw.weight0);
            if (bw.weight1 > 0f) AddWeighted(ref m, bones[bw.boneIndex1].localToWorldMatrix * bindPoses[bw.boneIndex1], bw.weight1);
            if (bw.weight2 > 0f) AddWeighted(ref m, bones[bw.boneIndex2].localToWorldMatrix * bindPoses[bw.boneIndex2], bw.weight2);
            if (bw.weight3 > 0f) AddWeighted(ref m, bones[bw.boneIndex3].localToWorldMatrix * bindPoses[bw.boneIndex3], bw.weight3);
            return m.MultiplyPoint3x4(bindVert);
        }

        static void AddWeighted(ref Matrix4x4 acc, Matrix4x4 m, float w)
        {
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 4; c++)
                    acc[r, c] += m[r, c] * w;
        }

        static long EdgeKey(int a, int b) =>
            a < b ? ((long)a << 32 | (uint)b) : ((long)b << 32 | (uint)a);
    }
}
