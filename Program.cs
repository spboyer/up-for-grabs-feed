using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace up_for_grabs_feed
{
  partial class Program
  {
    static void Main(string[] args)
    {

      var outputPath = Path.GetFullPath("projects.json");
      var result = new List<Project>();
      var errors = new List<Error>();

      var files = from file in Directory.EnumerateFiles(@"data", "*.yml", SearchOption.AllDirectories)
                  select new
                  {
                    File = file,
                    Lines = File.ReadAllText(file)
                  };

      Console.WriteLine("Processing files...");
      foreach (var f in files)
      {
        //Console.WriteLine("Processing " + f.File);
        try
        {
          var deserializer = new Deserializer();
          var content = new StringReader(f.Lines);
          var yamlObject = deserializer.Deserialize<Project>(content);

          result.Add(yamlObject);
        }
        catch (Exception ex)
        {
          errors.Add(new Error() { name = f.File, info = ex.Message, exception = ex.Source });
          continue;
        }
      }

      JsonSerializer serializer = new JsonSerializer();
      using (StreamWriter sw = new StreamWriter(outputPath))
      using (JsonWriter writer = new JsonTextWriter(sw))
      {
        serializer.Serialize(writer, result);
      }

      PostFile(outputPath).Wait();
    }

    private static async Task PostFile(string file)
    {
      CloudStorageAccount storageAccount;
      CloudBlobContainer cloudBlobContainer;

      string storageConnectionString = Environment.GetEnvironmentVariable("storageconnectionstring");
      if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
      {
        try
        {
          // Create the CloudBlobClient that represents the Blob storage endpoint for the storage account.
          Console.WriteLine("Checking for/creating blob container");
          CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
          cloudBlobContainer = cloudBlobClient.GetContainerReference("up-for-grabs");
          await cloudBlobContainer.CreateIfNotExistsAsync();

          // Set the permissions so the blobs are public.
          BlobContainerPermissions permissions = new BlobContainerPermissions
          {
            PublicAccess = BlobContainerPublicAccessType.Blob
          };
          await cloudBlobContainer.SetPermissionsAsync(permissions);

          Console.WriteLine("Pushing to CDN");
          CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference("projects.json");
          cloudBlockBlob.Properties.ContentType = "application/json";

          await cloudBlockBlob.UploadFromFileAsync(file);

        }
        catch (StorageException ex)
        {
          Console.WriteLine("Error returned from the service: {0}", ex.Message);
        }
        finally
        {
          Console.WriteLine("Process complete.");
        }
      }
    }
  }
}
