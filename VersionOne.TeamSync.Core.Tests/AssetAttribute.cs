using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionOne.TeamSync.Core.Tests
{
    public abstract class AssetNumberAttribute : Attribute
    {
        protected AssetNumberAttribute(string assetNumber)
        {
            
        }

        protected AssetNumberAttribute(string[] assets)
        {
            
        }
    }

    public class DefectNumber : AssetNumberAttribute
    {
        public DefectNumber(string assetNumber) : base(assetNumber)
        {
        }

        public DefectNumber(string[] assets) : base(assets)
        {
        }
    }

    public class StoryNumber : AssetNumberAttribute
    {
        public StoryNumber(string assetNumber) : base(assetNumber)
        {
        }

        public StoryNumber(string[] assets) : base(assets)
        {
        }
    }
}
