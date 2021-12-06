using System.Data;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Data;

/// <summary>
/// Data reader for Entity Framework Core entities.
/// </summary>
/// <typeparam name="TEntity">Type of the entity.</typeparam>
[SuppressMessage("ReSharper", "EF1001")]
public sealed class EntityDataReader<TEntity> : IEntityDataReader<TEntity>
   where TEntity : class
{
   private readonly DbContext _ctx;
   private readonly IEnumerator<TEntity> _enumerator;
   private readonly Dictionary<int, Func<DbContext, TEntity, object?>> _propertyGetterLookup;
   private readonly IReadOnlyList<TEntity>? _entities;
   private readonly List<TEntity>? _readEntities;

   /// <inheritdoc />
   public IReadOnlyList<PropertyWithNavigations> Properties { get; }

   /// <inheritdoc />
   public int FieldCount => Properties.Count;

   /// <summary>
   /// Initializes <see cref="EntityDataReader{T}"/>
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="propertyGetterCache">Property getter cache.</param>
   /// <param name="entities">Entities to read.</param>
   /// <param name="properties">Properties to read.</param>
   /// <param name="ensureReadEntitiesCollection">Makes sure the method <see cref="IEntityDataReader{T}.GetReadEntities"/> has a collection to return.</param>
   public EntityDataReader(
      DbContext ctx,
      IPropertyGetterCache propertyGetterCache,
      IEnumerable<TEntity> entities,
      IReadOnlyList<PropertyWithNavigations> properties,
      bool ensureReadEntitiesCollection)
   {
      if (propertyGetterCache == null)
         throw new ArgumentNullException(nameof(propertyGetterCache));
      if (entities == null)
         throw new ArgumentNullException(nameof(entities));

      _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
      Properties = properties ?? throw new ArgumentNullException(nameof(properties));

      if (properties.Count == 0)
         throw new ArgumentException("The properties collection cannot be empty.", nameof(properties));

      _propertyGetterLookup = BuildPropertyGetterLookup(propertyGetterCache, properties);
      _enumerator = entities.GetEnumerator();

      if (ensureReadEntitiesCollection)
      {
         if (entities is IReadOnlyList<TEntity> entityList)
         {
            _entities = entityList;
         }
         else
         {
            _readEntities = new List<TEntity>();
         }
      }
   }

   private static Dictionary<int, Func<DbContext, TEntity, object?>> BuildPropertyGetterLookup(
      IPropertyGetterCache propertyGetterCache,
      IReadOnlyList<PropertyWithNavigations> properties)
   {
      var lookup = new Dictionary<int, Func<DbContext, TEntity, object?>>();

      for (var i = 0; i < properties.Count; i++)
      {
         var getter = propertyGetterCache.GetPropertyGetter<TEntity>(properties[i]);
         lookup.Add(lookup.Count, getter);
      }

      return lookup;
   }

   /// <inheritdoc />
   public IEnumerable<(int index, PropertyWithNavigations property)> GetProperties()
   {
      return Properties.Select((p, i) => (i, p));
   }

   /// <inheritdoc />
   public int GetPropertyIndex(PropertyWithNavigations property)
   {
      for (var i = 0; i < Properties.Count; i++)
      {
         if (property.Equals(Properties[i]))
            return i;
      }

      throw new ArgumentException($"The property '{property.Property.Name}' of type '{property.Property.ClrType.ShortDisplayName()}' cannot be read by current reader.");
   }

   /// <inheritdoc />
   public IReadOnlyList<TEntity> GetReadEntities()
   {
      return _entities ?? _readEntities ?? throw new InvalidOperationException("'Read entities' were not requested previously.");
   }

   /// <inheritdoc />
#pragma warning disable 8766
   public object? GetValue(int i)
#pragma warning restore 8766
   {
      return _propertyGetterLookup[i](_ctx, _enumerator.Current);
   }

   /// <inheritdoc />
   public bool Read()
   {
      if (!_enumerator.MoveNext())
         return false;

      _readEntities?.Add(_enumerator.Current);
      return true;
   }

   /// <inheritdoc />
   public bool IsDBNull(int i)
   {
      // we are reading entities (.NET objects), there must be no properties of type "DBNull".
      return false;
   }

   /// <inheritdoc />
   public void Dispose()
   {
      _enumerator.Dispose();
   }

   // The following methods are not needed for bulk insert.
   // ReSharper disable ArrangeMethodOrOperatorBody
#pragma warning disable 1591
   object IDataRecord.this[int i] => throw new NotSupportedException();
   object IDataRecord.this[string name] => throw new NotSupportedException();
   int IDataReader.Depth => throw new NotSupportedException();
   int IDataReader.RecordsAffected => throw new NotSupportedException();
   bool IDataReader.IsClosed => throw new NotSupportedException();
   void IDataReader.Close() => throw new NotSupportedException();
   string IDataRecord.GetName(int i) => throw new NotSupportedException();
   string IDataRecord.GetDataTypeName(int i) => throw new NotSupportedException();
   Type IDataRecord.GetFieldType(int i) => throw new NotSupportedException();
   int IDataRecord.GetValues(object[] values) => throw new NotSupportedException();
   int IDataRecord.GetOrdinal(string name) => throw new NotSupportedException();
   bool IDataRecord.GetBoolean(int i) => throw new NotSupportedException();
   byte IDataRecord.GetByte(int i) => throw new NotSupportedException();
   long IDataRecord.GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
   char IDataRecord.GetChar(int i) => throw new NotSupportedException();
   long IDataRecord.GetChars(int i, long fieldOffset, char[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
   Guid IDataRecord.GetGuid(int i) => throw new NotSupportedException();
   short IDataRecord.GetInt16(int i) => throw new NotSupportedException();
   int IDataRecord.GetInt32(int i) => throw new NotSupportedException();
   long IDataRecord.GetInt64(int i) => throw new NotSupportedException();
   float IDataRecord.GetFloat(int i) => throw new NotSupportedException();
   double IDataRecord.GetDouble(int i) => throw new NotSupportedException();
   string IDataRecord.GetString(int i) => throw new NotSupportedException();
   decimal IDataRecord.GetDecimal(int i) => throw new NotSupportedException();
   DateTime IDataRecord.GetDateTime(int i) => throw new NotSupportedException();
   IDataReader IDataRecord.GetData(int i) => throw new NotSupportedException();
   DataTable IDataReader.GetSchemaTable() => throw new NotSupportedException();
   bool IDataReader.NextResult() => throw new NotSupportedException();
#pragma warning restore 1591
   // ReSharper restore ArrangeMethodOrOperatorBody
}
