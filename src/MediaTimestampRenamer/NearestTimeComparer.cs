using System;
using System.Collections.Generic;

namespace KsWare.MediaTimestampRenamer
{
	internal class NearestTimeComparer : IComparer<DateTime>
	{
		public static int CompareTimeSpan(TimeSpan? m1, TimeSpan? m2) =>
			FileUtils.CompareNearestDouble(m1.HasValue ? m1.Value.TotalDays : (double?)null, m2.HasValue ? m2.Value.TotalDays : (double?)null);

		public static int CompareTime(DateTime baseTime, DateTime x, DateTime y) =>
			CompareTimeSpan(x.Subtract(baseTime), y.Subtract(baseTime));

		private readonly DateTime _baseTime;
		public NearestTimeComparer(DateTime baseTime)
		{
			_baseTime = baseTime;
		}

		public int Compare(DateTime x, DateTime y) 
			=> CompareTime(_baseTime, x,y);
	}
}