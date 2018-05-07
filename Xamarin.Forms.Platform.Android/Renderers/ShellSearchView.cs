using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using System;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;
using AColor = Android.Graphics.Color;
using AView = Android.Views.View;
using LP = Android.Views.ViewGroup.LayoutParams;

namespace Xamarin.Forms.Platform.Android
{
	public class ShellSearchView : FrameLayout, IShellSearchView, TextView.IOnEditorActionListener
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

		private readonly IShellContext _shellContext;
		private CardView _cardView;
		private ImageButton _clearButton;
		private ImageButton _clearPlaceholderButton;
		private ImageButton _searchButton;
		private AppCompatAutoCompleteTextView _textBlock;

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
			_cardView = new CardView(context);
			_cardView.LayoutParameters = new LayoutParams(LP.MatchParent, LP.MatchParent);

			var linearLayout = new LinearLayout(context)
			{
				LayoutParameters = new LP(LP.MatchParent, LP.MatchParent),
				Orientation = Orientation.Horizontal
			};

			_cardView.AddView(linearLayout);

			int padding = (int)context.ToPixels(8);

			_searchButton = CreateImageButton(context, searchImage, Resource.Drawable.abc_ic_search_api_material, padding, 0);

			_textBlock = new AppCompatAutoCompleteTextView(context)
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
			_textBlock.SetDropDownBackgroundDrawable(new ClipDrawableWrapper(_textBlock.DropDownBackground));

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

			AddView(_cardView);
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
			width -= (int)Context.ToPixels(25);
			var height = bottom - top;
			for (int i = 0; i < ChildCount; i++)
			{
				var child = GetChildAt(i);
				child.Measure(MeasureSpecFactory.MakeMeasureSpec(width, MeasureSpecMode.Exactly),
							  MeasureSpecFactory.MakeMeasureSpec(height, MeasureSpecMode.Exactly));
				child.Layout(0, 0, width, height);
			}

			_textBlock.DropDownHorizontalOffset = -_textBlock.Left;
			_textBlock.DropDownVerticalOffset = -(int)Math.Ceiling(_cardView.Radius);
			_textBlock.DropDownWidth = width;
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
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

		private void OnTextBlockItemClicked(object sender, AdapterView.ItemClickEventArgs e)
		{
			var index = e.Position;
			var item = Controller.ListProxy[index];

			_textBlock.Text = "";
			_textBlock.HideKeyboard();
			SearchConfirmed?.Invoke(this, EventArgs.Empty);
			Controller.ItemSelected(item);
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

		private class ClipDrawableWrapper : DrawableWrapper
		{
			public ClipDrawableWrapper(Drawable dr) : base(dr)
			{
			}

			public override void Draw(Canvas canvas)
			{
				base.Draw(canvas);

				// Step 1: Clip out the top shadow that was drawn as it wont look right when ligned up
				var paint = new Paint();
				paint.Color = AColor.Black;
				paint.SetXfermode(new PorterDuffXfermode(PorterDuff.Mode.Clear));

				canvas.DrawRect(0, -100, canvas.Width, 0, paint);

				// Step 2: Draw separator line

				paint = new Paint();
				paint.Color = AColor.LightGray;
				canvas.DrawLine(0, 0, canvas.Width, 0, paint);
			}
		}

		private class TestDrawable : Drawable
		{
			public override int Opacity => 0;

			public override void Draw(Canvas canvas)
			{
				var width = Bounds.Width();
				var height = Bounds.Height();
				var paint = new Paint();
				paint.Color = Color.White.ToAndroid();

				canvas.DrawRect(0, 0, width, height, paint);

				paint.Color = AColor.LightGray;
				canvas.DrawLine(0, 0, width, 0, paint);
			}

			public override void SetAlpha(int alpha)
			{
			}

			public override void SetColorFilter(ColorFilter colorFilter)
			{
			}
		}
	}
}