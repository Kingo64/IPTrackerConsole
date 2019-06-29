using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace IPTrackerConsole {
	class Program {
		// If modifying these scopes, delete your previously saved credentials
		// at ~/.credentials/drive-dotnet-quickstart.json
		static string[] Scopes = { DriveService.Scope.Drive };
		static string ApplicationName = "IP Tracker";

		static void Main(string[] args) {
			while (true) {
				UpdateIP();
				var frequency = 12; //hours
				Thread.Sleep(frequency * 60 * 60 * 1000);
			}
		}

		static void UpdateIP() {
			UserCredential credential;

			using (var stream =
				new FileStream("client_secret.json", FileMode.Open, FileAccess.Read)) {

				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
					GoogleClientSecrets.Load(stream).Secrets,
					Scopes,
					"user",
					CancellationToken.None).Result;
			}

			// Create Drive API service.
			var service = new DriveService(new BaseClientService.Initializer() {
				HttpClientInitializer = credential,
				ApplicationName = ApplicationName,
			});

			//Get IP Address
			var publicIp = new WebClient().DownloadString("https://api.ipify.org");
			WriteLine("Got IP Address: " + publicIp);

			//Create file
			var file = new Google.Apis.Drive.v3.Data.File();
			file.Name = "IP Address";
			var ipStream = new MemoryStream(Encoding.ASCII.GetBytes(publicIp));

			//Find file to replace otherwise create new one
			var foundFile = retrieveAllFiles(service).Find(x => x.Name == "IP Address");
			var fileId = foundFile != null ? foundFile.Id : null;
			if (fileId != null) {
				WriteLine("Updating existing file");
				var request = service.Files.Update(file, fileId, ipStream, "text/plain");
				request.Upload();
			}
			else {
				WriteLine("Uploading new file");
				var request = service.Files.Create(file, ipStream, "text/plain");
				request.Upload();
			}

			WriteLine("Done!");
		}

		static List<Google.Apis.Drive.v3.Data.File> retrieveAllFiles(DriveService service) {
			var result = new List<Google.Apis.Drive.v3.Data.File>();
			var request = service.Files.List();

			do {
				try {
					var files = request.Execute();
					result.AddRange(files.Files);
					request.PageToken = files.NextPageToken;
				}
				catch (Exception e) {
					WriteLine("An error occurred: " + e.Message);
					request.PageToken = null;
				}
			}
			while (!String.IsNullOrEmpty(request.PageToken));
			return result;
		}

		static void WriteLine(String message) {
			Console.WriteLine(DateTime.Now.ToShortTimeString() + ": " + message);
		}
	}
}