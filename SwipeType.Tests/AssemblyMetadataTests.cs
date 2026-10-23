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

using System.Reflection;

namespace SwipeType.Tests
{
    [TestClass]
    public class AssemblyMetadataTests
    {
        [TestMethod]
        public void LibraryAssemblyContainsMetadata()
        {
            Assembly assembly = typeof(SwipeSuggester).Assembly;

            Assert.IsTrue(assembly.GetName().Version > new Version(0, 0, 0, 0), "Assembly version must be nonzero.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title), "Assembly title is missing.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description), "Assembly description is missing.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version), "Assembly file version is missing.");
        }
    }
}
