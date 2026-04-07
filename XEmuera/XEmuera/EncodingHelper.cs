using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace XEmuera
{
	internal static class EncodingHelper
	{
		public static readonly Encoding Utf8Encoding = new UTF8Encoding(false, true);

		public static readonly Encoding Utf8BomEncoding = new UTF8Encoding(true, true);

		public static readonly Encoding ShiftJisEncoding = Encoding.GetEncoding("SHIFT-JIS");

		public static Encoding DetectEncoding(string filePath)
		{
			try
			{
				using (var stream = File.OpenRead(filePath))
				{
					return DetectEncoding(stream);
				}
			}
			catch
			{
				return ShiftJisEncoding;
			}
		}

		public static Encoding DetectEncoding(Stream stream)
		{
			long originalPosition = stream.CanSeek ? stream.Position : 0;
			try
			{
				if (stream.CanSeek)
					stream.Seek(0, SeekOrigin.Begin);

				byte[] bom = new byte[3];
				int read = stream.Read(bom, 0, bom.Length);
				if (stream.CanSeek)
					stream.Seek(0, SeekOrigin.Begin);

				if (read >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
					return Utf8BomEncoding;

				using (var reader = new StreamReader(stream, Utf8Encoding, true, 1024, true))
				{
					reader.Peek();
					if (!Equals(reader.CurrentEncoding, Utf8Encoding))
					{
						if (stream.CanSeek)
							stream.Seek(0, SeekOrigin.Begin);
						return reader.CurrentEncoding;
					}
					reader.ReadToEnd();
				}

				if (stream.CanSeek)
					stream.Seek(0, SeekOrigin.Begin);
				return Utf8Encoding;
			}
			catch
			{
				if (stream.CanSeek)
					stream.Seek(0, SeekOrigin.Begin);
				return ShiftJisEncoding;
			}
			finally
			{
				if (stream.CanSeek)
					stream.Seek(originalPosition, SeekOrigin.Begin);
			}
		}

		public static string[] ReadAllLines(string filePath)
		{
			return File.ReadAllLines(filePath, DetectEncoding(filePath));
		}

		public static IEnumerable<string> ReadLines(string filePath)
		{
			return File.ReadLines(filePath, DetectEncoding(filePath));
		}

		public static string ReadLineAt(string filePath, int lineNo)
		{
			if (lineNo <= 0)
				return string.Empty;
			return ReadLines(filePath).Skip(lineNo - 1).FirstOrDefault() ?? string.Empty;
		}
	}
}
