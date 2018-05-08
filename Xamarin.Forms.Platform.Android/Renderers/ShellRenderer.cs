using System;
using System.ComponentModel;
using Android.Content;
using Android.Support.V4.App;
using Android.Support.V4.Widget;
using Android.Views;
using Android.Widget;
using AView = Android.Views.View;
using LP = Android.Views.ViewGroup.LayoutParams;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Xamarin.Forms.Platform.Android
{
	public class ShellRenderer : IVisualElementRenderer, IShellContext
	{
		#region IVisualElementRenderer
		VisualElement IVisualElementRenderer.Element => Element;

		VisualElementTracker IVisualElementRenderer.Tracker => null;

		ViewGroup IVisualElementRenderer.ViewGroup => _flyoutRenderer.AndroidView as ViewGroup;

		AView IVisualElementRenderer.View => _flyoutRenderer.AndroidView;

		event EventHandler<VisualElementChangedEventArgs> IVisualElementRenderer.ElementChanged
		{
			add { _elementChanged += value; }
			remove { _elementChanged -= value; }
		}

		event EventHandler<PropertyChangedEventArgs> IVisualElementRenderer.ElementPropertyChanged
		{
			add { _elementPropertyChanged += value; }
			remove { _elementPropertyChanged -= value; }
		}

		SizeRequest IVisualElementRenderer.GetDesiredSize(int widthConstraint, int heightConstraint)
		{
			return new SizeRequest(new Size(100, 100));
		}

		void IVisualElementRenderer.SetElement(VisualElement element)
		{
			if (Element != null)
				throw new NotSupportedException("Reuse of the Shell Renderer is not supported");
			Element = (Shell)element;
			Element.SizeChanged += OnElementSizeChanged;
			OnElementSet(Element);

			Element.PropertyChanged += OnElementPropertyChanged;
			_elementChanged?.Invoke(this, new VisualElementChangedEventArgs(null, Element));
		}

		void IVisualElementRenderer.SetLabelFor(int? id)
		{
		}

		void IVisualElementRenderer.UpdateLayout()
		{
			_flyoutRenderer.AndroidView.Layout(0, 0,
				(int)AndroidContext.ToPixels(Element.Width), (int)AndroidContext.ToPixels(Element.Height));
		}
		#endregion IVisualElementRenderer

		#region IShellContext

		Shell IShellContext.Shell => Element;

		Context IShellContext.AndroidContext => AndroidContext;

		IShellFlyoutContentRenderer IShellContext.CreateShellFlyoutContentRenderer()
		{
			var content = CreateShellFlyoutContentRenderer();

			content.ElementSelected += OnFlyoutItemSelected;

			return content;
		}

		IShellItemRenderer IShellContext.CreateShellItemRenderer()
		{
			return CreateShellItemRenderer();
		}

		IShellToolbarTracker IShellContext.CreateTrackerForToolbar(Toolbar toolbar)
		{
			return CreateTrackerForToolbar(toolbar);
		}

		// This is very bad, FIXME.
		// This assumes all flyouts will implement via DrawerLayout which is PROBABLY true but
		// I dont want to back us into a corner this time.
		DrawerLayout IShellContext.CurrentDrawerLayout => (DrawerLayout)_flyoutRenderer.AndroidView;

		#endregion IShellContext

		private event EventHandler<PropertyChangedEventArgs> _elementPropertyChanged;
		private event EventHandler<VisualElementChangedEventArgs> _elementChanged;

		private bool _disposed = false;
		private IShellFlyoutRenderer _flyoutRenderer;
		private FrameLayout _frameLayout;

		public ShellRenderer(Context context)
		{
			AndroidContext = context;
		}

		protected Shell Element { get; private set; }

		protected Context AndroidContext { get; }

		private FragmentManager FragmentManager => ((FormsAppCompatActivity)AndroidContext).SupportFragmentManager;

		protected virtual void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == Shell.CurrentItemProperty.PropertyName)
			{
				SwitchFragment(FragmentManager, _frameLayout, Element.CurrentItem);
			}

			_elementPropertyChanged?.Invoke(sender, e);
		}

		protected virtual void OnElementSet (Shell shell)
		{
			_flyoutRenderer = CreateShellFlyoutRenderer();
			_frameLayout = new FrameLayout(AndroidContext)
			{
				LayoutParameters = new LP(LP.MatchParent, LP.MatchParent),
				Id = Platform.GenerateViewId (),
			};
			_frameLayout.SetFitsSystemWindows(true);

			_flyoutRenderer.AttachFlyout(this, _frameLayout);
			_flyoutRenderer.AndroidView.SetBackgroundColor(Color.FromHex("#03A9F4").ToAndroid());

			SwitchFragment(FragmentManager, _frameLayout, shell.CurrentItem, false);
		}

		protected virtual void SwitchFragment (FragmentManager manager, AView targetView, ShellItem newItem, bool animate = true)
		{
			var route = newItem.Route ?? newItem.GetHashCode().ToString();

			var fragment = manager.FindFragmentByTag(route);
			if (fragment == null)
			{
				var shellItemRenderer = CreateShellItemRenderer();
				shellItemRenderer.ShellItem = newItem;
				fragment = shellItemRenderer.Fragment;
			}

			FragmentTransaction transaction = manager.BeginTransaction();

			if (animate)
				transaction.SetTransition((int)global::Android.App.FragmentTransit.FragmentFade);

			transaction.Replace(_frameLayout.Id, fragment);
			transaction.CommitAllowingStateLoss();
		}

		protected virtual IShellFlyoutRenderer CreateShellFlyoutRenderer ()
		{
			return new ShellFlyoutRenderer(this, AndroidContext);
		}

		protected virtual IShellFlyoutContentRenderer CreateShellFlyoutContentRenderer()
		{
			return new ShellFlyoutContentRenderer(this, AndroidContext);
		}

		protected virtual IShellItemRenderer CreateShellItemRenderer()
		{
			return new ShellNavigationFragment(this);
		}

		protected virtual IShellToolbarTracker CreateTrackerForToolbar(Toolbar toolbar)
		{
			return new ShellToolbarTracker(this, toolbar, ((IShellContext)this).CurrentDrawerLayout);
		}

		private async void GoTo(ShellItem item, ShellTabItem tab)
		{
			if (tab == null)
				tab = item.CurrentItem;
			var state = ((IShellController)Element).GetNavigationState(item, tab);
			await Element.GoToAsync(state).ConfigureAwait(false);
		}

		private void OnFlyoutItemSelected(object sender, ElementSelectedEventArgs e)
		{
			var element = e.Element;
			ShellItem shellItem = null;
			ShellTabItem shellTabItem = null;

			if (element is ShellItem.MenuShellItem menuShellItem)
			{
				menuShellItem.MenuItem.Activate();
			}
			else if (element is ShellItem item)
			{
				shellItem = item;
			}
			else if (element is ShellTabItem tab)
			{
				shellItem = tab.Parent as ShellItem;
				shellTabItem = tab;
			}
			else if (element is MenuItem menuItem)
			{
				menuItem.Activate();
			}

			_flyoutRenderer.CloseFlyout();
			if (shellItem != null && shellItem.IsEnabled)
				GoTo(shellItem, shellTabItem);
		}

		private void OnElementSizeChanged(object sender, EventArgs e)
		{
			int width = (int)AndroidContext.ToPixels(Element.Width);
			int height = (int)AndroidContext.ToPixels(Element.Height);
			_flyoutRenderer.AndroidView.Measure(MeasureSpecFactory.MakeMeasureSpec(width, MeasureSpecMode.Exactly), MeasureSpecFactory.MakeMeasureSpec(height, MeasureSpecMode.Exactly));
			_flyoutRenderer.AndroidView.Layout(0, 0, (int)width, (int)height);
		}

		#region IDisposable Support


		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					Element.PropertyChanged -= OnElementPropertyChanged;
					Element.SizeChanged -= OnElementSizeChanged;
				}

				Element = null;
				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				_disposed = true;
			}
		}

		void IDisposable.Dispose()
		{
			Dispose(true);
		}
		#endregion

	}
}