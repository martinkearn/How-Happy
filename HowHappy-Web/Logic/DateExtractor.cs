using ExifLib;
using Microsoft.AspNet.Http;
using System;
using System.Collections.Generic;

namespace HowHappy_Web.Logic
{
    public class DateExtractor
    {
        /// <summary>
        /// Embedded photo info fields that we can use to identify the date the photo was taken.
        /// Listed in order of preference.
        /// </summary>
        private List<ExifTags> UseableExifDateTimes = new List<ExifTags>
        {
            ExifTags.DateTime,
            ExifTags.DateTimeOriginal,
            ExifTags.DateTimeDigitized,
            ExifTags.GPSDateStamp
        };

        public DateTime? ReadDateFromImage(IFormFile file)
        {
            try
            {
                using (var reader = new ExifReader(file.OpenReadStream()))
                {
                    foreach (var dateField in UseableExifDateTimes)
                    {
                        DateTime datePictureTaken;
                        if (reader.GetTagValue(ExifTags.DateTimeDigitized, out datePictureTaken))
                            return datePictureTaken;
                    }
                }
            }
            catch (Exception) // No EXIF data in the image.
            {
            }
            return null; // No date fields found.
        }
    }
}
