On Error Resume Next

Dim ProcessArchitectureX86

Dim ProcessArchitectureW6432

Set objShell = CreateObject("WScript.Shell")

ProcessArchitectureX86 = objShell.ExpandEnvironmentStrings("%PROCESSOR_ARCHITECTURE%")

if objShell.ExpandEnvironmentStrings("%PROCESSOR_ARCHITEW6432%") = "%PROCESSOR_ARCHITEW6432%" Then

ProcessArchitectureW6432 = "Not Defined"

End If

IF ProcessArchitectureX86 = "x86" And ProcessArchitectureW6432 = "Not Defined" Then
   'OS is 32bit
   installService="cmd /K CD ""C:\Program Files\Shiitake Studios\PlaxFM\"" & PlaxFM.exe install"
ELSE
   'OS is 64bit
  installService="cmd /K CD ""C:\Program Files (x86)\Shiitake Studios\PlaxFM\"" & PlaxFM.exe install"
END IF

objShell.Run installService,0