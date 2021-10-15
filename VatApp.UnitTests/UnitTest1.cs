using NUnit.Framework;

namespace VatApp.UnitTests
{
    public class Tests : VatApp
    {
        /*[SetUp]
        public void Setup()
        {
        }*/

        [Test]
        public void StringToSha512()
        {
            string actual = Sha512_FromString("test"),
                   expected = "4e2cfebd53b5bb42ad0dea693369fd704d6e303b9d2dd7ba22f56595fd35f2b61bcc0506643c9b589fa1473d9053a0b39b4e12d62fd8442aa57960b4d2355b75";

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void IsValidNip()
        {
            Assert.IsTrue(IsValidNip("5170178188"));
        }

        [Test]
        public void IsValidNip_ReturnFalse()
        {
            Assert.IsTrue(!IsValidNip("123456789"));
        }
    }
}