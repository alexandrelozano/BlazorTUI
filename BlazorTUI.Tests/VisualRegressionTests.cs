using System.Drawing;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class VisualRegressionTests : BunitContext
{
    public VisualRegressionTests()
    {
        BunitJSModuleInterop keyboardModule = JSInterop.SetupModule("./_content/BlazorTUI/blazorTui.js");
        keyboardModule.Mode = JSRuntimeMode.Loose;
    }

    [Theory]
    [InlineData("clipping-overlays", VisualScenario.ClippingAndOverlays)]
    [InlineData("themes-unicode", VisualScenario.ThemesAndUnicode)]
    [InlineData("responsive-sizing", VisualScenario.ResponsiveSizing)]
    public void TextBufferSnapshotMatchesBaseline(string snapshotName, VisualScenario scenario)
    {
        Screen screen = scenario switch
        {
            VisualScenario.ClippingAndOverlays => CreateClippingAndOverlaysScreen(),
            VisualScenario.ThemesAndUnicode => CreateThemesAndUnicodeScreen(),
            VisualScenario.ResponsiveSizing => CreateResponsiveSizingScreen(),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };

        VisualSnapshotAssert.Matches(snapshotName, VisualSnapshot.CaptureTextBuffer(screen));
    }

    [Fact]
    public void BrowserDomProjectionSnapshotMatchesBaseline()
    {
        Screen screen = CreateThemesAndUnicodeScreen();
        var component = Render<global::BlazorTUI.BlazorTUI>(parameters => parameters
            .Add(instance => instance.Screen, screen)
            .Add(instance => instance.AriaLabel, "Visual regression terminal")
            .Add(instance => instance.AriaDescription, "DOM projection baseline."));

        VisualSnapshotAssert.Matches("browser-dom-projection", VisualSnapshot.CaptureDomProjection(component));
    }

    private static Screen CreateClippingAndOverlaysScreen()
    {
        var screen = new Screen(44, 16);
        var frame = new Frame(
            "overlayFrame",
            "CLIP + OVERLAYS",
            1, 1, 38, 13,
            Frame.BorderStyle.Line,
            Color.Yellow,
            Color.DarkBlue);
        screen.TopContainer.AddContainer(frame);

        var scroll = new ScrollViewer(
            "ordersScroll",
            2, 2, 22, 6,
            Color.Cyan,
            Color.Black);
        scroll.AddControl(new Label(
            "wideOrder",
            "Order 01 - customer and delivery details exceed viewport",
            0, 0, 58,
            Color.Yellow,
            Color.Black));
        scroll.AddControl(new Label(
            "secondOrder",
            "Order 02 - clipped horizontally and vertically",
            0, 1, 48,
            Color.Yellow,
            Color.Black));
        scroll.AddControl(new Label(
            "hiddenOrder",
            "Order 03 - appears after scroll",
            0, 7, 32,
            Color.Yellow,
            Color.Black));
        scroll.ScrollTo(12, 1);
        frame.AddContainer(scroll);

        var actions = new Button(
            "actions",
            "Actions",
            2, 9, 10,
            Color.White,
            Color.DarkGreen);
        frame.AddControl(actions);

        var tooltip = new Tooltip(
            "actionsTip",
            "Tooltip visible",
            "actions",
            13, 9, 16,
            Color.Black,
            Color.Cyan)
        {
            AutoShowOnFocus = false
        };
        tooltip.Show();
        frame.AddControl(tooltip);

        var contextMenu = new ContextMenu(
            "actionsMenu",
            new[]
            {
                new ContextMenuItem("refresh", "Refresh"),
                new ContextMenuItem("archive", "Archive"),
                new ContextMenuItem("disabled", "Disabled") { Enabled = false }
            },
            0, 0, 12,
            Color.Yellow,
            Color.Black,
            new[] { "actions" });
        frame.AddControl(contextMenu);
        contextMenu.OpenAt(8, 8);

        return screen;
    }

    private static Screen CreateThemesAndUnicodeScreen()
    {
        var screen = new Screen(42, 12);
        var frame = new Frame(
            "unicodeFrame",
            "THEME + UNICODE",
            1, 1, 38, 9,
            Frame.BorderStyle.DoubleLine,
            Color.Yellow,
            Color.DarkBlue);
        screen.TopContainer.AddContainer(frame);

        frame.AddControl(new Label(
            "unicodeLabel",
            "Cafe\u0301 🙂 漢字",
            2, 2, 18,
            Color.Yellow,
            Color.DarkBlue)
        {
            ThemeRole = TuiThemeRole.Accent
        });
        frame.AddControl(new TextBox(
            "unicodeInput",
            "e\u0301🙂漢",
            2, 4, 12,
            Color.White,
            Color.Black)
        {
            ThemeRole = TuiThemeRole.Input,
            ScreenReaderDescription = "Unicode input baseline."
        });
        frame.AddControl(new ToggleSwitch(
            "enabled",
            "Unicode mode",
            true,
            2, 6, 20,
            Color.White,
            Color.Black)
        {
            ThemeRole = TuiThemeRole.Action
        });
        frame.AddControl(new StatusBar(
            "status",
            "Theme: HighContrast",
            2, 7, 32,
            Color.Black,
            Color.Cyan));

        screen.ApplyTheme(TuiTheme.HighContrast);
        screen.SetFocus("unicodeInput");
        return screen;
    }

    private static Screen CreateResponsiveSizingScreen()
    {
        var screen = new Screen(84, 46);
        var frame = new Frame(
            "responsiveFrame",
            "RESPONSIVE 84x46",
            2, 2, 78, 40,
            Frame.BorderStyle.Line,
            Color.Yellow,
            Color.DarkBlue);
        screen.TopContainer.AddContainer(frame);

        frame.AddControl(new Label(
            "headline",
            "Rows above forty use the same CSS variable grid.",
            3, 3, 54,
            Color.White,
            Color.DarkBlue));
        frame.AddControl(new Label(
            "bottomRight",
            "bottom-right marker",
            55, 36, 19,
            Color.Cyan,
            Color.DarkBlue));
        frame.AddControl(new ProgressBar(
            "progress",
            ProgressBar.ProgressBarType.Solid,
            3, 6, 36,
            67, 100, true,
            Color.Yellow,
            Color.Black));
        frame.AddControl(new Slider(
            "slider",
            0, 100, 40, 10,
            3, 9, 24,
            Color.White,
            Color.Black));

        return screen;
    }

    public enum VisualScenario
    {
        ClippingAndOverlays,
        ThemesAndUnicode,
        ResponsiveSizing
    }

    private static class VisualSnapshot
    {
        public static string CaptureTextBuffer(Screen screen)
        {
            ArgumentNullException.ThrowIfNull(screen);
            screen.Render();

            var builder = new StringBuilder();
            builder.AppendLine(CultureInfo.InvariantCulture, $"screen: {screen.Width}x{screen.Height}");
            builder.AppendLine("text:");
            for (int y = 0; y < screen.Rows.Count; y++)
            {
                builder.Append(CultureInfo.InvariantCulture, $"{y:00}|");
                foreach (Cell cell in screen.Rows[y].ReadOnlyCells)
                    builder.Append(VisibleCharacter(cell));
                builder.AppendLine("|");
            }

            builder.AppendLine("styles:");
            for (int y = 0; y < screen.Rows.Count; y++)
            {
                foreach (StyleRun run in GetStyleRuns(screen.Rows[y].ReadOnlyCells))
                {
                    builder
                        .Append(CultureInfo.InvariantCulture, $"{y:00},{run.Start:00}-{run.End:00}:")
                        .Append(CultureInfo.InvariantCulture, $" fg={run.ForeColor} bg={run.BackgroundColor} deco={run.Decoration}")
                        .Append(CultureInfo.InvariantCulture, $" text={Escape(run.Text)}")
                        .AppendLine();
                }
            }

            return Normalize(builder.ToString());
        }

        public static string CaptureDomProjection(IRenderedComponent<global::BlazorTUI.BlazorTUI> component)
        {
            ArgumentNullException.ThrowIfNull(component);

            var builder = new StringBuilder();
            IElement grid = component.Find(".gridfs");
            builder.AppendLine("grid:");
            builder.AppendLine(CultureInfo.InvariantCulture, $"role={grid.GetAttribute("role")}");
            builder.AppendLine(CultureInfo.InvariantCulture, $"aria-label={grid.GetAttribute("aria-label")}");
            builder.AppendLine(CultureInfo.InvariantCulture, $"aria-describedby={NormalizeDynamicIds(NormalizeWhitespace(grid.GetAttribute("aria-describedby") ?? ""))}");
            builder.AppendLine(CultureInfo.InvariantCulture, $"style={NormalizeWhitespace(grid.GetAttribute("style") ?? "")}");
            builder.AppendLine(CultureInfo.InvariantCulture, $"shortcuts={NormalizeWhitespace(grid.GetAttribute("aria-keyshortcuts") ?? "")}");

            builder.AppendLine("control-summaries:");
            builder.AppendLine(NormalizeWhitespace(component.Find("[aria-label=\"Control summaries\"]").TextContent));

            builder.AppendLine("first-visible-tiles:");
            foreach (IElement tile in component.FindAll(".tilefs")
                .Where(tile => !string.IsNullOrWhiteSpace(tile.TextContent))
                .Take(16))
            {
                builder
                    .Append(Escape(tile.TextContent))
                    .Append(CultureInfo.InvariantCulture, $" style={NormalizeWhitespace(tile.GetAttribute("style") ?? "")}")
                    .AppendLine();
            }

            return Normalize(builder.ToString());
        }

        private static IEnumerable<StyleRun> GetStyleRuns(IReadOnlyList<Cell> cells)
        {
            if (cells.Count == 0)
                yield break;

            int start = 0;
            Cell current = cells[0];
            var text = new StringBuilder(VisibleCharacter(current));
            for (int index = 1; index < cells.Count; index++)
            {
                Cell cell = cells[index];
                if (HasSameVisual(current, cell))
                {
                    text.Append(VisibleCharacter(cell));
                    continue;
                }

                yield return CreateRun(start, index - 1, current, text.ToString());
                start = index;
                current = cell;
                text.Clear();
                text.Append(VisibleCharacter(cell));
            }

            yield return CreateRun(start, cells.Count - 1, current, text.ToString());
        }

        private static StyleRun CreateRun(int start, int end, Cell cell, string text)
            => new(
                start,
                end,
                ToHex(cell.ForeColor),
                ToHex(cell.BackgroundColor),
                cell.Decoration,
                text);

        private static bool HasSameVisual(Cell first, Cell second)
            => first.ForeColor.ToArgb() == second.ForeColor.ToArgb() &&
                first.BackgroundColor.ToArgb() == second.BackgroundColor.ToArgb() &&
                first.Decoration == second.Decoration &&
                first.IsVisible == second.IsVisible &&
                first.BackgroundImage == second.BackgroundImage &&
                first.ScaleX == second.ScaleX &&
                first.ScaleY == second.ScaleY;

        private static string VisibleCharacter(Cell cell)
            => !cell.IsVisible || string.IsNullOrEmpty(cell.Character) ? " " : cell.Character;

        private static string ToHex(Color color)
            => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        private static string Escape(string value)
            => value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\r", "\\r", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal);

        private static string NormalizeWhitespace(string value)
            => string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        private static string NormalizeDynamicIds(string value)
            => Regex.Replace(value, "blazortui-[0-9a-f]+-", "blazortui-id-", RegexOptions.IgnoreCase);

        private static string Normalize(string value)
            => value.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd() + "\n";

        private readonly record struct StyleRun(
            int Start,
            int End,
            string ForeColor,
            string BackgroundColor,
            Cell.TextDecoration Decoration,
            string Text);
    }

    private static class VisualSnapshotAssert
    {
        public static void Matches(string snapshotName, string actual)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(snapshotName);
            ArgumentNullException.ThrowIfNull(actual);

            string snapshotPath = GetSnapshotPath(snapshotName);
            if (Environment.GetEnvironmentVariable("BLAZORTUI_UPDATE_VISUAL_SNAPSHOTS") == "1")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
                File.WriteAllText(snapshotPath, actual, Encoding.UTF8);
            }

            if (!File.Exists(snapshotPath))
            {
                Assert.Fail($"Missing visual snapshot '{snapshotName}'. Create {snapshotPath} with:\n{actual}");
            }

            string expected = File.ReadAllText(snapshotPath, Encoding.UTF8)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .TrimEnd() + "\n";
            Assert.Equal(expected, actual);
        }

        private static string GetSnapshotPath(string snapshotName)
        {
            string? directory = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(directory))
            {
                if (File.Exists(Path.Combine(directory, "BlazorTUI.Tests.csproj")))
                    return Path.Combine(directory, "VisualSnapshots", snapshotName + ".snap");

                directory = Directory.GetParent(directory)?.FullName;
            }

            return Path.Combine(AppContext.BaseDirectory, "VisualSnapshots", snapshotName + ".snap");
        }
    }
}
