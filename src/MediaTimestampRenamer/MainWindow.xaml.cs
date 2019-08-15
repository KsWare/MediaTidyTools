using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using static KsWare.MediaTimestampRenamer.RegExUtils;

namespace KsWare.MediaTimestampRenamer {

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private static string[] oldPatterns =
		{
			@"(?<name>DSC_\d{4,5})(?<ext>\.(jpg))" /* DSC_0014.JPG */,
			/* 20180921_181653.jpeg */
			/* cameringo_20180908_102138.jpeg */
			/* 20180915_132402_panorama.jpeg* */
			/* IMG_20161008_190347.JPG */
			/* photo_1512241199761.jpg */
			/* MOV_0123.mp4 */
			/* PANO_20180915_135131.jpg */
			/* portal_20170809_083140_0.png */
			/* runtastic2018-09-09_17_36_10.jpg */
			/* sketch_pic_1512240675827.jpg */
			/* P1000715 */
			
			@"^(?<name>.+)(?<ext>\.[^.]+)$" /* name.ext */
		};
		
		
		public MainWindow() {
			InitializeComponent();
			AuthorSignTextBox.Text = "KS71";
			StorageLocationTextBox.Text = @"E:\Fotos";
		}

		public string AuthorSign
		{
			get => AuthorSignTextBox.Text;
			set => AuthorSignTextBox.Text = value;
		}

		public string StorageLocation {
			get => StorageLocationTextBox.Text;
			set => StorageLocationTextBox.Text = value;
		}

		private void UIElement_OnDragOver(object sender, DragEventArgs e)
		{
			e.Effects=DragDropEffects.Move;
		}

		private void UIElement_OnDrop(object sender, DragEventArgs e)
		{
			e.Effects = DragDropEffects.Move;

//			Debug.WriteLine($"{string.Join(", ",e.Data.GetFormats())}");

			var files = (string[])e.Data.GetData(DataFormats.FileDrop);
			ProcessFiles(files);
		}

		private void ProcessFiles(IEnumerable<string> files) 
		{
			var recursiveFiles=new List<string>();
			foreach (var fileName in files)
			{
				if (Directory.Exists(fileName))
				{
					FileUtils.ScanDirectoryRecursive(fileName, ref recursiveFiles);
				}
				else if (File.Exists(fileName))
				{
					recursiveFiles.Add(fileName);
				}
				else
				{
					Debug.WriteLine($"{fileName} not found!");
				}
			}
			recursiveFiles.Sort();

//			var unknownFileNameFormat=new List<string>();
//			for (int i = 0; i < recursiveFiles.Count; i++)
//			{
//				var fileName = recursiveFiles[i];
//				if (!CheckFileName(fileName))
//				{
//					unknownFileNameFormat.Add(fileName);
//					recursiveFiles.RemoveAt(i--);
//				}
//			}
//
//			if (unknownFileNameFormat.Count > 0)
//			{
//				var names = string.Join("\n", unknownFileNameFormat.Take(10));
//				if (unknownFileNameFormat.Count > 10) names += "\n...";
//				var result=MessageBox.Show("Some files has unsupported names.\nSkip those files and continue?\n\n"
//					+names,"Warning", MessageBoxButton.OKCancel);
//				switch (result)
//				{
//					case MessageBoxResult.OK: break;
//					case MessageBoxResult.Cancel: return;
//				}
//			}

			foreach (var fileName in recursiveFiles)
			{
				ProcessFile(fileName);
			}
		}

		private void ProcessFile(string fileName)
		{
			var file = new FileInfo(fileName);
			if (!file.Exists)
			{
				Debug.WriteLine($"File not found! Path:{file.FullName}");
				return;
			}

			if (IsMatch(Path.GetFileNameWithoutExtension(file.FullName),
				/*language=regexp*/@"^"
				                   + /*language=regexp*/ @"(?<timestamp>(\d{4})-(\d{2})-(\d{2})\s(\d{6}))"
				                   + /*language=regexp*/ @"(?<counter>-\d{1,3})?"
				                   + /*language=regexp*/ @"(?<ev>\sEV\d\.\d[+±-])?"
								   + /*language=regexp*/ @"\s(?<authorSign>([A-Z]{2})(\d{2}(A-Za-z)+)?)"
				                   + /*language=regexp*/ @"(\s(?<basename>{[^}]+}))?"
				                   + /*language=regexp*/ @"(?<suffix>.*)"
				                   + /*language=regexp*/ @"$", out var match))
			{
				// aktuelles Format, nichts zu tun.
				return;
			}

			// altes Format "2014-04-19 191023 DSC_0092_1.JPG" konvertieren
			if (IsMatch(Path.GetFileNameWithoutExtension(file.FullName),
				@"^"
				+ @"(?<timestamp>(\d{4})-(\d{2})-(\d{2})\s(\d{6,9}))"
				+ @"\s(?<basename>DSC_\d{4}(_\d+)?)"
				+ @"(?<suffix>.*)"
				+"$", out match))
			{
				var n = new MediaFileInfo(
					new FileInfo(fileName),
					match.Groups["timestamp"].Value,
					null,
					AuthorSign,
					match.Groups["basename"].Value,
					match.Groups["suffix"].Value);
				file.MoveTo(n.ToString());
				return;
			}

			// altes Format "2014-04-19 191023000 {DSC_0092_1}.JPG" konvertieren
			if (IsMatch(Path.GetFileNameWithoutExtension(file.FullName),
				/*language=regexp*/@"^"
				+ /*language=regexp*/ @"(?<timestamp>(\d{4})-(\d{2})-(\d{2})\s(\d{6,9}))"
				+ /*language=regexp*/ @"(\s(?<basename>{[^}]+}))?"
				+ /*language=regexp*/ @"(?<suffix>.*)"
				+ /*language=regexp*/ @"$", out match))
			{
				var timestamp = match.Groups["timestamp"].Value;
				var baseName = match.Groups["basename"].Value;
				var suffix = match.Groups["suffix"].Value;
				var counter = ((timestamp.Substring(17) != "000" && timestamp.Substring(17) != "") ? "-" + timestamp.Substring(0, 17) : "");
				timestamp = timestamp.Substring(0, 17);
				var fn = new MediaFileInfo(file, timestamp, counter, AuthorSign, baseName, suffix);
				file.MoveTo(fn.ToString());
				return;
			}

			{
				if (FileUtils.IsMovie(file.Name))
				{
					ProcessMovie(file);
					return;
				}

				ProcessBaseFile(file, out string baseFile, out MediaFileInfo mediaFileName);
				var refFile = baseFile ?? file.FullName;
				ProcessExposureNormalFile(ref refFile, ref mediaFileName);

				var ts = FileUtils.GetDateTakenOrAlternative(refFile);
				if (!ts.HasValue)
				{
					Debug.WriteLine($"No date!. {refFile}");
					return;
				}

				mediaFileName.Timestamp = ts.Value;
				file.MoveTo(mediaFileName.CreateUniqueFileName());
			}
		}

		private void ProcessBaseFile(FileInfo file, out string baseFile, out MediaFileInfo mediaFileInfo)
		{
			baseFile = null;
			if (FileUtils.SplitName(file.FullName, out var baseName, out var suffix))
			{
				FileUtils.TryGetBaseFile(file.FullName, baseName, out baseFile);
			}
			mediaFileInfo = new MediaFileInfo(file, null, null, AuthorSign, baseName, suffix);
		}

		private void ProcessExposureNormalFile(ref string refFile, ref MediaFileInfo mediaFileInfo)
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
				mediaFileInfo.GroupType=GroupType.ExposureValue;
				refFile = exposureDefaultFile;				
			}
			

		}

		private void ProcessMovie(FileInfo file)
		{
			var ts = FileUtils.GetDateTakenOrAlternative(file.FullName);
			var f = new MediaFileInfo(file, ts.Value, null, AuthorSign, file.Name, null);
			file.MoveTo(f.CreateUniqueFileName());
		}
	}
}
