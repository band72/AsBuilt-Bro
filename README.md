# RCS COGO Enterprise

![Version](https://img.shields.io/badge/version-2.1.0_Enterprise-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows_10%20%7C%2011-lightgrey.svg)
![Framework](https://img.shields.io/badge/framework-.NET_8.0-512BD4.svg)
![Database](https://img.shields.io/badge/database-SQLite-003B57.svg)

## Overview

**RCS COGO Enterprise** provides state-of-the-art Coordinate Geometry (COGO) calculations paired with comprehensive Pipe Network modeling and Asset Management capabilities. Built for modern enterprise needs, it enables engineers and surveyors to manage complex datasets efficiently.

### Key Features & Benefits

- **Advanced COGO Engine**: Calculate complex setup orientations, traverses, intersections, and alignments using our native commands (`ST`, `OC`, `BS`, `TRAV`, etc.).
- **Scriptable Pipelines & Networks**: Support for both UI-based and fully scriptable (batch) underground utility modeling with full 3-dimensional validation (Gravity, Wastewater, Pressure).
- **Asset & Material Management**: Maintain an active material database (DIP, PVC, HDPE) to instantly generate unified Bills of Materials (BOM) and project material schedules.
- **Multi-Format Exports**: Export seamlessly to AutoCAD DXF, Points XML/TXT, CSV BOM, and prepare data for ePANET INP topologies.
- **AI Integration**: AI-driven analysis of batch scripts for logical validation directly within the application.
- **High Performance & Secure**: Built on local high-performance SQLite/EF Core architecture paired with native C++ security verification.

---

## Getting Started

### Prerequisites

- **Operating System**: Windows 10/11 (64-bit) (x64 architecture required)
- **Framework**: [.NET 8.0 Windows Desktop Runtime](https://dotnet.microsoft.com/download)
- **Database**: SQLite (built-in, local file-based storage)

### Installation

1. Clone or download this repository.
2. Build the solution (`RCS.Cogo.Enterprise.Modern.sln`) targeting `x64` architecture.
3. Locate the compiled executable (`RCS.Cogo.Wpf.exe`).
4. **Important**: Ensure the native `RcsSecurityModule.dll` is preserved in the root executable output directory. Missing this DLL will trigger a hardware verification failure upon launch.
5. Launch `RCS.Cogo.Wpf.exe` to begin.
6. Download link: https://drive.google.com/file/d/1fTB1summljMXpA8QTK675HM27le2JIO2/view?usp=drive_link
### Usage Example

RCS COGO Enterprise shines in its batch processing and scripting engine. You can use the **Cogo Script** tab within the application to rapidly layout survey points and create geometric figures:

```text
# Basic Coordinate Setup
ST 1 1000 1000 100 START
OC 1 5.0
BS 2 0.0

# Define Traverse & Branches
FS 2 90 200 0.0 POINT2
FS 3 180 200 0.0 POINT3
FS 4 270 200 0.0 POINT4

# Generate Closed Shape "SQUARE"
B SQUARE 1
L 2
L 3
L 4
C
```

*For pipe networks:*
```text
# Start a transmission main from Point 1 to 2, 8-inch diameter, specified inverts
PRUN START 1 2 8 90 85
```

---

## Support & Documentation

For a comprehensive guide on all available COGO commands, UI references, and advanced configurations, please consult the extensive manual bundled in this repository:

- 📖 [Comprehensive User Manual & Testing Guide](./USER_MANUAL_AND_TESTING_GUIDE.txt)
- 📝 [Sample Scripts](./SampleScripts) - Collection of basic configuration and network capability scripts.

For troubleshooting UI freezes, database locks, or specialized integrations (like custom native security DLL configurations), please contact the **RCS Enterprise Technical Deployment Team**.

---

## Contributing and Maintainers

We welcome improvements, bug reports, and structural suggestions from developers and surveying engineers! 

1. Please evaluate [validation_rules.json](./validation_rules.json) if you're adjusting standard pipe drop or clearance constraints.
2. When making modifications to database interactions, ensure you heavily test EF Core object state tracking. Entity Framework tracking exceptions during figure generation/deletion have strict cleanup rules.
3. Submit a Pull Request targeting the `main` branch with comprehensive unit tests for any mathematical additions.

**Maintainers:**
- RCS Enterprise Technical Deployment Team
- *Open to community maintainer applications.*

---

## Proprietary License
Copyright (c) 2026 Band72

All rights reserved.

This software, including source code, binaries, and associated files, is the proprietary property of the author.

Permission is granted to download and use this software for personal, internal, or evaluation purposes only.

Commercial use, including but not limited to selling, licensing, distributing, or incorporating this software into other products, is strictly prohibited without prior written permission from the author.

No part of this software may be copied, modified, distributed, or sublicensed without explicit written consent.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED.

*Note: This application is optimized for enterprise map scaling. Always utilize the 'Compact Database' tool when dealing with massive datasets to reclaim file system space.*
