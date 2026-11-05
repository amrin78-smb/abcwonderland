using System;
using ABCWonderworld.UI;
using UnityEngine;

namespace ABCWonderworld.Characters
{
    /// <summary>Shared character state vocabulary (master prompt §11). Values are stable.</summary>
    public enum CharacterState { Idle = 0, Greet = 1, Happy = 2, Celebrate = 3, Surprised = 4, Interact = 5, Dance = 6, Encourage = 7 }

    /// <summary>
    /// Animation contract every character implements. The placeholder cast uses
    /// <see cref="ProceduralCharacterAnimator"/>; rigged production models plug in an Animator-based
    /// implementation (hashed parameters centralised in one class) without touching gameplay code (D-008).
    /// </summary>
    public interface ICharacterAnimator
    {
        void Play(CharacterState state, Action onComplete = null);
        void SetTalking(bool talking);
        void LookAt(Vector3 worldPoint);
        CharacterState Current { get; }
    }
}
