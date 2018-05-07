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
using R = Android.Resource;
using System.Linq;
using Android.Util;
using Android.Graphics.Drawables;
using Android.Graphics;
using Android.Content.Res;

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

			var appearance = ShellItemController.CurrentShellAppearance;
			if (appearance != null)
				ApplyAppearance(appearance);

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

		private Color _defaultBackgroundColor = Color.FromRgb(33, 150, 243);
		private Color _defaultForegroundColor = Color.White;
		private Color _defaultTitleColor = Color.White;
		private Color _defaultUnselectedColor = Color.FromRgba(255, 255, 255, 180);

		protected virtual void ApplyAppearance(ShellAppearance appearance)
		{
			var foreground = appearance.ForegroundColor;
			var background = appearance.BackgroundColor;
			var titleColor = appearance.TitleColor;
			var unselectedColor = appearance.UnselectedColor;

			SetColors(foreground, background, titleColor, unselectedColor);
		}

		protected virtual void ResetAppearance()
		{
			SetColors(_defaultForegroundColor, _defaultBackgroundColor, _defaultTitleColor, _defaultUnselectedColor);
		}

		private void SetColors(Color foreground, Color background, Color title, Color unselected)
		{
			var titleArgb = title.ToAndroid(_defaultTitleColor).ToArgb();
			var unselectedArgb = unselected.ToAndroid(_defaultUnselectedColor).ToArgb();

			_toolbar.SetTitleTextColor(titleArgb);
			_tablayout.SetTabTextColors(unselectedArgb, titleArgb);

			_toolbar.SetBackground(new ColorDrawable (background.ToAndroid(_defaultBackgroundColor)));
			_tablayout.SetBackground(new ColorDrawable (background.ToAndroid(_defaultBackgroundColor)));

			_toolbarTracker.TintColor = foreground.IsDefault ? _defaultForegroundColor : foreground;
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