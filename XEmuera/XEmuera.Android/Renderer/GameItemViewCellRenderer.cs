using Android.Content;
using Android.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using XEmuera.Droid.Renderer;
using XEmuera.Views;

[assembly: ExportRenderer(typeof(GameItemViewCell), typeof(GameItemViewCellRenderer))]
namespace XEmuera.Droid.Renderer
{
	public class GameItemViewCellRenderer : ViewCellRenderer
	{
		public GameItemViewCellRenderer(Context context) : base(context)
		{
		}

		protected override View GetCellCore(Cell item, View convertView, ViewGroup parent, Context context)
		{
			var view = base.GetCellCore(item, convertView, parent, context);
			if (view != null && item is GameItemViewCell gameItemCell)
			{
				view.LongClickable = true;
				view.SetOnLongClickListener(new GameItemLongClickListener(gameItemCell));
			}

			return view;
		}

		private sealed class GameItemLongClickListener : Java.Lang.Object, View.IOnLongClickListener
		{
			private readonly GameItemViewCell cell;

			public GameItemLongClickListener(GameItemViewCell cell)
			{
				this.cell = cell;
			}

			public bool OnLongClick(View v)
			{
				Device.BeginInvokeOnMainThread(cell.RaiseLongPressed);
				return true;
			}
		}
	}
}
