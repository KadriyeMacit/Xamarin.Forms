using Android.OS;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;
using AView = Android.Views.View;
using LP = Android.Views.ViewGroup.LayoutParams;
using R = Android.Resource;

namespace Xamarin.Forms.Platform.Android
{
	public class ShellNavigationFragment : Fragment, IShellItemRenderer
	{
		#region IShellItemRenderer

		Fragment IShellItemRenderer.Fragment => this;

		ShellItem IShellItemRenderer.ShellItem
		{
			get { return _shellItem; }
			set { _shellItem = value; }
		}

		#endregion IShellItemRenderer

		private readonly IShellContext _shellContext;
		private ShellTabItem _currentTabItem;
		private List<Fragment> _fragmentStack = new List<Fragment>();
		private FrameLayout _navigationTarget;
		private ShellItemRenderer _rootFragment;
		private ShellItem _shellItem;

		public ShellNavigationFragment(IShellContext shellContext)
		{
			_shellContext = shellContext;
		}

		public ShellTabItem CurrentTabItem
		{
			get { return _currentTabItem; }
			set
			{
				if (_currentTabItem != null)
				{
					UnhookTabEvents(_currentTabItem);
				}
				_currentTabItem = value;
				if (value != null)
				{
					HookTabEvents(value);
					EnsureNavigationState(NavigationRequestType.Push, null, false);
				}
			}
		}

		public override AView OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{

			_navigationTarget = new FrameLayout(Context)
			{
				LayoutParameters = new LP(LP.MatchParent, LP.MatchParent),
				Id = Platform.GenerateViewId()
			};
			_navigationTarget.SetBackgroundColor(global::Android.Graphics.Color.Black);

			HookEvents(_shellItem);

			return _navigationTarget;
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
			UnhookEvents(_shellItem);

			if (_navigationTarget != null)
			{
				_navigationTarget.Dispose();
			}

			_navigationTarget = null;
			CurrentTabItem = null;
		}

		protected virtual IShellObservableFragment CreateFragmentForPage(Page page)
		{
			return new ShellContentFragment(_shellContext, page);
		}

		protected virtual void HookEvents(ShellItem shellItem)
		{
			_shellItem.PropertyChanged += OnShellItemPropertyChanged;
			CurrentTabItem = shellItem.CurrentItem;
		}

		protected virtual void HookTabEvents(ShellTabItem shellTabItem)
		{
			((IShellTabItemController)shellTabItem).NavigationRequested += OnNavigationRequested;
		}

		protected virtual void OnShellItemPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == ShellItem.CurrentItemProperty.PropertyName)
				CurrentTabItem = _shellItem.CurrentItem;
		}

		protected virtual void UnhookEvents(ShellItem shellItem)
		{
			_shellItem.PropertyChanged -= OnShellItemPropertyChanged;
			CurrentTabItem = null;
		}

		protected virtual void UnhookTabEvents(ShellTabItem shellTabItem)
		{
			((IShellTabItemController)shellTabItem).NavigationRequested -= OnNavigationRequested;
		}

		private void OnNavigationRequested(object sender, NavigationRequestedEventArgs e)
		{
			EnsureNavigationState(e.RequestType, e.Page, e.Animated);
		}

		private Dictionary<Page, IShellObservableFragment> _fragmentMap = new Dictionary<Page, IShellObservableFragment>();
		private ShellTabItem _lastEnsuredItem;
		private Fragment _currentFragment;

		private Task<bool> EnsureNavigationState(NavigationRequestType navType, Page page, bool animated)
		{
			TaskCompletionSource<bool> result = new TaskCompletionSource<bool>();

			var stack = CurrentTabItem.Stack;

			if (_rootFragment == null)
			{
				_rootFragment = new ShellItemRenderer(_shellContext) { ShellItem = _shellItem };
			}

			if (_currentFragment == _rootFragment && stack.Count == 1)
			{
				result.TrySetResult(true);
				return result.Task;
			}

			if (_lastEnsuredItem != CurrentTabItem)
			{
				// we switched tabs, we need to clear out the old fragments
				_fragmentMap.Clear();

				foreach (var p in stack)
				{
					if (p == null)
						continue;

					var fragment = CreateFragmentForPage(p);
					_fragmentMap[p] = fragment;
				}

				_lastEnsuredItem = CurrentTabItem;
			}
			else
			{
				switch (navType)
				{
					case NavigationRequestType.Push:
					case NavigationRequestType.Insert:
						if (page != null)
							_fragmentMap[page] = CreateFragmentForPage(page);
						break;
					case NavigationRequestType.Pop:
					case NavigationRequestType.Remove:
						_fragmentMap.Remove(page);
						break;
					case NavigationRequestType.PopToRoot:
						_fragmentMap.Clear();
						break;
				}
			}

			Fragment targetFragment = null;

			if (stack.Count == 1 || navType == NavigationRequestType.PopToRoot)
			{
				targetFragment = _rootFragment;
			}
			else
			{
				var lastPage = stack[stack.Count - 1];
				targetFragment = _fragmentMap[lastPage].Fragment;
			}

			if (targetFragment == _currentFragment)
			{
				result.TrySetResult(true);
				return result.Task;
			}

			var t = ChildFragmentManager.BeginTransaction();

			if (animated)
			{
				if (navType == NavigationRequestType.Push)
					t.SetCustomAnimations(Resource.Animation.EnterFromRight, Resource.Animation.ExitToLeft);
				else
					t.SetCustomAnimations(Resource.Animation.EnterFromLeft, Resource.Animation.ExitToRight);
			}

			Fragment animationTrackFragment = null;
			switch (navType)
			{
				case NavigationRequestType.Push:
					animationTrackFragment = targetFragment;

					if (_currentFragment != null)
						t.Hide(_currentFragment);
						
					t.Add(_navigationTarget.Id, targetFragment);
					t.Show(targetFragment);

					break;

				case NavigationRequestType.Pop:
				case NavigationRequestType.PopToRoot:
					animationTrackFragment = _currentFragment;

					if (_currentFragment != null)
						t.Remove(_currentFragment);

					if (!targetFragment.IsAdded)
						t.Add(_navigationTarget.Id, targetFragment);
					t.Show(targetFragment);

					break;
			}

			if (animated)
			{
				if (animationTrackFragment is IShellObservableFragment observableFragment)
				{
					void callback(object s, EventArgs e)
					{
						observableFragment.AnimationFinished -= callback;
						result.TrySetResult(true);
					}
					observableFragment.AnimationFinished += callback;
				}
				else
				{
					result.TrySetResult(true);
				}
			}
			else
			{
				result.TrySetResult(true);
			}


			t.CommitAllowingStateLoss();

			_currentFragment = targetFragment;

			return result.Task;
		}
	}
}