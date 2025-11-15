[Setup]
AppName=Recycle Bin Tracker
AppVersion=1.0
DefaultDirName={pf}\RecycleBinTracker
DefaultGroupName=Recycle Bin Tracker
OutputDir=installer
OutputBaseFilename=RecycleBinTrackerSetup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "bin\Release\net8.0-windows\*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{group}\Recycle Bin Tracker"; Filename: "{app}\WindowBinTracker.exe"
Name: "{group}\Uninstall Recycle Bin Tracker"; Filename: "{uninstallexe}"
Name: "{userstartup}\Recycle Bin Tracker"; Filename: "{app}\WindowBinTracker.exe"

[Run]
Filename: "{app}\WindowBinTracker.exe"; Description: "Launch Recycle Bin Tracker"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{userappdata}\WindowBinTracker"
