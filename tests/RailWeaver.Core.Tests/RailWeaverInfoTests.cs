using System.Text.RegularExpressions;
using RailWeaver.Core;

namespace RailWeaver.Core.Tests;

public class RailWeaverInfoTests
{
    [Fact]
    public void Version_IsSemVer()
    {
        Assert.Matches(new Regex(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$"), RailWeaverInfo.Version);
    }
}
