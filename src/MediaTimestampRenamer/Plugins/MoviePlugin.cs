using System;
using System.IO;
using System.Text.RegularExpressions;
using KsWare.MediaFileLib.Shared;

namespace KsWare.MediaTimestampRenamer.Plugins
{
	internal class MoviePlugin : IProcessPlugin
	{
		//TODO parse Movie suffix
		public ProcessPriority Priority => ProcessPriority.ExtensionMatch;

		public bool IsMatch(FileInfo fileInfo, out Match match) =>
			RegExUtils.IsMatch(fileInfo.Name, @"^.*?\.(mp4|mov)$", out match);

		public MediaFileInfo CreateMediaFileInfo(FileInfo fileInfo, Match match, string authorSign)
		{
			return new MediaFileInfo(fileInfo, authorSign);
		}

		public bool Process(MediaFileInfo fileInfo)
		{

			fileInfo.BaseName = Path.GetFileNameWithoutExtension(fileInfo.Name);
			fileInfo.Timestamp = FileUtils.GetDateTakenOrAlternative(fileInfo.FullName).Value;
			fileInfo.Rename();
			return true;
		}
	}
}