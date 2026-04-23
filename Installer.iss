#define MyAppName "RCS COGO Enterprise"
#define MyAppVersion "3.0.8"
#define MyAppPublisher "BANKS & BANKS CONSULTING"
#define MyAppExeName "RCS.Cogo.Wpf.exe"
#define MyAppAssocName MyAppName + " Project"
#define MyAppAssocExt ".cogo"
#define MyAppAssocKey StringChange(MyAppAssocName, " ", "") + MyAppAssocExt

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do NOT change it.
AppId={{FD0827BF-1172-4D17-BE48-AE1A7F9DD987}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
ChangesAssociations=yes
OutputBaseFilename=RCS.ASBUILT.PRO
OutputDir=.\Installer
SolidCompression=yes
Compression=lzma2/ultra64
LZMAUseSeparateProcess=yes
WizardStyle=modern
SetupIconFile=rcs_cogo_icon.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; ── Core application (self-contained publish) ──────────────────────────────────
Source: "rcs_cogo_icon.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "src\RCS.Cogo.Wpf\bin\Release\net8.0-windows\win-x64\publish\RCS.Cogo.Wpf.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "src\RCS.Cogo.Wpf\bin\Release\net8.0-windows\win-x64\publish\*";               DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; ── Symbol + Block libraries ───────────────────────────────────────────────────
Source: "SymbolsLibrary\*"; DestDir: "{app}\SymbolsLibrary"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "RCS_Blocks\*";     DestDir: "{app}\RCS_Blocks";     Flags: ignoreversion recursesubdirs createallsubdirs

; ── Sample scripts ─────────────────────────────────────────────────────────────
Source: "SampleScripts\*"; DestDir: "{app}\SampleScripts"; Flags: ignoreversion recursesubdirs createallsubdirs

; ── Documentation & Help files ────────────────────────────────────────────────
; Manuals (PDF + HTML + Markdown)
Source: "docs\Cogo_Script_Manual.pdf";        DestDir: "{app}\docs"; Flags: ignoreversion
Source: "docs\Cogo_Script_Manual.html";       DestDir: "{app}\docs"; Flags: ignoreversion
Source: "docs\Cogo_Script_Manual.md";         DestDir: "{app}\docs"; Flags: ignoreversion
Source: "docs\Piping_Script_Manual.pdf";      DestDir: "{app}\docs"; Flags: ignoreversion
Source: "docs\Piping_Script_Manual.html";     DestDir: "{app}\docs"; Flags: ignoreversion
Source: "docs\Piping_Script_Manual.md";       DestDir: "{app}\docs"; Flags: ignoreversion
; Guides
Source: "docs\USER_GUIDE.md";                 DestDir: "{app}\docs"; Flags: ignoreversion
Source: "docs\TUTORIAL.md";                   DestDir: "{app}\docs"; Flags: ignoreversion
Source: "docs\DATA_IMPORT_REFERENCE.md";      DestDir: "{app}\docs"; Flags: ignoreversion
Source: "docs\EXAMPLE_PROJECT_WALKTHROUGH.md";DestDir: "{app}\docs"; Flags: ignoreversion
Source: "docs\PIPING_MANUAL.md";              DestDir: "{app}\docs"; Flags: ignoreversion
Source: "docs\PipeScriptManualEdits.md";      DestDir: "{app}\docs"; Flags: ignoreversion
Source: "docs\PERSISTENCE_BEST_PRACTICES.md"; DestDir: "{app}\docs"; Flags: ignoreversion
; Example scripts bundled with docs
Source: "docs\examples\*"; DestDir: "{app}\docs\examples"; Flags: ignoreversion recursesubdirs createallsubdirs

; ── AsBuilt Demo project (JEA W-04471) ────────────────────────────────────────
; Accessed via Help → 🏗 AsBuilt Demo menu
Source: "AsBuiltDemo\README_LOAD_ORDER.txt";    DestDir: "{app}\AsBuiltDemo"; Flags: ignoreversion
Source: "AsBuiltDemo\W-04471_PNEZD.csv";        DestDir: "{app}\AsBuiltDemo"; Flags: ignoreversion
Source: "AsBuiltDemo\W-04471_COGO.cogo";        DestDir: "{app}\AsBuiltDemo"; Flags: ignoreversion
Source: "AsBuiltDemo\W-04471_DESIGN.dxf";       DestDir: "{app}\AsBuiltDemo"; Flags: ignoreversion
Source: "AsBuiltDemo\W-04471_JEA_TEMPLATE.csv"; DestDir: "{app}\AsBuiltDemo"; Flags: ignoreversion
Source: "AsBuiltDemo\W-04471_ASBUILT.dxf";      DestDir: "{app}\AsBuiltDemo"; Flags: ignoreversion
Source: "AsBuiltDemo\W-04471_EXPORT_PNEZD.csv"; DestDir: "{app}\AsBuiltDemo"; Flags: ignoreversion
Source: "AsBuiltDemo\W-04471_REPORT.txt";        DestDir: "{app}\AsBuiltDemo"; Flags: ignoreversion

[Registry]
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocExt}\OpenWithProgids"; ValueType: string; ValueName: "{#MyAppAssocKey}"; ValueData: ""; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}"; ValueType: string; ValueName: ""; ValueData: "{#MyAppAssocName}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\rcs_cogo_icon.ico"
Name: "{autodesktop}\{#MyAppName}";  Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\rcs_cogo_icon.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
