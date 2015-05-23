Option Explicit

On Error Resume Next

Dim objWshShell

Set objWshShell = CreateObject("WScript.Shell")

objWshShell.Run "sc stop Plax.FM",0,true
objWshShell.Run "sc delete  Plax.FM ",0,true


Set objWshShell = Nothing