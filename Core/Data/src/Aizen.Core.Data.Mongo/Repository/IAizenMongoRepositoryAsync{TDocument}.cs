using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Aizen.Core.Data.Mongo.Builders;
using Aizen.Core.Data.Mongo.Document;
using Aizen.Core.Common.Abstraction.Collections;
using MongoDB.Bson;

namespace Aizen.Core.Data.Mongo.Repository
{
    public interface IAizenMongoRepositoryAsync<TDocument>
        where TDocument : AizenDocumentBase
    {
        #region Index
        void IndexCreate(IndexKeysDefinition<TDocument> entity);
        List<BsonDocument> GetIndexs();

        #endregion

        #region Single Operations
        /// <summary>
        ///
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="orderBy"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TDocument> FindAsync(
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="orderBy"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TDocument> FindAsync(
            Func<AizenFilterDefinitionBuilder<TDocument>, FilterDefinition<TDocument>> filter,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selector"></param>
        /// <param name="predicate"></param>
        /// <param name="orderBy"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TResult> FindAsync<TResult>(
            Expression<Func<TDocument, TResult>> selector,
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null,
            CancellationToken cancellationToken = default)
            where TResult : class;
        #endregion

        #region List Operations

        Task<IEnumerable<TDocument>> FilterWithPaginationBySortAsync(
            FilterDefinition<TDocument> filterDefinition, SortDefinition<TDocument> sortDefinition, int page,
            int pageSize);


        Task<IEnumerable<TDocument>> WhereAsyncByFilterDefinition(FilterDefinition<TDocument> filterDefinition);

        
        /// <summary>
        ///
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="orderBy"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IList<TDocument>> FindManyAsync(
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null,
            int topCount = 1000,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="predicate"></param>
        /// <param name="orderBy"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IList<TResult>> FindManyAsync<TResult>(
            Expression<Func<TDocument, TResult>> selector,
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null,
            int topCount = 1000,
            CancellationToken cancellationToken = default)
            where TResult : class;
        #endregion

        #region Count Operations
        /// <summary>
        ///
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<long> CountAsync(
            Expression<Func<TDocument, bool>> predicate = null,
            CancellationToken cancellationToken = default);
        #endregion

        #region Any Operations
        /// <summary>
        ///
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> AnyAsync(
            Expression<Func<TDocument, bool>> predicate = null,
            CancellationToken cancellationToken = default);
        #endregion

        #region IPagedList Operations
        /// <summary>
        ///
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="orderBy"></param>
        /// <param name="index"></param>
        /// <param name="size"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IPagedList<TDocument>> FindPagedListAsync(
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null,
            int index = 1,
            int size = 20,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selector"></param>
        /// <param name="predicate"></param>
        /// <param name="orderBy"></param>
        /// <param name="index"></param>
        /// <param name="size"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IPagedList<TResult>> FindPagedListAsync<TResult>(
            Expression<Func<TDocument, TResult>> selector,
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null,
            int index = 1,
            int size = 20,
            CancellationToken cancellationToken = default)
            where TResult : class;
        #endregion

        #region Find and Update Operations
        /// <summary>
        ///
        /// </summary>
        /// <param name="update"></param>
        /// <param name="predicate"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TDocument> FindAndUpdateAsync(
            Func<AizenUpdateDefinitionBuilder<TDocument>, UpdateDefinition<TDocument>> update,
            Expression<Func<TDocument, bool>> predicate,
            CancellationToken cancellationToken = default);

        #endregion

        #region Add Operations
        /// <summary>
        ///
        /// </summary>
        /// <param name="document"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task AddAsync(TDocument document, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="documents"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task AddAsync(IEnumerable<TDocument> documents, CancellationToken cancellationToken = default);
        #endregion

        #region Replace Operations
        /// <summary>
        ///
        /// </summary>
        /// <param name="document"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ReplaceAsync(TDocument document, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="document"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TDocument> FindAndReplaceAsync(
            TDocument document,
            Expression<Func<TDocument, bool>> predicate,
            CancellationToken cancellationToken = default);
        #endregion

        #region Update Operations

        Task UpdateAsync(
            Func<AizenUpdateDefinitionBuilder<TDocument>, UpdateDefinition<TDocument>> update,
            Expression<Func<TDocument, bool>> predicate,
            CancellationToken cancellationToken = default);

        #endregion

        #region Delete Operations
        /// <summary>
        /// Deletes the first matching documents in the collection.
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteAsync(
            Expression<Func<TDocument, bool>> predicate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all matching documents in the collection.
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteManyAsync(
            Expression<Func<TDocument, bool>> predicate,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteAsync(
            string id,
            CancellationToken cancellationToken = default);
        #endregion
    }
}
