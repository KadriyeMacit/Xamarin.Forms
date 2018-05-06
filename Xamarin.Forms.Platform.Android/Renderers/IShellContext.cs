using Android.Content;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;

namespace Xamarin.Forms.Platform.Android
{
	public interface IShellContext
	{
		Shell Shell { get; }

		Context AndroidContext { get; }

		DrawerLayout CurrentDrawerLayout { get; }

		IShellFlyoutContentRenderer CreateShellFlyoutContentRenderer();

		IShellItemRenderer CreateShellItemRenderer();

		IShellToolbarTracker CreateTrackerForToolbar(Toolbar toolbar);
	}
}