<img src="https://github.com/shiitake/Plax.FM/blob/master/Plax.FM.png" width="50" height="50" align="left" />
<img src="https://github.com/shiitake/Plax.FM/blob/master/Plax.FM.png" width="50" height="50" align="right" />
<h1 align="center">Plax.FM</h1>

Plax.FM is a small windows service which scrobbles music you've played on your Plex Media server. 

I've tried to make the Plax.FM installation and setup as easy as possible. It should be able to run on any recent Windows versions without having to install any 3rd party dependencies. 

Plax.FM was inspired by [Plex-Lastfm-Scrobbler](https://github.com/jesseward/plex-lastfm-scrobbler). If you are running on Linux or OSX (or you just like Python) I highly recommend checking it out. 


##### System requirements: 
- Windows 7, 8, 8.1  
- Microsoft .NET Framework 4.5
- Plex Media Server needs to be installed locally.


##### Installation instructions:
1. Download the installation files [here](https://www.dropbox.com/s/fwuoinfror2sbd7/Plax.FM.zip?dl=0)
2. Unzip the files and run setup.exe
3. After completing the installation you should have the option to start Plax.FM.
4. You should see the system tray application which will give you the initial setup options and let you start/stop the service. 
5. At the initial setup screen you will need to do the following: 
  * Input your plex media credentials
  * After submitting this you should get a prompt which will open your browser window to the last.fm website so you can authorize Plax.FM to scrobble to your account. You will likely have to login first. 

##### Caveats/Issues: 
- Plex Media server defaults to localhost on port 32400 - (this can be configured manually by editing the app.config file)
- You will want to install this using the same windows profile that the Plex is using. 
- There will sometimes be contention issues when Plax.FM tries to read the Plex Media Server log. This shouldn't cause to much of an issue - it will just try again.


##### Future Features:
- Adding better options for custom server location/port
- Adding support for multiple plex and last.fm user

To see the current development status you can checkout the [trello board.](https://trello.com/b/zrBfd00v/plax-fm-development-board)
