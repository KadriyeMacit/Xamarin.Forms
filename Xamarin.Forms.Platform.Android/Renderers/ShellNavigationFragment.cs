using Android.OS;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
				bool setup = false;
				if (_currentTabItem != null)
				{
					UnhookTabEvents(_currentTabItem);
					setup = true;
				}
				_currentTabItem = value;
				if (value != null)
				{
					HookTabEvents(value);
					if (setup)
						SwitchFragmentStack(value);
				}
			}
		}

		public override AView OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			HookEvents(_shellItem);

			_navigationTarget = new FrameLayout(Context)
			{
				LayoutParameters = new LP(LP.MatchParent, LP.MatchParent),
				Id = Platform.GenerateViewId()
			};

			_rootFragment = new ShellItemRenderer(_shellContext) { ShellItem = _shellItem };

			SetupFragmentStack(CurrentTabItem);

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

		protected virtual void OnInsertRequested(Page page, Page beforePage)
		{
			var fragment = CreateFragmentForPage(page);
			var stack = CurrentTabItem.Stack;
			int i = 0;
			for (; i < stack.Count; i++)
			{
				if (stack[i] == page)
					break;
			}
			_fragmentStack.Insert(i, fragment.Fragment);
		}

		protected virtual Task<bool> OnPopRequested(bool animated)
		{
			return PopFragment(animated);
		}

		protected virtual Task<bool> OnPopToRootRequested(bool animated)
		{
			// The general idea here is to clear out all the fragments between the current fragment and the
			// root one, and then call OnPopRequested to perform the final pop. Its like doing a remove page on
			// everything in the middle.

			if (_fragmentStack.Count > 2)
			{
				var transaction = ChildFragmentManager.BeginTransaction();
				for (int i = _fragmentStack.Count - 2; i > 0; i--)
				{
					var fragment = _fragmentStack[i];
					transaction.Remove(fragment);
					_fragmentStack.RemoveAt(i);
				}
				transaction.CommitAllowingStateLoss();
			}

			return OnPopRequested(animated);
		}

		protected virtual Task<bool> OnPushRequested(Page page, bool animated)
		{
			var fragment = CreateFragmentForPage(page);
			return PushFragment(fragment.Fragment, animated);
		}

		protected virtual void OnShellItemPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == ShellItem.CurrentItemProperty.PropertyName)
				CurrentTabItem = _shellItem.CurrentItem;
		}

		protected virtual Task<bool> PopFragment(bool animated = true)
		{
			if (_fragmentStack.Count < 2)
				throw new InvalidOperationException("Cannot pop last page");

			var tcs = new TaskCompletionSource<bool>();
			var transaction = ChildFragmentManager.BeginTransaction();
			var fragmentToRemove = _fragmentStack[_fragmentStack.Count - 1];

			if (animated)
			{
				transaction.SetCustomAnimations(R.Animation.FadeIn, R.Animation.FadeOut);
				if (fragmentToRemove is IShellObservableFragment observableFragment)
				{
					void callback(object s, EventArgs e)
					{
						observableFragment.AnimationFinished -= callback;
						tcs.TrySetResult(true);
					}
					observableFragment.AnimationFinished += callback;
				}
				else
				{
					tcs.TrySetResult(true);
				}
			}
			else
			{
				tcs.TrySetResult(true);
			}

			Fragment fragmentToShow = null;

			if (_fragmentStack.Count > 0)
				fragmentToShow = _fragmentStack[_fragmentStack.Count - 2];

			if (fragmentToShow.IsAdded)
			{
				transaction.Remove(fragmentToRemove);
				transaction.Show(fragmentToShow);
			}
			else
			{
				transaction.Replace(_navigationTarget.Id, fragmentToShow);
				transaction.Show(fragmentToShow);
			}

			transaction.CommitAllowingStateLoss();

			_fragmentStack.Remove(fragmentToRemove);

			return tcs.Task;
		}

		protected virtual Task<bool> PushFragment(Fragment fragment, bool animated = true)
		{
			var tcs = new TaskCompletionSource<bool>();
			var transaction = ChildFragmentManager.BeginTransaction();

			if (animated)
			{
				transaction.SetCustomAnimations(R.Animation.FadeIn, R.Animation.FadeOut);
				if (fragment is IShellObservableFragment observableFragment)
				{
					void callback(object s, EventArgs e)
					{
						observableFragment.AnimationFinished -= callback;
						tcs.TrySetResult(true);
					}
					observableFragment.AnimationFinished += callback;
				}
				else
				{
					tcs.TrySetResult(true);
				}
			}
			else
			{
				tcs.TrySetResult(true);
			}

			Fragment currentFragment = null;

			if (_fragmentStack.Count > 0)
				currentFragment = _fragmentStack[_fragmentStack.Count - 1];

			transaction.Add(_navigationTarget.Id, fragment);
			if (currentFragment != null)
				transaction.Hide(currentFragment);
			transaction.CommitAllowingStateLoss();

			_fragmentStack.Add(fragment);

			return tcs.Task;
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

		private void OnNavigationRequested(object sender, Internals.NavigationRequestedEventArgs e)
		{
			switch (e.RequestType)
			{
				case Internals.NavigationRequestType.Push:
					e.Task = OnPushRequested(e.Page, e.Animated);
					break;

				case Internals.NavigationRequestType.Pop:
					e.Task = OnPopRequested(e.Animated);
					break;

				case Internals.NavigationRequestType.PopToRoot:
					e.Task = OnPopToRootRequested(e.Animated);
					break;

				case Internals.NavigationRequestType.Insert:
					OnInsertRequested(e.Page, e.BeforePage);
					break;

				case Internals.NavigationRequestType.Remove:
					break;
			}
		}

		private void SetupFragmentStack(ShellTabItem tabItem, bool pushRoot = true)
		{
			var stack = tabItem.Stack;
			if (stack.Count > 1 || !pushRoot)
			{
				_fragmentStack.Add(_rootFragment);
				for (int i = 1; i < stack.Count; i++)
				{
					var page = stack[i];
					var fragment = CreateFragmentForPage(page);
					if (i == stack.Count - 1)
					{
						// last page actually push it
						PushFragment(fragment.Fragment, false);
					}
					else
					{
						// add to the stack
						_fragmentStack.Add(fragment.Fragment);
					}
				}
			}
			else
			{
				PushFragment(_rootFragment, false);
			}
		}

		private void SwitchFragmentStack(ShellTabItem tabItem)
		{
			var trans = ChildFragmentManager.BeginTransaction();
			for (int i = 1; i < _fragmentStack.Count; i++)
			{
				var frag = _fragmentStack[i];
				trans.Remove(frag);
			}
			trans.Show(_rootFragment);
			trans.CommitAllowingStateLoss();

			_fragmentStack.Clear();
			SetupFragmentStack(tabItem, false);
		}
	}
}