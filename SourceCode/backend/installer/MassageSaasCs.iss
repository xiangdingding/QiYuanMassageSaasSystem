; 齐源按摩门店SaaS系统 · CS 桌面端安装包（Inno Setup 6）
; 编译：用 Inno Setup 打开本文件点 Build，或命令行
;   & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" backend\installer\MassageSaasCs.iss
; 产物输出到 SourceCode\发布包\齐源按摩SaaS-Setup-<版本>.exe
; 发新版只需改下面 MyAppVersion，并保持 AppId 不变（覆盖升级靠同一 AppId）。

#define MyAppName "齐源按摩门店SaaS系统"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "天津齐源科技有限公司"
#define MyAppExeName "MassageSaas.Cs.exe"
; 自包含发布产物目录（先跑 backend\publish-cs.ps1 生成）
#define MyPublishDir "..\src\MassageSaas.Cs\bin\Release\net8.0-windows\publish-cs"

[Setup]
; AppId 全局唯一且固定，切勿更改，否则升级会被当成新软件并行安装
AppId={{8F3A6B21-9C4D-4E7A-B5F0-1A2B3C4D5E6F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\齐源按摩SaaS
DefaultGroupName=齐源按摩SaaS
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
OutputDir=..\..\发布包
OutputBaseFilename=齐源按摩SaaS-Setup-{#MyAppVersion}
SetupIconFile=..\src\MassageSaas.Cs\Assets\app.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; 仅 64 位 Windows
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
; 安装到 Program Files，需要管理员
PrivilegesRequired=admin
; 升级/覆盖安装时自动关闭正在运行的程序（配合 CS 端自动更新）
CloseApplications=force
RestartApplications=no

[Languages]
Name: "chs"; MessagesFile: "ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\齐源按摩门店SaaS系统"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\卸载齐源按摩门店SaaS系统"; Filename: "{uninstallexe}"
Name: "{autodesktop}\齐源按摩门店SaaS系统"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

