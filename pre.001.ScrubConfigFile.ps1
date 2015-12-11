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
    $xml.configuration.serviceSettings.syncIntervalInMinutes = "15"

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
            $scrubProjectMapping = "true"
            $firstProjectMapping = $_.projectMappings.project[0]
            $_.projectMappings.project | % {
                if ($scrubProjectMapping -eq "true") {
                    $_.enabled = "false"
                    $_.v1Project = "Scope:XXXX"
                    $_.jiraProject = "JIRA Project Key"
                    $_.epicSyncType = "EpicCategory:XXX"
                    $scrubStatusMapping = "true"
                    $firstStatusMapping = $_.statusMappings.status[0]
                    $_.statusMappings.status | % {
                        if ($scrubStatusMapping -eq "true") {
                            $_.enabled = "false"
                            $_.v1Status = ""
                            $_.jiraStatus = ""
                            $scrubStatusMapping = "false"
                        } else {
                            $firstStatusMapping.ParentNode.RemoveChild($_)
                        }
                    }
                    $scrubProjectMapping = "false"
                } else {
                    $firstProjectMapping.ParentNode.RemoveChild($_)
                }
            }
            $scrubPriorityMapping = "true"
            $firstPriorityMapping = $_.priorityMappings.priority[0]
            $_.priorityMappings.defaultJiraPriority = ""
            $_.priorityMappings.priority | % {
                if ($scrubPriorityMapping -eq "true") {
                    $_.enabled = "false"
                    $_.v1Priority = ""
                    $_.jiraPriority = ""
                    $scrubPriorityMapping = "false"
                } else {
                    $firstPriorityMapping.ParentNode.RemoveChild($_)
                }
            }
        } else {
            $xml.configuration.jiraSettings.servers.RemoveChild($_)
        }
    }

	$xml.Save($path);
}

Clean-ConfigFile
