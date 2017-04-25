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
using System.Configuration;

namespace MonitorStorage
{
    public class Storage<k> : IStorage<k> where k:Base
    {
        CloudTableClient _tableClient;
        CloudTable _table;

        private int Slice { get { return ConfigurationManager.AppSettings["Slice"] != null ? Convert.ToInt16(ConfigurationManager.AppSettings["Slice"].ToString()) : 100; } }
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
            //try
            //{
                int slice = entity.Count % Slice == 0 ? entity.Count / Slice : (entity.Count / Slice) + 1;
                for (int i = 0; i < slice; i++)
                {
                    IEnumerable<k> slicedData = entity.Skip(i* Slice).Take((i + 1) * Slice);
                    _table = _tableClient.GetTableReference(entity[0].GetType().Name);
                    TableBatchOperation batchOperation = new TableBatchOperation();
                    slicedData.ToList().ForEach(e =>
                    {
                        batchOperation.InsertOrReplace(e);
                    });
                    result = await _table.ExecuteBatchAsync(batchOperation).ConfigureAwait(false);
                }
            //}
            //catch (Exception ex)
            //{
            //}
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

        public  IEnumerable<k> SynReadEntity(k entity, string query)
        {
            Type typeArgument = Type.GetType(entity.GetType().AssemblyQualifiedName);
            Type genericClass = typeof(TableQuery<>);
            Type constructedClass = genericClass.MakeGenericType(typeArgument);
            dynamic created = Activator.CreateInstance(constructedClass);
            var filter = created.Where(query);
            IEnumerable<k> result =  _tableClient.GetTableReference(entity.GetType().Name).ExecuteQuerySegmented(filter, null);
            return result;
        }

        public void SynAddEntity(k entity)
        {
            CloudTable cTable = _tableClient.GetTableReference(entity.GetType().Name);
            TableOperation insertOperation = TableOperation.Insert(entity);
            cTable.Execute(insertOperation);
        }

        public async Task<IList<TableResult>> UpdateEntity(k entity)
        {
            IList<TableResult> tableResult = null;
            //try
            //{
                TableBatchOperation tbOpertion = new TableBatchOperation();
                CloudTable cTable = _tableClient.GetTableReference(entity.GetType().Name);
                tbOpertion.Replace(entity);
                tableResult = await cTable.ExecuteBatchAsync(tbOpertion).ConfigureAwait(false);
            //}
            //catch (Exception ex)
            //{
            //}
            return tableResult;
        }

        public TableResult SynUpdateEntity(k entity)
        {
            CloudTable cTable = _tableClient.GetTableReference(entity.GetType().Name);
            TableOperation updateOperation = TableOperation.Replace(entity);
            return cTable.Execute(updateOperation);
        }
    }
}
