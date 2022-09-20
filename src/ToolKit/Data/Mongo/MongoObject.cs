using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FatCat.Toolkit.Data.Mongo;

public abstract class MongoObject : EqualObject
{
	[BsonId]
	[BsonRepresentation(BsonType.ObjectId)]
	[JsonConverter(typeof(ObjectIdConverter))]
	public ObjectId Id { get; set; }

	protected MongoObject() => Id = ObjectId.GenerateNewId();
}