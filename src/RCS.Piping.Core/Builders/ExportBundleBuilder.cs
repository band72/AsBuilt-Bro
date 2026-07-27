using System;
using System.IO;
using RCS.Piping.Core.Workflow;

namespace RCS.Piping.Core.Builders;

/// <summary>
/// Container holding file path metrics for a complete deliverable export bundle.
/// </summary>
public record ExportBundleResult(
    string OutputDirectory,
    string DxfPath,
    string LandXmlPath,
    string PdfReportPath,
    string PnezdCsvPath
);

/// <summary>
/// Orchestrates the simultaneous generation of all 4 deliverable assets:
/// 1. DXF Vector CAD File (.dxf)
/// 2. LandXML 1.2 Pipe Network Schema (.xml)
/// 3. Professional Engineering QC PDF Report (.pdf)
/// 4. Civil 3D PNEZD Coordinate CSV (.csv)
/// </summary>
public sealed class ExportBundleBuilder
{
    private readonly DxfBuilder _dxfBuilder;
    private readonly PdfReportBuilder _pdfReportBuilder;
    private readonly PnezdExportBuilder _pnezdBuilder;

    public ExportBundleBuilder(
        DxfBuilder dxfBuilder,
        PdfReportBuilder pdfReportBuilder,
        PnezdExportBuilder pnezdBuilder)
    {
        _dxfBuilder = dxfBuilder;
        _pdfReportBuilder = pdfReportBuilder;
        _pnezdBuilder = pnezdBuilder;
    }

    /// <summary>
    /// Builds all 4 deliverable artifacts into the target output directory.
    /// </summary>
    public ExportBundleResult Build(AsBuiltJob job, string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        string safeJobNum = string.IsNullOrWhiteSpace(job.Identity.JobNumber) ? "AsBuilt_Deliverable" : job.Identity.JobNumber;
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            safeJobNum = safeJobNum.Replace(c, '_');
        }

        string dxfPath = Path.Combine(outputDir, $"{safeJobNum}.dxf");
        string xmlPath = Path.Combine(outputDir, $"{safeJobNum}.xml");
        string pdfPath = Path.Combine(outputDir, $"{safeJobNum}_Report.pdf");
        string csvPath = Path.Combine(outputDir, $"{safeJobNum}_PNEZD.csv");

        _dxfBuilder.Build(job, dxfPath);
        LandXmlBuilder.Export(job, xmlPath);
        _pdfReportBuilder.Build(job, pdfPath);
        _pnezdBuilder.Build(job, csvPath);

        return new ExportBundleResult(outputDir, dxfPath, xmlPath, pdfPath, csvPath);
    }
}
