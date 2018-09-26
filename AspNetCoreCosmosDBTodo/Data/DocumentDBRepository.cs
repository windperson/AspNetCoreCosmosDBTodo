using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using AspNetCoreCosmosDBTodo.Config;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Options;

namespace AspNetCoreCosmosDBTodo.Data
{
    public class DocumentDBRepository<T> : IDocumentDBRepository<T> where T : class 
    {
        DocumentClient _client;
        private string _databaseId;
        private string _collectionId;

        public DocumentDBRepository(IOptions<CosmosDBConf> optionAccessor)
        {
            var cosmosDbConf = optionAccessor.Value;
            _databaseId = cosmosDbConf.Database;
            _collectionId = cosmosDbConf.Collection;
            Initialize(cosmosDbConf);
        }

        private void Initialize(CosmosDBConf cosmosDbConf)
        {
            _client = new DocumentClient(new Uri(cosmosDbConf.EndPoint), cosmosDbConf.AuthKey);
            CreateDatabaseIfNotExistsAsync().Wait();
            CreateCollectionIfNotExistsAsync().Wait();
        }

        private async Task CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                await _client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(_databaseId));
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    await _client.CreateDatabaseAsync(new Database {Id = _databaseId});
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task CreateCollectionIfNotExistsAsync()
        {
            try
            {
                await _client.ReadDocumentCollectionAsync(
                    UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId));
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    await _client.CreateDocumentCollectionAsync(UriFactory.CreateDatabaseUri(_databaseId),
                        new DocumentCollection {Id = _collectionId}, new RequestOptions {OfferThroughput = 400});
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<T> GetItemAsync(string id)
        {
            try
            {
                var document =
                    await _client.ReadDocumentAsync<T>(UriFactory.CreateDocumentUri(_databaseId, _collectionId, id));

                return document.Document;
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                throw;
            }
        }

        public async Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
        {
            var query = _client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId),
                new FeedOptions {MaxItemCount = -1, EnableCrossPartitionQuery = true})
                .Where(predicate).AsDocumentQuery();

            var results = new List<T>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<T>());
            }

            return results;
        }

        public async Task<Document> CreateItemAsync(T item)
        {
            return await _client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId),
                item);
        }
        
        public async Task<Document> UpdateItemAsync(string id, T item)
        {
            return await _client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionId, id),
                item);
        }

        public async Task DeleteItemAsync(string id)
        {
            await _client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionId, id));
        }
    }
}