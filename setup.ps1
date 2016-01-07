param(
    [alias("t")]
    [string] $tasks = ''
)

function GetModulePath([string]$module){
	$commonPath = @(Get-ChildItem -r -Path packages -filter $module | Sort-Object FullName  -descending)
	return $commonPath[0].FullName
}

function DownloadNuget(){
	new-item (Get-Location).Path -name .nuget -type directory -force
	$destination = (Get-Location).Path + '\.nuget\nuget.exe'	
	Invoke-WebRequest -Uri "http://nuget.org/nuget.exe" -OutFile $destination
}

function DownloadAndImportModules(){
	.nuget\nuget.exe install psake -OutputDirectory packages -NoCache
	.nuget\nuget.exe install psake-tools -Source http://www.myget.org/F/versionone/api/v2/ -OutputDirectory  packages -NoCache
}

function CopyPsakeToolsToRoot(){
	$toolsPath = GetModulePath "psake-tools.ps1"
	Copy-Item -Path $toolsPath -Destination (Get-Location).Path -Force
	$helpersPath = GetModulePath "psake-tools-helpers.ps1"
	Copy-Item -Path $helpersPath -Destination (Get-Location).Path -Force
}

function ImportPsake(){
	$psakePath = GetModulePath "psake.psm1"
	Import-Module $psakePath
}

try{
	DownloadNuget
	DownloadAndImportModules
	ImportPsake
	CopyPsakeToolsToRoot	
	Invoke-psake psake-tools.ps1 ($tasks.Split(',').Trim())	
}
Catch {
	throw "Build failed."
}
Finally {
	if ($psake.build_success -eq $false) {
		throw "Build failed."    	
	}
}