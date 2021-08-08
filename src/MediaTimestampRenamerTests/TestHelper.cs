using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KsWare.MediaTimestampRenamerTests {

	internal static class TestHelper {
		public static string CreateTestFile(string fileName, string templateFile) {
			var name = Path.GetFileName(fileName);
			var newFileName = Path.Combine(Environment.CurrentDirectory, name);
			File.Copy(GetTestData(templateFile), newFileName);
			return newFileName;
		}

		public static string GetTestData(string fileName) {
			if (fileName.Contains(":")) return fileName;
			var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var newFileName = Path.Combine(assemblyDirectory, "TestData", fileName);
			return newFileName;
		}
	}

}
