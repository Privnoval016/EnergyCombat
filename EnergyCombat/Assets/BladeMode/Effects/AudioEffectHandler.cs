using System;
using BladeMode.Slicing;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace BladeMode.Effects
{
    /// <summary>
    /// Plays AudioClips in response to blade mode events.
    /// No scene AudioSource required — a temporary AudioSource GO is created and
    /// auto-destroyed per clip, giving full control over spatial blend and mixer routing.
    ///
    /// Assign multiple cut clips for random variation across cuts.
    /// </summary>
    [Serializable]
    public sealed class AudioEffect : IBladeModeEffect
    {
        [Header("Clips")]
        [Tooltip("Played once when blade mode is entered.")]
        [SerializeField] AudioClip _enterClip;

        [Tooltip("Played once when blade mode is exited.")]
        [SerializeField] AudioClip _exitClip;

        [Tooltip("One clip chosen at random each time a cut fires. " +
                 "Add multiple clips to reduce repetition.")]
        [SerializeField] AudioClip[] _cutClips;

        [Header("Playback")]
        [Tooltip("Volume scale applied to all clips.")]
        [Range(0f, 1f)] [SerializeField] float _volume = 1f;

        [Tooltip("0 = fully 2D (no distance falloff). 1 = full 3D spatial audio at the event position.")]
        [Range(0f, 1f)] [SerializeField] float _spatialBlend = 0f;

        [Tooltip("Optional mixer group for volume/EQ routing. Leave blank to use the master output.")]
        [SerializeField] AudioMixerGroup _outputGroup;

        Transform _playerRoot;

        public void Initialize(Transform playerTransform) => _playerRoot = playerTransform;

        public void OnEnterBladeMode() => PlayAt(_enterClip, PlayerPos());
        public void OnExitBladeMode()  => PlayAt(_exitClip,  PlayerPos());

        public void OnCutExecuted(SliceResult[] results, Vector3 planePoint, Vector3 planeNormal)
        {
            if (_cutClips == null || _cutClips.Length == 0) return;
            PlayAt(_cutClips[Random.Range(0, _cutClips.Length)], planePoint);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        Vector3 PlayerPos() => _playerRoot != null ? _playerRoot.position : Vector3.zero;

        void PlayAt(AudioClip clip, Vector3 position)
        {
            if (clip == null) return;
            var go = new GameObject("[BladeModeAudio]") { hideFlags = HideFlags.HideInHierarchy };
            go.transform.position = position;
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.volume = _volume;
            src.spatialBlend = _spatialBlend;
            if (_outputGroup != null) src.outputAudioMixerGroup = _outputGroup;
            src.Play();
            Object.Destroy(go, clip.length + 0.1f);
        }
    }
}
