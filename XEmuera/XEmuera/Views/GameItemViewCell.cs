using System;
using Xamarin.Forms;

namespace XEmuera.Views
{
	public class GameItemViewCell : ViewCell
	{
		public event EventHandler TappedCell;
		public event EventHandler LongPressed;

		public void RaiseTappedCell()
		{
			TappedCell?.Invoke(this, EventArgs.Empty);
		}

		public void RaiseLongPressed()
		{
			LongPressed?.Invoke(this, EventArgs.Empty);
		}
	}
}
