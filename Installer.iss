#define MyAppName "RCS COGO Enterprise"
#define MyAppVersion "2.1.0"
#define MyAppPublisher "BANKS & BANKS CONSULTING"
#define MyAppExeName "RCS.Cogo.Wpf.exe"
#define MyAppAssocName MyAppName + " Project"
#define MyAppAssocExt ".cogo"
#define MyAppAssocKey StringChange(MyAppAssocName, " ", "") + MyAppAssocExt

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
AppId={{FD0827BF-1172-4D17-BE48-AE1A7F9DD987}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
ChangesAssociations=yes
OutputBaseFilename=RCS.ASBUILT.PRO.Setup
OutputDir=.\Installer
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Core application files from the standalone publish directory
Source: "src\RCS.Cogo.Wpf\bin\Release\net8.0-windows\win-x64\publish\RCS.Cogo.Wpf.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "src\RCS.Cogo.Wpf\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Additional necessary assets
Source: "SymbolsLibrary\*"; DestDir: "{app}\SymbolsLibrary"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "docs\*";           DestDir: "{app}\docs";          Flags: ignoreversion recursesubdirs createallsubdirs
Source: "SampleScripts\*"; DestDir: "{app}\SampleScripts"; Flags: ignoreversion recursesubdirs createallsubdirs

[Registry]
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocExt}\OpenWithProgids"; ValueType: string; ValueName: "{#MyAppAssocKey}"; ValueData: ""; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}"; ValueType: string; ValueName: ""; ValueData: "{#MyAppAssocName}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
