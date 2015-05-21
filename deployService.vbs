Dim objShell

Set objShell = WScript.CreateObject("WSCript.shell")

'install executable as service
installService="cmd /K CD ""C:\Program Files (x86)\Shiitake Studios\PlexScrobble\"" & PlexScrobble.exe install"
'WScript.Echo installService

objShell.run installService,0