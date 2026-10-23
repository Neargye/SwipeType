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

using System;
using System.Globalization;

namespace SwipeType
{
    /// <summary>
    /// A key's character and position in keyboard-layout coordinates.
    /// Rows are distinguished by <see cref="Y"/>; one key unit is the distance between neighboring keys in a row.
    /// </summary>
    public readonly struct KeyboardKey : IEquatable<KeyboardKey>
    {
        /// <summary>
        /// Initializes a keyboard key.
        /// </summary>
        /// <param name="character">Key character. Layouts match it case-insensitively.</param>
        /// <param name="x">Horizontal key coordinate.</param>
        /// <param name="y">Vertical key coordinate; keys with equal values form one row.</param>
        public KeyboardKey(char character, double x, double y)
        {
            Character = character;
            X = x;
            Y = y;
        }

        /// <summary>
        /// Gets the key character.
        /// </summary>
        public char Character { get; }

        /// <summary>
        /// Gets the horizontal key coordinate.
        /// </summary>
        public double X { get; }

        /// <summary>
        /// Gets the vertical key coordinate.
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// Determines whether two keys have the same character and coordinates.
        /// </summary>
        public static bool operator ==(KeyboardKey left, KeyboardKey right) => left.Equals(right);

        /// <summary>
        /// Determines whether two keys differ in character or coordinates.
        /// </summary>
        public static bool operator !=(KeyboardKey left, KeyboardKey right) => !left.Equals(right);

        /// <inheritdoc/>
        public bool Equals(KeyboardKey other) => Character == other.Character && X.Equals(other.X) && Y.Equals(other.Y);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is KeyboardKey other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return (((Character.GetHashCode() * 397) ^ X.GetHashCode()) * 397) ^ Y.GetHashCode();
            }
        }

        /// <inheritdoc/>
        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "'{0}' ({1}, {2})", Character, X, Y);
    }
}
