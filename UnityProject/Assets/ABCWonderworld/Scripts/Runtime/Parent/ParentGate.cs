using System;
using System.Collections.Generic;

namespace ABCWonderworld.Parent
{
    /// <summary>
    /// Adult gate (master prompt §15): a 2-second hold, then a reading challenge. The child-facing screen
    /// shows a sentence such as "Tap seven, then two" with number buttons in shuffled order. Reading number
    /// words and following a two-step instruction is reliably beyond the 2–5 target audience, unlike a
    /// single arithmetic question. Pure logic; the UI lives in ParentGateView.
    /// </summary>
    public sealed class ParentGate
    {
        public const float HoldSeconds = 2f;
        public const int MaxWrongBeforeNewChallenge = 1;
        private static readonly string[] Words = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };

        private readonly Random _rng;
        private int _step;
        private int _wrong;

        public int First { get; private set; }
        public int Second { get; private set; }
        public bool HoldPassed { get; private set; }
        public bool Passed { get; private set; }
        public List<int> ButtonOrder { get; } = new List<int>();

        public string Prompt => $"Tap {Words[First]}, then {Words[Second]}";

        public ParentGate(Random rng = null)
        {
            _rng = rng ?? new Random();
            NewChallenge();
        }

        public void NewChallenge()
        {
            First = _rng.Next(1, 10);
            do Second = _rng.Next(1, 10); while (Second == First);
            _step = 0;
            _wrong = 0;
            ButtonOrder.Clear();
            for (int i = 1; i <= 9; i++) ButtonOrder.Add(i);
            for (int i = ButtonOrder.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (ButtonOrder[i], ButtonOrder[j]) = (ButtonOrder[j], ButtonOrder[i]);
            }
        }

        public bool UpdateHold(float heldSeconds)
        {
            if (heldSeconds >= HoldSeconds) HoldPassed = true;
            return HoldPassed;
        }

        /// <summary>Returns true when the gate is now open. A wrong tap resets with a new challenge.</summary>
        public bool Press(int number)
        {
            if (!HoldPassed || Passed) return Passed;
            int expected = _step == 0 ? First : Second;
            if (number == expected)
            {
                _step++;
                if (_step >= 2) Passed = true;
                return Passed;
            }
            _wrong++;
            if (_wrong > MaxWrongBeforeNewChallenge - 1) NewChallenge();
            else _step = 0;
            return false;
        }
    }
}
