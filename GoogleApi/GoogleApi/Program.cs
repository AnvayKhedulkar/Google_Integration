using Google.Apis.Auth.OAuth2;
using Google.Apis.Fitness.v1;
using Google.Apis.Fitness.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace GoogleFitTesting2
{
	internal class Program
	{
		public class UserCredentials
		{
			public string ClientId { get; set; }
			public string ClientSecret { get; set; }
			public string[] Scopes { get; set; }
			public string RedirectUri { get; set; }
		}

		private static List<UserCredentials> users = new List<UserCredentials>
		{
			new UserCredentials
			{
				ClientId = "974648303072-5egpctkn6ellqerevv26k0sj40me5q9k.apps.googleusercontent.com",
				ClientSecret = "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",
				Scopes = new string[] { "https://www.googleapis.com/auth/fitness.activity.read" , "https://www.googleapis.com/auth/fitness.heart_rate.read" },
				RedirectUri = "http://localhost:5000/callback"
			},
			new UserCredentials
			{
				ClientId = "974648303072-s8cuo79bnr9so0rhu01i5g7tak7m4vqm.apps.googleusercontent.com",
				ClientSecret = "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",
				Scopes = new string[] { "https://www.googleapis.com/auth/fitness.activity.read"," \"https://www.googleapis.com/auth/fitness.heart_rate.read\"" },
				RedirectUri = "http://localhost:5000/callback"
			}
		};

		private static void Main(string[] args)
    {
        Console.WriteLine("Initializing Google Fit Data Retrieval...");

        foreach (var user in users)
        {
            try
            {
                Console.WriteLine($"Retrieving data for user with ClientId: {user.ClientId}...");

                // Authorization code logic
                var authorizationCode = GetAuthorizationCode(user);

                // Exchange authorization code for access token
                var credential = ExchangeAuthorizationCodeForToken(user, authorizationCode);

                // Data retrieval and processing logic
                StepDataRetrieval.RetrieveAndProcessStepData(user, credential);

                Console.WriteLine($"Data retrieved successfully for user with ClientId: {user.ClientId}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }

		private static string GetAuthorizationCode(UserCredentials user)
		{
			// Build the authorization URL
			var authorizationUrl = $"https://accounts.google.com/o/oauth2/auth?client_id={user.ClientId}&redirect_uri=http://localhost:5000/callback&scope={Uri.EscapeUriString(string.Join(" ", user.Scopes))}&response_type=code&access_type=offline";

			Console.WriteLine("Automatically opening the authorization URL in the default web browser...");

			// Open the authorization URL in the default web browser
			try
			{
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(authorizationUrl) { UseShellExecute = true });
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to open the authorization URL: {ex.Message}");
				Console.WriteLine("Please open the following URL in your web browser manually:");
				Console.WriteLine(authorizationUrl);
			}

			Console.WriteLine("Waiting for authorization code...");

			// Start a simple HTTP server to capture the authorization code
			var authorizationCode = StartHttpListener();

			// Save the authorization code to a file for future use
			File.WriteAllText($"AuthorizationCode_{user.ClientId}.txt", authorizationCode);

			return authorizationCode;
		}

		private static string StartHttpListener()
		{
			var listener = new HttpListener();
			listener.Prefixes.Add("http://localhost:5000/callback/");
			listener.Start();

			Console.WriteLine("Waiting for callback...");

			var context = listener.GetContext();
			var request = context.Request;

			// Read the authorization code from the query parameters
			var authorizationCode = request.QueryString.Get("code");

			// Send a response to the browser
			var response = context.Response;
			string responseString = "<html><head><title>Authorization Received</title></head><body><h1>Authorization Received</h1><p>You can close this window.</p></body></html>";
			var buffer = Encoding.UTF8.GetBytes(responseString);
			response.ContentLength64 = buffer.Length;
			var output = response.OutputStream;
			output.Write(buffer, 0, buffer.Length);
			output.Close();

			listener.Stop();

			return authorizationCode;
		}



		private static UserCredential ExchangeAuthorizationCodeForToken(UserCredentials user, string authorizationCode)
		{
			// Exchange authorization code for access token
			var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
				new ClientSecrets
				{
					ClientId = user.ClientId,
					ClientSecret = user.ClientSecret
				},
				user.Scopes,
				"user",
				CancellationToken.None,
				new FileDataStore("Store")).Result;
			return credential;
		}

		private static void RetrieveAndProcessData(UserCredentials user, UserCredential credential)
		{
			// Data retrieval and processing logic
			// Define time range for data retrieval (adjust as needed)
			var startTimeMillis = DateTimeOffset.Now.AddDays(-75).ToUnixTimeMilliseconds();
			var endTimeMillis = DateTimeOffset.Now.ToUnixTimeMilliseconds();

			// Create the Fitness API service
			var service = new FitnessService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential,
				ApplicationName = "nuQare"
			});

			Console.WriteLine("Step Count Data:");
			var stepDataList = StepDataRetrieval.RetrieveStepData(service, user, startTimeMillis, endTimeMillis);

			// MongoDB integration for step data
			var mongoClient = new MongoClient("mongodb+srv://nuqaretestbench1:nuqare@nuqaretest.dox1jvy.mongodb.net/?retryWrites=true&w=majority");
			var database = mongoClient.GetDatabase("Test1");
			var stepDataCollection = database.GetCollection<BsonDocument>("StepData");
			stepDataCollection.InsertMany(stepDataList);

			Console.WriteLine("Step data retrieved and saved successfully.");

			Console.WriteLine("\nHeart Rate Data:");
			HeartRateDataRetrieval.RetrieveAndProcessHeartRateData(service, user, startTimeMillis, endTimeMillis);

			Console.WriteLine("Data retrieved successfully.");
		}

	}
}
