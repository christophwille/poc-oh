using Meziantou.Framework.InlineSnapshotTesting;
using Microsoft.EntityFrameworkCore;

namespace TestProject1;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var context = new BlogContext();
        var script = context.Database.GenerateCreateScript();
        // InlineSnapshot.Validate(script, /*lang=sql*/ ""); // Starting point

        InlineSnapshot.Validate(script, /*lang=sql*/ "");
    }
}