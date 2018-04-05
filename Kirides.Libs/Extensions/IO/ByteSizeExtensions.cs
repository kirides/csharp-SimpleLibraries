using System;

namespace Kirides.Libs.Extensions.IO
{
	public static class ByteSizeExtensions
	{
		public static readonly string[] Units = { "B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB", "ZiB", "YiB" };
		public static readonly string[] SiUnits = { "B", "kB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
		public static readonly int KibiByte = 1024;
		public static readonly int KiloByte = 1000;
		static string _formatString = "{0}{1:0.##} {2}";

		/// <param name="si">Gibt an ob binär oder dezimal potenzen verwendet werden sollen 
		/// <para />true: 1kB (1000 Bytes), false(default): 1KiB (1024 Bytes)</param>
		public static string Humanize(this double bytes, bool si = false)
		{
			if (bytes == 0) { return string.Format(_formatString, null, bytes, Units[0]); }
			
			var kb = si ? KiloByte : KibiByte;
			var units = si ? SiUnits : Units;
			
			double absVal = Math.Abs(bytes);
			int iUnit = (int)Math.Log(absVal, kb);
			iUnit = iUnit >= Units.Length ? Units.Length - 1 : iUnit;
			double value = absVal / Math.Pow(kb, iUnit);

			return string.Format(_formatString, bytes < 0 ? "-" : null, value, units[iUnit]);
		}
		
		/// <param name="si">Gibt an ob binär oder dezimal potenzen verwendet werden sollen 
		/// <para />true: 1kB (1000 Bytes), false(default): 1KiB (1024 Bytes)</param>
		public static string Humanize(this long bytes, bool si = false)
			=> Humanize((double)bytes, si);
			
		/// <param name="si">Gibt an ob binär oder dezimal potenzen verwendet werden sollen 
		/// <para />true: 1kB (1000 Bytes), false(default): 1KiB (1024 Bytes)</param>
		public static string Humanize(this int bytes, bool si = false)
			=> Humanize((double)bytes, si);
			
		/// <param name="si">Gibt an ob binär oder dezimal potenzen verwendet werden sollen 
		/// <para />true: 1kB (1000 Bytes), false(default): 1KiB (1024 Bytes)</param>
		public static string Humanize(this float bytes, bool si = false)
			=> Humanize((double)bytes, si);
	}
}