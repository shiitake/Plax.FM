Dim objShell

Set objShell = WScript.CreateObject("WSCript.shell")

'remove executable as service
installService="cmd /K CD ""C:\Program Files (x86)\Shiitake Studios\PlexScrobble\"" & PlexScrobble.exe uninstall"

objShell.run installService,0