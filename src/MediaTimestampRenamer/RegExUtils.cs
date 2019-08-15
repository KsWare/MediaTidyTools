using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace KsWare.MediaTimestampRenamer
{
	// using static KsWare.MediaTimestampRenamer.RegExUtils;

	public static class RegExUtils
	{
		public static bool IsMatch(string input, [RegexPattern] string pattern, out Match match)
		{
			match = Regex.Match(input, pattern, RegexOptions.IgnorePatternWhitespace);
			return match.Success;
		}
	}
}
