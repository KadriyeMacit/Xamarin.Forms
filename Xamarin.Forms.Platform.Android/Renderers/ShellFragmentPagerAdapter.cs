using Android.Database;
using Android.OS;
using Android.Support.V4.App;
using Java.Lang;
using System.Collections.Specialized;
using Xamarin.Forms.Platform.Android.AppCompat;

namespace Xamarin.Forms.Platform.Android
{
	internal class ShellFragmentPagerAdapter : FragmentPagerAdapter
	{
		private bool _disposed;
		private ShellItem _shellitem;

		public ShellFragmentPagerAdapter(ShellItem shellitem, FragmentManager fragmentManager) : base(fragmentManager)
		{
			_shellitem = shellitem;
			((INotifyCollectionChanged)shellitem.Items).CollectionChanged += OnItemsCollectionChanged;
		}

		protected virtual void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			NotifyDataSetChanged();
		}

		public int CountOverride { get; set; }
		public override int Count => _shellitem.Items.Count;

		public override Fragment GetItem(int position)
		{
			var shellTabItem = _shellitem.Items[position];
			return new ShellFragmentContainer(shellTabItem) { Arguments = new Bundle() };
		}

		public override long GetItemId(int position)
		{
			return _shellitem.Items[position].GetHashCode();
		}

		public override int GetItemPosition(Object objectValue)
		{
			var fragContainer = objectValue as ShellFragmentContainer;
			var shellTabItem = fragContainer?.ShellTabItem;
			if (shellTabItem != null)
			{
				int index = _shellitem.Items.IndexOf(shellTabItem);
				if (index >= 0)
					return index;
			}
			return PositionNone;
		}

		public override ICharSequence GetPageTitleFormatted(int position)
		{
			return new String(_shellitem.Items[position].Title);
		}

		// http://stackoverflow.com/questions/18642890/fragmentstatepageradapter-with-childfragmentmanager-fragmentmanagerimpl-getfra/19099987#19099987
		public override void RestoreState(IParcelable state, ClassLoader loader)
		{
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing && !_disposed)
			{
				((INotifyCollectionChanged)_shellitem.Items).CollectionChanged -= OnItemsCollectionChanged;
				_shellitem = null;
				_disposed = true;

			}
		}
	}
}