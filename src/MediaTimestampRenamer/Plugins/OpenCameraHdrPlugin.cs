using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using KsWare.MediaFileLib.Shared;

namespace KsWare.MediaTimestampRenamer.Plugins {

	// OpenCamera IMG_20210803_103458_*.jpg
	// HDR:          *_0.jpg, *_1.jpg, *_2.jpg, *_HDR.jpg
	// HDR RAW:      *_0.dng, *_0.jpg, *_1.dng, *_1.jpg, *_2.dng, *_2.jpg, *_HDR.jpg
	// HDR RAW only: *_HDR.jpg, *_0.dng, *_1.dng, *_2.dng
	// *_0 = -2, *_1 = 0, _*.2 = +2
	//
	// jpg TODO support |webp|png
	public class OpenCameraHdrPlugin : IProcessPlugin {

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private static readonly IFormatProvider enUS = new CultureInfo("en-US");

		private static Regex regex = new Regex(
			@"(^(?<baseName>IMG_\d{8}_\d{6})_(?<partSuffix>0|1|2|HDR))(?<ext>\.(dng|jpg|webp|png))$", RegexOptions.Compiled|RegexOptions.IgnoreCase|RegexOptions.IgnorePatternWhitespace|RegexOptions.ExplicitCapture);

		public string Name => "OpenCamera HDR";

		public ProcessPriority Priority => ProcessPriority.NameMatch;

		public bool IsMatch(FileInfo fileInfo, out Match match) {
			match = regex.Match(fileInfo.Name);
			return match.Success;
		}

		public MediaFileInfo CreateMediaFileInfo(FileInfo fileInfo, Match match, string authorSign) {
			return new MediaFileInfo(fileInfo, authorSign);
		}

		private FileInfo CreateFileInfo(MediaFileInfo mediaFile, Match match, string newPartSuffix, string newExtension) {
			var baseName = match.Groups["baseName"].Value;
			var fn = baseName + newPartSuffix + newExtension;
			var fi = new FileInfo(Path.Combine(mediaFile.OriginalFile.DirectoryName, fn));
			return fi;
		}

		private FileInfo FindFile(MediaFileInfo mediaFile, Match match, string newPartSuffix, string newExtension) {
			var fi = CreateFileInfo(mediaFile, match, newPartSuffix, newExtension);
			if (fi.Exists) return fi;

			var fn = match.Groups["baseName"].Value +newPartSuffix;
			var found =  Directory.GetFiles(mediaFile.OriginalFile.DirectoryName, "*{" + fn + "}" + newExtension).FirstOrDefault();
			if (found == null) return null;
			return new FileInfo(found);
		}

		public bool Process(MediaFileInfo file) {
			var match = regex.Match(file.OriginalFile.Name);
			var partSuffix = match.Groups["partSuffix"].Value;
			var ext = match.Groups["ext"].Value.ToLowerInvariant();
			
			var hdrFile = FindFile(file, match, "_HDR", ".jpg");
			if (!hdrFile.Exists) return false;

			var files=new FileInfo[3];
			var hasAllFiles = true;
			files[0] = FindFile(file, match, "_0", ".jpg");
			files[1] = FindFile(file, match, "_1", ".jpg");
			files[2] = FindFile(file, match, "_2", ".jpg");
			foreach (var fi in files) if (fi == null) hasAllFiles = false;

			var rawFiles=new FileInfo[3];
			var hasAllRawFiles = true;
			rawFiles[0] = FindFile(file, match, "_0", ".dng");
			rawFiles[1] = FindFile(file, match, "_1", ".dng");
			rawFiles[2] = FindFile(file, match, "_2", ".dng");
			foreach (var fi in rawFiles) if (fi == null) hasAllRawFiles = false;

			if (!hasAllFiles && !hasAllRawFiles) return false;

			// ---
			file.Timestamp = FileUtils.GetTimestampFromFileName(hdrFile.FullName).Value;
			file.GroupType = GroupType.ExposureValue;

			// INFO: Xiaomi OpenCamera EV is always 0, so emulate EV from brightness 

			if (partSuffix == "HDR") {
				file.GroupIndex = "HDR";
			} else if (ext == ".dng") {
				switch (match.Groups["partSuffix"].Value) {
					// INFO: dng file does not have a brightness value (currently), so provide a fallback
					case "0":{
						var brightness0 = GetBrightness(rawFiles[0].FullName, files[0]?.FullName, 2);
						var brightness1 = GetBrightness(rawFiles[1].FullName, files[1]?.FullName, 0);
						file.GroupIndex = FileUtils.ExposureValueToString(brightness1 - brightness0); // emulate EV from brightness
						break;
					}
					case "1": {
						file.GroupIndex = FileUtils.ExposureValueToString(0); 
						break;
					}
					case "2": {
						var brightness1 = GetBrightness(rawFiles[1].FullName, files[1]?.FullName, 0);
						var brightness2 = GetBrightness(rawFiles[2].FullName, files[2]?.FullName, -2);
						file.GroupIndex = FileUtils.ExposureValueToString(brightness1 - brightness2); // emulate EV from brightness
						break;
					}
				}
			}
			else {
				switch (match.Groups["partSuffix"].Value) {
					case "0": {
						var brightness0 = FileUtils.GetBrightness(files[0].FullName);
						var brightness1 = FileUtils.GetBrightness(files[1].FullName);
						file.GroupIndex = FileUtils.ExposureValueToString(brightness1 - brightness0); // emulate EV from brightness
						break;
					}
					case "1": {
						file.GroupIndex = FileUtils.ExposureValueToString(0); break;
					}
					case "2": {
						var brightness1 = FileUtils.GetBrightness(files[1].FullName);
						var brightness2 = FileUtils.GetBrightness(files[2].FullName);
						file.GroupIndex = FileUtils.ExposureValueToString(brightness1 - brightness2); // emulate EV from brightness
						break;
					}
				}
			}

			file.Rename(pluginName: Name);
			return true;
		}

		//dng file does not have a brightness value (currently), so provide a fallback.
		private static double GetBrightness(string file, string fileFallback, double fallbackValue) {
			var v = FileUtils.GetBrightness(file);
			if (v.HasValue) return v.Value;
			if(!string.IsNullOrEmpty(fileFallback) && File.Exists(fileFallback)) v = FileUtils.GetBrightness(fileFallback);
			if (v.HasValue) return v.Value;
			return fallbackValue;
		}
	}

}
