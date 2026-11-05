using System;
using ABCWonderworld.UI;
using UnityEngine;

namespace ABCWonderworld.Characters
{
    /// <summary>
    /// Code-driven animation for the placeholder cast: idle breathing and sway, blinking, talking mouth and
    /// one-shot reactions. Honors reduced motion. No per-frame allocations.
    /// </summary>
    public sealed class ProceduralCharacterAnimator : MonoBehaviour, ICharacterAnimator
    {
        public Transform body;
        public Transform[] eyes;
        public Transform[] pupils;
        public Transform mouth;

        private Vector3 _bodyPos, _bodyScale, _mouthScale;
        private Quaternion _bodyRot;
        private Vector3[] _eyeScale, _pupilPos;
        private float _stateTime;
        private float _stateLength;
        private Action _done;
        private float _nextBlink;
        private float _blinkT = -1f;
        private bool _talking;
        private Vector3 _look;
        private float _phase;

        public CharacterState Current { get; private set; } = CharacterState.Idle;

        private void Awake()
        {
            if (body == null) body = transform;
            _bodyPos = body.localPosition; _bodyScale = body.localScale; _bodyRot = body.localRotation;
            if (mouth != null) _mouthScale = mouth.localScale;
            _eyeScale = new Vector3[eyes?.Length ?? 0];
            for (int i = 0; i < _eyeScale.Length; i++) _eyeScale[i] = eyes[i].localScale;
            _pupilPos = new Vector3[pupils?.Length ?? 0];
            for (int i = 0; i < _pupilPos.Length; i++) _pupilPos[i] = pupils[i].localPosition;
            _phase = UnityEngine.Random.value * 10f;
            _nextBlink = Time.time + UnityEngine.Random.Range(1.5f, 4f);
        }

        public void Play(CharacterState state, Action onComplete = null)
        {
            _done?.Invoke();
            Current = state;
            _stateTime = 0;
            _done = onComplete;
            _stateLength = state switch
            {
                CharacterState.Greet => 1.2f,
                CharacterState.Happy => 0.9f,
                CharacterState.Celebrate => 1.4f,
                CharacterState.Surprised => 0.8f,
                CharacterState.Interact => 0.6f,
                CharacterState.Dance => 2.4f,
                CharacterState.Encourage => 1.0f,
                _ => 0f
            };
            if (state == CharacterState.Idle) { var d = _done; _done = null; d?.Invoke(); }
        }

        public void SetTalking(bool talking) => _talking = talking;
        public void LookAt(Vector3 worldPoint) => _look = worldPoint;

        private void Update()
        {
            float m = Tween.Motion;
            float t = Time.time + _phase;
            Vector3 pos = _bodyPos;
            Vector3 scale = _bodyScale;
            Quaternion rot = _bodyRot;

            // Idle: breathing squash and a gentle sway.
            float breathe = Mathf.Sin(t * 2.2f) * 0.025f * m;
            scale = Vector3.Scale(scale, new Vector3(1 - breathe * 0.5f, 1 + breathe, 1 - breathe * 0.5f));
            rot *= Quaternion.Euler(0, 0, Mathf.Sin(t * 1.1f) * 3f * m);

            if (Current != CharacterState.Idle)
            {
                _stateTime += Time.deltaTime;
                float k = Mathf.Clamp01(_stateTime / Mathf.Max(0.01f, _stateLength));
                float h = body.lossyScale.y > 0 ? 1f : 1f;
                switch (Current)
                {
                    case CharacterState.Greet:
                        pos += Vector3.up * Mathf.Abs(Mathf.Sin(k * Mathf.PI * 2f)) * 0.12f * m * h;
                        rot *= Quaternion.Euler(0, 0, Mathf.Sin(k * Mathf.PI * 4f) * 12f * m);
                        break;
                    case CharacterState.Happy:
                    case CharacterState.Encourage:
                        pos += Vector3.up * Mathf.Abs(Mathf.Sin(k * Mathf.PI * 2f)) * 0.18f * m;
                        scale = Vector3.Scale(scale, new Vector3(1 + Mathf.Sin(k * Mathf.PI * 4f) * 0.06f * m, 1 - Mathf.Sin(k * Mathf.PI * 4f) * 0.06f * m, 1));
                        break;
                    case CharacterState.Celebrate:
                        pos += Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.45f * m;
                        rot *= Quaternion.Euler(0, k * 360f * (m > 0.5f ? 1 : 0), 0);
                        break;
                    case CharacterState.Surprised:
                        scale = Vector3.Scale(scale, new Vector3(1 - Mathf.Sin(k * Mathf.PI) * 0.1f * m, 1 + Mathf.Sin(k * Mathf.PI) * 0.15f * m, 1));
                        break;
                    case CharacterState.Interact:
                        float sq = Mathf.Sin(k * Mathf.PI * 3f) * (1 - k) * 0.15f * m;
                        scale = Vector3.Scale(scale, new Vector3(1 + sq, 1 - sq, 1 + sq));
                        break;
                    case CharacterState.Dance:
                        pos += Vector3.up * Mathf.Abs(Mathf.Sin(k * Mathf.PI * 6f)) * 0.12f * m;
                        rot *= Quaternion.Euler(0, Mathf.Sin(k * Mathf.PI * 4f) * 25f * m, Mathf.Sin(k * Mathf.PI * 6f) * 10f * m);
                        break;
                }
                if (k >= 1f)
                {
                    Current = CharacterState.Idle;
                    Action d = _done; _done = null; d?.Invoke();
                }
            }
            body.localPosition = pos;
            body.localScale = scale;
            body.localRotation = rot;

            // Blink.
            if (_blinkT < 0 && Time.time >= _nextBlink) _blinkT = 0;
            float eyeY = 1f;
            if (_blinkT >= 0)
            {
                _blinkT += Time.deltaTime;
                eyeY = 1f - Mathf.Sin(Mathf.Clamp01(_blinkT / 0.16f) * Mathf.PI) * 0.9f;
                if (_blinkT > 0.16f) { _blinkT = -1; _nextBlink = Time.time + UnityEngine.Random.Range(2f, 5f); }
            }
            float surprise = Current == CharacterState.Surprised ? 1.25f : 1f;
            for (int i = 0; i < _eyeScale.Length; i++)
                if (eyes[i] != null) eyes[i].localScale = new Vector3(_eyeScale[i].x * surprise, _eyeScale[i].y * eyeY * surprise, _eyeScale[i].z);

            // Pupils drift toward the look target (or wander).
            for (int i = 0; i < _pupilPos.Length; i++)
            {
                if (pupils[i] == null) continue;
                Vector3 off = new Vector3(Mathf.Sin(t * 0.7f), Mathf.Sin(t * 0.53f) * 0.5f, 0) * 0.012f;
                pupils[i].localPosition = _pupilPos[i] + off;
            }

            // Talking mouth.
            if (mouth != null)
            {
                float open = _talking ? 0.6f + Mathf.Abs(Mathf.Sin(Time.time * 14f)) * 0.9f : 1f;
                mouth.localScale = new Vector3(_mouthScale.x, _mouthScale.y * open, _mouthScale.z);
            }
        }

        private void OnDisable() => _done = null;
    }
}
