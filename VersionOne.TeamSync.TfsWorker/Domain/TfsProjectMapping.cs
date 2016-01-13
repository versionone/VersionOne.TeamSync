using System;

namespace VersionOne.TeamSync.TfsWorker.Domain
{
    public class TfsProjectMapping
    {
        public string TfsProject { get; private set; }
        public string V1Project { get; private set; }
        public string EpicCategory { get; private set; }
        public DateTime RunFromThisDateOn { get; private set; }

        public TfsProjectMapping(string tfsProject, string v1Project, string epicCategory, DateTime runFromThisDateOn)
        {
            TfsProject = tfsProject;
            V1Project = v1Project;
            EpicCategory = epicCategory;
            RunFromThisDateOn = runFromThisDateOn;
        }
    }
}