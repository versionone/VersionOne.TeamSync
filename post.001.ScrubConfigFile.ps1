$path = Resolve-Path ".\VersionOne.TeamSync.Service\bin\Release\VersionOne.TeamSync.Service.exe.config"

function Clean-ConfigFile {
    $xml = New-Object System.Xml.XmlDocument
    $xml.PreserveWhitespace = $true
    $xml.Load($path)

    ## log4net
    $xml.configuration.log4net.appender | % { 
        $_.filter.levelMin.value = "INFO"
        $_.filter.levelMax.value = "INFO"
     }

    ## serviceSettings
    $xml.configuration.serviceSettings.syncIntervalInSeconds = "600"

	## v1Settings
    $xml.configuration.v1Settings.authenticationType = "0"
	$xml.configuration.v1Settings.url = "http://server/instance"
	$xml.configuration.v1Settings.accessToken = "accessToken"
	$xml.configuration.v1Settings.username = "username"
	$xml.configuration.v1Settings.password = "password"

    ## jiraSettings
    $xml.configuration.jiraSettings.servers.server | % {
        if ($_ -eq $xml.configuration.jiraSettings.servers.server[0]) {
            $_.enabled = ""
            $_.name = ""
            $_.url = "http://server/instance"
            $_.username = "username"
            $_.password = "password"
            $firstMapping = $_.projectMappings.project[0]
            $_.projectMappings.project | % {
                if ($_ -eq $firstMapping) {
                    $_.enabled = ""
                    $_.v1Project = ""
                    $_.jiraProject = ""
                    $_.epicSyncType = ""
                } else {
                    $firstMapping.ParentNode.RemoveChild($_)
                }
            }
        } else {
            $xml.configuration.jiraSettings.servers.RemoveChild($_)
        }
    }

	$xml.Save($path);
}

Clean-ConfigFile