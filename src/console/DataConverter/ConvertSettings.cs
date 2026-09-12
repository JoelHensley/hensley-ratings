using System;
using System.Globalization;

namespace DataConverter
{
    class ConvertSettings
    {
        public string RawDataFileName = "raw-games.txt";
        public string OutputFileName = "converted-games.csv";
        public DateTime? CutoffDate;

        public ConvertSettings()
        {
            var raw = Environment.GetEnvironmentVariable("CUTOFF_DATE");
            if (!string.IsNullOrEmpty(raw)
                && DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                CutoffDate = d.Date;
        }
    }
}
