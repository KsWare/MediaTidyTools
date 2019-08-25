using System;
using System.IO;
using System.Text.RegularExpressions;
using KsWare.MediaFileLib.Shared;

namespace KsWare.MediaTimestampRenamer.Plugins
{
	// SONY Video: C0001.MP4 C0001T01.JPG C0001M01.XML
	public class SonyVideoPlugin : IProcessPlugin
	{
		public ProcessPriority Priority => ProcessPriority.NameMatch;

		public bool IsMatch(FileInfo fileInfo, out Match match) =>
			RegExUtils.IsMatch(fileInfo.Name,
				@"(^(?<basename>C\d{4})(?<ext>\.mp4)$)|(^(?<basename>C\d{4})[MT]\d{2}(?<ext>\.(xml|jpg))$)", out match);

		public MediaFileInfo CreateMediaFileInfo(FileInfo file, Match match, string authorSign)
		{
			throw new NotImplementedException();
		}

		public bool Process(MediaFileInfo file)
		{
			MediaFileInfo mp4, xml, jpg;
			switch (file.Extension.ToLowerInvariant())
			{
				case ".mp4":
					mp4 = file;
					xml = new MediaFileInfo(Path.Combine(mp4.DirectoryName, $"{mp4.BaseName}M01.XML"));
					jpg = new MediaFileInfo(Path.Combine(mp4.DirectoryName, $"{mp4.BaseName}T01.JPG"));
					break;
				case ".xml":
					xml = file;
					mp4 = new MediaFileInfo(Path.Combine(xml.DirectoryName, $"{xml.BaseName}.MP4"));
					jpg = new MediaFileInfo(Path.Combine(xml.DirectoryName, $"{xml.BaseName}T01.JPG"));
					break;
				case ".jpg":
					jpg = file;
					xml = new MediaFileInfo(Path.Combine(jpg.DirectoryName, $"{jpg.BaseName}M01.XML"));
					mp4 = new MediaFileInfo(Path.Combine(jpg.DirectoryName, $"{jpg.BaseName}.MP4"));
					break;
				default:
					return false;
			}

			if (!mp4.Exists) return true;


			var ts = FileUtils.GetDateTakenOrAlternative(mp4.FullName);
			mp4.Timestamp = ts.Value;
			mp4.Rename();

			// pair C0003.MP4 <=> C0003M01.XML
			if (xml.OriginalFile.Exists)
			{
				xml.Timestamp = ts.Value;
				xml.Rename();
			}

			// pair C0003.MP4 <=> C0003T01.JPG
			if (jpg.OriginalFile.Exists)
			{
				jpg.Timestamp = ts.Value;
				jpg.Rename();
			}

			return true;
		}
	}
}
