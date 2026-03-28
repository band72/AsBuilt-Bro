using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RCS.Cogo.App.State;
using RCS.Data;

namespace RCS.Cogo.Wpf.Views
{
    public partial class EditCogoCodeWindow : Window
    {
        private readonly CogoCode _originalCode;
        public string ResultAction { get; private set; } = "None";

        // Resolved path to the RCS_Blocks folder (shared with ShellViewModel)
        // Mutable so GeneralSettings can push a configured override at runtime
        public static string BlocksDirectory { get; private set; } = ResolveBlocksDir();

        // All available block names (no extension) — reloaded when path changes
        public static System.Collections.Generic.List<string> AllBlockNames { get; private set; } = LoadBlockNames();

        /// <summary>Called by ShellViewModel.RcsBlocksPath setter to push a user-configured
        /// path. Reloads the block name list from the new directory.</summary>
        public static void OverrideBlocksDirectory(string newPath)
        {
            if (string.IsNullOrWhiteSpace(newPath) || !Directory.Exists(newPath)) return;
            BlocksDirectory = newPath;
            AllBlockNames   = LoadBlockNames();
        }

        private static string ResolveBlocksDir()
        {
            // Walk up from the binary output to find the repo root
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null && dir.GetDirectories("RCS_Blocks").Length == 0)
                dir = dir.Parent;
            return dir != null
                ? Path.Combine(dir.FullName, "RCS_Blocks")
                : string.Empty;
        }

        private static System.Collections.Generic.List<string> LoadBlockNames()
        {
            if (string.IsNullOrEmpty(BlocksDirectory) || !Directory.Exists(BlocksDirectory))
                return new();

            return Directory.GetFiles(BlocksDirectory, "*.dwg")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .OrderBy(n => n)
                .ToList();
        }

        public EditCogoCodeWindow(CogoCode code)
        {
            InitializeComponent();
            _originalCode = code;

            // Populate the block dropdown with all *.dwg names
            foreach (var name in AllBlockNames)
                cmbBlock.Items.Add(name);

            // Load data into fields
            txtLocalCode.Text   = code.LocalCode;
            txtSystemCode.Text  = code.SystemCode;
            txtDescription.Text = code.Description;
            cmbBlock.Text       = code.Block ?? string.Empty;
            txtBlockScale.Text  = code.BlockScale.ToString("G");

            UpdateBlockMatchLabel(code.Block);

            // Load symbol preview
            if (File.Exists(code.SymbolImagePath))
                imgPreview.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(code.SymbolImagePath));
        }

        // ── Block ComboBox filter ────────────────────────────────────────────
        private void OnBlockTextChanged(object sender, TextChangedEventArgs e)
        {
            var text = (sender as TextBox)?.Text ?? cmbBlock.Text;
            UpdateBlockMatchLabel(text);
        }

        private void UpdateBlockMatchLabel(string? blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName))
            {
                lblBlockMatch.Text = string.Empty;
                return;
            }
            bool exists = AllBlockNames.Contains(blockName, StringComparer.OrdinalIgnoreCase);
            lblBlockMatch.Text = exists
                ? "✓ Found in library"
                : "⚠ Not found in library";
            lblBlockMatch.Foreground = exists
                ? System.Windows.Media.Brushes.LightGreen
                : System.Windows.Media.Brushes.Orange;
        }

        // ── Auto-match: pick the closest block name from the library ─────────
        private void OnAutoMatchBlock(object sender, RoutedEventArgs e)
        {
            // Build a candidate string from the local code + description
            var candidate = _originalCode.LocalCode.ToUpperInvariant();
            var desc      = (_originalCode.Description ?? "").ToUpperInvariant();

            // 1. Exact match on LocalCode
            var match = AllBlockNames.FirstOrDefault(b =>
                b.Equals(candidate, StringComparison.OrdinalIgnoreCase));

            // 2. Prefix match on LocalCode (e.g. SSMH → SSMH.dwg, SSMH1.dwg, ...)
            if (match == null)
                match = AllBlockNames.FirstOrDefault(b =>
                    b.StartsWith(candidate, StringComparison.OrdinalIgnoreCase));

            // 3. Keyword match from description
            if (match == null)
            {
                var keywords = desc.Split(new[] { ' ', '-', '_', '/' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var kw in keywords)
                {
                    match = AllBlockNames.FirstOrDefault(b =>
                        b.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (match != null) break;
                }
            }

            if (match != null)
            {
                cmbBlock.Text = match;
                UpdateBlockMatchLabel(match);
                lblBlockMatch.Text += $"  (auto-matched for '{candidate}')";
            }
            else
            {
                lblBlockMatch.Text = "No match found. Set block manually.";
                lblBlockMatch.Foreground = System.Windows.Media.Brushes.Orange;
            }
        }

        // ── Save ─────────────────────────────────────────────────────────────
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLocalCode.Text))
            {
                MessageBox.Show("Local Code cannot be empty.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(txtBlockScale.Text, out double scale) || scale <= 0)
            {
                MessageBox.Show("Block Scale must be a positive number (e.g. 1.0).", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = new AppDbContext();
                var entity = db.CogoCodes.FirstOrDefault(c =>
                    c.LocalCode  == _originalCode.LocalCode &&
                    c.SystemCode == _originalCode.SystemCode);

                if (entity != null)
                {
                    entity.LocalCode  = txtLocalCode.Text.Trim();
                    entity.Block      = cmbBlock.Text.Trim();
                    entity.BlockScale = scale;
                    // Description and SystemCode are strictly Read-Only
                    db.SaveChanges();
                }

                // Update the in-memory model so the grid refreshes without a reload
                _originalCode.LocalCode  = entity?.LocalCode  ?? txtLocalCode.Text.Trim();
                _originalCode.Block      = entity?.Block      ?? cmbBlock.Text.Trim();
                _originalCode.BlockScale = scale;

                ResultAction = "Save";
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving to database:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Delete ────────────────────────────────────────────────────────────
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete the code '{_originalCode.LocalCode}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using var db = new AppDbContext();
                    var entity = db.CogoCodes.FirstOrDefault(c =>
                        c.LocalCode  == _originalCode.LocalCode &&
                        c.SystemCode == _originalCode.SystemCode);

                    if (entity != null)
                    {
                        db.CogoCodes.Remove(entity);
                        db.SaveChanges();
                    }

                    ResultAction = "Delete";
                    this.DialogResult = true;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting from database:\n{ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            ResultAction = "Close";
            this.Close();
        }
    }
}
