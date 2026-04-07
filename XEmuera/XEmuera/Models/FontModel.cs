using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using XEmuera.Drawing;
using System.Collections.ObjectModel;
using System.ComponentModel;
using XEmuera.Resources;

namespace XEmuera.Models
{
	public class FontModel : INotifyPropertyChanged
	{
		private static readonly string[] ExternalFontExtensions = new[] { "*.ttf", "*.otf" };

		private static readonly string[] ExternalFontDirectoryNames = new[] { "font", "fonts" };

		private static readonly Dictionary<string, string[]> KnownExternalFontAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
		{
			["EraMonoSC"] = new[] { "等距时代黑体 SC", "等距时代黑体SC", "EraMonoSC" },
			["EraPixel"] = new[] { "eraPixel", "EraPixel" },
		};

		public event PropertyChangedEventHandler PropertyChanged;

		private static bool Init;

		public const string PrefKeyEnabledFont = nameof(PrefKeyEnabledFont);

		private const string separator = "_|_";

		private const string DefaultFontName = "MS Gothic";

		private const string FinalFontName = "Microsoft YaHei";

		public static readonly List<FontModel> UserList = new List<FontModel>();

		public static FontGroup EnabledList;

		public static FontGroup DisabledList;

		private static readonly Dictionary<string, FontModel> AllModels = new Dictionary<string, FontModel>(StringComparer.OrdinalIgnoreCase);

		private static readonly Dictionary<string, string> FontAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public static FontModel Default { get; private set; }

		public static FontModel Final { get; private set; }

		private static readonly Mapping<string, string> FontNameMapping = new Mapping<string, string>();

		public string Name { get; private set; }

		public string OtherName { get; private set; }

		public SKTypeface Typeface { get; private set; }

		public bool Enabled
		{
			get { return _enabled; }
			set
			{
				if (_enabled == value && Init)
					return;
				_enabled = value;
				Sort();
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled)));
			}
		}
		bool _enabled;

		public FontModel(SKTypeface typeface)
		{
			Typeface = typeface;
			Name = typeface.FamilyName;
		}

		private void Sort()
		{
			EnabledList.Remove(this);
			DisabledList.Remove(this);

			if (_enabled)
				EnabledList.Add(this);
			else
				DisabledList.Insert(0, this);
		}

		public static void Load()
		{
			Init = false;

			InitFontMapping();

			AddFontFromResource(DefaultFontName);
			AddFontFromResource(FinalFontName);

			Default = AllModels[DefaultFontName];

			LoadFontsFolder(GameFolderModel.Instance.Path);

			EnabledList = new FontGroup
			{
				Name = StringsText.FontReplaceByOrder,
			};
			DisabledList = new FontGroup(AllModels.Values)
			{
				Name = StringsText.FontOnlyCallInGame,
			};

			string enabledFont = GameUtils.GetPreferences(PrefKeyEnabledFont, null);
			if (string.IsNullOrEmpty(enabledFont))
			{
				Default.Enabled = true;
				AllModels[FinalFontName].Enabled = true;
			}
			else
			{
				foreach (var item in enabledFont.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries))
				{
					if (AllModels.TryGetValue(item, out var fontModel))
						fontModel.Enabled = true;
				}
			}

			Save();

			Init = true;
		}

		public static void LoadCurrentGameFonts()
		{
			if (string.IsNullOrWhiteSpace(GameUtils.CurrentGamePath))
				return;

			if (LoadFontsFolder(GameUtils.CurrentGamePath))
				Save();
		}

		public static void Save()
		{
			UserList.Clear();
			foreach (var item in EnabledList)
				UserList.Add(item);
			foreach (var item in DisabledList)
				UserList.Add(item);

			if (EnabledList.Count > 0)
				Final = EnabledList.Last();
			else
				Final = Default;

			GameUtils.SetPreferences(PrefKeyEnabledFont, string.Join(separator, EnabledList.Select(item => item.Name)));

			DrawTextUtils.Load();
		}

		private static bool LoadFontsFolder(string basePath)
		{
			if (string.IsNullOrWhiteSpace(basePath) || !Directory.Exists(basePath))
				return false;

			bool added = false;
			HashSet<string> seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var path in EnumerateFontDirectories(basePath))
			{
				foreach (var pattern in ExternalFontExtensions)
				{
					foreach (var fontFile in FileUtils.GetFiles(path, pattern, SearchOption.AllDirectories))
					{
						if (!seenFiles.Add(fontFile))
							continue;
						added |= LoadExternalFont(fontFile);
					}
				}
			}
			return added;
		}

		private static void InitFontMapping()
		{
			FontNameMapping.Clear();
			FontAliases.Clear();

			AddKnownFontMapping("ＭＳ ゴシック", "MS Gothic");
			AddKnownFontMapping("ＭＳ Ｐゴシック", "MS PGothic");
			AddKnownFontMapping("ＭＳ 明朝", "MS Mincho");
			AddKnownFontMapping("ＭＳ Ｐ明朝", "MS PMincho");
			AddKnownFontMapping("微软雅黑", "Microsoft YaHei");
		}

		private static void AddFontFromResource(string fileName)
		{
			if (AllModels.ContainsKey(fileName))
				return;

			string path = $"XEmuera.Resources.Fonts.{fileName}.ttf";
			SKTypeface sKTypeface = SKTypeface.FromStream(GameUtils.GetManifestResourceStream(path));

			if (sKTypeface == null)
				throw new ArgumentNullException("找不到字体文件。");

			AddFontModel(sKTypeface, fileName);
		}

		private static void AddFontModel(SKTypeface sKTypeface, string fileName)
		{
			var fontModel = new FontModel(sKTypeface);
			AllModels.Add(fontModel.Name, fontModel);

			FontNameMapping.GetByValue(fontModel.Name, out var otherName, fileName);
			fontModel.OtherName = otherName;

			RegisterFontAlias(fontModel.Name, fontModel.Name);
			RegisterFontAlias(fileName, fontModel.Name);
			RegisterFontAlias(StripVersionSuffix(fileName), fontModel.Name);
			RegisterKnownExternalAliases(fileName, fontModel.Name);

			if ((EnabledList != null) && (DisabledList != null))
				DisabledList.Insert(0, fontModel);
		}

		public static bool HasFont(string fontName)
		{
			fontName = ResolveFontName(fontName);
			return AllModels.ContainsKey(fontName);
		}

		public static FontModel GetFont(string fontName)
		{
			fontName = ResolveFontName(fontName);
			if (AllModels.TryGetValue(fontName, out FontModel model))
				return model;
			return Default;
		}

		private static void AddKnownFontMapping(string alias, string fontName)
		{
			FontNameMapping.Add(alias, fontName);
			RegisterFontAlias(alias, fontName);
		}

		private static IEnumerable<string> EnumerateFontDirectories(string basePath)
		{
			HashSet<string> seenDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var directory in Directory.GetDirectories(basePath))
			{
				string name = Path.GetFileName(directory);
				if (!ExternalFontDirectoryNames.Any(item => item.Equals(name, StringComparison.OrdinalIgnoreCase)))
					continue;
				if (seenDirectories.Add(directory))
					yield return directory;
			}
		}

		private static bool LoadExternalFont(string fontFile)
		{
			string fileName = Path.GetFileNameWithoutExtension(fontFile);
			var typeface = SKTypeface.FromFile(fontFile);
			if (typeface == null)
				return false;

			string familyName = typeface.FamilyName;
			if (string.IsNullOrWhiteSpace(familyName))
				familyName = fileName;

			if (AllModels.ContainsKey(familyName))
			{
				RegisterFontAlias(fileName, familyName);
				RegisterFontAlias(StripVersionSuffix(fileName), familyName);
				RegisterKnownExternalAliases(fileName, familyName);
				typeface.Dispose();
				return false;
			}

			AddFontModel(typeface, fileName);
			return true;
		}

		private static void RegisterKnownExternalAliases(string fileName, string canonicalFontName)
		{
			string normalizedFileName = StripVersionSuffix(fileName);
			if (!KnownExternalFontAliases.TryGetValue(normalizedFileName, out var aliases))
				return;

			foreach (var alias in aliases)
				RegisterFontAlias(alias, canonicalFontName);
		}

		private static void RegisterFontAlias(string alias, string canonicalFontName)
		{
			if (string.IsNullOrWhiteSpace(alias) || string.IsNullOrWhiteSpace(canonicalFontName))
				return;

			alias = alias.Trim();
			canonicalFontName = canonicalFontName.Trim();
			if (!FontAliases.ContainsKey(alias))
				FontAliases.Add(alias, canonicalFontName);

			string compactAlias = CompactFontName(alias);
			if (!string.Equals(compactAlias, alias, StringComparison.Ordinal) && !FontAliases.ContainsKey(compactAlias))
				FontAliases.Add(compactAlias, canonicalFontName);
		}

		private static string ResolveFontName(string fontName)
		{
			if (string.IsNullOrWhiteSpace(fontName))
				return fontName;

			fontName = fontName.Trim();
			if (FontAliases.TryGetValue(fontName, out string canonicalFontName))
				return canonicalFontName;

			string compactFontName = CompactFontName(fontName);
			if (!string.Equals(compactFontName, fontName, StringComparison.Ordinal) && FontAliases.TryGetValue(compactFontName, out canonicalFontName))
				return canonicalFontName;

			if (FontNameMapping.GetByKey(fontName, out canonicalFontName))
				return canonicalFontName;

			return fontName;
		}

		private static string CompactFontName(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return value;

			StringBuilder builder = new StringBuilder(value.Length);
			foreach (char c in value)
			{
				if (!char.IsWhiteSpace(c))
					builder.Append(c);
			}
			return builder.ToString();
		}

		private static string StripVersionSuffix(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				return fileName;

			int separatorIndex = fileName.LastIndexOf('-');
			if ((separatorIndex <= 0) || (separatorIndex >= fileName.Length - 1))
				return fileName;

			bool foundDigit = false;
			for (int i = separatorIndex + 1; i < fileName.Length; i++)
			{
				char c = fileName[i];
				if (char.IsDigit(c))
				{
					foundDigit = true;
					continue;
				}
				if ((c == '.') || (c == '_'))
					continue;
				return fileName;
			}
			return foundDigit ? fileName.Substring(0, separatorIndex) : fileName;
		}
	}

	public class FontGroup : ObservableCollection<FontModel>
	{
		public string Name { get; set; }

		public FontGroup() : base() { }

		public FontGroup(IEnumerable<FontModel> models) : base(models) { }
	}
}
