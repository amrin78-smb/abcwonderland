using System;
using ABCWonderworld.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ABCWonderworld.Characters
{
    /// <summary>
    /// Interaction contract shared by all 26 characters: identity, anchor points, animation and tap events.
    /// </summary>
    public sealed class LetterCharacter : MonoBehaviour, IPointerClickHandler
    {
        public string letterId;
        public string characterId;
        public Transform faceAnchor;
        public Transform aboveHeadAnchor;
        public Transform leftHandAnchor;
        public Transform rightHandAnchor;
        public Transform feetAnchor;
        [Tooltip("True for procedurally generated placeholder characters.")]
        public bool isPlaceholder = true;

        private ICharacterAnimator _animator;
        private float _lastTap = -10f;
        public event Action<LetterCharacter> Tapped;

        public ICharacterAnimator Animator => _animator ??= GetComponentInChildren<ICharacterAnimator>() ?? gameObject.AddComponent<ProceduralCharacterAnimator>();

        public void Play(CharacterState s, Action done = null) => Animator.Play(s, done);

        private void Update()
        {
            // Mouth moves while this character's line is playing (any voice line in placeholder mode).
            AudioServiceTalkingSync();
        }

        private bool _speaking;
        public void SetSpeaking(bool speaking) => _speaking = speaking;

        private void AudioServiceTalkingSync()
        {
            bool talking = _speaking && (GameServices.Current?.Audio?.IsSpeaking ?? false);
            Animator.SetTalking(talking);
            if (_speaking && !(GameServices.Current?.Audio?.IsSpeaking ?? false)) _speaking = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Time.unscaledTime - _lastTap < 0.4f) return;
            _lastTap = Time.unscaledTime;
            Play(CharacterState.Interact);
            Tapped?.Invoke(this);
        }
    }
}
