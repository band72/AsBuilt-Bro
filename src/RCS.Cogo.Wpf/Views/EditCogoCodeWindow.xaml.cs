using System;
using System.Linq;
using System.Windows;
using RCS.Cogo.App.State;
using RCS.Data;

namespace RCS.Cogo.Wpf.Views
{
    public partial class EditCogoCodeWindow : Window
    {
        private readonly CogoCode _originalCode;
        public string ResultAction { get; private set; } = "None";

        public EditCogoCodeWindow(CogoCode code)
        {
            InitializeComponent();
            _originalCode = code;

            // Load data into fields
            txtLocalCode.Text = code.LocalCode;
            txtSystemCode.Text = code.SystemCode;
            txtDescription.Text = code.Description;
            txtBlock.Text = code.Block;

            // Load symbol preview
            if (System.IO.File.Exists(code.SymbolImagePath))
            {
                imgPreview.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(code.SymbolImagePath));
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLocalCode.Text))
            {
                MessageBox.Show("Local Code cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    // Find existing record by LocalCode and SystemCode
                    var entity = db.CogoCodes.FirstOrDefault(c => c.LocalCode == _originalCode.LocalCode && c.SystemCode == _originalCode.SystemCode);
                    if (entity != null)
                    {
                        entity.LocalCode = txtLocalCode.Text.Trim();
                        entity.Block = txtBlock.Text.Trim();
                        // Description and SystemCode are strictly Read-Only per logic requirements
                        db.SaveChanges();
                    }
                }
                ResultAction = "Save";
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving to database: {ex.Message}", "Error");
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show($"Are you sure you want to delete the code '{_originalCode.LocalCode}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var db = new AppDbContext())
                    {
                        var entity = db.CogoCodes.FirstOrDefault(c => c.LocalCode == _originalCode.LocalCode && c.SystemCode == _originalCode.SystemCode);
                        if (entity != null)
                        {
                            db.CogoCodes.Remove(entity);
                            db.SaveChanges();
                        }
                    }
                    ResultAction = "Delete";
                    this.DialogResult = true;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting from database: {ex.Message}", "Error");
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
