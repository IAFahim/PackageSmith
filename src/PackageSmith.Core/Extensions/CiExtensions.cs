using System.Text;
using PackageSmith.Data.State;

namespace PackageSmith.Core.Extensions;

public static class CiExtensions
{
    public static bool TryGenerateWorkflow(in this PackageCapabilityState caps, out string yamlContent)
    {
        var sb = new StringBuilder();
        sb.AppendLine("name: Test");
        sb.AppendLine("on: [push, pull_request]");
        sb.AppendLine("jobs:");
        sb.AppendLine("  test:");
        sb.AppendLine("    runs-on: ubuntu-latest");
        sb.AppendLine("    steps:");
        sb.AppendLine("      - uses: actions/checkout@v4");

        if (caps.HasEditModeTests)
        {
            sb.AppendLine("      - name: EditMode Tests");
            sb.AppendLine("        uses: game-ci/unity-test-runner@v4");
            sb.AppendLine("        with:");
            sb.AppendLine("          testMode: editmode");
        }

        if (caps.HasPlayModeTests)
        {
            sb.AppendLine("      - name: PlayMode Tests");
            sb.AppendLine("        uses: game-ci/unity-test-runner@v4");
            sb.AppendLine("        with:");
            sb.AppendLine("          testMode: playmode");
        }

        yamlContent = sb.ToString();
        return true;
    }
}