using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using RCS.Piping.Core.Workflow;
using RCS.Piping.Core.Models;

namespace RCS.Piping.Core.Builders;

public static class LandXmlBuilder
{
    public static void Export(AsBuiltJob job, string outputPath)
    {
        XNamespace ns = "http://www.landxml.org/schema/LandXML-1.2";
        
        var structs = new XElement(ns + "Structs");
        foreach (var s in job.Network.Structures.Values)
        {
            var p = job.PointRows.FirstOrDefault(pr => pr.PointId == s.PointId);
            double x = p?.Easting ?? 0.0;
            double y = p?.Northing ?? 0.0;
            double z = s.RimElevation ?? p?.Elevation ?? 0.0;
            
            var strElement = new XElement(ns + "Struct",
                new XAttribute("name", s.PointId),
                new XAttribute("desc", s.Type ?? ""),
                new XElement(ns + "Center", $"{y:F4} {x:F4} {z:F4}"),
                new XElement(ns + "Invert", new XAttribute("elev", s.InvertOut ?? s.InvertIn ?? 0.0))
            );
            structs.Add(strElement);
        }

        var pipes = new XElement(ns + "Pipes");
        foreach (var r in job.Network.Runs.Values)
        {
            var pipeElement = new XElement(ns + "Pipe",
                new XAttribute("name", r.Id ?? ""),
                new XAttribute("refStart", r.FromPointId ?? ""),
                new XAttribute("refEnd", r.ToPointId ?? ""),
                new XAttribute("desc", r.Type ?? ""),
                new XElement(ns + "PipeFlow", 
                    new XAttribute("startInvert", r.InvertStart ?? 0.0),
                    new XAttribute("endInvert", r.InvertEnd ?? 0.0)
                ),
                new XElement(ns + "Size", 
                    new XAttribute("diameter", r.Diameter)
                )
            );
            pipes.Add(pipeElement);
        }

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(ns + "LandXML",
                new XAttribute("date", DateTime.Now.ToString("yyyy-MM-dd")),
                new XAttribute("time", DateTime.Now.ToString("HH:mm:ss")),
                new XAttribute("version", "1.2"),
                new XElement(ns + "Project", new XAttribute("name", job.Identity.JobNumber ?? "Job")),
                new XElement(ns + "Application", new XAttribute("name", "RCS.Cogo.Enterprise")),
                new XElement(ns + "PipeNetworks",
                    new XElement(ns + "PipeNetwork", new XAttribute("name", "AsBuilt"), structs, pipes)
                )
            )
        );

        doc.Save(outputPath);
    }
}
