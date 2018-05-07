using Android.Content;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using System;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;
using AView = Android.Views.View;
using LP = Android.Views.ViewGroup.LayoutParams;

namespace Xamarin.Forms.Platform.Android
{
	public class ShellSearchView : CardView, IShellSearchView, TextView.IOnEditorActionListener
	{
		#region IShellSearchView

		public event EventHandler SearchConfirmed;

		public SearchHandler SearchHandler { get; set; }

		AView IShellSearchView.View
		{
			get
			{
				if (_searchButton == null)
					throw new InvalidOperationException("LoadView must be called before accessing View");
				return this;
			}
		}

		void IShellSearchView.LoadView()
		{
			LoadView(SearchHandler);
		}

		#endregion IShellSearchView

		private ImageButton _clearButton;
		private ImageButton _clearPlaceholderButton;
		private ImageButton _searchButton;
		private AutoCompleteTextView _textBlock;
		private readonly IShellContext _shellContext;

		public ShellSearchView(Context context, IShellContext shellContext) : base(context)
		{
			_shellContext = shellContext;
		}

		private ISearchHandlerController Controller => SearchHandler;

		bool TextView.IOnEditorActionListener.OnEditorAction(TextView v, ImeAction actionId, KeyEvent e)
		{
			// Fire Completed and dismiss keyboard for hardware / physical keyboards
			if (actionId == ImeAction.Done || (actionId == ImeAction.ImeNull && e.KeyCode == Keycode.Enter && e.Action == KeyEventActions.Up))
			{
				_textBlock.ClearFocus();
				v.HideKeyboard();
				SearchConfirmed?.Invoke(this, EventArgs.Empty);
				Controller.QueryConfirmed();
			}

			return true;
		}

		protected async virtual Task<Bitmap> LoadImage(ImageSource source)
		{
			var handler = Registrar.Registered.GetHandlerForObject<IImageSourceHandler>(source);
			return await handler.LoadImageAsync(source, Context);
		}

		protected virtual void LoadView(SearchHandler searchHandler)
		{
			var searchImage = searchHandler.QueryIcon;
			var clearImage = searchHandler.ClearIcon;
			var clearPlaceholderImage = searchHandler.ClearPlaceholderIcon;
			var query = searchHandler.Query;
			var placeholder = searchHandler.Placeholder;

			var context = Context;
			var linearLayout = new LinearLayout(context)
			{
				LayoutParameters = new LP(LP.MatchParent, LP.MatchParent),
				Orientation = Orientation.Horizontal
			};

			int padding = (int)context.ToPixels(8);

			_searchButton = CreateImageButton(context, searchImage, Resource.Drawable.abc_ic_search_api_material, padding, 0);

			_textBlock = new AutoCompleteTextView(context)
			{
				LayoutParameters = new LinearLayout.LayoutParams(0, LP.MatchParent)
				{
					Gravity = GravityFlags.Fill,
					Weight = 1
				},
				Text = query,
				Hint = placeholder,
				ImeOptions = ImeAction.Done
			};
			_textBlock.SetBackground(null);
			_textBlock.SetPadding(padding, 0, padding, 0);
			_textBlock.SetSingleLine(true);
			_textBlock.Threshold = 1;
			_textBlock.Adapter = new ShellSearchViewAdapter(SearchHandler, _shellContext);
			_textBlock.ItemClick += OnTextBlockItemClicked;

			_clearButton = CreateImageButton(context, clearImage, Resource.Drawable.abc_ic_clear_material, 0, padding);
			_clearPlaceholderButton = CreateImageButton(context, clearPlaceholderImage, -1, 0, padding);

			linearLayout.AddView(_searchButton);
			linearLayout.AddView(_textBlock);
			linearLayout.AddView(_clearButton);
			linearLayout.AddView(_clearPlaceholderButton);

			UpdateClearButtonState();

			// hook all events down here to avoid getting events while doing setup
			_textBlock.AfterTextChanged += TextChanged;
			_textBlock.SetOnEditorActionListener(this);
			_clearButton.Click += OnClearButtonClicked;
			_clearPlaceholderButton.Click += OnClearPlaceholderButtonClicked;
			_searchButton.Click += OnSearchButtonClicked;

			AddView(linearLayout);
		}

		private void OnTextBlockItemClicked(object sender, AdapterView.ItemClickEventArgs e)
		{
			var index = e.Position;
			var item = Controller.ListProxy[index];

			_textBlock.Text = "";
			_textBlock.HideKeyboard();
			SearchConfirmed?.Invoke(this, EventArgs.Empty);
			Controller.ItemSelected(item);
		}

		protected override async void OnAttachedToWindow()
		{
			base.OnAttachedToWindow();

			Alpha = 0;
			Animate().Alpha(1).SetDuration(200).SetListener(null);

			// need to wait so keyboard will show
			await Task.Delay(200);

			_textBlock.RequestFocus();
			Context.ShowKeyboard(_textBlock);
		}

		protected virtual void OnClearButtonClicked(object sender, EventArgs e)
		{
			_textBlock.Text = "";
		}

		protected virtual void OnClearPlaceholderButtonClicked(object sender, EventArgs e)
		{
			Controller.ClearPlaceholderClicked();
		}

		protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
		{
			var width = right - left;
			var height = bottom - top;
			for (int i = 0; i < ChildCount; i++)
			{
				var child = GetChildAt(i);
				child.Measure(MeasureSpecFactory.MakeMeasureSpec(width, MeasureSpecMode.Exactly),
							  MeasureSpecFactory.MakeMeasureSpec(height, MeasureSpecMode.Exactly));
				child.Layout(0, 0, width, height);
			}
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			var measureWidth = MeasureSpecFactory.GetSize(widthMeasureSpec);
			var measureHeight = MeasureSpecFactory.GetSize(heightMeasureSpec);

			SetMeasuredDimension(measureWidth, (int)Context.ToPixels(35));
		}

		protected virtual void OnSearchButtonClicked(object sender, EventArgs e)
		{
		}

		protected virtual void TextChanged(object sender, global::Android.Text.AfterTextChangedEventArgs e)
		{
			var text = _textBlock.Text;

			if (text == ShellSearchViewAdapter.DoNotUpdateMarker)
			{
				return;
			}

			UpdateClearButtonState();

			SearchHandler.SetValueCore(SearchHandler.QueryProperty, text);
			
			if (SearchHandler.ShowsResults)
			{
				if (string.IsNullOrEmpty(text))
				{
					_textBlock.DismissDropDown();
				}
				else
				{
					_textBlock.ShowDropDown();
				}
			}
		}

		private ImageButton CreateImageButton(Context context, ImageSource image, int defaultImage, int leftMargin, int rightMargin)
		{
			var result = new ImageButton(context);
			result.SetPadding(0, 0, 0, 0);
			result.SetFocusable(ViewFocusability.NotFocusable);
			SetImage(result, image, defaultImage);
			result.LayoutParameters = new LinearLayout.LayoutParams(LP.WrapContent, LP.MatchParent)
			{
				LeftMargin = leftMargin,
				RightMargin = rightMargin
			};
			result.SetBackground(null);

			return result;
		}

		private void OnPlaceholderSet(string value)
		{
			if (_textBlock == null)
				return;
			if (_textBlock.Hint != value)
				_textBlock.Hint = value;
		}

		private void OnQuerySet(string value)
		{
			if (_textBlock == null)
				return;
			if (_textBlock.Text != value)
				_textBlock.Text = value;
			UpdateClearButtonState();
		}

		private async void SetImage(ImageButton button, ImageSource image, int defaultValue)
		{
			if (image != null)
				button.SetImageBitmap(await LoadImage(image));
			else if (defaultValue > 0)
				button.SetImageResource(defaultValue);
			else
				button.SetImageDrawable(null);
		}

		private void UpdateClearButtonState()
		{
			if (string.IsNullOrEmpty(_textBlock.Text))
			{
				_clearButton.Visibility = ViewStates.Gone;
				if (SearchHandler.ClearPlaceholderIcon != null)
					_clearPlaceholderButton.Visibility = ViewStates.Visible;
				else
					_clearPlaceholderButton.Visibility = ViewStates.Gone;
			}
			else
			{
				_clearPlaceholderButton.Visibility = ViewStates.Gone;
				_clearButton.Visibility = ViewStates.Visible;
			}
		}
	}
}