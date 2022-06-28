﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

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
			AllModels.Clear();

			GameItemModel gameItem;

			foreach (var listPath in GameFolderModel.AllModels.Select(item => item.Path))
			{
				if (!Directory.Exists(listPath))
					continue;

				var gameItemPaths = Directory.GetDirectories(listPath);
				if (gameItemPaths.Length == 0)
					continue;

				foreach (var itemPath in gameItemPaths)
				{
					if (!Directory.Exists(itemPath + "/CSV"))
						continue;
					if (!Directory.Exists(itemPath + "/ERB"))
						continue;

					gameItem = new GameItemModel
					{
						Name = System.IO.Path.GetFileName(itemPath),
						Path = itemPath,
					};
					AllModels.Add(gameItem);
				}
			}

			string favoritePaths = GameUtils.GetPreferences(PrefKeyFavoriteItem, null);
			if (!string.IsNullOrEmpty(favoritePaths))
			{
				foreach (var itemPath in favoritePaths.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries))
				{
					gameItem = AllModels.FirstOrDefault(item => item.Path.Equals(itemPath, StringComparison.OrdinalIgnoreCase));
					if (gameItem != null)
						gameItem.Favorite = true;
				}
			}
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