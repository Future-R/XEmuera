using Android.Content;
using Android.Views;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using XEmuera.Droid.Renderer;
	using XEmuera.Views;
	using AView = Android.Views.View;

[assembly: ExportRenderer(typeof(GameItemViewCell), typeof(GameItemViewCellRenderer))]
	namespace XEmuera.Droid.Renderer
	{
		public class GameItemViewCellRenderer : ViewCellRenderer
		{
		protected override AView GetCellCore(Cell item, AView convertView, ViewGroup parent, Context context)
		{
			var view = base.GetCellCore(item, convertView, parent, context);
			if (view != null && item is GameItemViewCell gameItemCell)
			{
				view.Clickable = true;
				view.LongClickable = true;
				view.SetOnClickListener(new GameItemClickListener(gameItemCell));
				view.SetOnLongClickListener(new GameItemLongClickListener(gameItemCell));
			}

			return view;
		}

		private sealed class GameItemClickListener : Java.Lang.Object, AView.IOnClickListener
		{
			private readonly GameItemViewCell cell;

			public GameItemClickListener(GameItemViewCell cell)
			{
				this.cell = cell;
			}

			public void OnClick(AView v)
			{
				if (GameItemLongClickListener.ShouldSuppressTap())
					return;
				Device.BeginInvokeOnMainThread(cell.RaiseTappedCell);
			}
		}

		private sealed class GameItemLongClickListener : Java.Lang.Object, AView.IOnLongClickListener
		{
			private readonly GameItemViewCell cell;
			private static DateTime suppressTapUntilUtc;

			public GameItemLongClickListener(GameItemViewCell cell)
			{
				this.cell = cell;
			}

			public static bool ShouldSuppressTap()
			{
				return DateTime.UtcNow <= suppressTapUntilUtc;
			}

			public bool OnLongClick(AView v)
			{
				suppressTapUntilUtc = DateTime.UtcNow.AddMilliseconds(500);
				Device.BeginInvokeOnMainThread(cell.RaiseLongPressed);
				return true;
			}
		}
	}
}
