using Gw2_GuildEmblem_Cdn.Utility;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Gw2_GuildEmblem_Cdn.Test
{
    public class CacheTests
    {
        private ITestOutputHelper _output;

        public CacheTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestRegex()
        {
            string[] names = File.ReadAllLines(@"Resources\emblems.txt");

            foreach(string name in names)
            {
                _output.WriteLine(name);
                Assert.Matches(CacheUtility.CACHE_NAME_VALIDATOR, name);
            }
        }

        [Fact]
        public void TestRegexFailure()
        {
            Assert.DoesNotMatch(CacheUtility.CACHE_NAME_VALIDATOR, "1-112_269-473.1a14_128");
            Assert.DoesNotMatch(CacheUtility.CACHE_NAME_VALIDATOR, "1-112_269_473.114_128");
            Assert.DoesNotMatch(CacheUtility.CACHE_NAME_VALIDATOR, "1-1112_269_473.114_128");
            Assert.DoesNotMatch(CacheUtility.CACHE_NAME_VALIDATOR, "null_");
        }
    }
}
