using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace RCS.Cogo.Wpf.Views;

/// <summary>
/// Lightweight syntax highlighter that colorizes a WPF <see cref="RichTextBox"/>
/// using COGO / Piping Script keyword patterns.
///
/// Usage:
///   1. Replace the plain <c>TextBox</c> in XAML with a <c>RichTextBox</c>.
///   2. Attach via <c>CogoSyntaxHighlighter.Attach(richTextBox)</c>.
///   3. The highlighter re-colorizes after each keystroke using a short debounce.
///
/// Token categories (applied in priority order):
///   Comment   — // … to end of line                  #6A9955 (green)
///   Keyword   — TRAVERSE, CURVE, PT, POB, …          #569CD6 (blue)
///   Direction — N/S/E/W bearing tokens                #CE9178 (orange)
///   Number    — integers and decimals                 #B5CEA8 (light green)
///   Operator  — = , ;                                 #D4D4D4 (light grey)
/// </summary>
public static class CogoSyntaxHighlighter
{
    // ── Token colours ─────────────────────────────────────────────────────────
    private static readonly SolidColorBrush BrComment   = Freeze(Color.FromRgb(0x6A, 0x99, 0x55));
    private static readonly SolidColorBrush BrKeyword   = Freeze(Color.FromRgb(0x56, 0x9C, 0xD6));
    private static readonly SolidColorBrush BrDirection = Freeze(Color.FromRgb(0xCE, 0x91, 0x78));
    private static readonly SolidColorBrush BrNumber    = Freeze(Color.FromRgb(0xB5, 0xCE, 0xA8));
    private static readonly SolidColorBrush BrDefault   = Freeze(Color.FromRgb(0xCC, 0xCC, 0xCC));

    private static SolidColorBrush Freeze(Color c)
    { var b = new SolidColorBrush(c); b.Freeze(); return b; }

    // ── Token patterns (in match priority order) ──────────────────────────────
    private static readonly (Regex Rx, SolidColorBrush Color)[] Tokens = new[]
    {
        // Comments // … to EOL — must be first
        (new Regex(@"//[^\n]*",           RegexOptions.Compiled), BrComment),

        // COGO/Piping keywords (case-insensitive)
        (new Regex(@"\b(?:TRAVERSE|CURVE|LEFT|RIGHT|PT|POB|POC|POR|INVERSE|" +
                       @"ANGLE|AREA|MAPCHECK|CLOSURE|STORE|RECALL|OUTPUT|OFF|ON|" +
                       @"PIPE|SEWERLINE|WATERLINE|STORM|GAS|ELECTRIC|CABLE|TELECOM|" +
                       @"CONNECT|FROM|TO|DIAMETER|MATERIAL|SLOPE|DEPTH|OFFSET|" +
                       @"STATION|BACKSIGHT|FORESIGHT|PRISM|SIDESHOT|HI|HR|" +
                       @"NORTH|SOUTH|EAST|WEST|FEET|METER|CHAIN|LINK|ROD)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase), BrKeyword),

        // Bearing direction N/S + degrees + E/W  e.g. N45.3000E  s12.1530w
        (new Regex(@"\b[nNsS]\d+\.\d+[eEwW]\b", RegexOptions.Compiled), BrDirection),

        // Standalone cardinal letters used in bearing context
        (new Regex(@"\b[NSEW]\b", RegexOptions.Compiled), BrDirection),

        // Numbers (integers and decimals)
        (new Regex(@"\b\d+(?:\.\d+)?\b",  RegexOptions.Compiled), BrNumber),
    };

    // ── Attach ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Attaches the highlighter to a <see cref="RichTextBox"/>.
    /// Call once after the control is created.
    /// </summary>
    public static void Attach(RichTextBox rtb)
    {
        // Run once on attach to colour any pre-existing content
        Colorize(rtb);

        // Use a short-interval debounce timer to avoid re-painting on every keystroke
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        timer.Tick += (_, _) => { timer.Stop(); Colorize(rtb); };

        rtb.TextChanged += (_, _) =>
        {
            // Reset debounce on every keystroke
            timer.Stop();
            timer.Start();
        };
    }

    // ── Core colorizer ────────────────────────────────────────────────────────

    private static void Colorize(RichTextBox rtb)
    {
        // Suspend layout to avoid a flicker per character
        rtb.IsUndoEnabled = false;  // also suppresses undo history bloat

        var doc      = rtb.Document;
        var fullStart = doc.ContentStart;
        var fullEnd   = doc.ContentEnd;

        // 1. Strip all existing foreground runs — reset to default colour
        var clearRange = new TextRange(fullStart, fullEnd);
        clearRange.ApplyPropertyValue(TextElement.ForegroundProperty, BrDefault);

        // 2. Walk each paragraph (= line) and apply token colours
        foreach (var block in doc.Blocks)
        {
            if (block is not Paragraph para) continue;

            var lineStart = para.ContentStart;
            var lineEnd   = para.ContentEnd;
            var lineText  = new TextRange(lineStart, lineEnd).Text;

            // Apply each token type
            foreach (var (rx, color) in Tokens)
            {
                foreach (Match m in rx.Matches(lineText))
                {
                    // Translate char offsets in lineText to TextPointers
                    var ts = GetPointerAtOffset(lineStart, m.Index);
                    var te = GetPointerAtOffset(lineStart, m.Index + m.Length);
                    if (ts == null || te == null) continue;

                    var range = new TextRange(ts, te);
                    range.ApplyPropertyValue(TextElement.ForegroundProperty, color);
                }
            }
        }

        rtb.IsUndoEnabled = true;
    }

    /// <summary>
    /// Returns a <see cref="TextPointer"/> offset by <paramref name="charOffset"/>
    /// character positions from <paramref name="start"/>, skipping non-text symbols.
    /// </summary>
    private static TextPointer? GetPointerAtOffset(TextPointer start, int charOffset)
    {
        var nav = start;
        int remaining = charOffset;

        while (nav != null && remaining > 0)
        {
            if (nav.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
            {
                string run   = nav.GetTextInRun(LogicalDirection.Forward);
                int    avail = run.Length;
                if (avail >= remaining)
                    return nav.GetPositionAtOffset(remaining);
                remaining -= avail;
            }
            nav = nav.GetNextContextPosition(LogicalDirection.Forward);
        }
        return nav;
    }
}
