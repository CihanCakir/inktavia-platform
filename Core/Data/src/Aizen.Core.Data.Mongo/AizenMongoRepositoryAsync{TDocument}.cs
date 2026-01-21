using Aizen.Core.Common.Abstraction.Collections;
using Aizen.Core.Data.Mongo.Builders;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Linq.Expressions;

namespace Aizen.Core.Data.Mongo
{
    public partial class AizenMongoRepository<TDocument>
    {
        public async Task AddAsync(
            TDocument document,
            CancellationToken cancellationToken = default)
        {
            this.SetCreationProperties(document);
            this.SetModificationProperties(document);
            await this.Collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        }

        public async Task AddAsync(
            IEnumerable<TDocument> documents,
            CancellationToken cancellationToken = default)
        {
            foreach (var document in documents)
            {
                this.SetCreationProperties(document);
                this.SetModificationProperties(document);
            }
            await this.Collection.InsertManyAsync(documents, cancellationToken: cancellationToken);
        }

        public async Task<bool> AnyAsync(
            Expression<Func<TDocument, bool>> predicate = null,
            CancellationToken cancellationToken = default)
        {
            return await this.Collection.AsQueryable()
                .AnyAsync(predicate, cancellationToken);
        }

        public async Task<long> CountAsync(
            Expression<Func<TDocument, bool>> predicate = null,
            CancellationToken cancellationToken = default)
        {
            return await this.Collection.AsQueryable()
                .CountAsync(predicate, cancellationToken);
        }

        public async Task DeleteAsync(
            Expression<Func<TDocument, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            await this.Collection.DeleteOneAsync(predicate, cancellationToken);
        }

        public async Task DeleteManyAsync(
            Expression<Func<TDocument, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            await this.Collection.DeleteManyAsync(predicate, cancellationToken);
        }

        public async Task DeleteAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            var predicate = new FilterDefinitionBuilder<TDocument>().Eq(a => a.Id, id);
            await this.Collection.DeleteOneAsync(predicate, cancellationToken: cancellationToken);
        }

        public async Task<TDocument> FindAndUpdateAsync(
            Func<AizenUpdateDefinitionBuilder<TDocument>, UpdateDefinition<TDocument>> update,
            Expression<Func<TDocument, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            var filter = new FilterDefinitionBuilder<TDocument>().Where(predicate);

            var updateDefinitionBuilder = new AizenUpdateDefinitionBuilder<TDocument>();
            var updateDefinition = update(updateDefinitionBuilder);

            return await this.Collection.FindOneAndUpdateAsync(filter, updateDefinition, new FindOneAndUpdateOptions<TDocument>()
            {
                ReturnDocument = ReturnDocument.After
            }, cancellationToken: cancellationToken);
        }
        
        public async Task<IEnumerable<TDocument>> FilterWithPaginationBySortAsync(FilterDefinition<TDocument> filterDefinition, SortDefinition<TDocument> sortDefinition, int page, int pageSize)
        {
            return await Collection.Find(filterDefinition).Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .Sort(sortDefinition)
                .ToListAsync();
        }

        public async Task<TDocument> FindAsync(
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null,
            CancellationToken cancellationToken = default)
        {
            var query = this.CreateQuery(predicate, orderBy);

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<TDocument> FindAsync(
            Func<AizenFilterDefinitionBuilder<TDocument>, FilterDefinition<TDocument>> filter,
            CancellationToken cancellationToken = default)
        {
            var filterDefinitionBuilder = new AizenFilterDefinitionBuilder<TDocument>();
            var filterDefinition = filter(filterDefinitionBuilder);

            var result = await this.Collection.FindAsync(filterDefinition, cancellationToken: cancellationToken);
            return await result.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<TResult> FindAsync<TResult>(
            Expression<Func<TDocument, TResult>> selector,
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null,
            CancellationToken cancellationToken = default)
            where TResult : class
        {
            var query = this.CreateQuery(predicate, orderBy);
            var result = await Task.Run(() => query.Select(selector.Compile()).FirstOrDefault());

            return result;
        }

        public async Task<IList<TDocument>> FindManyAsync(
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null,
            int topCount = TopCount,
            CancellationToken cancellationToken = default)
        {
            var query = this.CreateQuery(predicate, orderBy);

            if (topCount != -1)
            {
                query = query.Take(topCount);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IList<TResult>> FindManyAsync<TResult>(
            Expression<Func<TDocument, TResult>> selector,
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null,
            int topCount = TopCount,
            CancellationToken cancellationToken = default)
            where TResult : class
        {
            var query = this.CreateQuery(predicate, orderBy);

            if (topCount != -1)
            {
                query = query.Take(topCount);
            }

            return await Task.Run(() => query.Select(selector.Compile()).ToList());
        }

        public async Task<IPagedList<TDocument>> FindPagedListAsync(
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null,
            int index = 1,
            int size = 20,
            CancellationToken cancellationToken = default)
        {
            var query = this.CreateQuery(predicate, orderBy);

            var result = await Task.Run(() => query.ToPagedList(index, size), cancellationToken);
            return result;
        }

        public async Task<IPagedList<TResult>> FindPagedListAsync<TResult>(
            Expression<Func<TDocument, TResult>> selector,
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null,
            int index = 1,
            int size = 20,
            CancellationToken cancellationToken = default)
            where TResult : class
        {
            var query = this.CreateQuery(predicate, orderBy);
            var result = await Task.Run(() => query.Select(selector.Compile()).ToPagedList(index, size), cancellationToken);

            return result;
        }

        public async Task ReplaceAsync(
            TDocument document,
            CancellationToken cancellationToken = default)
        {
            this.SetModificationProperties(document);
            await this.Collection.ReplaceOneAsync(a =>
                    a.Id == document.Id,
                document,
                cancellationToken: cancellationToken);
        }

        public async Task<TDocument> FindAndReplaceAsync(
            TDocument document,
            Expression<Func<TDocument, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            this.SetModificationProperties(document);
            var filter = new FilterDefinitionBuilder<TDocument>().Where(predicate);

            return await this.Collection.FindOneAndReplaceAsync(filter, document, cancellationToken: cancellationToken);
        }

        public async Task UpdateAsync(
            Func<AizenUpdateDefinitionBuilder<TDocument>, UpdateDefinition<TDocument>> update,
            Expression<Func<TDocument, bool>> predicate,
            CancellationToken  cancellationToken = default)
        {
            var filter = new FilterDefinitionBuilder<TDocument>().Where(predicate);

            var updateDefinitionBuilder = new AizenUpdateDefinitionBuilder<TDocument>();
            var updateDefinition = update(updateDefinitionBuilder);
            await this.Collection.UpdateManyAsync(filter, updateDefinition, cancellationToken: cancellationToken);
        }
    }
}
