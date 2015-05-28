using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionOne.Api.Interfaces
{
    public interface IV1Asset
    {
        string AssetType { get; }
        string ID { get; }

        string Error { get; }
        bool HasErrors { get; }
    }
}
