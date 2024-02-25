using Google.Apis.Fitness.v1;
using Google.Apis.Fitness.v1.Data;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;


namespace GoogleFitTesting2
{
	internal class HeartRateDataRetrieval
	{
		public static void RetrieveAndProcessHeartRateData(FitnessService service, Program.UserCredentials user, long startTimeMillis, long endTimeMillis)
		{
			// Data retrieval and processing logic
			Console.WriteLine("\nHeart Rate Data:");

			var heartRateRequest = service.Users.Dataset.Aggregate(
				new AggregateRequest
				{
					AggregateBy = new List<AggregateBy>
					{
						new AggregateBy
						{
							DataTypeName = "com.google.heart_rate.bpm"
						}
					},
					StartTimeMillis = startTimeMillis,
					EndTimeMillis = endTimeMillis,
					BucketByTime = new BucketByTime
					{
						DurationMillis = 86400000 // 1 day
					}
				}, "me");

			var heartRateResponse = heartRateRequest.Execute();

			// Serialize heart rate data and write it to a file
			var heartRateResponseJson = JsonConvert.SerializeObject(heartRateResponse);
			File.WriteAllText($"heartRateData_{user.ClientId}.json", heartRateResponseJson);

			foreach (var heartRateBucket in heartRateResponse.Bucket)
			{
				foreach (var heartRateDataset in heartRateBucket.Dataset)
				{
					foreach (var heartRatePoint in heartRateDataset.Point)
					{
						var heartRate = heartRatePoint.Value[0].FpVal;
						Console.WriteLine($"Heart Rate (bpm): {heartRate}");

						// Additional processing logic (if needed)
					}
				}
			}

			// Additional processing logic (if needed)
		}
	}
}
