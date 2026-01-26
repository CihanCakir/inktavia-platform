using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Newtonsoft.Json.Linq;
using Aizen.Core.Common.Abstraction.Exception;
using Aizen.Core.Common.Abstraction.Util;
using Aizen.Core.Data.Mongo.Attributes;
using Aizen.Core.Data.Mongo.Document;
using Aizen.Core.Data.Mongo.Repository;
using MongoDB.Bson;

namespace Aizen.Core.Data.Mongo
{
    public partial class AizenMongoRepository<TDocument> : IAizenMongoRepository<TDocument>
        where TDocument : AizenDocumentBase
    {
        private const int TopCount = 1000;
        protected IMongoCollection<TDocument> Collection { get; }

        private static readonly object _lockObject = new();
        private readonly AizenMongoContext _mongoContext;
        private readonly JObject _repositoryConfiguration;

        public AizenMongoRepository(
            AizenMongoContext mongoContext,
            JObject repositoryConfiguration,
            string collectionName = default)
        {
            lock (_lockObject)
            {
                if (!BsonClassMap.IsClassMapRegistered(typeof(TDocument))
                    && BsonClassMap.GetRegisteredClassMaps().ToList().All(x => x.ClassType != typeof(TDocument)))
                {
                    BsonClassMap.RegisterClassMap<TDocument>(
                        cm =>
                        {
                            cm.AutoMap();
                            cm.SetIgnoreExtraElements(true);
                        });
                }
            }

            this._mongoContext = mongoContext;
            this._repositoryConfiguration = repositoryConfiguration;
            this.CollectionName = !string.IsNullOrEmpty(collectionName)
                ? collectionName
                : this.GetCollectionName();

            this.Collection = this._mongoContext.Database.GetCollection<TDocument>(this.CollectionName);
            this._repositoryConfiguration = repositoryConfiguration;
        }

        public string CollectionName { get; }

        public IMongoQueryable<TDocument> CreateQuery(
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null)
        {
            var query = this.Collection.AsQueryable();

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            var globalFilters = this.GenerateGlobalFilters<TDocument>();
            if (globalFilters != null)
            {
                query = query.Where(globalFilters);
            }

            return query;
        }

        private Expression<Func<TDocument, bool>> GenerateGlobalFilters<T>()
        {
            var document = Activator.CreateInstance<T>();

            var globalFilters = (document as AizenDocumentBase).GetGlobalFilters();
            if (globalFilters == null || globalFilters.Count == 0)
            {
                return null;
            }

            var parameter = Expression.Parameter(
                document.GetType(),
                "p");
            Expression expressionBody = null;
            foreach (var globalFilter in globalFilters)
            {
                var property = document.GetType().GetProperty(globalFilter.Name);
                var expression = AizenGlobalFilterHelper.GenerateExpression(
                    parameter,
                    property,
                    globalFilter.Type,
                    globalFilter.Value);

                if (expressionBody == default)
                {
                    expressionBody = expression;
                }
                else if (globalFilter.OperationType == AizenGlobalFilterOperationType.And)
                {
                    expressionBody = Expression.AndAlso(
                        expressionBody,
                        expression);
                }
                else if (globalFilter.OperationType == AizenGlobalFilterOperationType.Or)
                {
                    expressionBody = Expression.Or(
                        expressionBody,
                        expression);
                }
            }

            return Expression.Lambda<Func<TDocument, bool>>(
                expressionBody,
                parameter);
        }

        private string GetCollectionName()
        {
            var infoAttribute = typeof(TDocument).GetCustomAttribute<AizenCollectionInfoAttribute>();
            return infoAttribute != null && !string.IsNullOrEmpty(infoAttribute.CollectionName)
                ? infoAttribute.CollectionName
                : typeof(TDocument).Name.ToLower(CultureInfo.CurrentCulture);
        }

        private void SetCreationProperties(
            TDocument document)
        {
            if (!document.GetType().IsSubclassOf(typeof(AizenDefinitionDocumentBase)))
            {
                return;
            }

            var aizenCreationDefinition = (document as AizenDefinitionDocumentBase)?.GetCreationDefinition();
            if (aizenCreationDefinition == null)
            {
                return;
            }

            this.SetDateProperty(
                document,
                aizenCreationDefinition.CreatedOn.PropertyName,
                aizenCreationDefinition.CreatedOn.OverrideExisting);

            this.SetUserProperty(
                document,
                aizenCreationDefinition.CreatedBy.PropertyName,
                aizenCreationDefinition.CreatedBy.OverrideExisting);
        }

        private void SetModificationProperties(
            TDocument document)
        {
            if (!document.GetType().IsSubclassOf(typeof(AizenDefinitionDocumentBase)))
            {
                return;
            }

            var aizenModificationDefinition =
                (document as AizenDefinitionDocumentBase)?.GetModificationDefinition();
            if (aizenModificationDefinition == null)
            {
                return;
            }

            this.SetDateProperty(
                document,
                aizenModificationDefinition.ModifiedOn.PropertyName,
                aizenModificationDefinition.ModifiedOn.OverrideExisting);

            this.SetUserProperty(
                document,
                aizenModificationDefinition.ModifiedBy.PropertyName,
                aizenModificationDefinition.ModifiedBy.OverrideExisting);
        }

        private void SetDateProperty(
            object document,
            string propertyName,
            bool overrideExisting)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return;
            }

            var propertyInfo = document.GetType().GetProperty(propertyName);
            if (propertyInfo == null || propertyInfo.PropertyType != typeof(DateTime))
            {
                throw new AizenException("Type not supported!");
            }

            var currentValue = (DateTime)propertyInfo.GetValue(document);
            if (currentValue == default || overrideExisting)
            {
                propertyInfo.SetValue(
                    document,
                    AizenDateTime.Now);
            }
        }

        private void SetUserProperty(
            object document,
            string propertyName,
            bool overrideExisting)
        {
            var userIdString = this._repositoryConfiguration["userId"]?.ToString();
            if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(userIdString))
            {
                return;
            }

            var userId = long.Parse(userIdString);
            if (userId == 0)
            {
                return;
            }

            var propertyInfo = document.GetType().GetProperty(propertyName);
            if (propertyInfo == null || propertyInfo.PropertyType != typeof(long))
            {
                throw new AizenException("Type not supported!");
            }

            var currentValue = (long)propertyInfo.GetValue(document);
            if (currentValue == default || overrideExisting)
            {
                propertyInfo.SetValue(
                    document,
                    userId);
            }
        }

        public void IndexCreate(IndexKeysDefinition<TDocument> entity)
        {
            this.Collection.Indexes.CreateOne(entity);
        }

        public List<BsonDocument> GetIndexs()
        {
            return this.Collection.Indexes.List().ToList();
        }

        public async Task<IEnumerable<TDocument>> WhereAsyncByFilterDefinition(FilterDefinition<TDocument> filterDefinition)
        {
            return await this.Collection.Find(filterDefinition).ToListAsync();
        }
    }
}