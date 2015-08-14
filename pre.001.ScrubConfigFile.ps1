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
    $scrubServer = "true"
    
    $xml.configuration.jiraSettings.servers.server | % {
        if ($scrubServer -eq "true") {
            $_.enabled = "false"
            $_.name = ""
            $_.url = "http://server/instance"
            $_.username = ""
            $_.password = ""
            $scrubServer = "false"
            $scrubMapping = "true"
            $_.projectMappings.project | % {
                if ($scrubMapping -eq "true") {
                    $_.enabled = "false"
                    $_.v1Project = ""
                    $_.jiraProject = ""
                    $_.epicSyncType = "EpicCategory:208"
                    $scrubMapping = "false"
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
