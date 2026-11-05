using ABCWonderworld.UI;
using UnityEngine;

namespace ABCWonderworld.Characters
{
    /// <summary>Small floating bob for badges and collectibles.</summary>
    public sealed class Bob : MonoBehaviour
    {
        public float height = 0.06f;
        public float speed = 2f;
        private Vector3 _p;
        private float _o;
        private void OnEnable() { _p = transform.localPosition; _o = UnityEngine.Random.value * 6f; }
        private void Update() => transform.localPosition = _p + Vector3.up * Mathf.Sin(Time.time * speed + _o) * height * UI.Tween.Motion;
    }
}
