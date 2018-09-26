using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;

namespace AspNetCoreCosmosDBTodo.Data
{
    public interface IDocumentDBRepository<T> where T : class
    {
        #region Basic CRUD 

        Task<T> GetItemAsync(string id);
        Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate);
        Task<Document> CreateItemAsync(T item);
        Task<Document> UpdateItemAsync(string id, T item);
        Task DeleteItemAsync(string id);
        
        #endregion

    }
}