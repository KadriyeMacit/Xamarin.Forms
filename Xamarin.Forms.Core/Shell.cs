using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;

namespace Xamarin.Forms
{
	[ContentProperty("Items")]
	public class Shell : Page, IShellController
	{
		#region Attached Properties

		public static readonly BindableProperty BackButtonBehaviorProperty =
			BindableProperty.CreateAttached("BackButtonBehavior", typeof(BackButtonBehavior), typeof(Shell), null, BindingMode.OneTime);

		public static readonly BindableProperty FlyoutBehaviorProperty =
			BindableProperty.CreateAttached("FlyoutBehavior", typeof(FlyoutBehavior), typeof(Shell), FlyoutBehavior.Popover);

		public static readonly BindableProperty SearchHandlerProperty =
			BindableProperty.CreateAttached("SearchHandler", typeof(SearchHandler), typeof(Shell), null, BindingMode.OneTime);

		public static readonly BindableProperty SetPaddingInsetsProperty =
			BindableProperty.CreateAttached("SetPaddingInsets", typeof(bool), typeof(Shell), false);

		public static BackButtonBehavior GetBackButtonBehavior(BindableObject obj)
		{
			return (BackButtonBehavior)obj.GetValue(BackButtonBehaviorProperty);
		}

		public static FlyoutBehavior GetFlyoutBehavior(BindableObject obj)
		{
			return (FlyoutBehavior)obj.GetValue(FlyoutBehaviorProperty);
		}

		public static SearchHandler GetSearchHandler(BindableObject obj)
		{
			return (SearchHandler)obj.GetValue(SearchHandlerProperty);
		}

		public static bool GetSetPaddingInsets(BindableObject obj)
		{
			return (bool)obj.GetValue(SetPaddingInsetsProperty);
		}

		public static void SetBackButtonBehavior(BindableObject obj, BackButtonBehavior behavior)
		{
			obj.SetValue(BackButtonBehaviorProperty, behavior);
		}

		public static void SetFlyoutBehavior(BindableObject obj, FlyoutBehavior value)
		{
			obj.SetValue(FlyoutBehaviorProperty, value);
		}

		public static void SetSearchHandler(BindableObject obj, SearchHandler handler)
		{
			obj.SetValue(SearchHandlerProperty, handler);
		}

		public static void SetSetPaddingInsets(BindableObject obj, bool value)
		{
			obj.SetValue(SetPaddingInsetsProperty, value);
		}

		#endregion Attached Properties

		#region PropertyKeys

		private static readonly BindablePropertyKey CurrentStatePropertyKey =
			BindableProperty.CreateReadOnly(nameof(CurrentState), typeof(ShellNavigationState), typeof(Shell), null);

		private static readonly BindablePropertyKey ItemsPropertyKey = BindableProperty.CreateReadOnly(nameof(Items), typeof(ShellItemCollection), typeof(Shell), null,
				defaultValueCreator: bo => new ShellItemCollection { Inner = new ElementCollection<ShellItem>(((Shell)bo).InternalChildren) });

		private static readonly BindablePropertyKey MenuItemsPropertyKey =
			BindableProperty.CreateReadOnly(nameof(MenuItems), typeof(MenuItemCollection), typeof(Shell), null, defaultValueCreator: bo => new MenuItemCollection());

		private static readonly BindablePropertyKey ShellNavigationStatePropertyKey =
			BindableProperty.CreateReadOnly(nameof(ShellNavigationState), typeof(ShellNavigationState), typeof(Shell), null);

		#endregion PropertyKeys

		#region IShellController

		event EventHandler IShellController.HeaderChanged
		{
			add { _headerChanged += value; }
			remove { _headerChanged -= value; }
		}

		event EventHandler IShellController.StructureChanged
		{
			add { _structureChanged += value; }
			remove { _structureChanged -= value; }
		}

		private event EventHandler _headerChanged;

		private event EventHandler _structureChanged;

		View IShellController.FlyoutHeader
		{
			get { return FlyoutHeaderView; }
		}

		ShellNavigationState IShellController.GetNavigationState(ShellItem item, ShellTabItem tab)
		{
			return GetNavigationState(item, tab, tab.Stack.ToList());
		}

		bool IShellController.ProposeNavigation(ShellNavigationSource source, ShellItem item, ShellTabItem tab, IList<Page> stack, bool canCancel)
		{
			var proposedState = GetNavigationState(item, tab, stack);
			return ProposeNavigation(source, proposedState, canCancel);
		}

		void IShellController.UpdateCurrentState(ShellNavigationSource source)
		{
			var oldState = CurrentState;
			var shellItem = CurrentItem;
			var tab = shellItem?.CurrentItem;
			var stack = tab?.Stack;
			var result = GetNavigationState(shellItem, tab, stack.ToList());

			SetValueFromRenderer(CurrentStatePropertyKey, result);

			OnNavigated(new ShellNavigatedEventArgs(oldState, CurrentState, source));
		}

		#endregion IShellController

		public static readonly BindableProperty CurrentItemProperty =
			BindableProperty.Create(nameof(CurrentItem), typeof(ShellItem), typeof(Shell), null, BindingMode.TwoWay,
				propertyChanged: OnCurrentItemChanged);

		public static readonly BindableProperty CurrentStateProperty = CurrentStatePropertyKey.BindableProperty;

		public static readonly BindableProperty FlyoutBackgroundColorProperty =
				BindableProperty.Create(nameof(FlyoutBackgroundColor), typeof(Color), typeof(Shell), Color.Default, BindingMode.OneTime);

		public static readonly BindableProperty FlyoutHeaderBehaviorProperty =
			BindableProperty.Create(nameof(FlyoutHeaderBehavior), typeof(FlyoutHeaderBehavior), typeof(Shell), FlyoutHeaderBehavior.Default, BindingMode.OneTime);

		public static readonly BindableProperty FlyoutHeaderProperty =
			BindableProperty.Create(nameof(FlyoutHeader), typeof(object), typeof(Shell), null, BindingMode.OneTime,
				propertyChanged: OnFlyoutHeaderChanged);

		public static readonly BindableProperty FlyoutHeaderTemplateProperty =
			BindableProperty.Create(nameof(FlyoutHeaderTemplate), typeof(DataTemplate), typeof(Shell), null, BindingMode.OneTime,
				propertyChanged: OnFlyoutHeaderTemplateChanged);

		public static readonly BindableProperty FlyoutIsPresentedProperty = BindableProperty.Create(nameof(FlyoutIsPresented), typeof(bool), typeof(Shell), false, BindingMode.TwoWay);

		public static readonly BindableProperty GroupHeaderTemplateProperty =
			BindableProperty.Create(nameof(GroupHeaderTemplate), typeof(DataTemplate), typeof(Shell), null, BindingMode.OneTime);

		public static readonly BindableProperty ItemsProperty = ItemsPropertyKey.BindableProperty;

		public static readonly BindableProperty ItemTemplateProperty =
			BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(Shell), null, BindingMode.OneTime);

		public static readonly BindableProperty MenuItemsProperty = MenuItemsPropertyKey.BindableProperty;

		public static readonly BindableProperty MenuItemTemplateProperty =
			BindableProperty.Create(nameof(MenuItemTemplate), typeof(DataTemplate), typeof(Shell), null, BindingMode.OneTime);

		public static readonly BindableProperty ShellNavigationStateProperty = ShellNavigationStatePropertyKey.BindableProperty;

		private ShellNavigatedEventArgs _accumulatedEvent;
		private bool _accumulateNavigatedEvents;
		private View _flyoutHeaderView;

		public event EventHandler<ShellNavigatedEventArgs> Navigated;

		public event EventHandler<ShellNavigatingEventArgs> Navigating;

		public Shell()
		{
			((INotifyCollectionChanged)Items).CollectionChanged += (s, e) => SendStructureChanged();
		}

		public ShellItem CurrentItem
		{
			get { return (ShellItem)GetValue(CurrentItemProperty); }
			set { SetValue(CurrentItemProperty, value); }
		}

		public ShellNavigationState CurrentState => (ShellNavigationState)GetValue(CurrentStateProperty);

		public Color FlyoutBackgroundColor
		{
			get { return (Color)GetValue(FlyoutBackgroundColorProperty); }
			set { SetValue(FlyoutBackgroundColorProperty, value); }
		}

		public FlyoutBehavior FlyoutBehavior
		{
			get { return (FlyoutBehavior)GetValue(FlyoutBehaviorProperty); }
			set { SetValue(FlyoutBehaviorProperty, value); }
		}

		public object FlyoutHeader
		{
			get { return GetValue(FlyoutHeaderProperty); }
			set { SetValue(FlyoutHeaderProperty, value); }
		}

		public FlyoutHeaderBehavior FlyoutHeaderBehavior
		{
			get { return (FlyoutHeaderBehavior)GetValue(FlyoutHeaderBehaviorProperty); }
			set { SetValue(FlyoutHeaderBehaviorProperty, value); }
		}

		public DataTemplate FlyoutHeaderTemplate
		{
			get { return (DataTemplate)GetValue(FlyoutHeaderTemplateProperty); }
			set { SetValue(FlyoutHeaderTemplateProperty, value); }
		}

		public bool FlyoutIsPresented
		{
			get { return (bool)GetValue(FlyoutIsPresentedProperty); }
			set { SetValue(FlyoutIsPresentedProperty, value); }
		}

		public DataTemplate GroupHeaderTemplate
		{
			get { return (DataTemplate)GetValue(GroupHeaderTemplateProperty); }
			set { SetValue(GroupHeaderTemplateProperty, value); }
		}

		public ShellItemCollection Items => (ShellItemCollection)GetValue(ItemsProperty);

		public DataTemplate ItemTemplate
		{
			get { return (DataTemplate)GetValue(ItemTemplateProperty); }
			set { SetValue(ItemTemplateProperty, value); }
		}

		public MenuItemCollection MenuItems => (MenuItemCollection)GetValue(MenuItemsProperty);

		public DataTemplate MenuItemTemplate
		{
			get { return (DataTemplate)GetValue(MenuItemTemplateProperty); }
			set { SetValue(MenuItemTemplateProperty, value); }
		}

		public string Route
		{
			get { return Routing.GetRoute(this); }
			set { Routing.SetRoute(this, value); }
		}

		public string RouteHost { get; set; }

		public string RouteScheme { get; set; } = "app";

		public ShellNavigationState ShellNavigationState => (ShellNavigationState)GetValue(ShellNavigationStateProperty);

		private View FlyoutHeaderView
		{
			get { return _flyoutHeaderView; }
			set
			{
				if (_flyoutHeaderView == value)
					return;

				if (_flyoutHeaderView != null)
					OnChildRemoved(_flyoutHeaderView);
				_flyoutHeaderView = value;
				if (_flyoutHeaderView != null)
					OnChildAdded(_flyoutHeaderView);
				_headerChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public bool AddToBackStack(ShellNavigationState state)
		{
			throw new NotImplementedException();
		}

		public async Task GoToAsync(ShellNavigationState state, bool animate = true)
		{
			// FIXME: This should not be none, we need to compute the delta and set flags correctly
			var accept = ProposeNavigation(ShellNavigationSource.None, state, true);
			if (!accept)
				return;

			_accumulateNavigatedEvents = true;

			var uri = state.Location;
			var queryString = uri.Query;
			var queryData = ParseQueryString(queryString);
			var path = uri.AbsolutePath;

			var parts = path.Substring(1).Split('/');

			if (path.Length < 2)
				throw new InvalidOperationException("Path must be at least 2 items long in Shell navigation");

			var shellRoute = parts[0];
			var shellItemRoute = parts[1];
			var shellTabItemRoute = parts.Length > 2 ? parts[2] : null;

			if (Routing.GetRoute(this) != shellRoute)
				throw new NotImplementedException();

			ApplyQueryAttributes(this, queryData, false);

			// Find ShellItem
			foreach (var shellItem in Items)
			{
				if (Routing.GetRoute(shellItem) == shellItemRoute)
				{
					ApplyQueryAttributes(shellItem, queryData, parts.Length == 2);
					if (CurrentItem != shellItem)
						SetValueFromRenderer(CurrentItemProperty, shellItem);
					break;
				}
			}

			// Find ShellTabItem
			ShellTabItem selectedTabItem = null;
			if (shellTabItemRoute != null)
			{
				var shellItem = CurrentItem;
				foreach (var tabItem in shellItem.Items)
				{
					if (Routing.GetRoute(tabItem) == shellTabItemRoute)
					{
						ApplyQueryAttributes(tabItem, queryData, parts.Length == 3);
						if (shellItem.CurrentItem != tabItem)
							shellItem.SetValueFromRenderer(ShellItem.CurrentItemProperty, tabItem);
						selectedTabItem = tabItem;
						break;
					}
				}
			}

			// Send out navigation to ShellTabItem
			if (parts.Length > 3 && selectedTabItem != null)
			{
				List<string> navParts = new List<string>();
				for (int i = 3; i < parts.Length; i++)
				{
					navParts.Add(parts[i]);
				}
				await selectedTabItem.GoToAsync(navParts, queryData);
			}

			_accumulateNavigatedEvents = false;
			OnNavigated(_accumulatedEvent);
		}

		internal static void ApplyQueryAttributes(Element element, IDictionary<string, string> query, bool isLastItem)
		{
			if (query.Count == 0)
				return;

			string prefix = "";
			if (!isLastItem)
			{
				var route = Routing.GetRoute(element);
				if (string.IsNullOrEmpty(route))
					return;
				prefix = route + ".";
			}

			var typeInfo = element.GetType().GetTypeInfo();
			object[] effectAttributes = typeInfo.GetCustomAttributes(typeof(QueryPropertyAttribute), true).ToArray();

			if (effectAttributes.Length == 0)
				return;

			foreach (var a in effectAttributes)
			{
				if (a is QueryPropertyAttribute attrib)
				{
					if (query.TryGetValue(prefix + attrib.QueryId, out var value))
					{
						PropertyInfo prop = null;

						while (prop == null && typeInfo != null)
						{
							prop = typeInfo.GetDeclaredProperty(attrib.Name);
							typeInfo = typeInfo.BaseType.GetTypeInfo();
						}

						if (prop != null && prop.CanWrite && prop.SetMethod.IsPublic)
						{
							prop.SetValue(element, value);
						}
					}
				}
			}
		}

		internal void SendStructureChanged ()
		{
			_structureChanged?.Invoke(this, EventArgs.Empty);
		}

		protected override void OnChildAdded(Element child)
		{
			base.OnChildAdded(child);

			if (child is ShellItem shellItem && CurrentItem == null && !(child is ShellItem.MenuShellItem))
			{
				SetValueFromRenderer(CurrentItemProperty, shellItem);
			}
		}

		protected override void OnChildRemoved(Element child)
		{
			base.OnChildRemoved(child);

			if (child == CurrentItem)
			{
				if (Items.Count > 0)
					SetValueFromRenderer(CurrentItemProperty, Items[0]);
			}
		}

		protected virtual void OnNavigated(ShellNavigatedEventArgs args)
		{
			if (_accumulateNavigatedEvents)
			{
				_accumulatedEvent = args;
			}
			else
			{
				Navigated?.Invoke(this, args);
				System.Diagnostics.Debug.WriteLine("Navigated: " + args.Current.Location);
			}
		}

		protected virtual void OnNavigating(ShellNavigatingEventArgs args)
		{
			Navigating?.Invoke(this, args);
		}

		private static string GenerateQueryString(Dictionary<string, string> queryData)
		{
			if (queryData.Count == 0)
				return string.Empty;
			string result = "?";

			bool addAnd = false;
			foreach (var kvp in queryData)
			{
				if (addAnd)
					result += "&";
				result += kvp.Key + "=" + kvp.Value;
				addAnd = true;
			}

			return result;
		}

		private static void GetQueryStringData(Element element, bool isLastItem, Dictionary<string, string> result)
		{
			string prefix = string.Empty;
			if (!isLastItem)
			{
				var route = Routing.GetRoute(element);
				if (string.IsNullOrEmpty(route))
					return;
				prefix = route + ".";
			}

			var typeInfo = element.GetType().GetTypeInfo();
			object[] effectAttributes = typeInfo.GetCustomAttributes(typeof(QueryPropertyAttribute), true).ToArray();

			if (effectAttributes.Length == 0)
				return;

			foreach (var a in effectAttributes)
			{
				if (a is QueryPropertyAttribute attrib)
				{
					PropertyInfo prop = null;

					while (prop == null && typeInfo != null)
					{
						prop = typeInfo.GetDeclaredProperty(attrib.Name);
						typeInfo = typeInfo.BaseType.GetTypeInfo();
					}

					if (prop != null && prop.CanRead && prop.GetMethod.IsPublic)
					{
						var val = (string)prop.GetValue(element);
						var key = isLastItem ? prefix + attrib.QueryId : attrib.QueryId;
						result[key] = val;
					}
				}
			}
		}

		private static void OnCurrentItemChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var shell = (Shell)bindable;
			((IShellController)shell).UpdateCurrentState(ShellNavigationSource.ShellItemChanged);
		}

		private static void OnFlyoutHeaderChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var shell = (Shell)bindable;
			shell.OnFlyoutHeaderChanged(oldValue, newValue);
		}

		private static void OnFlyoutHeaderTemplateChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var shell = (Shell)bindable;
			shell.OnFlyoutHeaderTemplateChanged((DataTemplate)oldValue, (DataTemplate)newValue);
		}

		private static Dictionary<string, string> ParseQueryString(string query)
		{
			if (query.StartsWith("?"))
				query = query.Substring(1);
			Dictionary<string, string> lookupDict = new Dictionary<string, string>();
			if (query == null)
				return lookupDict;
			var parts = query.Split('&');
			foreach (var part in parts)
			{
				var p = part.Split('=');
				if (p.Length != 2)
					continue;
				lookupDict[p[0]] = p[1];
			}

			return lookupDict;
		}

		private ShellNavigationState GetNavigationState(ShellItem item, ShellTabItem tab, IList<Page> stack)
		{
			var state = RouteScheme + "://" + RouteHost + "/" + Route + "/";
			Dictionary<string, string> queryData = new Dictionary<string, string>();

			GetQueryStringData(this, false, queryData);

			if (item != null)
			{
				state += Routing.GetRouteStringForElement(item);
				state += "/";

				GetQueryStringData(item, tab == null, queryData);

				if (tab != null)
				{
					state += Routing.GetRouteStringForElement(tab);
					state += "/";

					GetQueryStringData(tab, stack == null || stack.Count <= 1, queryData);

					if (stack != null)
					{
						for (int i = 1; i < stack.Count; i++)
						{
							var page = stack[i];
							state += Routing.GetRouteStringForElement(page);
							GetQueryStringData(page, i == stack.Count - 1, queryData);
							if (i < stack.Count - 1)
								state += "/";
						}
					}
				}
			}

			var queryString = GenerateQueryString(queryData);

			return state + queryString;
		}

		private void OnFlyoutHeaderChanged(object oldVal, object newVal)
		{
			if (FlyoutHeaderTemplate == null)
			{
				if (newVal is View newFlyoutHeader)
					FlyoutHeaderView = newFlyoutHeader;
				else
					FlyoutHeaderView = null;
			}
			else
			{
				FlyoutHeaderView.BindingContext = newVal;
			}
		}

		private void OnFlyoutHeaderTemplateChanged(DataTemplate oldValue, DataTemplate newValue)
		{
			if (newValue == null)
			{
				if (FlyoutHeader is View flyoutHeaderView)
					FlyoutHeaderView = flyoutHeaderView;
				else
					FlyoutHeaderView = null;
			}
			else
			{
				var newHeaderView = (View)newValue.CreateContent(FlyoutHeader, this);
				newHeaderView.BindingContext = FlyoutHeader;
				FlyoutHeaderView = newHeaderView;
			}
		}

		private bool ProposeNavigation(ShellNavigationSource source, ShellNavigationState proposedState, bool canCancel)
		{
			if (_accumulateNavigatedEvents)
				return true;

			var navArgs = new ShellNavigatingEventArgs(CurrentState, proposedState, source, canCancel);

			OnNavigating(navArgs);
			System.Diagnostics.Debug.WriteLine("Proposed: " + proposedState.Location);
			return !navArgs.Cancelled;
		}
	}
}