using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;


namespace MonitorStorage
{
    public interface IStorage<T> where T : TableEntity
    {
        Task CreateTable(T tableName);
        Task<IList<TableResult>> AddEntity(List<T> entity);
        Task<IList<TableResult>> UpdateEntity(T entity);
        Task<IList<TableResult>> DeleteEntity(T entity);
        Task<IEnumerable<T>> ReadEntity(T entity, string query);
    }
}
