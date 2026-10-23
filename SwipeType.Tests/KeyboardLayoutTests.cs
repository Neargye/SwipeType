// Copyright 2018 - 2026 Daniil Goncharov <neargye@gmail.com>.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace SwipeType.Tests
{
    [TestClass]
    public class KeyboardLayoutTests
    {
        [TestMethod]
        public void ConstructorRejectsInvalidKeys()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new KeyboardLayout(null!));
            Assert.ThrowsExactly<ArgumentException>(() => new KeyboardLayout(Array.Empty<KeyboardKey>()));
            Assert.ThrowsExactly<ArgumentException>(
                () => new KeyboardLayout(new[] { new KeyboardKey('a', 0.0, 0.0), new KeyboardKey('A', 1.0, 0.0) }));
            Assert.ThrowsExactly<ArgumentException>(
                () => new KeyboardLayout(new[] { new KeyboardKey('a', 0.0, 0.0), new KeyboardKey(' ', 1.0, 1.0) }));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new KeyboardLayout(new[] { new KeyboardKey('a', double.NaN, 0.0) }));
        }

        [TestMethod]
        public void KeysKeepTheirOrderAndSpellingAndAreCopied()
        {
            var keys = new[] { new KeyboardKey('B', 1.0, 0.0), new KeyboardKey('a', 0.0, 0.0) };
            var layout = new KeyboardLayout(keys);
            keys[0] = new KeyboardKey('z', 9.0, 9.0);

            CollectionAssert.AreEqual(new[] { new KeyboardKey('B', 1.0, 0.0), new KeyboardKey('a', 0.0, 0.0) }, layout.Keys.ToArray());
        }

        [TestMethod]
        public void QwertyHasThreeStaggeredLetterRows()
        {
            var keys = KeyboardLayout.Qwerty.Keys;

            Assert.AreEqual("qwertyuiopasdfghjklzxcvbnm", new string(keys.Select(key => key.Character).ToArray()));
            Assert.AreEqual(new KeyboardKey('q', 0.0, 0.0), keys[0]);
            Assert.AreEqual(new KeyboardKey('a', 0.5, 1.0), keys[10]);
            Assert.AreEqual(new KeyboardKey('z', 1.0, 2.0), keys[19]);
        }

        [TestMethod]
        public void KeyboardKeysCompareByCharacterAndPosition()
        {
            var key = new KeyboardKey('q', 0.0, 1.0);

            Assert.AreEqual(new KeyboardKey('q', 0.0, 1.0), key);
            Assert.IsTrue(key == new KeyboardKey('q', 0.0, 1.0));
            Assert.IsTrue(key != new KeyboardKey('Q', 0.0, 1.0));
            Assert.IsTrue(key != new KeyboardKey('q', 0.5, 1.0));
            Assert.AreEqual(key.GetHashCode(), new KeyboardKey('q', 0.0, 1.0).GetHashCode());
            Assert.AreEqual("'q' (0, 1)", key.ToString());
        }
    }
}
