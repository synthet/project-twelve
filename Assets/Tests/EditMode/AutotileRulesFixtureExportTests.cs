using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

/// <summary>
/// Exports autotile rule tables to <c>tools/tile-viz/data/autotile-rules.*.json</c> so the
/// Node tile-viz port loads the same patterns as <see cref="AutotileRuleTables"/>.
/// </summary>
public sealed class AutotileRulesFixtureExportTests
{
    [Test]
    public void ExportGroundAndCoverRulesJson()
    {
        WriteRules("autotile-rules.ground.json", AutotileRuleTables.Ground, AutotileRuleTables.GroundSpriteCount);
        WriteRules("autotile-rules.cover.json", AutotileRuleTables.Cover, AutotileRuleTables.GroundSpriteCount);
        Assert.IsTrue(File.Exists(Path.Combine(DataDir(), "autotile-rules.ground.json")));
        Assert.IsTrue(File.Exists(Path.Combine(DataDir(), "autotile-rules.cover.json")));
    }

    [Test]
    public void ExportedGroundRuleCountMatchesEngine()
    {
        Assert.AreEqual(32, AutotileRuleTables.Ground.Count);
        Assert.AreEqual(6, AutotileRuleTables.Cover.Count);
    }

    private static void WriteRules(string fileName, IReadOnlyList<AutotileRule> rules, int groundSpriteCount)
    {
        var ci = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.AppendFormat(ci, "  \"format\": \"project-twelve/autotile-rules/v1\",\n");
        sb.AppendFormat(ci, "  \"fallbackSpriteId\": \"{0}\",\n", AutotileRuleTables.FallbackSpriteId);
        sb.AppendFormat(ci, "  \"groundSpriteCount\": {0},\n", groundSpriteCount);
        sb.Append("  \"rules\": [\n");
        for (int i = 0; i < rules.Count; i++)
        {
            AutotileRule rule = rules[i];
            sb.Append("    {");
            sb.AppendFormat(ci, "\"spriteId\":\"{0}\",\"weight\":{1},\"pattern\":[", rule.SpriteId, rule.Weight);
            int[,] mask = rule.ToMask();
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    if (x > 0 || y > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append(mask[x, y].ToString(ci));
                }
            }

            sb.Append("]}");
            if (i < rules.Count - 1)
            {
                sb.Append(',');
            }

            sb.Append('\n');
        }

        sb.Append("  ]\n}\n");
        Directory.CreateDirectory(DataDir());
        File.WriteAllText(Path.Combine(DataDir(), fileName), sb.ToString());
    }

    private static string DataDir()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "tools", "tile-viz", "data"));
    }
}
