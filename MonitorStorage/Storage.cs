using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading;
using System.Threading.Tasks;
using MonitorStorage.Models;

namespace MonitorStorage
{
    public class Storage<k> : IStorage<k> where k:Base
    {
        CloudTableClient _tableClient;
        CloudTable _table;
        public Storage(CloudStorageAccount cloudStorageAccount)
        {
            _tableClient = cloudStorageAccount.CreateCloudTableClient();
        }

        public async Task CreateTable(k tableName)
        {
            _table = _tableClient.GetTableReference(tableName.GetType().Name);
            await _table.CreateIfNotExistsAsync();
        }
        public async Task<IList<TableResult>> AddEntity(List<k> entity)
        {
            IList<TableResult> result = null;
            try
            {
                int slice = entity.Count % 100 == 0 ? entity.Count / 100 : (entity.Count / 100) + 1;
                for (int i = 0; i < slice; i++)
                {
                    IEnumerable<k> slicedData = entity.Skip(i*100).Take((i + 1) * 100);
                    _table = _tableClient.GetTableReference(entity[0].GetType().Name);
                    TableBatchOperation batchOperation = new TableBatchOperation();
                    slicedData.ToList().ForEach(e =>
                    {
                        batchOperation.InsertOrReplace(e);
                    });
                    result = await _table.ExecuteBatchAsync(batchOperation).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }

        public async Task<IList<TableResult>> DeleteEntity(k entity)
        {
            TableBatchOperation tbOpertion = new TableBatchOperation();
            CloudTable cTable = _tableClient.GetTableReference(entity.GetType().Name);
            tbOpertion.Delete(entity);
            IList<TableResult> tableResult = await cTable.ExecuteBatchAsync(tbOpertion).ConfigureAwait(false);
            return tableResult;
        }

        public async Task<IEnumerable<k>> ReadEntity(k entity,string query)
        {
            Type typeArgument = Type.GetType(entity.GetType().AssemblyQualifiedName);
            Type genericClass = typeof(TableQuery<>);
            Type constructedClass = genericClass.MakeGenericType(typeArgument);
            dynamic created = Activator.CreateInstance(constructedClass);
            var filter = created.Where(query);
            IEnumerable<k> result = await _tableClient.GetTableReference(entity.GetType().Name).ExecuteQuerySegmentedAsync(filter, null).ConfigureAwait(false);
            return result;
        }

        public async Task<IList<TableResult>> UpdateEntity(k entity)
        {
            IList<TableResult> tableResult = null;
            try
            {
                TableBatchOperation tbOpertion = new TableBatchOperation();
                CloudTable cTable = _tableClient.GetTableReference(entity.GetType().Name);
                tbOpertion.Replace(entity);
                tableResult = await cTable.ExecuteBatchAsync(tbOpertion).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
            }
            return tableResult;
        }

    }
}
