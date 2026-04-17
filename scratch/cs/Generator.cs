using System;
using System.IO;

public class Generator {
    public static void Main() {
        var txt = "Subtype Chilled Fitting\nCross\nElbow 11.25\nElbow 22.5\nElbow 45\nElbow 90\nPlug\nReducer\nRepair Coupling\nSleeve\nTapping Sleeve\nTee\nTransition Coupling\nOther\nUnknown Fitting\nVertical\n\nSubtype Locate Box\nMarker Ball\nLocate Wire Box\n\nSubtype Manhole\nCollection\nEffluent\nForce Main\nLow Pressure\nTrunk\n\nSubtype Reclaimed Fitting\nCross\nElbow 11.25\nElbow 22.5\nElbow 45\nElbow 90\nLateral Main Connection\nPlug\nReducer\nRepair Coupling\nService Lateral Fitting\nSleeve\nTapping Sleeve\nTapping Saddle\nTee\nTransition Coupling\nWYE\nOther\nUnknown Fitting\nVertical\nCap, Tapped\nStub\nCap\n\nSubtype Reclaimed Meter\nControl Meter\nMajor Meter\nMinor Meter\nPlant Meter\n\nSubtype Reclaimed Pipe\nAugmentation Main\nHydrant Lateral\nReclaimed Main\nService Lateral\n\nSubtype Reclaimed Valve\nValve\nBackflow Preventor\nHydrant Valve\n\nSubtype Sewer Customer Point\nCustomer Point\nSewer Flow Meter\n\nSubtype Sewer Fitting\nCleanout\nCross\nElbow 11.25\nElbow 22.5\nElbow 45\nElbow 90\nLateral Main Connection\nOther\nPlug\nReducer\nRepair Coupling\nService Lateral Fitting\nSleeve\nStub\nTapping Sleeve\nTapping Saddle\nTee\nTransition Coupling\nUnknown Fitting\nVertical\nWYE\nCap, Tapped\nStub\nCap\n\nSubtype Sewer Gravity Pipe\nCollection Main\nTrunk Main\nCollection Lateral\n\nSubtype Sewer Valve\nValve\nPump Out\nAir Release Valve\n\nSubtype Water Fitting\nCross\nElbow 11.25\nElbow 22.5\nElbow 45\nElbow 90\nLateral Main Connection\nPlug\nReducer\nRepair Coupling\nService Lateral Fitting\nSleeve\nTapping Sleeve\nTapping Saddle\nTee\nTransition Coupling\nVertical\nWYE\nOther\nUnknown Fitting\nCap, Tapped\nStub\nCap\n\nSubtype Water Meter\nInterconnect\nMajor Meter\nMinor Meter\nPlant Meter\nIrrigation Meter\nFire Meter\n\nSubtype Water Pipe\nDistribution Main\nFire Line Main\nRaw Water Main\nTransmission Main\nService Lateral\nHydrant Lateral\n\nSubtype Water Valve\nValve\nBackflow Preventor\nHydrant Valve\nAir Release Valve\n\nChilled Pipe Class\nCL50\nCL51\nDR11\nDR14\nDR17\nDR18\nDR25\nPC150\nPC250\nN/A\nOther \nUnknown\n\nChilled Pipe Role\nReturn\nSupply\n\nCounty\nClay\nDuval\nNassau\nSt Johns\n\nCrossing Pipe Type\nPotable Water\nGravity Sewer\nForce Main\nVacuum Sewer\nReclaimed\nStorm\n\nFacility Owner\nJEA\nPrivate\nUnknown\n\nFitting Manufacturers\nAmerican Cast Iron Pipe Company\nCascade Waterworks Mfg\nCharlotte Pipe and Foundry Co\nChemtrol/NIBCO\nClow Valve\nDresser Inc/GE\nFERNCO\nFord Meter Box\nGalaxy Plastics\nGeorg Fisher Sloane Manufacturing\nGPK Products Inc\nHarco Inc\nHarrington Corporation (HARCO)\nIpex\nJCM Industries Inc\nLasco Fittings Inc\nM&H Valve Company\nMueller\nMueller Aqua Grip\nMueller Company\nMulti-Fittings\nOther\nPlastic Trends (Royal Building Projects)\nPower Seal\nRomac\nRomac Industries Inc\nSigma Corp (Russell Pipe)\nSIP Industries\nSmith-Blair\nSpears Manufacturing\nStar Pipe Products\nTigreADS USA\nTPS Hymax\nTyler Union\nUnknown\nUS Pipe\n\nHydrant Model\nAmerican Darling\nAmerican Flow\nAVK\nClow\nKennedy\nM&H\nMatthews\nMueller\nUS Pipe\nWaterous\nOther\nUnknown\n\nManhole Drop Type\nOutside\nInside\nUnknown\n\nManhole Exterior Joint Tape Manufacturer\nCon Seal\nRub-R-Nek/Henry Company\nWrapid Seal (CCI Pipeline systems)\nOther\nUnknown";
        var lines = txt.Split('\n');
        string currentCategory = "";
        var str = "// Auto-generated seeds\n";
        foreach(var raw in lines) {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("Subtype ") || line == "Chilled Pipe Class" || line == "Chilled Pipe Role" || line == "County" || line == "Crossing Pipe Type" || line == "Facility Owner" || line == "Fitting Manufacturers" || line == "Hydrant Model" || line == "Manhole Drop Type" || line == "Manhole Exterior Joint Tape Manufacturer") {
                currentCategory = line;
                if (currentCategory.StartsWith("Subtype ")) {
                    currentCategory = currentCategory.Substring(8);
                }
                str += "\nif (!context.AssetSubtypes.Any(s => s.Category == \"" + currentCategory + "\")) {\n";
            } else {
                str += "    context.AssetSubtypes.Add(new Entities.AssetSubtypeEntity { Category = \"" + currentCategory + "\", SubtypeName = \"" + line + "\" });\n";
            }
            if (line.Length == 0 || line == lines[lines.Length-1]) {
                if (currentCategory != "") {
                    str += "}\n";
                }
            }
        }
        File.WriteAllText("seed_output.txt", str);
    }
}
