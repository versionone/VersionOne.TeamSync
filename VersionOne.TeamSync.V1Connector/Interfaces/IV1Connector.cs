using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace VersionOne.TeamSync.V1Connector.Interfaces
{
    public interface IV1Connector
    {
        string InstanceUrl { get; }

        Task<XDocument> Operation(IV1Asset asset, string operation); //this is higher level so should this live here?
        Task<XDocument> Post(IV1Asset asset, XDocument postPayload);
        Task<List<T>> Query<T>(string asset, string[] properties, string[] wheres, Func<XElement, T> returnObject);
        Task<List<T>> Query<T>(string asset, string[] properties, Func<XElement, T> returnObject);
        Task<XDocument> Operation(string assetType, string assetId, string operation);
        bool IsConnectionValid();
        Task QueryOne(string assetType, string assetId, string[] properties, Action<XElement> assetResult);
    }
}
