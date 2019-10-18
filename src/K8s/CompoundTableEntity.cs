using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;

namespace AzSignalR.Monitor.Storage.Entities
{
    public class CompoundTableEntity : TableEntity
    {
        public CompoundTableEntity() : base() { }
        public CompoundTableEntity(string partitionKey, string rowKey) : base(partitionKey, rowKey) { }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var results = base.WriteEntity(operationContext);
            //EntityPropertySerializer.Serialize(this, results);
            return results;
        }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            base.ReadEntity(properties, operationContext);
            //EntityPropertySerializer.DeSerialize(this, properties);
        }
    }
}
