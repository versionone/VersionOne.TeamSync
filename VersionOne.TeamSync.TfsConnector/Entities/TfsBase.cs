using System.Collections.Generic;

namespace VersionOne.TeamSync.TfsConnector.Entities
{
    public abstract class TfsBase
    {

        protected TfsBase()
        {
            ErrorMessages = new List<string>();
        }

        public List<string> ErrorMessages { get; set; }
        public Dictionary<string, string> Errors { get; set; }

        public bool HasErrors
        {
            get { return ErrorMessages.Count > 0; }
        }

    }

    public class BadResult : TfsBase
    {
        //for just mess ups
    }

}