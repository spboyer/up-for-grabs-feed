using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
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

      IConfiguration config = new ConfigurationBuilder()
          .AddEnvironmentVariables()
          .Build();

      var outputPath = Path.GetFullPath("/mnt/output/project.json");
      var result = new List<Project>();
      var errors = new List<Error>();

      var files = from file in Directory.EnumerateFiles(@"data", "*.yml", SearchOption.AllDirectories)
                  select new
                  {
                    File = file,
                    Lines = File.ReadAllText(file)
                  };

      foreach (var f in files)
      {
        Console.WriteLine("Processing " + f.File);
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

      PostFile(outputPath).GetAwaiter().GetResult();
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
          CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
          cloudBlobContainer = cloudBlobClient.GetContainerReference("up-for-grabs");
          await cloudBlobContainer.CreateIfNotExistsAsync();
          Console.WriteLine("Created container '{0}'", cloudBlobContainer.Name);
          Console.WriteLine();

          // Set the permissions so the blobs are public.
          BlobContainerPermissions permissions = new BlobContainerPermissions
          {
            PublicAccess = BlobContainerPublicAccessType.Blob
          };
          await cloudBlobContainer.SetPermissionsAsync(permissions);

          CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference("projects.json");
          await cloudBlockBlob.UploadFromFileAsync(file);

          // List the blobs in the container.
          Console.WriteLine("Listing blobs in container.");
          BlobContinuationToken blobContinuationToken = null;
          do
          {
            var results = await cloudBlobContainer.ListBlobsSegmentedAsync(null, blobContinuationToken);
            // Get the value of the continuation token returned by the listing call.
            blobContinuationToken = results.ContinuationToken;
            foreach (IListBlobItem item in results.Results)
            {
              Console.WriteLine(item.Uri);
            }
          } while (blobContinuationToken != null); // Loop while the continuation token is not null.
          Console.WriteLine();


        }
        catch (StorageException ex)
        {
          Console.WriteLine("Error returned from the service: {0}", ex.Message);
        }
      }
    }
  }
}
