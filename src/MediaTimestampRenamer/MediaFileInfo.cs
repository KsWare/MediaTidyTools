using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace KsWare.MediaTimestampRenamer
{
	internal class MediaFileInfo : MediaFileName
	{
		private static readonly IFormatProvider enUS = new CultureInfo("en-US");

		private Lazy<double?> _exposureValue;
		private Lazy<DateTime?> _dateTaken;
		private Lazy<double?> _focalLength;
		private Lazy<double?> _digitalZoom;
		private Lazy<double?> _exposureTimeNumerator;
		private Lazy<double?> _exposureTimeDenominator;
		private Lazy<double?> _exposureTime;

		private MediaFileInfo() : base()
		{
		}

		public MediaFileInfo(string file)
		{
			ParseCore(file, this);
		}

		public MediaFileInfo(FileInfo file)
		{
			ParseCore(file, this);
		}

		public MediaFileInfo(FileInfo fileInfo, object timestamp, string counter, string authorSign, string baseName, string suffix)
		{
			OriginalFile = fileInfo;
			MediaFileName.ParseCore(fileInfo.FullName, this);

			Timestamp = timestamp==null 
				? FileUtils.GetDateTakenOrAlternative(fileInfo.FullName)??fileInfo.LastWriteTime  
				: ParseTimestamp(timestamp);
			Counter = counter;
			AuthorSign = authorSign;
			BaseName = baseName;
			Suffix = suffix;
			IsRenamerd = false;
			InitalizeLazyProperties();
		}

		private void InitalizeLazyProperties()
		{
			_dateTaken = new Lazy<DateTime?>(() => FileUtils.GetDateTaken(OriginalFile.FullName));
			_focalLength = new Lazy<double?>(() =>
				FileUtils.GetDouble(OriginalFile.FullName, p => p.System.Photo.FocalLength));
			_digitalZoom = new Lazy<double?>(() =>
				FileUtils.GetDouble(OriginalFile.FullName, p => p.System.Photo.DigitalZoom));
			_exposureTimeNumerator = new Lazy<double?>(() =>
				FileUtils.GetDouble(OriginalFile.FullName, p => p.System.Photo.ExposureTimeNumerator));
			_exposureTimeDenominator = new Lazy<double?>(() =>
				FileUtils.GetDouble(OriginalFile.FullName, p => p.System.Photo.ExposureTimeDenominator));
			_exposureTime = new Lazy<double?>(() =>
				_exposureTimeNumerator.Value.HasValue && _exposureTimeDenominator.Value.HasValue
					? _exposureTimeNumerator.Value.Value / _exposureTimeDenominator.Value.Value
					: (double?)null);
			_exposureValue = new Lazy<double?>(() =>
				FileUtils.GetDouble(OriginalFile.FullName, p => p.System.Photo.ExposureBias));
		}

		public new double? ExposureValue => _exposureValue.Value;

		public bool IsRenamerd { get; set; }

		public bool Exists => File.Exists(ToString());

		public FileInfo OriginalFile { get; set; }
		

		public string TimestampString => Timestamp.ToString(MediaFileName.TimestampFormat);
		public DateTime? DateTaken => _dateTaken.Value;
		public double? FocalLength => _focalLength.Value;
		public double? DigitalZoom => _digitalZoom.Value;
		public double? ExposureTimeNumerator => _exposureTimeNumerator.Value;
		public double? ExposureTimeDenominator => _exposureTimeDenominator.Value;
		public double? ExposureTime => _exposureTime.Value;
		public TimeSpan? DiffTime { get; set; }

		public GroupType GroupType { get; set; }

		public override string ToString()
		{
			var baseName = string.IsNullOrEmpty(BaseName) ? "" : $" {{{BaseName}}}";
			var exposureValue = GroupType==GroupType.ExposureValue ? ExposureValueToString(ExposureValue) : null;
			var name = Timestamp.ToString(MediaFileName.TimestampFormat) + Counter + exposureValue + " " + AuthorSign + baseName;
			if (!string.IsNullOrEmpty(Suffix)) name += Suffix;
			else if (!name.EndsWith(" ") && !string.IsNullOrEmpty(Suffix) && !Suffix.StartsWith(" ")) name += " "+ Suffix;
			else name += Suffix;

			return Path.Combine(DirectoryName, name + Extension);
		}

		public string CreateUniqueFileName()
		{
			while (true)
			{
				if (!Exists) break;
				if (string.IsNullOrEmpty(Counter)) Counter = "-2";
				else if (Counter.Length == 4)
				{
					var c = int.Parse(Counter.Substring(1)) + 1;
					Counter = $"-{c:D3}";
				}
				else
				{
					var c = int.Parse(Counter.Substring(1)) + 1;
					Counter = $"-{c}";
				}

				if (Counter.Length > 4)
				{
					//TODO
				}
			}

			return ToString();
		}

		private static bool ParseCore(string fileName, MediaFileInfo f) => ParseCore(new FileInfo(fileName), f);
		private static bool ParseCore(FileInfo file, MediaFileInfo f)
		{
			f.OriginalFile = file;
			if (MediaFileName.ParseCore(file.FullName, f))
			{
				f.IsRenamerd = true;
			}
			else
			{
				FileUtils.SplitName(f.OriginalFile.Name, out var baseName, out var suffix);
				f.BaseName = baseName;
				f.Suffix = suffix;
				f.Timestamp = FileUtils.GetDateTaken(f.OriginalFile.FullName) ?? f.OriginalFile.LastWriteTime;
				// f.AuthorSign
				f.IsRenamerd = false;
			}

			f.InitalizeLazyProperties();
			return true;
		}

//		public static MediaFileInfo Parse(string fileName)
//		{
//			var f = new MediaFileInfo();
//			if(!ParseCore(fileName, f)) throw new ArgumentException(); //TODO ArgumentException message
//			return f;
//		}
//
//		public static bool TryParse(string fileName, out MediaFileInfo mediaFileInfo)
//		{
//			var f = new MediaFileInfo();
//			mediaFileInfo = ParseCore(fileName, f) ? f : null;
//			return mediaFileInfo != null;
//		}

		private string ExposureValueToString(double? value)
		{
			if (!value.HasValue) return null;
			if (value.Value < 0) return $" EV{Math.Abs(value.Value).ToString("F1", enUS)}-";
			if (value.Value > 0) return $" EV{value.Value.ToString("F1", enUS)}+";
			return $" EV0.0±";
		}
	}

	internal enum GroupType
	{
		None,
		ExposureValue
	}
}