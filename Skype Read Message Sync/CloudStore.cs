using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Skype_Message_Sync
{
	public class MessageEntity : TableServiceEntity
	{
		public MessageEntity(string username, ulong messageId)
			: base(username, messageId.ToString())
		{
		}
	}

	public class MessageDataServiceContext : TableServiceContext
	{
		public MessageDataServiceContext(string baseAddress, StorageCredentials credentials)
			: base(baseAddress, credentials)
		{
		}

		public const string MessagesTableName = "Messages";

		public IQueryable<MessageEntity> Messages
		{
			get
			{
				return this.CreateQuery<MessageEntity>(MessagesTableName);
			}
		}
	}

	public class MessageDataSource
	{
		private MessageDataServiceContext serviceContext = null;

		public MessageDataSource()
		{
			var storageAccount = CloudStorageAccount.Parse(Properties.Settings.Default.AzureConnectionString);
			//var storageAccount = CloudStorageAccount.FromConfigurationSetting("DataConnectionString");
			serviceContext = new MessageDataServiceContext(storageAccount.TableEndpoint.ToString(), storageAccount.Credentials);

            // When no results are available from a query, Azure will
            // return a ResourceNotFound exception. Settings this property
            // will have it return an empty data set
            serviceContext.IgnoreResourceNotFoundException = true;

			// Create the tables
			// In this case, just a single table.  
			storageAccount.CreateCloudTableClient().CreateTableIfNotExist(MessageDataServiceContext.MessagesTableName);
        }

        public MessageDataServiceContext ServiceContext
        {
            get { return serviceContext; }
        }

        public bool WasReadElsewhere(MessageEntity item)
		{
			var query = (from c in serviceContext.Messages
						where c.PartitionKey == item.PartitionKey && c.RowKey == item.RowKey
                         select c).AsTableServiceQuery();

            return query.ToList().Count() > 0;
		}

		public void Delete(MessageEntity itemToDelete)
		{
            try
            {
                serviceContext.AttachTo(MessageDataServiceContext.MessagesTableName, itemToDelete, "*");
            }
            catch (InvalidOperationException) 
            {
                // May already be tracking the entity from previous operations
            }

            serviceContext.DeleteObject(itemToDelete);
			serviceContext.SaveChanges();
		}

		public void Insert(MessageEntity newItem)
		{
			serviceContext.AddObject(MessageDataServiceContext.MessagesTableName, newItem);
			serviceContext.SaveChanges();
		}
	}
}
