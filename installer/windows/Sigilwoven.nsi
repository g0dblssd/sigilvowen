Unicode True

!ifndef SOURCE_DIR
  !error "SOURCE_DIR is required"
!endif
!ifndef OUTPUT_FILE
  !define OUTPUT_FILE "Sigilwoven-Setup-Windows-x86_64.exe"
!endif

Name "Sigilwoven"
OutFile "${OUTPUT_FILE}"
InstallDir "$LOCALAPPDATA\Programs\Sigilwoven"
InstallDirRegKey HKCU "Software\Sigilwoven" "InstallDir"
RequestExecutionLevel user
SetCompressor /SOLID lzma
BrandingText "Sigilwoven Patch 039"

VIProductVersion "0.0.39.0"
VIAddVersionKey "ProductName" "Sigilwoven"
VIAddVersionKey "FileDescription" "Sigilwoven Windows Installer"
VIAddVersionKey "FileVersion" "0.0.39"
VIAddVersionKey "ProductVersion" "0.0.39"

Page directory
Page instfiles
UninstPage uninstConfirm
UninstPage instfiles

Section "Sigilwoven" SEC_GAME
  SetOutPath "$INSTDIR"
  File /r "${SOURCE_DIR}\*.*"
  WriteRegStr HKCU "Software\Sigilwoven" "InstallDir" "$INSTDIR"
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  CreateDirectory "$SMPROGRAMS\Sigilwoven"
  CreateShortcut "$SMPROGRAMS\Sigilwoven\Sigilwoven.lnk" "$INSTDIR\Sigilwoven.exe"
  CreateShortcut "$SMPROGRAMS\Sigilwoven\Uninstall Sigilwoven.lnk" "$INSTDIR\Uninstall.exe"
  CreateShortcut "$DESKTOP\Sigilwoven.lnk" "$INSTDIR\Sigilwoven.exe"

  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Sigilwoven" "DisplayName" "Sigilwoven"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Sigilwoven" "DisplayVersion" "0.0.39"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Sigilwoven" "Publisher" "Sigilwoven"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Sigilwoven" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Sigilwoven" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Sigilwoven" "NoModify" 1
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Sigilwoven" "NoRepair" 1
SectionEnd

Section "Uninstall"
  Delete "$DESKTOP\Sigilwoven.lnk"
  Delete "$SMPROGRAMS\Sigilwoven\Sigilwoven.lnk"
  Delete "$SMPROGRAMS\Sigilwoven\Uninstall Sigilwoven.lnk"
  RMDir "$SMPROGRAMS\Sigilwoven"
  RMDir /r "$INSTDIR"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Sigilwoven"
  DeleteRegKey HKCU "Software\Sigilwoven"
SectionEnd
