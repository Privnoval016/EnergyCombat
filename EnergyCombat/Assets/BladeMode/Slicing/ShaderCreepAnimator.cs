using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BladeMode.Slicing
{
    // Self-attaching, self-destroying. Animates _BurnProgress / _FreezeProgress
    // from 0→1 over the set duration, then removes itself.
    [DisallowMultipleComponent]
    public sealed class ShaderCreepAnimator : MonoBehaviour
    {
        [SerializeField] float _duration = 0.8f;

        static readonly int CutPlaneNormalId  = Shader.PropertyToID("_CutPlaneNormal");
        static readonly int CutPlaneOriginId  = Shader.PropertyToID("_CutPlaneOrigin");
        static readonly int BurnProgressId    = Shader.PropertyToID("_BurnProgress");
        static readonly int FreezeProgressId  = Shader.PropertyToID("_FreezeProgress");

        Vector3 _planePoint;
        Vector3 _planeNormal;

        public void Init(Vector3 planePoint, Vector3 planeNormal)
        {
            _planePoint  = planePoint;
            _planeNormal = planeNormal;
            AnimateAsync(destroyCancellationToken).Forget();
        }

        async UniTaskVoid AnimateAsync(System.Threading.CancellationToken token)
        {
            var renderers = GetComponentsInChildren<Renderer>();
            var mpb = new MaterialPropertyBlock();
            float elapsed = 0f;

            while (elapsed < _duration && !token.IsCancellationRequested)
            {
                float t = elapsed / _duration;
                foreach (var r in renderers)
                {
                    r.GetPropertyBlock(mpb);
                    mpb.SetVector(CutPlaneNormalId, _planeNormal);
                    mpb.SetVector(CutPlaneOriginId, _planePoint);
                    mpb.SetFloat(BurnProgressId,   t);
                    mpb.SetFloat(FreezeProgressId, t);
                    r.SetPropertyBlock(mpb);
                }

                elapsed += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            // Ensure fully crept on last frame
            if (!token.IsCancellationRequested)
            {
                foreach (var r in renderers)
                {
                    r.GetPropertyBlock(mpb);
                    mpb.SetFloat(BurnProgressId,   1f);
                    mpb.SetFloat(FreezeProgressId, 1f);
                    r.SetPropertyBlock(mpb);
                }
            }

            Destroy(this);
        }
    }
}
