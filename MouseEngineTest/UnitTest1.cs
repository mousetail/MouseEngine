using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MouseEngine;

namespace MouseEngineTest
{
    [TestClass]
    public class RangeTester
    {
        [TestMethod]
        public void TestNoIntersect()
        {
            Range r1 = new Range(1, 3);
            Range r2 = new Range(4, 18);
            Assert.AreEqual(false, r1.intersects(r2));
        }
        [TestMethod]
        public void TestNoIntersectInverse()
        {
            Range r1 = new Range(1, 3);
            Range r2 = new Range(4, 18);
            Assert.AreEqual(false, r2.intersects(r1));
        }
        [TestMethod]
        public void TestIntersect()
        {
            Range r1 = new Range(1, 4);
            Range r2 = new Range(4, 13);
            Assert.AreEqual(true, r1.intersects(r2));
        }
        [TestMethod]
        public void TestIntersectInverse()
        {
            Range r1 = new Range(1, 4);
            Range r2 = new Range(4, 14);
            Assert.AreEqual(true, r2.intersects(r1));
        }

    }

    [TestClass]
    public class MatcherTester
    {
        public Matcher getMatcher()
        {
            return new MultiStringMatcher(new[] { "1", "2" }, "sentance 1","sentance 2","sentance 3");
        }

        [TestMethod]
        [TestCategory("Matcher")]
        public void testMatch1()
        {
            Assert.AreEqual(true, getMatcher().match("sentance 1dskfjsentance 2dfkljdsentance 3"));
        }
        [TestMethod]
        [TestCategory("Matcher")]
        public void testNoMatch1()
        {
            Assert.AreEqual(false, getMatcher().match("sentance 1sdfjkssentance 2sdfjks"));
        }

        [TestMethod]
        [TestCategory("Matcher")]
        public void testArgumentsBasic()
        {
            Matcher m = getMatcher();
            Assert.AreEqual(true,m.match("sentance 1[phrase1]sentance 2[phrase2]sentance 3"));
            Dictionary<string, string> expected = new Dictionary<string, string> { { "1", "[phrase1]" },
                {"2","[phrase2]" } };
            Dictionary<string, string> actual = m.getArgs();
            Assert.AreEqual(expected.toAdvancedString(), actual.toAdvancedString());
        }

        public Matcher getMatcherPlus()
        {
            return new MultiStringMatcher(new string[] { "1", "2" }, "|", "+", "|");
        }

        [TestMethod]
        [TestCategory("Matcher")]
        public void testArgumentParenthesis1()
        {
            Matcher m = getMatcherPlus();
            Assert.IsTrue( m.match("|1+(1+1)|"));
            Assert.AreEqual(new Dictionary<string, string> { { "1", "1" }, { "2", "(1+1)" } }.toAdvancedString(),
                m.getArgs().toAdvancedString());
        }
        [TestMethod]
        [TestCategory("Matcher")]
        public void testArgumentParenthesis2()
        {
            Matcher m = getMatcherPlus();
            Assert.IsTrue(m.match("|(1+1)+1|"));
            Assert.AreEqual(new Dictionary<string, string> { { "1", "(1+1)" }, { "2", "1" } }.toAdvancedString(),
                m.getArgs().toAdvancedString());
        }

        public Matcher getMatcherSpace()
        {
            return new MultiStringMatcher(new string[] { "1", "2" }, "", "^", "");
        }

        [TestMethod]
        [TestCategory("Matcher")]
        public void testBlankSpace1()
        {
            Matcher m = getMatcherSpace();
            Assert.IsTrue(m.match("1^2"));
            Assert.AreEqual(new Dictionary<string, string>() { { "1", "1" }, { "2", "2" } }
            .toAdvancedString(),
                m.getArgs().toAdvancedString());
        }
        [TestMethod]
        [TestCategory("Matcher")]
        public void testBlankSpace2()
        {
            Matcher m = getMatcherSpace();
            Assert.IsTrue(m.match("1^(2^3)"));
            Assert.AreEqual(new Dictionary<string, string>()
            { {"1","1" }, {"2","(2^3)" } }.toAdvancedString(),
            m.getArgs().toAdvancedString());
        }

    }
}
