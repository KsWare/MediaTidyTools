using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KsWare.MediaTimestampRenamerTests
{
	internal static class TestTools
	{
		public static string GetTestDataPath(string name)
		{
			var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			return Path.Combine(folder, "TestData", name);
		}
	}
}
