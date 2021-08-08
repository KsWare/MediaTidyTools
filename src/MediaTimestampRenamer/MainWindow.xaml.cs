using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using KsWare.MediaFileLib.Shared;
using KsWare.MediaTimestampRenamer.Plugins;
using static KsWare.MediaFileLib.Shared.RegExUtils;

namespace KsWare.MediaTimestampRenamer {

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		private static string[] oldPatterns = {
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

		private static IProcessPlugin[] _processPlugins = new IProcessPlugin[] {
			new SonyVideoPlugin(),
			new MoviePlugin(),
			new OpenCameraHdrPlugin(),
			new DefaultPlugin(),
		};


		public MainWindow() {
			InitializeComponent();
			AuthorSignTextBox.Text = "KS71"; // TODO load default settings
			StorageLocationTextBox.Text = @"E:\Fotos";
		}

		public string AuthorSign {
			get => AuthorSignTextBox.Text;
			set => AuthorSignTextBox.Text = value;
		}

		public string StorageLocation {
			get => StorageLocationTextBox.Text;
			set => StorageLocationTextBox.Text = value;
		}

		private void UIElement_OnDragOver(object sender, DragEventArgs e) {
			e.Effects = DragDropEffects.Move;
		}

		private async void UIElement_OnDrop(object sender, DragEventArgs e) {
			e.Effects = DragDropEffects.Move;

//			Debug.WriteLine($"{string.Join(", ",e.Data.GetFormats())}");

			var files = (string[]) e.Data.GetData(DataFormats.FileDrop);
			var options = new Options {
				AuthorSign = AuthorSign
			};
			var timer = new DispatcherTimer(TimeSpan.FromMilliseconds(25), DispatcherPriority.Normal, (o, args) => {
				((DispatcherTimer) o).Stop();
				ProcessFiles(files, options);
			}, Dispatcher);
			// await Dispatcher.InvokeAsync(new Action(() => ProcessFiles(files, options)), DispatcherPriority.Normal);
		}

		private void ProcessFiles(IEnumerable<string> files, Options options) {
			var allFiles = new List<string>();
			foreach (var fileName in files) {
				if (Directory.Exists(fileName)) {
					FileUtils.ScanDirectoryRecursive(fileName, ref allFiles);
				}
				else if (File.Exists(fileName)) {
					allFiles.Add(fileName);
				}
				else {
					Debug.WriteLine($"{fileName} not found!");
				}
			}

			allFiles.Sort();

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

			foreach (var fileName in allFiles) ProcessFile(fileName, options);
		}

		private void ProcessFile(string fileName, Options options) {
			var file = new FileInfo(fileName);
			if (!file.Exists) {
				Debug.WriteLine($"File not found! Path:{file.FullName}");// possibly already processed
				return;
			}

			foreach (var processPlugin in _processPlugins) {
				if (!processPlugin.IsMatch(file, out var match)) continue;
				var success = processPlugin.Process(processPlugin.CreateMediaFileInfo(file, match, options.AuthorSign));
				if(success) break;
			}

//			if (IsMatch(Path.GetFileNameWithoutExtension(file.FullName),
//				/*language=regexp*/@"^"
//				                   + /*language=regexp*/ @"(?<timestamp>(\d{4})-(\d{2})-(\d{2})\s(\d{6}))"
//				                   + /*language=regexp*/ @"(?<counter>-\d{1,3})?"
//				                   + /*language=regexp*/ @"(?<ev>\sEV\d\.\d[+±-])?"
//								   + /*language=regexp*/ @"\s(?<authorSign>([A-Z]{2})(\d{2}(A-Za-z)+)?)"
//				                   + /*language=regexp*/ @"(\s(?<basename>{[^}]+}))?"
//				                   + /*language=regexp*/ @"(?<suffix>.*)"
//				                   + /*language=regexp*/ @"$", out var match))
//			{
//				// aktuelles Format, nichts zu tun.
//				return;
//			}

//			// altes Format "2014-04-19 191023 DSC_0092_1.JPG" konvertieren
//			if (IsMatch(Path.GetFileNameWithoutExtension(file.FullName),
//				@"^"
//				+ @"(?<timestamp>(\d{4})-(\d{2})-(\d{2})\s(\d{6,9}))"
//				+ @"\s(?<basename>DSC_\d{4}(_\d+)?)"
//				+ @"(?<suffix>.*)"
//				+"$", out match))
//			{
//				var n = new MediaFileInfo(
//					new FileInfo(fileName),
//					match.Groups["timestamp"].Value,
//					null,
//					AuthorSign,
//					match.Groups["basename"].Value,
//					match.Groups["suffix"].Value);
//				file.MoveTo(n.ToString());
//				return;
//			}

//			// altes Format "2014-04-19 191023000 {DSC_0092_1}.JPG" konvertieren
//			if (IsMatch(Path.GetFileNameWithoutExtension(file.FullName),
//				/*language=regexp*/@"^"
//				+ /*language=regexp*/ @"(?<timestamp>(\d{4})-(\d{2})-(\d{2})\s(\d{6,9}))"
//				+ /*language=regexp*/ @"(\s(?<basename>{[^}]+}))?"
//				+ /*language=regexp*/ @"(?<suffix>.*)"
//				+ /*language=regexp*/ @"$", out match))
//			{
//				var timestamp = match.Groups["timestamp"].Value;
//				var baseName = match.Groups["basename"].Value;
//				var suffix = match.Groups["suffix"].Value;
//				var counter = ((timestamp.Substring(17) != "000" && timestamp.Substring(17) != "") ? "-" + timestamp.Substring(0, 17) : "");
//				timestamp = timestamp.Substring(0, 17);
//				var fn = new MediaFileInfo(file, timestamp, counter, AuthorSign, baseName, suffix);
//				file.MoveTo(fn.ToString());
//				return;
//			}

			{

			}
		}

	}

	internal class Options {
		public string AuthorSign { get; set; }
	}

}
