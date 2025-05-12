!include "MUI.nsh"

!define MUI_ICON "icon.ico"

name "SimplePasswordManager"
InstallDir "$PROGRAMFILES\SimplePasswordManager"
OutFile "install.exe"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_DIRECTORY
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

Section "Install"
    SetOutPath "$INSTDIR"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SPM" "DisplayName" "SimplePasswordManager"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SPM" "DisplayVersion" "0.0.1a"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SPM" "Publisher" "alpenstorm"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SPM" "DisplayIcon" "$INSTDIR\spm.ico"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SPM" "UninstallString" "$INSTDIR\uninstall.exe"
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SPM" "NoModify" 1
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SPM" "NoRepair" 1
    
    File "bin/debug/net8.0/SimplePasswordManager.exe"
    File "bin/debug/net8.0/SimplePasswordManager.dll"
    File "spm.ico"

    # Add to system PATH
    ReadRegStr $0 HKLM "SYSTEM\CurrentControlSet\Control\Session Manager\Environment" "Path"
    StrCpy $1 "$INSTDIR"
    ${If} $0 != ""
        StrCpy $0 "$0;$1"
    ${Else}
        StrCpy $0 "$1"
    ${EndIf}
    WriteRegExpandStr HKLM "SYSTEM\CurrentControlSet\Control\Session Manager\Environment" "Path" "$0"
    SendMessage ${HWND_BROADCAST} ${WM_SETTINGCHANGE} 0 "STR:Environment" /TIMEOUT=5000

    WriteUninstaller "$INSTDIR\uninstall.exe"
SectionEnd

Section "Uninstall"
    # Remove from system PATH
    ReadRegStr $0 HKLM "SYSTEM\CurrentControlSet\Control\Session Manager\Environment" "Path"
    StrCpy $1 "$INSTDIR"
    ${If} $0 != ""
        StrLen $2 "$1"
        StrCpy $3 $0 $2 0
        ${If} $3 == $1
            StrCpy $0 $0 "" $2
            ${If} $0 != ""
                StrCpy $0 $0 "" 1
            ${EndIf}
        ${Else}
            StrCpy $4 $0 1 -1
            ${If} $4 == ";"
                StrCpy $0 $0 -1
            ${EndIf}
            StrCpy $0 $0 -1
            StrCpy $3 $0 $2 0
            ${If} $3 == $1
                StrCpy $0 $0 "" $2
            ${EndIf}
        ${EndIf}
        WriteRegExpandStr HKLM "SYSTEM\CurrentControlSet\Control\Session Manager\Environment" "Path" "$0"
        SendMessage ${HWND_BROADCAST} ${WM_SETTINGCHANGE} 0 "STR:Environment" /TIMEOUT=5000
    ${EndIf}

    Delete "$INSTDIR\SimplePasswordManager.exe"
    Delete "$INSTDIR\SimplePasswordManager.dll"
    Delete "$INSTDIR\spm.ico"
    Delete "$INSTDIR\uninst.exe"

    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SPM"
SectionEnd