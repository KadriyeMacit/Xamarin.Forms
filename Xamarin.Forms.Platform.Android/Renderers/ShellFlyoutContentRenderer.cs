using Android.Content;
using Android.Graphics.Drawables;
using Android.Support.Design.Widget;
using Android.Views;
using System;
using System.Collections.Generic;
using static Android.Support.Design.Widget.NavigationView;
using AView = Android.Views.View;

namespace Xamarin.Forms.Platform.Android
{
	public class ShellFlyoutContentRenderer : NavigationView, IShellFlyoutContentRenderer, IOnNavigationItemSelectedListener
	{
		#region IShellFlyoutContentRenderer

		public event EventHandler<ElementSelectedEventArgs> ElementSelected;

		AView IShellFlyoutContentRenderer.AndroidView => this;

		#endregion IShellFlyoutContentRenderer

		private AView _headerView;
		private readonly Dictionary<IMenuItem, Element> _lookupTable = new Dictionary<IMenuItem, Element>();
		private IShellContext _shellContext;
		private bool _disposed;

		public ShellFlyoutContentRenderer(IShellContext shellContext, Context context) : base(context)
		{
			_shellContext = shellContext;

			((IShellController)_shellContext.Shell).StructureChanged += OnShellStructureChanged;

			SetNavigationItemSelectedListener(this);

			BuildMenu();

			_headerView = new ContainerView(context, ((IShellController)shellContext.Shell).FlyoutHeader);

			AddHeaderView(_headerView);
		}

		bool IOnNavigationItemSelectedListener.OnNavigationItemSelected(IMenuItem menuItem)
		{
			if (_lookupTable.TryGetValue(menuItem, out var element))
			{
				ElementSelected?.Invoke(this, new ElementSelectedEventArgs { Element = element });
				return true;
			}
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing && !_disposed)
			{
				_disposed = true;

				((IShellController)_shellContext.Shell).StructureChanged -= OnShellStructureChanged;
				_lookupTable.Clear();
				_headerView.Dispose();
				_headerView = null;
				_shellContext = null;
			}
		}

		private void BuildMenu()
		{
			_lookupTable.Clear();

			var menu = Menu;
			menu.Clear();
			var shell = _shellContext.Shell;

			ShellItemGroupBehavior previous = ShellItemGroupBehavior.HideTabs;
			ISubMenu section = null;
			foreach (var shellItem in shell.Items)
			{
				bool isCurrentShellItem = shell.CurrentItem == shellItem;
				var groupBehavior = shellItem.GroupBehavior;
				if (groupBehavior == ShellItemGroupBehavior.ShowTabs)
				{
					section = menu.AddSubMenu(new Java.Lang.String(shellItem.Title));
					foreach (var tabItem in shellItem.Items)
					{
						var item = section.Add(new Java.Lang.String(tabItem.Title));
						item.SetEnabled(tabItem.IsEnabled);
						if (tabItem.Icon != null)
						{
							SetMenuItemIcon(item, tabItem.Icon);
						}
						item.SetCheckable(true);
						_lookupTable[item] = tabItem;
						// when an item is selected we will display its menu items
						if (isCurrentShellItem && tabItem == shellItem.CurrentItem)
						{
							foreach (var menuItem in tabItem.MenuItems)
							{
								var subItem = section.Add(new Java.Lang.String(menuItem.Text));
								subItem.SetEnabled(menuItem.IsEnabled);
								if (menuItem.Icon != null)
								{
									SetMenuItemIcon(subItem, menuItem.Icon);
								}
								_lookupTable[subItem] = menuItem;
							}
						}
					}
				}
				else
				{
					var subItem = menu.Add(new Java.Lang.String(shellItem.Title));
					subItem.SetEnabled(shellItem.IsEnabled);
					if (shellItem.Icon != null)
					{
						SetMenuItemIcon(subItem, shellItem.Icon);
					}
					subItem.SetCheckable(true);
					_lookupTable[subItem] = shellItem;
				}

				previous = groupBehavior;
			}

			if (shell.MenuItems.Count > 0)
			{
				section = menu.AddSubMenu(null);
				foreach (var menuItem in shell.MenuItems)
				{
					var subItem = section.Add(new Java.Lang.String(menuItem.Text));
					subItem.SetEnabled(menuItem.IsEnabled);
					if (menuItem.Icon != null)
					{
						SetMenuItemIcon(subItem, menuItem.Icon);
					}
					_lookupTable[subItem] = menuItem;
				}
			}
		}

		private void OnShellStructureChanged(object sender, EventArgs e)
		{
			BuildMenu();
		}

		private async void SetMenuItemIcon(IMenuItem menuItem, ImageSource source)
		{
			var drawable = await Context.GetFormsDrawable(source);
			menuItem.SetIcon(drawable);
		}
	}
}