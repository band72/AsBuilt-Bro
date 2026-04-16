using System;
using System.IO;
using System.Linq;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using RCS.Piping.Core.Models;
using RCS.Piping.Core.Workflow;

namespace RCS.Piping.Core.Builders;

/// <summary>
/// Autonomous PDF document assembly engine generating sealed Professional Grade Analysis Reports.
/// Synthesizes Intake Metrics, Pipeline Capacities, Network Analytics, and Validations 
/// to PDF utilizing PdfSharpCore, replacing legacy manual byte streaming.
/// </summary>
public sealed class PdfReportBuilder
{
    public void Build(AsBuiltJob job, string outputPath)
    {
        var document = new PdfDocument();
        document.Info.Title = "Enterprise As-Built Deliverable Report";
        document.Info.Author = "BoundaryQC AI System";
        document.Info.CreationDate = DateTime.Now;

        // === COVER PAGE ===
        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        
        var titleFont = new XFont("Segoe UI", 24, XFontStyle.Bold);
        var subFont = new XFont("Segoe UI", 14, XFontStyle.Regular);
        var bodyFont = new XFont("Segoe UI", 11, XFontStyle.Regular);
        var boldFont = new XFont("Segoe UI", 11, XFontStyle.Bold);
        var headerBrush = XBrushes.DarkSlateBlue;

        gfx.DrawString("ENTERPRISE AS-BUILT REPORT", titleFont, headerBrush, 
            new XRect(0, 80, page.Width, 50), XStringFormats.TopCenter);
            
        string jobNum = string.IsNullOrWhiteSpace(job.Identity.JobNumber) ? "DRAFT" : job.Identity.JobNumber;
        gfx.DrawString($"Job Number: {jobNum} (Rev {job.Identity.RevisionNumber})", subFont, XBrushes.Black, 
            new XRect(0, 130, page.Width, 50), XStringFormats.TopCenter);
            
        gfx.DrawString($"Client: {job.Identity.ClientName} | Field Date: {job.Identity.FieldDate:MM/dd/yyyy}", subFont, XBrushes.DarkGray, 
            new XRect(0, 155, page.Width, 50), XStringFormats.TopCenter);

        gfx.DrawString($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Coordinates: {job.Environment}", subFont, XBrushes.Gray, 
            new XRect(0, 180, page.Width, 50), XStringFormats.TopCenter);

        // Section 1: LiDAR & Intake Telemetry
        int yPos = 260;
        gfx.DrawString("1. System Intake & Topography Analytics", titleFont, XBrushes.Black, 50, yPos);
        yPos += 30;
        
        bool hasLidar = job.BaseSurface != null && job.BaseSurface.Points.Count > 0;
        gfx.DrawString($"LiDAR Surface Nodes: {(hasLidar ? job.BaseSurface!.Points.Count.ToString("N0") : "N/A")} Points Analyzed", boldFont, XBrushes.DarkOliveGreen, 50, yPos); yPos += 20;
        gfx.DrawString($"Network Nodes: {job.Network.Structures.Count} Structures mapped", bodyFont, XBrushes.Black, 50, yPos); yPos += 20;
        gfx.DrawString($"Network Matrix: {job.Network.Runs.Count} Pipeline Runs registered", bodyFont, XBrushes.Black, 50, yPos); yPos += 40;

        // Section 2: Validation Results
        gfx.DrawString("2. Engineering Validation Matrix", titleFont, XBrushes.Black, 50, yPos);
        yPos += 30;

        var engine = new ValidationEngine();
        var report = engine.Validate(job);

        int warnings = report.Issues.Count(i => i.Severity == IssueSeverity.Warning);
        int errors = report.Issues.Count(i => i.Severity == IssueSeverity.Error);

        gfx.DrawString($"Geometry & Hydrology Validations Completed. Errors: {errors} | Warnings: {warnings}", boldFont, 
            errors > 0 ? XBrushes.DarkRed : XBrushes.DarkGreen, 50, yPos);
        yPos += 30;

        foreach (var issue in report.Issues)
        {
            if (yPos > page.Height - 80)
            {
                page = document.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                yPos = 50;
            }

            XBrush br = issue.Severity == IssueSeverity.Error ? XBrushes.DarkRed : XBrushes.DarkGoldenrod;
            gfx.DrawString($"[{issue.Severity.ToString().ToUpper()}] {issue.Category} // {issue.RuleName}", boldFont, br, 50, yPos);
            yPos += 15;
            
            // Text wrapping primitive
            string msg = issue.Message;
            if (msg.Length > 85) 
            {
                gfx.DrawString(msg.Substring(0, 85), bodyFont, XBrushes.Black, 70, yPos); yPos += 15;
                gfx.DrawString(msg.Substring(85), bodyFont, XBrushes.Black, 70, yPos); yPos += 25;
            }
            else
            {
                gfx.DrawString(msg, bodyFont, XBrushes.Black, 70, yPos);
                yPos += 25;
            }
        }

        if (report.Issues.Count == 0)
        {
            gfx.DrawString("All systems completely clear. Zero structural or capacity violations detected.", bodyFont, XBrushes.DarkGreen, 50, yPos);
            yPos += 40;
        }

        // Add Table of Runs
        if (yPos > page.Height - 150)
        {
            page = document.AddPage();
            gfx = XGraphics.FromPdfPage(page);
            yPos = 50;
        }

        gfx.DrawString("3. Pipeline Asset Inventory", titleFont, XBrushes.Black, 50, yPos);
        yPos += 30;
        
        var runs = job.Network.Runs.Values.ToList();
        gfx.DrawString("From Pt", boldFont, XBrushes.Black, 50, yPos);
        gfx.DrawString("To Pt", boldFont, XBrushes.Black, 120, yPos);
        gfx.DrawString("Diameter", boldFont, XBrushes.Black, 190, yPos);
        gfx.DrawString("Length (ft)", boldFont, XBrushes.Black, 270, yPos);
        gfx.DrawString("Slope %", boldFont, XBrushes.Black, 350, yPos);
        gfx.DrawString("Capacity CFS", boldFont, XBrushes.Black, 430, yPos);
        yPos += 20;

        foreach (var run in runs)
        {
            if (yPos > page.Height - 60)
            {
                page = document.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                yPos = 50;
            }

            gfx.DrawString(run.FromPointId ?? "-", bodyFont, XBrushes.DimGray, 50, yPos);
            gfx.DrawString(run.ToPointId ?? "-", bodyFont, XBrushes.DimGray, 120, yPos);
            gfx.DrawString($"{run.Diameter}\"", bodyFont, XBrushes.DimGray, 190, yPos);
            gfx.DrawString($"{run.ComputedLength:F2}", bodyFont, XBrushes.DimGray, 270, yPos);
            gfx.DrawString($"{run.SlopePercent:F2}%", bodyFont, XBrushes.DimGray, 350, yPos);
            gfx.DrawString($"{run.MaxFlowCfs:F2}", bodyFont, XBrushes.DimGray, 430, yPos);
            yPos += 15;
        }

        document.Save(outputPath);
    }
}
