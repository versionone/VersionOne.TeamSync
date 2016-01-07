
function Clean-Characters {
	param(
		[Parameter(Mandatory=$true, ValueFromPipeline=$true)]
		[object]$obj
	)
		$obj.psobject.properties |
		? { $_.Value.GetType().Name.Equals("String") -and $_.Value.Contains(';')} |
		% {	$_.Value = ($_.Value -replace ';', '`;') }

		$obj.psobject.properties |
		? { $_.Value.GetType().Name.Equals("Object[]") } |
		% {	$_.Value | % { $_ = (Clean-Characters $_ ) } }

		$obj.psobject.properties |
		? { $_.Value.GetType().Name.Equals("Object") } |
		% {	$_.Value | % { $_ = (Clean-Characters $_ ) } }

		$obj
}


function Get-ConfigObjectFromFile {
	param([string]$fileName)

	gc $fileName -Raw |
	ConvertFrom-Json |
	Clean-Characters
}



function Get-EnvironmentVariableOrDefault {
	param([string]$variable, [string]$default)
	if([Environment]::GetEnvironmentVariable($variable))
	{
		[Environment]::GetEnvironmentVariable($variable)
	}
	else
	{
		$default
	}
}

function Get-NewestFilePath {
	param([string]$startingPath,[string]$file)

	$paths = @(ls -r -Path $startingPath -filter $file | sort FullName -descending)
	$paths[0].FullName
}

function New-NugetDirectory {
	param([string]$path)
	New-Item $path -name .nuget -type directory -force
}

function Get-NugetBinary {
	param([string]$path)
	$destination = $path + '\.nuget\nuget.exe'
	curl -Uri "http://nuget.org/nuget.exe" -OutFile $destination
}

function Get-BuildCommand {
	"msbuild $($config.solution) -t:Build -p:Configuration=$($config.configuration) `"-p:Platform=$($config.platform)`""
}

function Get-CleanCommand {
	"msbuild $($config.solution) -t:Clean -p:Configuration=$($config.configuration) `"-p:Platform=$($config.platform)`""
}

function Get-PublishCommand {
	"msbuild $($config.projectToPublish) -t:Publish -p:Configuration=$($config.configuration) `"-p:Platform=AnyCPU`""
}

function Get-RestorePackagesCommand {
	".\\.nuget\nuget.exe restore $($config.solution) -Source $($config.nugetSources)"
}

function Get-UpdatePackagesCommand {
	".\\.nuget\nuget.exe update $($config.solution) -Source $($config.nugetSources) -NonInteractive -FileConflictAction Overwrite"
}

function Get-GeneratePackageCommand {
    param([string]$project)

    $props = "Configuration=$($config.configuration)"
    if ($config.nuspecTokens) {
    	$props = $props + ";" + (Stringify $config.nuspecTokens)
    }
    ".\\.nuget\nuget.exe pack $project -Verbosity Detailed -Version $version -prop ""$props"""
}

function Get-GeneratePackageCommandFromNuspec {
    param([string]$nuspecFilePath)
	".\\.nuget\nuget.exe pack $nuspecFilePath -Verbosity Detailed"
}

function Get-PushMygetCommand {
	param([string]$apiKey,[string]$repoUrl)
	".\\.nuget\nuget.exe push *.nupkg $apiKey -Source $repoUrl"
}

function Get-PushNugetCommand {
	param([string]$apiKey)
	".\\.nuget\nuget.exe push *.nupkg $apiKey"
}

function Get-InstallNRunnersCommand {
	".\\.nuget\nuget.exe install NUnit.Runners -OutputDirectory packages"
}

function Get-InstallNSpecCommand {
	".\\.nuget\nuget.exe install nspec -OutputDirectory packages"
}

function Get-ProjectsToPackage {
    ($config.projectToPackage).Split(",")
}

function Get-ProjectsToZip {
	($config.projectsToZip).Split(",")
}

function Get-Assemblies {
	param([string]$startingPath)
	if (-not $startingPath) { $startingPath = (pwd).Path }

	@(ls -r -path $startingPath -filter AssemblyInfo.cs) +
	@(ls -r -path $startingPath -filter AssemblyInfo.fs)
}

function Update-Assemblies {
	param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        $file,
        $cfg
	)

	begin {
		$versionPattern = 'AssemblyVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)'
		$versionAssembly = 'AssemblyVersion("' + $version + '")';
		$versionFilePattern = 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)'
		$versionAssemblyFile = 'AssemblyFileVersion("' + $version + '")'
	}

	process
	{
    echo Updating file $file.FullName
		$tmp = ($file.FullName + ".tmp")
		if (test-path ($tmp)) { remove-item $tmp }
		if ($cfg.assemblyInfo -ne $null){
			$info = $cfg.assemblyInfo | Where-Object { (get-item $file.DirectoryName).Parent.Name -eq $_.id }
			if ($info) {
				$product = if($info.product) { $info.product } else { $cfg.product }
				$title = if($info.title) { $info.title } else { $cfg.title }
				$description = if($info.description) { $info.description } else { $cfg.description }
				$company = if($info.company) { $info.company } else { $cfg.company }
				$copyright = if($info.copyright) { $info.copyright } else { $cfg.copyright }

				"using System;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
[assembly: AssemblyVersion(""$version"")]
[assembly: AssemblyFileVersion(""$version"")]

[assembly: AssemblyProduct(""$($product)"")]
[assembly: AssemblyTitle(""$($title)"")]
[assembly: AssemblyDescription(""$($description)"")]
[assembly: AssemblyCompany(""$($company)"")]
[assembly: AssemblyCopyright(""$($copyright)"")]
[assembly: AssemblyConfiguration(""$($cfg.configuration)"")]" > $tmp

			if (test-path ($file.FullName)) { remove-item $file.FullName }
				move-item $tmp $file.FullName -force
			}
		}
		else {
			(gc $file.FullName) |
			% {$_ -replace $versionFilePattern, $versionAssemblyFile } |
			% {$_ -replace $versionPattern, $versionAssembly } `
			> $tmp
			if (test-path ($file.FullName)) { remove-item $file.FullName }
			move-item $tmp $file.FullName -force
		}
	}
}

function Get-Version {
    param([DateTime]$currentUtcDate, [string]$buildNumber)
    if(($config.version -ne $null) -and ($config.version -match '[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+')){
        $version = $config.version
    }
    else {
        $patch = $config.patch
        if (-not $patch) {
            $year = $currentUtcDate.ToString("yy")
            if( -not $buildNumber) { $buildNumber = $currentUtcDate.ToString("HHmm") }

            $dayOfyear = $currentUtcDate.DayOfYear
            if(([string]$dayOfyear).Length -eq 1) {
                $dayOfyear=  "00" + $dayOfyear
            }
            elseif(([string]$dayOfyear).Length -eq 2) {
                $dayOfyear = "0" + $dayOfyear
            }
            $patch = "$year$dayOfyear"
        }
        $version = "$($config.major).$($config.minor).$patch.$buildNumber"
    }
    return $version
}

function Get-PreExtensions {
	param([string]$path)
    [array](gci *.ps1 -Path $path |
	? { $_.FullName -match "pre.[0-9]{3}\..*?\.ps1" }  |
    sort FullName)
}

function Get-PostExtensions {
	param([string]$path)
	[array](gci *.ps1 -Path $path |
	? { $_.FullName -match "post.[0-9]{3}\..*?\.ps1" }  |
    sort FullName)
}

function Invoke-Extensions {
	param([Parameter(Mandatory=$false,ValueFromPipeline=$true)]$extension)

	process {
		if(-not $extension) { return }
        echo "The next extension has been loaded: $($extension.Name)"
		& ($extension.FullName)
	}
}

function Get-UnitTests {
	param([string]$path)
	,@(ls -r *.Tests.dll -Path $path |
	? { $_.FullName -like "*\bin\$($config.configuration)\*.Tests.dll" })
}

function Get-IntegrationTests {
	param([string]$path)
	,@(ls -r *.IntegrationTests.dll -Path $path |
	? { $_.FullName -like "*\bin\$($config.configuration)\*.IntegrationTests.dll" })
}

function Invoke-NunitTests {
	param([string]$path)
	$target = Get-UnitTests $path
	$bin = Get-NewestFilePath "$path\packages" "nunit-console-x86.exe"
	Invoke-TestsRunner $bin $target
}

function Invoke-NspecTests {
	param([string]$path)
	$target = Get-UnitTests $path
	$bin = Get-NewestFilePath "$path\packages" "NSpecRunner.exe"
	Invoke-TestsRunner $bin $target
}

function Invoke-MsTests {
	param(
		[Parameter(Mandatory=$true,ValueFromPipeline=$true)]$target,
		[Parameter(Mandatory=$true,Position=0)]$resultPath)
	process{
		#$bin = Get-NewestFilePath (Get-ChildItem -path $env:systemdrive\ -filter "mstest.exe" -erroraction silentlycontinue -recurse)[0].FullName
		$bin = "C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\MSTest.exe"
		$target | % {
			iex "& '$bin' /testcontainer:'$($_.FullName)' /resultsfile:'$resultPath\$($_.Name -replace '.Tests.dll', '.TestResults.trx')'"

			if($lastExitCode -ne 0){
				throw 'Invoke-MsTests failed.'
			}
		}

	}
}

function Invoke-TestsRunner {
	param($bin,$target)

	if ($target.Length -ne 0) {
		$target | % { iex "& '$bin' '$($_.FullName)'" }
	} else {
		Write-Host "There are no targets specified to run."
	}
}

function Publish-Documentation {
	# ----- Prepare Branches ------------------------------------------------------
	git checkout -f gh-pages
	git checkout master

	# ----- Publish Documentation -------------------------------------------------
	## Publishes a subdirectory "doc" of the main project to the gh-pages branch.
	## From: http://happygiraffe.net/blog/2009/07/04/publishing-a-subdirectory-to-github-pages/
	$docHash = ((git ls-tree -d HEAD doc) -split "\s+")[2]
	$newCommit = (Write-Host "Auto-update docs." | git commit-tree $docHash -p refs/heads/gh-pages)
	git update-ref refs/heads/gh-pages $newCommit


	# ----- Push Docs -------------------------------------------------------------
	## Push changes.
	git push origin gh-pages
}

function Get-PublishCatalogConfig {
    param([string]$fileName)

	if (Test-Path $fileName) {
		Get-Content $fileName -Raw | ConvertFrom-Json
	}
}

function Publish-Catalog {
    param([string]$productFile='product.json')

	$staging = Get-PublishCatalogConfig 'staging.json'

    if ($staging -eq $null) {
        $staging = @{}
		$staging.url = $Env:staging_url;
		$staging.username = $Env:staging_username;
		$staging.password = $Env:staging_password;
	}

	if (-not (Test-Path $productFile)) {
		"File $productFile does not exist. Upload aborted."
	}

    try{
	    $response = Invoke-WebRequest `
		    -Uri $staging.url `
		    -Headers @{"Authorization" = "Basic "+[System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($staging.username+":"+$staging.password ))} `
		    -Method Put `
		    -InFile $productFile `
		    -ContentType 'application/json'
	}
	catch {
	    if($_.Exception.Response -ne $null){
	        $stream = $_.Exception.Response.GetResponseStream()
	        [void]$stream.Seek(0, [System.IO.SeekOrigin]::Begin)
	        $reader = New-Object System.IO.StreamReader $stream
	        $response = $reader.ReadToEnd()
	        $reader.close()
	        $stream.close()
	        throw $_.Exception.Message + "`nFile $productFile failed with response: " + $response
	    }
	    else{
	        throw $_.Exception
	    }
	}

    if ($response.StatusCode -ne "200") {
        throw $response.Content
    }

    Write-Host $response.Content
}

function Promote-Catalog {
    param([string]$productId)

	$staging = Get-PublishCatalogConfig 'staging.json'

	if ($staging -eq $null) {
        $staging = @{}
		$staging.url = $Env:staging_url;
	}

	$production = Get-PublishCatalogConfig 'production.json'
	if ($production -eq $null) {
        $production = @{}
		$production.url = $Env:production_url;
		$production.username = $Env:production_username;
		$production.password = $Env:production_password;
	}

	$parameters = @{'id'= $productId}

	$stagingResponse = Invoke-WebRequest `
	    -Uri $staging.url `
	    -Method Get `
	    -Body $parameters

	Write-Debug "staging: $($stagingResponse.StatusCode) - $($stagingResponse.StatusDescription)"

	$productionResponse = Invoke-WebRequest `
	    -Uri $production.url `
	    -Headers @{"Authorization" = "Basic "+[System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($production.username+":"+$production.password ))} `
	    -Method Put `
	    -Body $stagingResponse.Content `
	    -ContentType 'application/json'

	Write-Debug "production: $($productionResponse.StatusCode) - $($productionResponse.StatusDescription)"

	$productionResponse
}

function Publish-CatalogFromGitShow {
	$staging = Get-PublishCatalogConfig 'staging.json'
	if ($staging -eq $null) {
        $staging = @{}
		$staging.url = $Env:staging_url;
		$staging.username = $Env:staging_username;
		$staging.password = $Env:staging_password;
	}

	$git_files =  git show --name-only --pretty="format:"

	foreach($git_file in $git_files){
		if($git_file -ne "") {
			if ((Test-Path $git_file) -and ($git_file.EndsWith(".json",1))) {
				Write-Debug "Processing: $git_file"
				try{
					$stagingResponse = Invoke-WebRequest `
					    -Uri $staging.url `
					    -Headers @{"Authorization" = "Basic "+[System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($staging.username+":"+$staging.password ))} `
					    -Method Put `
					    -InFile $git_file `
					    -ContentType 'application/json'

					Write-Debug " > $($stagingResponse.StatusCode) - $($stagingResponse.StatusDescription)"
				}
				catch {
				    if($_.Exception.Response -ne $null){
				        $stream = $_.Exception.Response.GetResponseStream()
				        [void]$stream.Seek(0, [System.IO.SeekOrigin]::Begin)
				        $reader = New-Object System.IO.StreamReader $stream
				        $response = $reader.ReadToEnd()
				        $reader.close()
				        $stream.close()
				        throw $_.Exception.Message + "`nFile $git_file failed with response: " + $response
				    }
				    else{
				        throw $_.Exception
				    }
				}



                Write-Host $stagingResponse.Content
			}
			else{
				Write-Debug "Nothing to do."
			}
		}
	}
}

function Compress-Folder {
    param($targetFolder, $zipPathDestination)

    $zipFileName = Split-Path $zipPathDestination -Leaf
	[Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.FileSystem") | Out-Null
	$compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
	[System.IO.Compression.ZipFile]::CreateFromDirectory($targetFolder,
	    "$Env:TEMP\$zipFileName", $compressionLevel, $false)

    Move-Item -Path "$Env:TEMP\$zipFileName" -Destination $zipPathDestination -Force
}

function Compress-FileList {
	param([string]$path)
	[Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.FileSystem")
	[Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.ZipFile")
	[Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.ZipFileExtensions")
	$compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
	if($config.zip -ne $null){
		$config.zip |
		% {
			$zipFilePath = "$path\$($_.name)_$version.zip"
			Write-Host $zipFilePath
			if(Test-Path $zipFilePath) { Remove-Item $zipFilePath }
			$archive = [System.IO.Compression.ZipFile]::Open($zipFilePath,"Update")
			$_.filesToZip.Split(",") |
			% {
				$file = Get-NewestFilePath $path $_
				$null = [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file, $_, $compressionLevel)
			}
			$archive.Dispose()
		}
	}
}

function IsPathRooted {
	Param(
		[Parameter(Mandatory=$true, ValueFromPipeline=$true)]
		[String] $Path)
	return [System.IO.Path]::IsPathRooted("$Path")
}

function Root-Path {
	Param(
		[Parameter(Mandatory=$true, ValueFromPipeline=$true)]
		[String] $Path,

		[String] $Parent = (Get-Location).Path)

	if (IsPathRooted $Path) {
		return $Path
	}

	return Join-Path -Path $Parent -ChildPath $Path
}

function Compress-Files {
	Param(
		[parameter(Mandatory=$true)] [String] $ZipPath,
		[parameter(Mandatory=$true)] [String[]] $Files)

	[Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.FileSystem")
	[Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.ZipFile")
	[Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.ZipFileExtensions")
	$compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
  try{
  	$path = Root-Path $ZipPath
  	$parent = Split-Path $path -Parent

  	## Ensure path to zip file exists
  	if ($parent -and !(Test-Path $parent -PathType Container)) {
  		New-Item $parent -ItemType Directory -Force
  	}

  	$archive = [System.IO.Compression.ZipFile]::Open($path,"Update")

  	$Files | % {
			$rootedPath = Root-Path $_
			if (Test-Path $rootedPath -pathType leaf) {
				$entryName = Split-Path -Path $rootedPath -Leaf
				[System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $rootedPath, $entryName, $compressionLevel)
			}
			else {
				$parent = (Get-Item $rootedPath).Parent.FullName
				Get-ChildItem $rootedPath -recurse | Where-Object { !($_.PSIsContainer) } | % {
					$entryName = $_.FullName.Replace($parent, "")

					if ($entryName.StartsWith('\')) {
						$entryName = $entryName.Substring(1)
					}
					[System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $_.FullName, $entryName, $compressionLevel)
				}
			}
		}
	}
  finally {
  	if ($archive) {
  		$archive.Dispose()
  	}
  }
}

function Extract-File {
	[CmdletBinding()]
	param (
		[Parameter(Mandatory=$true, ValueFromPipeline=$true)]
		[String] $File,

		[ValidateNotNullOrEmpty()]
		[String] $Destination = (Get-Location).Path)

	[System.Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.FileSystem") | Out-Null
	[System.IO.Compression.ZipFile]::ExtractToDirectory((Root-Path $File), (Root-Path $Destination))
}

function Stringify {
	param ([Parameter(Mandatory=$true)] $obj)

	$result = ""
	$obj.psobject.properties | % {
		$result = $result + "$($_.Name)=$($_.Value);"
	}
	return $result
}

function Clean-ConfigFile {
	$path = Resolve-Path ".\bin\VersionOne.ServiceHost.exe.config"
	$xml = [xml](Get-Content $path)

	## GENERAL SETTINGS
	$xml.configuration.Services.LogService.Console.LogLevel = "Info"
	$xml.configuration.Services.LogService.File.LogLevel = "Info"
	$xml.configuration.Services.ProfileFlushTimer.Interval = "30000"

	$node = $xml.configuration.Services.ChildNodes | where { $_.Name -Like "*ServiceTimer" }
	if ($node -ne $null) {
	    $node.Interval = "60000"
	}

	## VERSIONONE SYSTEM
	$xml.configuration.Services.WorkitemWriterService.Settings.ApplicationUrl = "http://server/instance"
	$accessTokenNode = Get-XmlNode -XmlDocument $xml -NodePath "configuration.Services.WorkitemWriterService.Settings.AccessToken"
	if ($accessTokenNode -ne $null) {
		$accessTokenNode.InnerText = "accessToken"
	}
	$xml.configuration.Services.WorkitemWriterService.Settings.Username = "username"
	$xml.configuration.Services.WorkitemWriterService.Settings.Password = "password"
	$uriNode = Get-XmlNode -XmlDocument $xml -NodePath "configuration.Services.WorkitemWriterService.Settings.ProxySettings.Uri"
	if ($uriNode -ne $null) {
		$uriNode.InnerText = "http://proxyhost"
	}
	else {
		$urlNode = Get-XmlNode -XmlDocument $xml -NodePath "configuration.Services.WorkitemWriterService.Settings.ProxySettings.Url"
		if ($urlNode -ne $null) {
			$urlNode.InnerText = "http://proxyhost"
		}
	}
	$xml.configuration.Services.WorkitemWriterService.Settings.ProxySettings.UserName = "username"
	$xml.configuration.Services.WorkitemWriterService.Settings.ProxySettings.Password = "password"
    $xml.configuration.Services.WorkitemWriterService.Settings.ProxySettings.Domain = "domain"

	## TARGET SYSTEM
	$serviceName = $config.targetSystemConfig.PSObject.Properties | select -First 1 Name
	$serviceNode = $xml.configuration.Services.ChildNodes | where { $_.Name -Like $serviceName.Name }
	if ($serviceNode -ne $null) {
	    $config.targetSystemConfig.$($serviceNode.Name).PSObject.Properties | % {
			$currentConfig = $serviceNode.$($_.Name)
	        if ($_.Value.Attributes -ne $null) {
				$_.Value.Attributes | % {
					 $_.PSObject.Properties | % { $currentConfig.$($_.Name)=$_.Value }
				}
			} else {
				if ([string]::IsNullOrEmpty($_.Value) -and $serviceNode.$($_.Name).ChildNodes.Count -gt 0) {
					$currentConfig.Mapping | % { $currentConfig.RemoveChild($_) }
				} else {
					$serviceNode.$($_.Name)=$_.Value
				}
			}
	    }
	}

	$xml.Save($path);
}

function Get-XmlNode([ xml ]$XmlDocument, [string]$NodePath, [string]$NamespaceURI = "", [string]$NodeSeparatorCharacter = '.')
{
    # If a Namespace URI was not given, use the Xml document's default namespace.
    if ([string]::IsNullOrEmpty($NamespaceURI)) { $NamespaceURI = $XmlDocument.DocumentElement.NamespaceURI }

    # In order for SelectSingleNode() to actually work, we need to use the fully qualified node path along with an Xml Namespace Manager, so set them up.
    $xmlNsManager = New-Object System.Xml.XmlNamespaceManager($XmlDocument.NameTable)
    $xmlNsManager.AddNamespace("ns", $NamespaceURI)
    $fullyQualifiedNodePath = "/ns:$($NodePath.Replace($($NodeSeparatorCharacter), '/ns:'))"

    # Try and get the node, then return it. Returns $null if the node was not found.
    $node = $XmlDocument.SelectSingleNode($fullyQualifiedNodePath, $xmlNsManager)
    return $node
}