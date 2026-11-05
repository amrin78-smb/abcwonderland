using System.Linq;
using ABCWonderworld.Activities;
using ABCWonderworld.Content;
using NUnit.Framework;
using UnityEditor;

namespace ABCWonderworld.Tests
{
    /// <summary>Validates the generated A–Z content (run Tools/setup.sh first).</summary>
    public class CatalogTests
    {
        private ContentCatalog _c;

        [OneTimeSetUp]
        public void Load()
        {
            _c = AssetDatabase.LoadAssetAtPath<ContentCatalog>("Assets/ABCWonderworld/Resources/ContentCatalog.asset");
            Assert.NotNull(_c, "ContentCatalog not generated");
            _c.Reindex();
        }

        [Test]
        public void CharacterPrefabsHaveNoMissingScriptsAndCanAnimate()
        {
            // A MonoBehaviour declared in a file with a different name cannot be saved into a prefab: it silently
            // becomes a "missing script" and the character never blinks, talks or dances.
            foreach (LetterDefinition l in _c.letters)
            {
                UnityEngine.GameObject prefab = _c.CharacterFor(l)?.prefab;
                Assert.NotNull(prefab, l.id);
                int missing = prefab.GetComponentsInChildren<UnityEngine.Transform>(true)
                    .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                Assert.That(missing, Is.EqualTo(0), l.id + " prefab has missing scripts");
                Assert.NotNull(prefab.GetComponent<Characters.ProceduralCharacterAnimator>(), l.id + " has no animator");
                Assert.NotNull(prefab.GetComponent<Characters.LetterCharacter>(), l.id);
            }
        }

        [Test]
        public void ValidatorReportsNoErrors()
        {
            var errors = ContentValidator.Validate(_c).Where(i => i.Severity == IssueSeverity.Error).ToList();
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void CompleteAlphabetWithStableIds()
        {
            Assert.That(_c.letters.Count, Is.EqualTo(26));
            for (int i = 0; i < 26; i++)
            {
                LetterDefinition l = _c.letters[i];
                Assert.That(l.upper, Is.EqualTo((char)('A' + i)));
                Assert.That(l.id, Is.EqualTo("letter_" + (char)('a' + i)));
                Assert.That(l.vocabulary.Count, Is.GreaterThanOrEqualTo(3));
                Assert.NotNull(l.upperTracing); Assert.NotNull(l.lowerTracing);
                Assert.NotNull(_c.CharacterFor(l)?.prefab, l.id);
                Assert.That(l.nameAudioKey, Is.Not.EqualTo(l.phonemeAudioKey));
            }
            Assert.That(_c.letters.SelectMany(l => l.vocabulary).Count(), Is.GreaterThanOrEqualTo(78));
        }

        [Test]
        public void WorldsMatchTheGdd()
        {
            string[] expected = { "ABCDE", "FGHIJ", "KLMNO", "PQRST", "UVWXYZ" };
            Assert.That(_c.worlds.Count, Is.EqualTo(5));
            for (int i = 0; i < 5; i++)
                Assert.That(string.Concat(_c.LettersIn(_c.worlds[i]).Select(l => l.upper)), Is.EqualTo(expected[i]));
        }

        [Test]
        public void IrregularSoundsAreFlaggedNotTaughtAsInitialSounds()
        {
            string[] notInitial = { "giraffe", "ice cream", "orange", "owl", "unicorn", "xylophone", "x-ray", "fox" };
            foreach (var v in _c.letters.SelectMany(l => l.vocabulary))
                Assert.That(v.startsWithBeginnerSound, Is.EqualTo(!notInitial.Contains(v.word)), v.word);
            Assert.That(_c.Letter('X').vocabulary.First(v => v.word == "fox").soundPosition, Is.EqualTo("end"));
        }

        [Test]
        public void EveryAdventureRunsAnImplementedArchetype()
        {
            foreach (LetterDefinition l in _c.letters)
                Assert.That(AdventureActivity.Implemented, Does.Contain(AdventureActivity.Effective(l.adventure.archetype)), l.id);
        }

        [Test]
        public void RequiredVoiceClipsExist()
        {
            var missing = ContentValidator.RequiredAudioKeys(_c).Where(k => _c.audio.Clip(k) == null).ToList();
            Assert.That(missing, Is.Empty, string.Join(", ", missing));
        }

        [Test]
        public void MissingReferencesAreReportedNotThrown()
        {
            var broken = UnityEngine.Object.Instantiate(_c);
            broken.letters = broken.letters.Take(25).ToList();
            broken.characters = broken.characters.Skip(1).ToList();
            var issues = ContentValidator.Validate(broken);
            Assert.That(issues.Any(i => i.Subject == "letter_z" && i.Severity == IssueSeverity.Error));
            Assert.That(issues.Any(i => i.Message.Contains("character_a")));
        }
    }
}
