using Aizen.Core.Common.Abstraction.Collections;
using Aizen.Core.Data.Mongo.Builders;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Linq.Expressions;

namespace Aizen.Core.Data.Mongo
{
    public partial class AizenMongoRepository<TDocument>
    {
        public void Add(TDocument document)
        {
            this.SetCreationProperties(document);
            this.SetModificationProperties(document);
            this.Collection.InsertOne(document);
        }

        public void Add(IEnumerable<TDocument> documents)
        {
            foreach (var document in documents)
            {
                this.SetCreationProperties(document);
                this.SetModificationProperties(document);
            }

            this.Collection.InsertMany(documents);
        }

        public bool Any(Expression<Func<TDocument, bool>> predicate = null)
        {
            var query = this.CreateQuery(predicate);
            return query.Any();
        }

        public long Count(Expression<Func<TDocument, bool>> predicate = null)
        {
            var query = this.CreateQuery(predicate);
            return query.Count();
        }

        public void Delete(Expression<Func<TDocument, bool>> predicate)
        {
            this.Collection.FindOneAndDelete(predicate);
        }

        public void Delete(string id)
        {
            var predicate = new FilterDefinitionBuilder<TDocument>().Eq(a => a.Id, id);
            this.Collection.FindOneAndDelete(predicate);
        }

        public TDocument FindAndUpdate(
            Func<AizenUpdateDefinitionBuilder<TDocument>, UpdateDefinition<TDocument>> update,
            Expression<Func<TDocument, bool>> predicate)
        {
            var filter = new FilterDefinitionBuilder<TDocument>().Where(predicate);

            var updateDefinitionBuilder = new AizenUpdateDefinitionBuilder<TDocument>();
            var updateDefinition = update(updateDefinitionBuilder);

            return this.Collection.FindOneAndUpdate(
                filter,
                updateDefinition,
                new FindOneAndUpdateOptions<TDocument>() { ReturnDocument = ReturnDocument.After });
        }

        public TDocument Find(
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null)
        {
            var query = this.CreateQuery(predicate, orderBy);
            return query.FirstOrDefault();
        }

        public TDocument Find(Func<AizenFilterDefinitionBuilder<TDocument>, FilterDefinition<TDocument>> filter)
        {
            var filterDefinitionBuilder = new AizenFilterDefinitionBuilder<TDocument>();
            var filterDefinition = filter(filterDefinitionBuilder);

            var result = this.Collection.Find(filterDefinition);
            return result.FirstOrDefault();
        }

        public TResult Find<TResult>(
            Expression<Func<TDocument, TResult>> selector,
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null)
            where TResult : class
        {
            var query = this.CreateQuery(predicate, orderBy);
            var result = query.Select(selector.Compile()).FirstOrDefault();

            return result;
        }

        public IList<TDocument> FindMany(
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null,
            int topCount = TopCount)
        {
            var query = this.CreateQuery(predicate, orderBy);

            if (topCount != -1)
            {
                query = query.Take(topCount);
            }

            return query.ToList();
        }

        public IList<TResult> FindMany<TResult>(
            Expression<Func<TDocument, TResult>> selector,
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null,
            int topCount = TopCount)
            where TResult : class
        {
            var query = this.CreateQuery(predicate, orderBy);

            if (topCount != -1)
            {
                query = query.Take(topCount);
            }

            return query.Select(selector.Compile()).ToList();
        }

        public IPagedList<TDocument> FindPagedList(
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null,
            int index = 1,
            int size = 20)
        {
            var query = this.CreateQuery(predicate, orderBy);

            var result = query.ToPagedList(index, size);
            return result;
        }

        public IPagedList<TResult> FindPagedList<TResult>(
            Expression<Func<TDocument, TResult>> selector,
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null,
            int index = 1,
            int size = 20)
            where TResult : class
        {
            var query = this.CreateQuery(predicate, orderBy);
            var result = query.Select(selector.Compile())
                .ToPagedList(index, size);

            return result;
        }

        public void Replace(TDocument document)
        {
            this.SetModificationProperties(document);
            this.Collection.ReplaceOne(a =>
                    a.Id == document.Id,
                document);
        }

        public TDocument FindAndReplace(
           TDocument document,
           Expression<Func<TDocument, bool>> predicate)
        {
            this.SetModificationProperties(document);
            var filter = new FilterDefinitionBuilder<TDocument>().Where(predicate);

            return this.Collection.FindOneAndReplace(filter, document);
        }

        public void Update(
            Func<AizenUpdateDefinitionBuilder<TDocument>, UpdateDefinition<TDocument>> update,
            Expression<Func<TDocument, bool>> predicate)
        {
            var filter = new FilterDefinitionBuilder<TDocument>().Where(predicate);

            var updateDefinitionBuilder = new AizenUpdateDefinitionBuilder<TDocument>();
            var updateDefinition = update(updateDefinitionBuilder);
            this.Collection.UpdateMany(filter, updateDefinition);
        }
    }
}
