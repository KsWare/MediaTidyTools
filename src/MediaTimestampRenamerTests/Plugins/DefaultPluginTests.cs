using KsWare.MediaTimestampRenamer.Plugins;
using NUnit.Framework;
using System;
using System.IO;
using System.Text.RegularExpressions;
using KsWare.MediaFileLib.Shared;
using KsWare.MediaTimestampRenamer;

namespace KsWare.MediaTimestampRenamerTests.Plugins {

	[TestFixture]
	public class DefaultPluginTests {

		[TestCase(@"E:\2000-12-24 123456 EV1.0- XY00 {Z0001}~suffix+foo", true)]
		public void IsMatch(string fileName, bool expectedResult) {
			var sut = new DefaultPlugin();
			var result = sut.IsMatch(new FileInfo(fileName), out _);

			Assert.That(result, Is.EqualTo(expectedResult));
		}

		[TestCase(@"X:\Z0001.jpg", "W256NoExif.jpg", true, null, null, null, "AA88", "Z0001", null)]
		[TestCase(@"X:\Z0001~suffix.jpg", "W256NoExif.jpg", true, null, null, null, "AA88", "Z0001", "~suffix")]
		public void CreateMediaFileInfo_Basic(string fileName, string template, bool returnValue, string timeStamp,
			string counter, double? expValue, string authorSign, string baseName, string suffix) {
			fileName = TestHelper.CreateTestFile(fileName, template);
			var sut = new DefaultPlugin();
			var fileInfo = new FileInfo(fileName);
			var isMatch = sut.IsMatch(fileInfo, out var match);
			Assert.That(isMatch, Is.EqualTo(returnValue));
			var mediaFileInfo = sut.CreateMediaFileInfo(fileInfo, match, "AA88");

			Assert.That(mediaFileInfo.OriginalFile, Is.EqualTo(fileInfo));
//			Assert.That(mediaFileInfo.Timestamp, Is.EqualTo(new DateTime(2000, 12, 24, 12, 34, 56)));
			Assert.That(mediaFileInfo.Counter, Is.EqualTo(counter));
			Assert.That(mediaFileInfo.ExposureValue, Is.EqualTo(expValue));
			Assert.That(mediaFileInfo.AuthorSign, Is.EqualTo(authorSign)); //TODO
			Assert.That(mediaFileInfo.BaseName, Is.EqualTo(baseName));
			Assert.That(mediaFileInfo.Suffix, Is.EqualTo(suffix));
		}

		[TestCase(@"X:\Z0001.jpg", "W256NoExif.jpg", true, null, null, null, "AA88", "Z0001", null)]
		public void Process(string fileName, string template, bool returnValue, string timeStamp, string counter,
			double? expValue, string authorSign, string baseName, string suffix) {
			fileName = TestHelper.CreateTestFile(fileName, template);
			var sut = new DefaultPlugin();
			var fileInfo = new FileInfo(fileName);
			var isMatch = sut.IsMatch(fileInfo, out var match);
			Assert.That(isMatch, Is.EqualTo(returnValue));
			var mediaFileInfo = sut.CreateMediaFileInfo(fileInfo, match, "AA88");
			sut.Process(mediaFileInfo);
			Assert.That(mediaFileInfo.IsRenamed, Is.EqualTo(true));
			Assert.That(mediaFileInfo.OriginalFile, Is.EqualTo(fileInfo));
			//			Assert.That(mediaFileInfo.Timestamp, Is.EqualTo(new DateTime(2000, 12, 24, 12, 34, 56)));
			Assert.That(mediaFileInfo.Counter, Is.EqualTo(counter));
			Assert.That(mediaFileInfo.ExposureValue, Is.EqualTo(expValue));
			Assert.That(mediaFileInfo.AuthorSign, Is.EqualTo(authorSign)); //TODO
			Assert.That(mediaFileInfo.BaseName, Is.EqualTo(baseName));
			Assert.That(mediaFileInfo.Suffix, Is.EqualTo(suffix));

			Assert.That(mediaFileInfo.OldFile.FullName, Is.EqualTo(fileName));
			Assert.That(mediaFileInfo.OldFile.Exists, Is.EqualTo(false));
			Assert.That(mediaFileInfo.OriginalFile.Exists, Is.EqualTo(true));
			Assert.That(mediaFileInfo.OriginalFile.Name.Contains($"{{{baseName}}}"), Is.EqualTo(true));
			Assert.That(mediaFileInfo.OriginalFile.Name.Contains($"{authorSign}"), Is.EqualTo(true));
			if (suffix != null) Assert.That(mediaFileInfo.OriginalFile.Name.Contains($"{suffix}"), Is.EqualTo(true));
		}
	}

}