function DownloadFile($filename) {
    Write-Host "Downloading $($filename)"
	Invoke-WebRequest "https://github.com/rickparrish/GameSrv/raw/master/bin/Release/$($filename)" -OutFile $filename
}

DownloadFile("GameSrv.dll")
DownloadFile("GameSrvConfig.exe")
DownloadFile("GameSrvConsole.exe")
DownloadFile("GameSrvGUI.exe")
DownloadFile("GameSrvService.exe")
DownloadFile("RMLib.dll")
DownloadFile("RMLibUI.dll")
DownloadFile("Upgrade.exe")
DownloadFile("W32Door.exe")
DownloadFile("update_from_git.ps1")