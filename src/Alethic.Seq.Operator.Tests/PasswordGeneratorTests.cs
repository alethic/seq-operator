using System;
using System.Collections.Generic;
using System.Linq;

using Alethic.Seq.Operator.Instance;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alethic.Seq.Operator.Tests
{

    /// <summary>
    /// Covers the generator for the admin and first-run passwords of deployed Instances.
    /// </summary>
    [TestClass]
    public class PasswordGeneratorTests
    {

        [TestMethod]
        public void GeneratesRequestedLength()
        {
            foreach (var length in new[] { 4, 20, 64 })
                Assert.AreEqual(length, V1alpha1InstanceController.GeneratePassword(length).Length);
        }

        [TestMethod]
        public void ContainsEveryCharacterClass()
        {
            for (var i = 0; i < 1000; i++)
            {
                var password = V1alpha1InstanceController.GeneratePassword(20);
                Assert.IsTrue(password.Any(char.IsAsciiLetterLower), password);
                Assert.IsTrue(password.Any(char.IsAsciiLetterUpper), password);
                Assert.IsTrue(password.Any(char.IsAsciiDigit), password);
                Assert.IsTrue(password.Any(c => !char.IsAsciiLetterOrDigit(c)), password);
            }
        }

        /// <summary>
        /// The previous generator always put a special character first. Nothing about a position should be fixed.
        /// </summary>
        [TestMethod]
        public void FirstCharacterIsNotFixedToOneClass()
        {
            var first = new HashSet<bool>();
            for (var i = 0; i < 1000; i++)
                first.Add(char.IsAsciiLetterOrDigit(V1alpha1InstanceController.GeneratePassword(20)[0]));

            Assert.AreEqual(2, first.Count);
        }

        [TestMethod]
        public void DoesNotRepeat()
        {
            var passwords = Enumerable.Range(0, 1000).Select(_ => V1alpha1InstanceController.GeneratePassword(20)).ToHashSet();
            Assert.AreEqual(1000, passwords.Count);
        }

        [TestMethod]
        public void RejectsLengthTooShortForEveryClass()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => V1alpha1InstanceController.GeneratePassword(3));
        }

    }

}
