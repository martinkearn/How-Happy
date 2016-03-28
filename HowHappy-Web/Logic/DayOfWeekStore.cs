using HowHappy_Web.Models;
using Microsoft.Extensions.OptionsModel;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using Newtonsoft.Json;

namespace HowHappy_Web.Logic
{
    public class DayOfWeekStore
    {
        const string ContainerName = "howhappy"; // Container names must be lowercase.
        const string SummaryEntityBlobName = "WeekdaySummary.json";
        AzureAppSettings options;
        CloudBlobContainer container;
        CloudBlockBlob blockBlob;

        public DayOfWeekStore(IOptions<AzureAppSettings> _options)
        {
            options = _options.Value;
            InitialiseBlobContainer();
        }

        public WeekSummary SaveHappinessForEachFace(List<Face> faces, DayOfWeek dayTaken)
        {
            if (string.IsNullOrWhiteSpace(options.StorageConnectionString))
                return new WeekSummary(); // No storage configured, so can't record min / max of happiness for a day.
            if (faces.Count == 0)
                return new WeekSummary();
            return SaveJsonBlob(faces, dayTaken);
            // TODO: Lock on read, write update.
        }

        private WeekSummary SaveJsonBlob(List<Face> faces, DayOfWeek dayTaken)
        {
            var summary = ReadCurrentSummary();

            var day = summary.FirstOrDefault(d => d.DayOfWeek == dayTaken);
            if (day == null)
            {
                day = new WeekdaySummary();
                day.DayOfWeek = dayTaken;
                summary.Add(day);
            }
            int existingCount;

            foreach (var face in faces)
            {
                int happiness = (int)(face.scores.happiness * 100);
                if (!day.HappinessScores.TryGetValue(happiness, out existingCount))
                    day.HappinessScores.Add(happiness, 1);
                else
                    day.HappinessScores[happiness] = existingCount + 1;
            }

            WriteSummary(summary);
            return summary;
        }

        public WeekSummary ReadCurrentSummary()
        {
            using (var memoryStream = new MemoryStream())
            {
                blockBlob.DownloadToStream(memoryStream);
                var text = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());

                return JsonConvert.DeserializeObject<WeekSummary>(text);
            }
        }

        private void WriteSummary(WeekSummary summary)
        {
            blockBlob.Properties.ContentType = "application/json";
            blockBlob.UploadText(JsonConvert.SerializeObject(summary));
        }

        private void InitialiseBlobContainer()
        {
            var storageAccount = CloudStorageAccount.Parse(options.StorageConnectionString);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            container = blobClient.GetContainerReference(ContainerName);

            // Create the container if it doesn't already exist.
            container.CreateIfNotExists();

            // Get a reference to the JSON file.
            blockBlob = container.GetBlockBlobReference(SummaryEntityBlobName);

            if (blockBlob.Exists())
                return;
            // Create a new, empty weekday summary as the json blob does not exist.
            var summary = new WeekSummary();
            WriteSummary(summary);
        }
    }
}
