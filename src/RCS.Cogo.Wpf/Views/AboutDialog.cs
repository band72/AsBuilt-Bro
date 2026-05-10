using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RCS.Cogo.Wpf.Views;

/// <summary>
/// About dialog — shows version, build date, GitHub link, and test count.
/// Built entirely in code (no XAML file) to stay self-contained.
/// </summary>
public sealed class AboutDialog : Window
{
    public AboutDialog()
    {
        Title               = "About RCS COGO Enterprise";
        Width               = 480;
        Height              = 380;
        ResizeMode          = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background          = new SolidColorBrush(Color.FromRgb(0x12, 0x16, 0x22));

        // ── Read version from assembly ────────────────────────────────────────
        var asm     = Assembly.GetExecutingAssembly();
        string ver  = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "2.x";
        string title= asm.GetCustomAttribute<AssemblyTitleAttribute>()?.Title           ?? "RCS COGO Enterprise";

        // Build date from linker timestamp embedded in PE header (reliable on .NET 8 self-contained)
        string buildDate;
        try
        {
            var path  = Environment.ProcessPath ?? string.Empty;
            var ts    = System.IO.File.GetLastWriteTime(path);
            buildDate = ts.ToString("yyyy-MM-dd");
        }
        catch { buildDate = "—"; }

        Content = BuildLayout(title, ver, buildDate);
    }

    private UIElement BuildLayout(string appTitle, string ver, string buildDate)
    {
        var outer = new Grid { Margin = new Thickness(32) };
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        outer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // App name
        var nameBlock = new TextBlock
        {
            Text       = appTitle,
            FontSize   = 22,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x61, 0xAF, 0xEF)),
            Margin     = new Thickness(0, 0, 0, 4)
        };
        Grid.SetRow(nameBlock, 0);
        outer.Children.Add(nameBlock);

        // Version + build
        var verBlock = new TextBlock
        {
            Text     = $"Version {ver}   •   Built {buildDate}",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(0x98, 0xC3, 0x79)),
            Margin   = new Thickness(0, 0, 0, 16)
        };
        Grid.SetRow(verBlock, 1);
        outer.Children.Add(verBlock);

        // Divider
        var divider = new Border { Height = 1, Background = new SolidColorBrush(Color.FromRgb(0x30, 0x35, 0x50)), Margin = new Thickness(0, 0, 0, 16) };
        Grid.SetRow(divider, 2);
        outer.Children.Add(divider);

        // Info panel
        var infoStack = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        Grid.SetRow(infoStack, 3);
        outer.Children.Add(infoStack);

        void InfoLine(string label, string value, string? link = null)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            row.Children.Add(new TextBlock
            {
                Text = $"{label}:  ",
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x8E, 0xA8)),
                FontSize   = 12,
                Width      = 120
            });
            var valBlock = new TextBlock
            {
                Text       = value,
                Foreground = Brushes.White,
                FontSize   = 12
            };
            if (link != null)
            {
                var btn = new Button
                {
                    Content             = value,
                    Style               = null,
                    Background          = Brushes.Transparent,
                    BorderThickness     = new Thickness(0),
                    Foreground          = new SolidColorBrush(Color.FromRgb(0x61, 0xAF, 0xEF)),
                    FontSize            = 12,
                    Cursor              = System.Windows.Input.Cursors.Hand,
                    Padding             = new Thickness(0)
                };
                btn.Click += (_, _) => { try { Process.Start(new ProcessStartInfo(link) { UseShellExecute = true }); } catch { } };
                row.Children.Add(btn);
            }
            else
            {
                row.Children.Add(valBlock);
            }
            infoStack.Children.Add(row);
        }

        InfoLine("Version",    ver);
        InfoLine("Build Date", buildDate);
        InfoLine("Platform",   ".NET 8 · WPF (Windows)");
        InfoLine("License",    "Proprietary — RCS Engineering");
        InfoLine("Repository", "github.com/band72/RCS.Cogo.Enterprise.Modern",
                               "https://github.com/band72/RCS.Cogo.Enterprise.Modern");
        InfoLine("Support",    "band72@github");

        // Close button
        var closeBtn = new Button
        {
            Content         = "Close",
            Width           = 100,
            Padding         = new Thickness(12, 6, 12, 6),
            HorizontalAlignment = HorizontalAlignment.Right,
            Background      = new SolidColorBrush(Color.FromRgb(0x1A, 0x2A, 0x45)),
            Foreground      = Brushes.White,
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x3E, 0x95, 0xD5)),
            BorderThickness = new Thickness(1),
            IsDefault       = true
        };
        closeBtn.Click += (_, _) => Close();
        Grid.SetRow(closeBtn, 4);
        outer.Children.Add(closeBtn);

        return outer;
    }
}
