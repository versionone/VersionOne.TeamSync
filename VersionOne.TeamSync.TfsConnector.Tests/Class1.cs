using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Authenticators;
using VersionOne.TeamSync.Core;
using VersionOne.TeamSync.TfsConnector.Connector;

namespace VersionOne.TeamSync.TfsConnector.Tests
{
    public class TestThisThingBeforeGettingIntoUnitTestHole
    {
        public static void Main(string[] args)
        {
            const string baseUrl = "http://v1tfs2015.cloudapp.net:8080";

            var client = new RestClient(new Uri(new Uri(baseUrl), "/tfs/DefaultCollection/_apis/wit").ToString());
            client.Authenticator = new HttpBasicAuthenticator("v1deploy", "Versi0n1.c26nu");
            var tfsConnector = new Connector.TfsConnector(client, new V1LogFactory());
            var response = tfsConnector.Get("/workitems?id=6");
            System.Console.Write(response);
        }

    }
   
}

