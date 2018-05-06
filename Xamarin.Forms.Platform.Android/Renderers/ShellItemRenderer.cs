using System;
using System.ComponentModel;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.View;
using Android.Support.V4.Widget;
using Android.Views;
using Xamarin.Forms.Platform.Android.AppCompat;
using AView = Android.Views.View;
using LP = Android.Views.ViewGroup.LayoutParams;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using Fragment = Android.Support.V4.App.Fragment;
using ActionBarDrawerToggle = Android.Support.V7.App.ActionBarDrawerToggle;
using System.Linq;

namespace Xamarin.Forms.Platform.Android
{

	public class ShellItemRenderer : Fragment, ViewPager.IOnPageChangeListener, AView.IOnClickListener
	{
		#region IOnPageChangeListener

		void ViewPager.IOnPageChangeListener.OnPageScrollStateChanged(int state)
		{
		}

		void ViewPager.IOnPageChangeListener.OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
		{
		}

		void ViewPager.IOnPageChangeListener.OnPageSelected(int position)
		{
			// TODO : Find a way to make this cancellable
			var shellitem = ShellItem;
			var tab = shellitem.Items[position];
			var stack = tab.Stack.ToList();
			ShellController.ProposeNavigation(ShellNavigationSource.ShellTabItemChanged, shellitem, tab, stack, false);

			ShellItem.SetValueFromRenderer(ShellItem.CurrentItemProperty, tab);

			_toolbarTracker.Page = ((IShellTabItemController)ShellItem.CurrentItem).CurrentPage;
		}

		#endregion IOnPageChangeListener

		#region IOnClickListener

		void AView.IOnClickListener.OnClick(AView v)
		{
		}

		#endregion IOnClickListener

		private Toolbar _toolbar;
		private TabLayout _tablayout;
		private AView _rootView = null;
		private ViewPager _viewPager;
		private IShellContext _shellContext;
		private IShellToolbarTracker _toolbarTracker;

		public ShellItemRenderer(IShellContext shellContext)
		{
			_shellContext = shellContext;
		}

		protected ShellItemRenderer(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
		}

		public ShellItem ShellItem { get; set; }

		private IShellItemController ShellItemController => ShellItem;

		private IShellController ShellController => _shellContext.Shell;

		public override AView OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			var shellItem = ShellItem;
			if (shellItem == null)
				return null;

			HookEvents();
			
			var root = inflater.Inflate(Resource.Layout.RootLayout, null).JavaCast<CoordinatorLayout>();

			_toolbar = root.FindViewById<Toolbar>(Resource.Id.main_toolbar);
			var scrollview = root.FindViewById<NestedScrollView>(Resource.Id.main_scrollview);
			_tablayout = root.FindViewById<TabLayout>(Resource.Id.main_tablayout);

			_viewPager = new FormsViewPager(Context)
			{
				LayoutParameters = new LP(LP.MatchParent, LP.MatchParent),
			};

			_viewPager.AddOnPageChangeListener(this);
			_viewPager.Id = Platform.GenerateViewId();

			var adapter = new ShellFragmentPagerAdapter(shellItem, ChildFragmentManager);

			_viewPager.Adapter = adapter;
			_viewPager.OverScrollMode = OverScrollMode.Never;

			_tablayout.SetupWithViewPager(_viewPager);

			var currentPage = ((IShellTabItemController)shellItem.CurrentItem).GetOrCreateContent();
			var currentIndex = ShellItem.Items.IndexOf(ShellItem.CurrentItem);

			_toolbarTracker = _shellContext.CreateTrackerForToolbar(_toolbar);
			_toolbarTracker.Page = currentPage;

			_viewPager.CurrentItem = currentIndex;
			scrollview.AddView(_viewPager);


			return _rootView = root;
		}

		protected virtual void OnShellAppearanceChanged(object sender, EventArgs e)
		{
			var appearance = ShellItemController.CurrentShellAppearance;
			
			if (appearance != null)
				ApplyAppearance(appearance);
			else
				ResetAppearance();
		}

		protected virtual void ApplyAppearance(ShellAppearance appearance)
		{
			var foreground = appearance.ForegroundColor;
			var background = appearance.BackgroundColor;
			var disabledColor = appearance.DisabledColor; //unused currently
			var titleColor = appearance.TitleColor;
			var unselectedColor = appearance.UnselectedColor;

			var titleArgb = titleColor.ToAndroid(Color.White).ToArgb();
			var unselectedArgb = unselectedColor.ToAndroid(Color.White).ToArgb();
			
			_toolbar.SetTitleTextColor(titleArgb);
			_tablayout.SetTabTextColors(titleArgb, unselectedArgb);
		}

		protected virtual void ResetAppearance()
		{
			// no idea what to do here for some of this shit
		}

		void HookEvents ()
		{
			ShellItemController.CurrentShellAppearanceChanged += OnShellAppearanceChanged;
			ShellItem.PropertyChanged += OnShellItemPropertyChanged;
		}

		void UnhookEvents ()
		{
			ShellItemController.CurrentShellAppearanceChanged -= OnShellAppearanceChanged;
			ShellItem.PropertyChanged -= OnShellItemPropertyChanged;
		}

		// Use OnDestroy instead of OnDestroyView because OnDestroyView will be 
		// called before the animation completes. This causes tons of tiny issues.
		public override void OnDestroy()
		{
			base.OnDestroy();

			if (_rootView != null)
			{
				UnhookEvents();
				_viewPager.RemoveOnPageChangeListener(this);
				_rootView.Dispose();
				_toolbarTracker.Dispose();
			}

			_toolbarTracker = null;
			_toolbar = null;
			_tablayout = null;
			_rootView = null;
			_viewPager = null;
		}

		protected virtual void OnShellItemPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (_rootView == null)
				return;

			if (e.PropertyName == ShellItem.CurrentItemProperty.PropertyName)
			{
				var newIndex = ShellItem.Items.IndexOf(ShellItem.CurrentItem);

				if (newIndex >= 0)
				{
					_viewPager.CurrentItem = newIndex;
				}
			}
		}
	}
}