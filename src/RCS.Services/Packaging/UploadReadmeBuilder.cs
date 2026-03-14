namespace RCS.Packaging.Readme;

public static class UploadReadmeBuilder
{
    public static string Build(string availNo, string projectName, string revisionLabel)
    {
        // Defensive normalization (optional but recommended)
        availNo = availNo?.Trim() ?? "UNKNOWN";
        projectName = projectName?.Trim() ?? "UNKNOWN";
        revisionLabel = revisionLabel?.Trim() ?? "N/A";

        return $"""
                UPLOAD PACKAGE README

                Availability No: {availNo}
                Project: {projectName}
                Revision: {revisionLabel}

                Contents:
                - 01_LandXML: Pipe Network LandXML export
                - 02_DXF: Utility DXF export
                - 03_Points: PNEZD / point exports
                - 04_Parts: Parts usage report
                - 05_Certification: Signed certification PDF
                - 99_Manifest: manifest.json with SHA-256 hashes

                Notes:
                - Filenames are revision-locked using suffix format _REV1, _REV2, etc.
                - Verify file integrity using SHA-256 hashes in 99_Manifest\manifest.json
                """;
    }
}
