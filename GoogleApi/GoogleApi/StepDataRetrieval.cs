using Google.Apis.Auth.OAuth2;
using Google.Apis.Fitness.v1;
using Google.Apis.Fitness.v1.Data;
using Google.Apis.Services;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace GoogleFitTesting2
{
	internal class StepDataRetrieval
	{
		public static void RetrieveAndProcessStepData(Program.UserCredentials user, UserCredential credential)
		{
			// Data retrieval and processing logic
			// Define time range for data retrieval (adjust as needed)
			var startTimeMillis = DateTimeOffset.Now.AddDays(-30).ToUnixTimeMilliseconds();
			var endTimeMillis = DateTimeOffset.Now.ToUnixTimeMilliseconds();

			// Create the Fitness API service
			var service = new FitnessService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential,
				ApplicationName = "nuQare"
			});

			Console.WriteLine("Step Count Data:");
			var stepDataList = RetrieveStepData(service, user, startTimeMillis, endTimeMillis);

			// MongoDB integration for step data
			var mongoClient = new MongoClient("mongodb+srv://nuqaretestbench1:nuqare@nuqaretest.dox1jvy.mongodb.net/?retryWrites=true&w=majority");
			var database = mongoClient.GetDatabase("Test1");
			var stepDataCollection = database.GetCollection<BsonDocument>("StepData");
			stepDataCollection.InsertMany(stepDataList);

			Console.WriteLine("Step data retrieved and saved successfully.");
		}

		public static List<BsonDocument> RetrieveStepData(FitnessService service, Program.UserCredentials user, long startTimeMillis, long endTimeMillis)
		{
			var stepDataList = new List<BsonDocument>();

			var stepRequest = service.Users.Dataset.Aggregate(
				new AggregateRequest
				{
					AggregateBy = new List<AggregateBy>
					{
						new AggregateBy
						{
							DataSourceId = "derived:com.google.step_count.delta:com.google.android.gms:estimated_steps"
						}
					},
					StartTimeMillis = startTimeMillis,
					EndTimeMillis = endTimeMillis,
					BucketByTime = new BucketByTime
					{
						DurationMillis = 86400000 // 1 day
					}
				}, "me");

			var stepResponse = stepRequest.Execute();

			foreach (var stepBucket in stepResponse.Bucket)
			{
				foreach (var stepDataset in stepBucket.Dataset)
				{
					foreach (var stepPoint in stepDataset.Point)
					{
						var stepCount = stepPoint.Value[0].IntVal;
						var timestampMillis = stepPoint.StartTimeNanos / 1000000L;
						var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)timestampMillis).UtcDateTime;

						Console.WriteLine($"Step Count: {stepCount}, Timestamp: {timestamp}");

						var stepDataEntry = new BsonDocument
						{
							{ "StepCount", stepCount },
							{ "Timestamp", timestamp }
						};

						stepDataList.Add(stepDataEntry);
					}
				}
			}

			// Additional processing logic (if needed)

			return stepDataList;
		}
	}
}
