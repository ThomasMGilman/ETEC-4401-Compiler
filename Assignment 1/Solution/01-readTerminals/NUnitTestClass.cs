using NUnit.Framework;

namespace readTerminals
{
    [TestFixture()]
    public class NUnitTestClass
    {
        [Test()]
        public void TestCase () { new Grammar("grammars/good1.txt", false); }
        [Test()]
        public void TestCase2 () { new Grammar("grammars/good2.txt", false); }
        [Test()]
        public void TestCase3 () { new Grammar("grammars/good3.txt", false); }
        [Test()]
        public void TestCase4 () { new Grammar("grammars/good4.txt", false); }
        [Test()]
        public void TestCase5 () { Assert.That( () => {new Grammar("grammars/bad1.txt", false); } , Throws.Exception); }
        [Test()]
        public void TestCase6 () { Assert.That( () => {new Grammar("grammars/bad2.txt", false); } , Throws.Exception); }
        [Test()]
        public void TestCase7 () { Assert.That( () => {new Grammar("grammars/bad3.txt", false); } , Throws.Exception); }
        [Test()]
        public void TestCase8 () { Assert.That( () => {new Grammar("grammars/bad4.txt", false); } , Throws.Exception); }
        [Test()]
        public void TestCase9 () { Assert.That( () => {new Grammar("grammars/bad5.txt", false); } , Throws.Exception); }
        [Test()]
        public void TestCase10 () { Assert.That( () => {new Grammar("grammars/bad6.txt", false); } , Throws.Exception); }
    }
}

