using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Widget;
using Android.Views;
using System;
using AView = Android.Views.View;
using AndroidAnimation = Android.Views.Animations.Animation;
using AnimationSet = Android.Views.Animations.AnimationSet;
using Android.Views.Animations;
using Android.Support.V7.Widget;

namespace Xamarin.Forms.Platform.Android
{

	public class ShellContentFragment : Fragment, AndroidAnimation.IAnimationListener, IShellObservableFragment
	{
		private Page _page;
		private IVisualElementRenderer _renderer;
		private AView _root;
		private readonly IShellContext _shellContext;
		private ShellTabItem _shellTabItem;
		private ShellPageContainer _shellPageContainer;
		private IShellToolbarTracker _toolbarTracker;

		public ShellContentFragment(IShellContext shellContext, ShellTabItem shellTabItem)
		{
			_shellContext = shellContext;
			_shellTabItem = shellTabItem;
		}

		public ShellContentFragment(IShellContext shellContext, Page page)
		{
			_shellContext = shellContext;
			_page = page;
		}

		public event EventHandler AnimationFinished;

		public Fragment Fragment => this;


		public override AndroidAnimation OnCreateAnimation(int transit, bool enter, int nextAnim)
		{
			var result = base.OnCreateAnimation(transit, enter, nextAnim);

			if (result == null && nextAnim != 0)
			{
				result = AnimationUtils.LoadAnimation(Context, nextAnim);
			}

			if (result == null)
				return result;

			// This is very strange what we are about to do. For whatever reason if you take this animation
			// and wrap it into an animation set it will have a 1 frame glitch at the start where the
			// fragment shows at the final position. That sucks. So instead we reach into the returned
			// set and hook up to the first item. This means any animation we use depends on the first item
			// finishing at the end of the animation.

			if (result is AnimationSet set)
			{
				set.Animations[0].SetAnimationListener(this);
			}

			return result;
		}

		public override AView OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			if (_shellTabItem != null)
			{
				_page = ((IShellTabItemController)_shellTabItem).GetOrCreateContent();
			}

			_root = inflater.Inflate(Resource.Layout.ShellContent, null).JavaCast<CoordinatorLayout>();

			var scrollview = _root.FindViewById<NestedScrollView>(Resource.Id.shellcontent_scrollview);
			var toolbar = _root.FindViewById<Toolbar>(Resource.Id.shellcontent_toolbar);

			_renderer = Platform.CreateRenderer(_page, Context);
			Platform.SetRenderer(_page, _renderer);

			_shellPageContainer = new ShellPageContainer(Context, _renderer);

			scrollview.AddView(_shellPageContainer);

			_toolbarTracker = _shellContext.CreateTrackerForToolbar(toolbar);
			_toolbarTracker.Page = _page;
			// this is probably not the most ideal way to do that
			_toolbarTracker.CanNavigateBack = _shellTabItem == null;

			return _root;
		}

		// Use OnDestroy instead of OnDestroyView because OnDestroyView will be 
		// called before the animation completes. This causes tons of tiny issues.
		public override void OnDestroy()
		{
			base.OnDestroy();

			_shellPageContainer.RemoveAllViews();
			_renderer?.Dispose();
			_root?.Dispose();
			_toolbarTracker.Dispose();

			if (_shellTabItem != null)
			{
				((IShellTabItemController)_shellTabItem).RecyclePage(_page);
				_page.ClearValue(Platform.RendererProperty);
				_page = null;
			}

			_toolbarTracker = null;
			_root = null;
			_renderer = null;
		}

		void AndroidAnimation.IAnimationListener.OnAnimationEnd(AndroidAnimation animation)
		{
			AnimationFinished?.Invoke(this, EventArgs.Empty);
		}

		void AndroidAnimation.IAnimationListener.OnAnimationRepeat(AndroidAnimation animation)
		{
		}

		void AndroidAnimation.IAnimationListener.OnAnimationStart(AndroidAnimation animation)
		{
		}
	}
}