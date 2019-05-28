using ExternalNonChangableLibrary;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDbAggregateSample
{
	static public class Program
	{
		static readonly Random _random = new Random();

		const int _childPropertyCount = 20;

		async static Task Main()
		{
			/*
			 * The class DataObjectExample is defined in a separate assembly to denote real-world use.
			 * 
			 * I can't change them, I don't own them. And there are a lot off them, so I'm trying to avoid
			 * writing a custom type for each mapping back and forth. It would also be easy to miss properties when they're updated.
			 *
			 * And I need to provide a typed query, allowing consumers to pass in an Expression<Func<T, bool>> (where T is an DataObjectExmaple in this instance)
			 * to allow for typed querying.
			 */

			ConfigureClassMap();

			var client = GetMongoDbClient();
			var database = client.GetDatabase("aggregate-demo");
			var collection = database.GetCollection<DataObjectExample>("data-object-example");

			await PopulateAsync(collection);

			Query(collection);
		}

		static void Query(IMongoCollection<DataObjectExample> collection)
		{
			var results = collection.AsQueryable().Where(m => m.SomeProperty >= 5 && m.SomeProperty <= 10);
			var count = results.Count();

			Console.WriteLine($"Result(s) ({count}):");
			foreach(var result in results)
			{
				Console.WriteLine($"> {result.Details.Id}: {result.SomeProperty}: {result.AComplexTypeArray.Length == _childPropertyCount}: { result.AStringArray.Length == _childPropertyCount}".PadLeft(2));
			}
		}

		async static Task PopulateAsync(IMongoCollection<DataObjectExample> collection, CancellationToken cancellationToken = default)
		{
			var count = await collection.EstimatedDocumentCountAsync();
			if (count > 0)
				return;

			for (var i = 0; i < 10; i++)
			{
				var dataObject = new DataObjectExample
				{
					Details = new DataObjectInfo { Id = $"dataobject_{i + 1}" }
				};

				dataObject.SetSomeProperty(i + 1);
				for (var m = 0; m < _childPropertyCount; m++) {
					dataObject.AddComplexType(new SomeComplexType { SoAmI = m + 1, ImAComplexTypeProperty = $"childprofile_{m + 1}_from_dataobject_{i + 1}" });
					dataObject.AddString(_random.Next().ToString());
				};

				await collection.InsertOneAsync(dataObject, new InsertOneOptions(), cancellationToken);
			}
		}

		static IMongoClient GetMongoDbClient()
		{
			return new MongoClient("mongodb://localhost:27017");
		}

		static void ConfigureClassMap()
		{
			var t = typeof(DataObjectExample);

			var classMap = new BsonClassMap(t);
			classMap.AutoMap();
			classMap.SetIdMember(new BsonMemberMap(classMap, t.GetProperty("Details").PropertyType.GetProperty("Id")));
		}
	}
}