using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MouseEngine;
using System.Linq;

namespace MouseEngineTest
{
    [TestClass]
    public class RangeTester
    {
        [TestMethod]
        [TestCategory("Range")]
        public void TestNoIntersect()
        {
            Range r1 = new Range(1, 3);
            Range r2 = new Range(4, 18);
            Assert.AreEqual(false, r1.intersects(r2));
        }
        [TestMethod]
        [TestCategory("Range")]
        public void TestNoIntersectInverse()
        {
            Range r1 = new Range(1, 3);
            Range r2 = new Range(4, 18);
            Assert.AreEqual(false, r2.intersects(r1));
        }
        [TestMethod]
        [TestCategory("Range")]
        public void TestIntersect()
        {
            Range r1 = new Range(1, 4);
            Range r2 = new Range(4, 13);
            Assert.AreEqual(true, r1.intersects(r2));
        }
        [TestMethod]
        [TestCategory("Range")]
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
            return new MultiStringMatcher(new[] { "1", "2" }, "sentance 1", "sentance 2", "sentance 3");
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
            Assert.AreEqual(true, m.match("sentance 1[phrase1]sentance 2[phrase2]sentance 3"));
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
            Assert.IsTrue(m.match("|1+(1+1)|"));
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
        /// <summary>
        /// Test outer parenthesis
        /// </summary>
        [TestMethod]
        [TestCategory("Matcher")]
        public void testArgumentOuterParenthesis()
        {
            Matcher m = getMatcherPlus();
            Assert.IsTrue(m.match("((|1+2|))"));
            Assert.AreEqual(
                new Dictionary<string, string>()
                {
                    {"1","1" },
                    {"2","2" }
                }.toAdvancedString(),
                m.getArgs().toAdvancedString()
                );
        }

        [TestMethod]
        [TestCategory("Matcher")]
        public void TestMatcherBeginning1()
        {
            Matcher m = getMatcherPlus();

            if (m.match("(hello)|1+2|")) {
                Assert.IsFalse(true, m.getArgs().toAdvancedString());
            }
        }

        [TestMethod]
        [TestCategory("Matcher")]
        public void TestMatcherBeginning2()
        {
            Matcher m = getMatcherPlus();
            Assert.IsFalse(m.match("(hello) and |1+2|"));


        }
        [TestMethod]
        [TestCategory("Matcher")]
        public void TestMatcherBeginning3()
        {
            Matcher m = getMatcherPlus();
            Assert.IsFalse(m.match("bbb|1+2"));

        }

        /// <summary>
        /// the matcher (something) ^ (something)
        /// </summary>
        /// <returns></returns>
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
        [TestMethod]
        [TestCategory("Matcher")]
        public void testBlankSpaceParinthesies()
        {
            Matcher m = getMatcherSpace();
            Assert.IsTrue(m.match("((1^2))"));
            Assert.AreEqual(
                new Dictionary<string, string>()
                {
                    {"1","1" },
                    {"2","2" }
                }.toAdvancedString(),
                m.getArgs().toAdvancedString()
                );
        }

        [TestMethod]
        [TestCategory("Matcher")]
        public void TestWhiteSpaceStrip1()
        {
            Matcher m = getMatcherPlus();
            Assert.IsTrue(m.match("   |1+2|"));
            Assert.AreEqual(
                new Dictionary<string, string>()
                {
                    {"1","1" },
                    {"2","2" }
                }.toAdvancedString(),
                m.getArgs().toAdvancedString()
                );

        }
        [TestMethod]
        [TestCategory("Matcher")]
        public void TestWhiteSpaceStrip2()
        {
            Matcher m = getMatcherPlus();
            Assert.IsTrue(m.match("|1+2|    "));
            Assert.AreEqual(
                new Dictionary<string, string>()
                {
                    {"1","1" },
                    {"2","2" }
                }.toAdvancedString(),
                m.getArgs().toAdvancedString()
                );

        }

        [TestMethod]
        [TestCategory("Matcher")]
        public void testEnd1()
        {
            Matcher m = getMatcherPlus();
            Assert.IsFalse(m.match("|1+2|a"));
        }

        [TestMethod]
        [TestCategory("Matcher")]
        public void testEnd2()
        {
            Matcher m = getMatcherPlus();
            Assert.IsFalse(m.match("|1+2|(a)"));
        }

        [TestMethod]
        [TestCategory("Matcher")]
        public void testEnd3()
        {
            Matcher m = getMatcherPlus();
            Assert.IsFalse(m.match("|1+2|a(a)"));
        }

        [TestMethod]
        [TestCategory("Matcher")]
        public void TestEndParenthesisArgs()
        {
            Matcher m = getMatcherSpace();
            Assert.IsTrue(m.match("(ab)^(bc)"));
            Assert.AreEqual(
                new Dictionary<string, string> {
                    {"1","(ab)"}, {"2","(bc)" }
                    }.toAdvancedString(),
                m.getArgs().toAdvancedString()
                );
        }

        [TestMethod]
        [TestCategory("Matcher")]
        public void testHalfWayParenthesis()
        {
            Matcher m = getMatcherSpace();
            Assert.IsTrue(m.match("aaa (ab) ^ fff(ab^ql )"));
            Assert.AreEqual(
                new Dictionary<string, string>
                {
                    {"1","aaa (ab) " },  //The spaces are important, the real
                    {"2"," fff(ab^ql )" }//program usually strip again every
                }.toAdvancedString(), //iteration, but now we have the unstripped
                m.getArgs().toAdvancedString() //raw format

                );
        }
    }
    [TestClass]
    public class UtilityMatchers {

        [TestMethod]
        [TestCategory("Util")]
        public void testGetSections()
        {
            List<Range> output= StringUtil.getProtectedParts("er (was) eens");
            Assert.AreEqual(1, output.Count, "To many items");
            Assert.AreEqual(3, output[0].start, "start is wrong");
            Assert.AreEqual(7, output[0].end, "end is wrong");
        }
        [TestMethod]
        [TestCategory("Util")]
        public void testGetInverse()
        {
            Range[] output = ArrayUtil.getRangeInverse(new []
            {
                new Range(2, 3),
                new Range(6, 10)
            },15).ToArray();
            Assert.AreEqual(3, output.Length, "length is wrong");
            Assert.AreEqual(new[]
            {
                new Range(0,1),
                new Range(4,5),
                new Range(11,14)
            }.toAdvancedString(),
            output.toAdvancedString());

        }

        [TestMethod]
        [TestCategory("Util")]
        public void testGetInsideStrings()
        {
            string testString = "01234567890123456789";
            string[] output = StringUtil.getInsideStrings(
                new Range[]
                {
                    new Range(2,8)
                },
                testString
                );
            Assert.AreEqual(output.Length, 1);
            Assert.AreEqual(
                new[] { "2345678" }.toAdvancedString(),
                output.toAdvancedString()
                );
        }
        [TestMethod]
        [TestCategory("Util")]
        public void testNoReduce2()
        {
            string testStr = "er (was)";
            List<Range> parts = StringUtil.getProtectedParts(testStr, true);
            Assert.AreEqual(1, parts.Count, "Length should be 0");
            Assert.AreEqual(new[] { new Range(3, 7) }.toAdvancedString(),
                parts.ToArray().toAdvancedString());
        }
        [TestMethod]
        [TestCategory("Util")]
        public void testNoReduce1()
        {
            string testStr = "er was";
            List<Range> parts = StringUtil.getProtectedParts(testStr, true);
            Assert.AreEqual(0, parts.Count, "Length should be 0");
        }

        [TestMethod]
        [TestCategory("Util")]
        public void testReduce1()
        {

            string testStr = "(er (was))";
            List<Range> parts = StringUtil.getProtectedParts(testStr, true);
            Assert.AreEqual(1, parts.Count, "Length should be 0");
            Assert.AreEqual(new[] { new Range(4, 8) }.toAdvancedString(),
                parts.ToArray().toAdvancedString());
        }
        [TestMethod]
        [TestCategory("Util")]
        public void testReduce2()
        {

            string testStr = "(((er (was))))";
            List<Range> parts = StringUtil.getProtectedParts(testStr, true);
            Assert.AreEqual(1, parts.Count, "Length should be 0");
            Assert.AreEqual(new[] { new Range(6, 10) }.toAdvancedString(),
                parts.ToArray().toAdvancedString());
        }

        [TestMethod]
        [TestCategory("Util")]
        public void testUnprotectedParts()
        {
            string testStr = "(er(f)gha)";
            List<Range> parts = StringUtil.getUnprotectedParts(testStr, true);
            Assert.AreEqual(
                new[]
                {
                    new Range(1, 2),
                    new Range(6, 8)
                }.toAdvancedString(),
                parts.ToArray().toAdvancedString()

                );

        }

        [TestMethod][TestCategory("Util")]
        public void testUnpotectedParts2()
        {
            string testStr = "  er (was) eens";
            List<Range> parts = StringUtil.getUnprotectedParts(testStr, true);
            Assert.AreEqual(
                new[]
                {
                    new Range(2,4),
                    new Range(10,14)
                }.toAdvancedString(),
                parts.ToArray().toAdvancedString()
                );

            //A bug gives 0-4,10-14
        }

        [TestMethod][
            TestCategory("Util")]
        public void testGetInverse1()
        {
            List<Range> output = ArrayUtil.getRangeInverse(new[]
            {
                new Range(4,5),
                new Range(9,12)
            }, 2, 15, true);

            Assert.AreEqual(output.ToArray().toAdvancedString(),
                new[]
                {
                    new Range(2,3),
                    new Range(6,8),
                    new Range(13,14)
                }.toAdvancedString());
        }

        [TestMethod]
        [TestCategory("Util")]
        public void TestGetEmpty1()
        {
            int[] empty = { };
            Assert.IsTrue(empty.isEmpty());
        }

        [TestMethod]
        [TestCategory("Util")]
        public void TestGetEmpty2()
        {
            int[] notEmpty = { 1 };
            Assert.IsFalse(notEmpty.isEmpty());
        }


        


    }
}
