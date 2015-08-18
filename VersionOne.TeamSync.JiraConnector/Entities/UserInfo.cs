using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionOne.TeamSync.JiraConnector.Entities
{
    public class UserInfo
    {
        //        public string self { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string EmailAddress { get; set; }
        //        public AvatarUrls avatarUrls { get; set; }
        public string DisplayName { get; set; }
        public bool Active { get; set; }
        public string TimeZone { get; set; }
        public string Locale { get; set; }
        public Groups Groups { get; set; }
        //        public string expand { get; set; }
    }

    //public class AvatarUrls
    //{
    //    public string small_16x16 { get; set; }
    //    public string medium_24x24 { get; set; }
    //    public string large_32x32 { get; set; }
    //    public string xlarge_48x48 { get; set; }
    //}

    public class GroupItem
    {
        public string Name { get; set; }
        //        public string self { get; set; }
    }

    public class Groups
    {
        public int Size { get; set; }
        public List<GroupItem> Items { get; set; }
    }
}
