using System.Reflection;

using App.Mobile.Android.Localization;

namespace App.Mobile.Android.Foundation.Tests;

public sealed class MobileUiTextLocalizationTests
{
    [Fact]
    public void UserFacingText_does_not_contain_common_mojibake_markers()
    {
        var markers = new[]
        {
            "\u0420\u040e",
            "\u0420\u045f",
            "\u0420\u040c",
            "\u0420\u040e",
            "\u0420\u0491",
            "\u0420\u00b5",
            "\u0420\u00b0",
            "\u0420\u0451",
            "\u0420\u0455",
            "\u0421\u0402",
            "\u0421\u0403",
            "\u0421\u201a",
            "\u0421\u040a",
            "\u0421\u2021",
            "\u0421\u2039",
            "\u0421\u040f",
            "\u00d0",
            "\u00d1",
            "\u00c2",
            "\ufffd"
        };

        var values = typeof(MobileUiText)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(string))
            .Select(field => new
            {
                field.Name,
                Value = (string?)field.GetValue(null)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Value));

        foreach (var item in values)
        {
            foreach (var marker in markers)
            {
                Assert.DoesNotContain(marker, item.Value, StringComparison.Ordinal);
            }
        }
    }
}