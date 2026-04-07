using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using Xamarin.Essentials;

namespace XEmuera.Models
{
	/// <summary>
	/// 游戏目录下的游戏项目
	/// </summary>
	public class GameItemModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public const string PrefKeyFavoriteItem = nameof(PrefKeyFavoriteItem);

		private const string separator = "_|_";

		public static readonly ObservableCollection<GameItemModel> AllModels = new ObservableCollection<GameItemModel>();

		public string Name { get; private set; }
		public string Path { get; private set; }

		public bool Favorite
		{
			get { return _favorite; }
			set
			{
				if (_favorite == value)
					return;
				_favorite = value;
				Sort();
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Favorite)));
			}
		}
		private bool _favorite;

		public bool HasError { get; private set; }

		public string Error { get; private set; }

		private void Sort()
		{
			var list = AllModels.ToList();
			list.Sort(GameItemSorter);

			AllModels.Clear();

			foreach (var item in list)
			{
				AllModels.Add(item);
			}

			SaveFavorite();
		}

		public static void Load()
		{
			string mainPath = GameFolderModel.Instance.Path;
			List<GameItemModel> loadedItems = new List<GameItemModel>();
			if (!Directory.Exists(mainPath))
			{
				ApplyLoadedItems(loadedItems);
				return;
			}

			var gameItemPaths = Directory.GetDirectories(mainPath);
			if (gameItemPaths.Length == 0)
			{
				ApplyLoadedItems(loadedItems);
				return;
			}

			GameItemModel gameItem;
			char directorySeparatorChar = System.IO.Path.DirectorySeparatorChar;

			foreach (var itemPath in gameItemPaths)
			{
				if (!Directory.Exists(itemPath + directorySeparatorChar + "ERB"))
					if (!Directory.Exists(itemPath + directorySeparatorChar + "erb"))
						continue;

				gameItem = new GameItemModel
				{
					Name = System.IO.Path.GetFileName(itemPath),
					Path = itemPath,
				};

				if (!Directory.Exists(itemPath + directorySeparatorChar + "CSV"))
				{
					if (!Directory.Exists(itemPath + directorySeparatorChar + "csv"))
                    {
						gameItem.HasError = true;
						gameItem.Error = "(缺少CSV文件夹)";
					}
				}

				loadedItems.Add(gameItem);
			}

			string favoritePaths = GameUtils.GetPreferences(PrefKeyFavoriteItem, null);
			if (!string.IsNullOrEmpty(favoritePaths))
			{
				HashSet<string> favorites = new HashSet<string>(
					favoritePaths.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries),
					StringComparer.OrdinalIgnoreCase);

				foreach (var item in loadedItems)
				{
					if (favorites.Contains(item.Path))
						item._favorite = true;
				}
			}

			loadedItems.Sort(GameItemSorter);

			ApplyLoadedItems(loadedItems);
		}

		private static void ApplyLoadedItems(List<GameItemModel> loadedItems)
		{
			if (MainThread.IsMainThread)
			{
				ReplaceAllModels(loadedItems);
			}
			else
			{
				MainThread.InvokeOnMainThreadAsync(() => ReplaceAllModels(loadedItems)).GetAwaiter().GetResult();
			}
		}

		private static void ReplaceAllModels(List<GameItemModel> loadedItems)
		{
			AllModels.Clear();
			foreach (var item in loadedItems)
				AllModels.Add(item);
		}

		private static int GameItemSorter(GameItemModel a, GameItemModel b)
		{
			if (a.Favorite == b.Favorite)
				return a.Path == b.Path ? 0 : a.Path.CompareTo(b.Path);
			return -(a.Favorite.CompareTo(b.Favorite));
		}

		public static void SaveFavorite()
		{
			if (AllModels.Count == 0)
				return;

			var list = AllModels.Where(item => item.Favorite).Select(item => item.Path);
			GameUtils.SetPreferences(PrefKeyFavoriteItem, string.Join(separator, list));
		}
	}
}
