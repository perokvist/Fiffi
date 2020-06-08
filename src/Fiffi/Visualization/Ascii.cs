using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fiffi.Visualization
{
	public class AsciiTable
	{
		public List<AsciiColumn> Columns { get; set; } = new List<AsciiColumn>();
		public List<List<string>> Rows { get; set; } = new List<List<string>>();

		public override string ToString()
		{
			var s = new StringBuilder();
			var startSeparator =
				"\u250F" + string.Join("\u2533", Columns.Select(c => new string('\u2501', c.Width + 2))) + "\u2513";
			var middleSeparator =
				"\u2523" + string.Join("\u254B", Columns.Select(c => new string('\u2501', c.Width + 2))) + "\u252B";
			var endSeparator =
				"\u2517" + string.Join("\u253B", Columns.Select(c => new string('\u2501', c.Width + 2))) + "\u251B";

			s.AppendLine(startSeparator);
			s.AppendLine(Row(Columns.Select(c => (c.Header, c.Width, false))));
			s.AppendLine(middleSeparator);
			foreach (var row in Rows)
			{
				var columns = Columns.Zip(row, (column, value) => (value, column.Width, column.PadLeft));
				s.AppendLine(Row(columns));
			}

			s.AppendLine(endSeparator);
			return s.ToString();
		}

		private static string Row(IEnumerable<(string value, int width, bool padLeft)> columns)
		{
			return
				"\u2503" +
				string.Join("\u2503", columns.Select(c => " " + Pad(c.value, c.width, c.padLeft) + " ")) +
				"\u2503";

			string Pad(string value, int width, bool padLeft)
			{
				return value.Length > width
					? value.Substring(0, width)
					: padLeft ? value.PadLeft(width) : value.PadRight(width);
			}
		}
	}

	public class AsciiColumn
	{
		public AsciiColumn(string header, int width, bool padLeft = false)
		{
			Header = header;
			Width = width;
			PadLeft = padLeft;
		}

		public string Header { get; }
		public int Width { get; }
		public bool PadLeft { get; }
	}
}
