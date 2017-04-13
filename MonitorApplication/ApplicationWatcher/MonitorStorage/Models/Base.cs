using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace MonitorStorage.Models
{
    [Serializable]
    public class Base : TableEntity
    {
        private string projectName;
        public Base(string projectName)
        {
            this.projectName = base.PartitionKey = projectName;
            base.RowKey = DateTime.Now.Ticks.ToString();
            
        }

        public string ID
        {
            set;
            get;

        }
        public new string PartitionKey
        {
            get
            {
                return base.PartitionKey;
            }

        }

        public new string RowKey
        {
            get
            {
                return base.RowKey;
            }
        }
    }
}
