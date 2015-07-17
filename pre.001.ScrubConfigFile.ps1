$path = Resolve-Path ".\VersionOne.TeamSync.Service\App.config"

function Clean-ConfigFile {
    $xml = New-Object System.Xml.XmlDocument
    $xml.PreserveWhitespace = $true
    $xml.Load($path)

    ## log4net
    $xml.configuration.log4net.appender | % { 
        if ($_.name -eq "RemotingAppender") {
            $_.filter.level.value = "ALL"
        } else{
            if ($_.name -eq "RollingLogFileAppender") {
                $_.filter.level.value = "WARN"
            }
        } 
     }

    ## serviceSettings
    $xml.configuration.serviceSettings.syncIntervalInSeconds = "1800"

	## v1Settings
    $xml.configuration.v1Settings.authenticationType = "0"
	$xml.configuration.v1Settings.url = "http://server/instance"
	$xml.configuration.v1Settings.accessToken = ""
	$xml.configuration.v1Settings.username = ""
	$xml.configuration.v1Settings.password = ""

    ## jiraSettings
    $xml.configuration.jiraSettings.servers.server | % {
        if ($_ -eq $xml.configuration.jiraSettings.servers.server[0]) {
            $_.enabled = "false"
            $_.name = ""
            $_.url = "http://server/instance"
            $_.username = ""
            $_.password = ""
            $firstMapping = $_.projectMappings.project[0]
            $_.projectMappings.project | % {
                if ($_ -eq $firstMapping) {
                    $_.enabled = "false"
                    $_.v1Project = ""
                    $_.jiraProject = ""
                    $_.epicSyncType = "EpicCategory:208"
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
