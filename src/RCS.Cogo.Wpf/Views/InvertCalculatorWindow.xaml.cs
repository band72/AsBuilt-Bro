using System.Linq;
using System.Windows;
using RCS.Data;
using RCS.Data.Entities;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RCS.Cogo.Wpf.Views
{
    public partial class InvertCalculatorWindow : Window, INotifyPropertyChanged
    {
        private InstalledAsset _asset;
        public double? ComputedValue { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private string _partNumber = string.Empty;
        public string PartNumber
        {
            get => _partNumber;
            set
            {
                _partNumber = value;
                OnPropertyChanged();
            }
        }

        private double? _outerDiameter;
        public double? OuterDiameter
        {
            get => _outerDiameter;
            set
            {
                _outerDiameter = value;
                OnPropertyChanged();
            }
        }

        private double? _nominalDiameter;
        public double? NominalDiameter
        {
            get => _nominalDiameter;
            set
            {
                _nominalDiameter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EstimatedInvert));
            }
        }

        private double? _topOutsideWallElev;
        public double? TopOutsideWallElev
        {
            get => _topOutsideWallElev;
            set
            {
                _topOutsideWallElev = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EstimatedInvert));
            }
        }

        private double? _outerWallThicknessTop;
        public double? OuterWallThicknessTop
        {
            get => _outerWallThicknessTop;
            set
            {
                _outerWallThicknessTop = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EstimatedInvert));
            }
        }

        private double? _innerDiameter;
        public double? InnerDiameter
        {
            get => _innerDiameter;
            set
            {
                _innerDiameter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EstimatedInvert));
            }
        }

        private double? _deflection;
        public double? Deflection
        {
            get => _deflection;
            set
            {
                _deflection = value;
                OnPropertyChanged();
            }
        }

        private string _partNote = string.Empty;
        public string PartNote
        {
            get => _partNote;
            set
            {
                _partNote = value;
                OnPropertyChanged();
            }
        }

        public double? EstimatedInvert
        {
            get
            {
                if (TopOutsideWallElev.HasValue && InnerDiameter.HasValue)
                {
                    double thickness = OuterWallThicknessTop ?? 0.0;
                    double nominal = NominalDiameter.HasValue && NominalDiameter.Value != 0 ? NominalDiameter.Value : 1.0;
                    
                    return System.Math.Round(TopOutsideWallElev.Value - ((thickness + InnerDiameter.Value) / nominal), 3);
                }
                return null;
            }
        }

        public InvertCalculatorWindow(InstalledAsset asset)
        {
            InitializeComponent();
            _asset = asset;
            
            // Try to auto-populate part number from base asset
            PartNumber = !string.IsNullOrWhiteSpace(asset.ManufacturerPartNo) ? asset.ManufacturerPartNo : asset.PartKey ?? "";

            TopOutsideWallElev = asset.TopOutsideWallElev;
            OuterWallThicknessTop = asset.OuterWallThicknessTop;
            InnerDiameter = asset.InnerDiameter;
            
            DataContext = this;
            
            // Auto look-up specs if we have a valid part number
            if (!string.IsNullOrWhiteSpace(PartNumber))
            {
                LookupSpecs();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void LoadSpecs_Click(object sender, RoutedEventArgs e)
        {
            var window = new PipeCharacteristicsWindow(isSelectMode: true);
            window.Owner = Window.GetWindow(this);
            if (window.ShowDialog() == true && window.SelectedPart != null)
            {
                PartNumber = window.SelectedPart.PartNumber;
                
                // Populate the form fields with the selected part directly
                var spec = window.SelectedPart;
                if (spec.OuterDiameter.HasValue) OuterDiameter = spec.OuterDiameter;
                if (spec.NominalDiameter.HasValue) NominalDiameter = spec.NominalDiameter;
                if (spec.PipeThickness.HasValue) OuterWallThicknessTop = spec.PipeThickness;
                if (spec.InnerDiameter.HasValue) InnerDiameter = spec.InnerDiameter;
                if (spec.Deflection.HasValue) Deflection = spec.Deflection;
                if (!string.IsNullOrWhiteSpace(spec.Note)) PartNote = spec.Note;
            }
        }

        private void LookupSpecs()
        {
            if (string.IsNullOrWhiteSpace(PartNumber)) return;

            using (var db = new AppDbContext())
            {
                var spec = db.PartSpecifications.FirstOrDefault(p => p.PartNumber.ToLower() == PartNumber.ToLower());
                if (spec != null)
                {
                    if (!OuterDiameter.HasValue && spec.OuterDiameter.HasValue) OuterDiameter = spec.OuterDiameter;
                    if (!NominalDiameter.HasValue && spec.NominalDiameter.HasValue) NominalDiameter = spec.NominalDiameter;
                    if (!OuterWallThicknessTop.HasValue && spec.PipeThickness.HasValue) OuterWallThicknessTop = spec.PipeThickness;
                    if (!InnerDiameter.HasValue && spec.InnerDiameter.HasValue) InnerDiameter = spec.InnerDiameter;
                    if (!Deflection.HasValue && spec.Deflection.HasValue) Deflection = spec.Deflection;
                    if (string.IsNullOrWhiteSpace(PartNote)) PartNote = spec.Note;
                }
            }
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (EstimatedInvert.HasValue)
            {
                ComputedValue = EstimatedInvert.Value;

                // save dimension values to asset so they are retained in DB
                _asset.TopOutsideWallElev = TopOutsideWallElev;
                _asset.OuterWallThicknessTop = OuterWallThicknessTop;
                _asset.InnerDiameter = InnerDiameter;

                // save specification globally to DB if PartNumber provided
                if (!string.IsNullOrWhiteSpace(PartNumber))
                {
                    try
                    {
                        using (var db = new AppDbContext())
                        {
                            var spec = db.PartSpecifications.FirstOrDefault(p => p.PartNumber.ToLower() == PartNumber.ToLower());
                            if (spec == null)
                            {
                                spec = new PartSpecificationEntity { PartNumber = PartNumber.Trim() };
                                db.PartSpecifications.Add(spec);
                            }
                            
                            spec.OuterDiameter = OuterDiameter;
                            spec.NominalDiameter = NominalDiameter;
                            spec.PipeThickness = OuterWallThicknessTop;
                            spec.InnerDiameter = InnerDiameter;
                            spec.Deflection = Deflection;
                            spec.Note = PartNote ?? "";
                            
                            db.SaveChanges();
                        }
                    } 
                    catch { } // Ignore DB errors here, it's just a spec cache
                }

                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please enter enough data to compute an invert.", "Missing Data", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
