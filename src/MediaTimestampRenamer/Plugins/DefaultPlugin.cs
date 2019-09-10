using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using KsWare.MediaFileLib.Shared;
using Path = System.IO.Path;

namespace KsWare.MediaTimestampRenamer.Plugins
{
	internal class DefaultPlugin : IProcessPlugin
	{
		private static readonly List<string> RawDataFormats = LoadRawDataFormats();

		private static List<string> LoadRawDataFormats()
		{
			var af = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var fn = Path.Combine(af, "Resources", "RawDataFormats.txt");
			var list = new List<string>();
			using (var r=File.OpenText(fn))
			{
				string line;
				while ((line=r.ReadLine()?.Trim()) != null)
				{
					if(line==String.Empty) break;
					if(line.StartsWith("#")) break;
					var tl=line.Split(new[] {':'}, 2);
					var extensions = tl[1].Split(',').Select(s=>s.Trim().ToLower());
					foreach (var ext in extensions)
					{
						if(!list.Contains(ext)) list.Add(ext);
					}
				}
			}

			return list;
		}

		public ProcessPriority Priority => ProcessPriority.Default;

		public bool IsMatch(FileInfo fileInfo, out Match match)
		{
			match = null;
			return true;
		}

		public MediaFileInfo CreateMediaFileInfo(FileInfo fileInfo, Match match, string authorSign)
		{
			return new MediaFileInfo(fileInfo, authorSign);
		}

		public bool Process(MediaFileInfo fileInfo)
		{
			if (FileUtils.IsMovie(fileInfo.Name)) { return ProcessMovie(fileInfo); }

			ProcessBaseFile(fileInfo, out string baseFile);
			var refFile = baseFile ?? fileInfo.OriginalFile.FullName;
			ProcessExposureNormalFile(fileInfo, ref refFile);

			var ts = FileUtils.GetDateTakenOrAlternative(refFile);
			if (!ts.HasValue)
			{
				Debug.WriteLine($"No date!. {refFile}");
				return false;
			}

			fileInfo.Timestamp = ts.Value;
			if (!fileInfo.Rename()) return false;
		

			// Rename dependencies 
			fileInfo.RenameDependency(".thumbs", ".256");
			fileInfo.RenameDependency(".preview", "_~AuroraPreviewAuto");
			fileInfo.RenameDependencyRawExtension(RawDataFormats,"RAW", true);
			fileInfo.RenameDependencyExtension(".AUH", "AUH", true, out _); // Aurora HDR
			return true;
		}

		private bool ProcessMovie(MediaFileInfo movieFile)
		{
			return false;
		}

		private void ProcessBaseFile(MediaFileInfo file, out string baseFile)
		{
			baseFile = null;
			if (!string.IsNullOrEmpty(file.Suffix))
			{
				FileUtils.TryGetBaseFile(file.OriginalFile.FullName, null, out baseFile);
			}
		}

		private void ProcessExposureNormalFile(MediaFileInfo mediaFileInfo, ref string refFile)
		{

			if ((mediaFileInfo.Suffix ?? "").StartsWith("~Aurora"))
			{
				// Aurora HDR verwendet manchmal falsche Basis Datei.
				if (!FileUtils.TryGetExposureBias(refFile, out var ev)) return;
				if (!FileUtils.TryFindExposureDefaultFile(refFile, out var exposureDefaultFile)) return;
				mediaFileInfo.BaseName = FileUtils.GetOriginalName(exposureDefaultFile);
				refFile = exposureDefaultFile;
			}
			else if (!string.IsNullOrEmpty(mediaFileInfo.Suffix))
				return;
			else
			{
				if (!FileUtils.TryGetExposureBias(refFile, out var ev)) return;
				if (!FileUtils.TryFindExposureDefaultFile(refFile, out var exposureDefaultFile)) return;
				mediaFileInfo.GroupType = GroupType.ExposureValue;
				refFile = exposureDefaultFile;
			}


		}

	}
}