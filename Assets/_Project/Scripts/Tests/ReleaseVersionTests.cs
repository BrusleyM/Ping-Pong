using NUnit.Framework;
using PingPong.EditorTools.Build;

namespace PingPong.Tests
{
    public sealed class ReleaseVersionTests
    {
        static readonly ReleaseVersion V = new ReleaseVersion(1, 2, 3, 208);

        [Test]
        public void Develop_BumpsZ()
        {
            Assert.That(V.Next(ReleaseKind.Develop), Is.EqualTo(new ReleaseVersion(1, 2, 4, 209)));
        }

        [Test]
        public void Staging_BumpsY_AndResetsZ()
        {
            Assert.That(V.Next(ReleaseKind.Staging), Is.EqualTo(new ReleaseVersion(1, 3, 0, 209)));
        }

        [Test]
        public void Production_BumpsX_AndResetsYAndZ()
        {
            Assert.That(V.Next(ReleaseKind.Production), Is.EqualTo(new ReleaseVersion(2, 0, 0, 209)));
        }

        [Test]
        public void Code_GoesPastTheHighestReleasedCode()
        {
            Assert.That(V.Next(ReleaseKind.Develop, releasedCode: 300).Code, Is.EqualTo(301), "another branch released higher");
            Assert.That(V.Next(ReleaseKind.Develop, releasedCode: 100).Code, Is.EqualTo(209), "the file is already higher");
        }

        [TestCase("0.0.6", 0, 0, 6)]
        [TestCase("1.4", 1, 4, 0)]
        [TestCase("", 0, 0, 0)]
        [TestCase("1.x.2", 1, 0, 2)]
        public void Parse_ReadsXYZ_WithMissingPartsAsZero(string name, int x, int y, int z)
        {
            Assert.That(ReleaseVersion.Parse(name, 7), Is.EqualTo(new ReleaseVersion(x, y, z, 7)));
        }
    }
}
