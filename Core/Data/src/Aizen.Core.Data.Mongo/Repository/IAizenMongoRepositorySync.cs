using Aizen.Core.Common.Abstraction.Collections;
using Aizen.Core.Data.Mongo.Builders;
using Aizen.Core.Data.Mongo.Document;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Linq.Expressions;

namespace Aizen.Core.Data.Mongo.Repository
{
    public interface IAizenMongoRepositorySync<TDocument>
        where TDocument : AizenDocumentBase
    {
        #region Single Operations
        /// <summary>
        ///
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        TDocument Find(
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null);

        /// <summary>
        ///
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        TDocument Find(Func<AizenFilterDefinitionBuilder<TDocument>, FilterDefinition<TDocument>> filter);

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selector"></param>
        /// <param name="predicate"></param>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        TResult Find<TResult>(
            Expression<Func<TDocument, TResult>> selector,
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null)
            where TResult : class;
        #endregion

        #region List Operations
        /// <summary>
        ///
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        IList<TDocument> FindMany(
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null,
            int topCount = 1000);

        /// <summary>
        ///
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="predicate"></param>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        IList<TResult> FindMany<TResult>(
            Expression<Func<TDocument, TResult>> selector,
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null,
            int topCount = 1000)
            where TResult : class;
        #endregion

        #region Count Operations
        /// <summary>
        ///
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        long Count(Expression<Func<TDocument, bool>> predicate = null);
        #endregion

        #region Any Operations
        /// <summary>
        ///
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        bool Any(Expression<Func<TDocument, bool>> predicate = null);
        #endregion

        #region IPagedList Operations
        /// <summary>
        ///
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="orderBy"></param>
        /// <param name="index"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        IPagedList<TDocument> FindPagedList(
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null,
            int index = 1,
            int size = 20);

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selector"></param>
        /// <param name="predicate"></param>
        /// <param name="orderBy"></param>
        /// <param name="index"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        IPagedList<TResult> FindPagedList<TResult>(
            Expression<Func<TDocument, TResult>> selector,
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null,
            int index = 1,
            int size = 20)
            where TResult : class;
        #endregion

        #region Find and Update Operations
        /// <summary>
        ///
        /// </summary>
        /// <param name="update"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        TDocument FindAndUpdate(
            Func<AizenUpdateDefinitionBuilder<TDocument>, UpdateDefinition<TDocument>> update,
            Expression<Func<TDocument, bool>> predicate);
        #endregion

        #region Add Operations
        /// <summary>
        ///
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        void Add(TDocument document);

        /// <summary>
        ///
        /// </summary>
        /// <param name="documents"></param>
        /// <returns></returns>
        void Add(IEnumerable<TDocument> documents);
        #endregion

        #region Replace Operations
        /// <summary>
        ///
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        void Replace(TDocument document);

        /// <summary>
        ///
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        TDocument FindAndReplace(
          TDocument document,
          Expression<Func<TDocument, bool>> predicate);
        #endregion

        #region Delete Operations
        /// <summary>
        ///
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        void Delete(Expression<Func<TDocument, bool>> predicate);

        /// <summary>
        ///
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        void Delete(string id);
        #endregion

        #region Update Operations

        void Update(
            Func<AizenUpdateDefinitionBuilder<TDocument>, UpdateDefinition<TDocument>> update,
            Expression<Func<TDocument, bool>> predicate);

        #endregion
    }
}
