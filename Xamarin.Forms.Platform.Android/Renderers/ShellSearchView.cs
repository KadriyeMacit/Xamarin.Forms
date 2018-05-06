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

		public event EventHandler ClearPlaceholderPressed;

		public event EventHandler ClearPressed;

		public event EventHandler QueryChanged;

		public event EventHandler SearchPressed;

		string IShellSearchView.Placeholder
		{
			get { return _textBlock?.Hint ?? _placeholder; }
			set
			{
				_placeholder = value;
				OnPlaceholderSet(value);
			}
		}

		string IShellSearchView.Query
		{
			get { return _textBlock?.Text ?? _query; }
			set
			{
				_query = value;
				OnQuerySet(value);
			}
		}

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
			LoadView(_searchImage, _clearImage, _clearPlaceholderImage, _query, _placeholder);
		}

		void IShellSearchView.SetClearImage(ImageSource clearImage)
		{
			_clearImage = clearImage;
			if (_clearButton != null)
			{
				SetImage(_clearButton, clearImage, -1);
			}
		}

		void IShellSearchView.SetClearPlaceholderImage(ImageSource clearPlaceholderImage)
		{
			_clearPlaceholderImage = clearPlaceholderImage;
			if (_clearPlaceholderButton != null)
			{
				SetImage(_clearPlaceholderButton, clearPlaceholderImage, -1);
			}
		}

		void IShellSearchView.SetSearchImage(ImageSource searchImage)
		{
			_searchImage = searchImage;
			if (_searchButton != null)
			{
				SetImage(_searchButton, searchImage, -1);
			}
		}

		#endregion IShellSearchView

		private ImageButton _clearButton;
		private ImageSource _clearImage;
		private ImageButton _clearPlaceholderButton;
		private ImageSource _clearPlaceholderImage;
		private string _placeholder;
		private string _query;
		private ImageButton _searchButton;
		private ImageSource _searchImage;
		private EditText _textBlock;

		public ShellSearchView(Context context) : base(context)
		{
		}

		protected async virtual Task<Bitmap> LoadImage(ImageSource source)
		{
			var handler = Registrar.Registered.GetHandlerForObject<IImageSourceHandler>(source);
			return await handler.LoadImageAsync(source, Context);
		}

		protected virtual void LoadView(ImageSource searchImage, ImageSource clearImage, ImageSource clearPlaceholderImage, string query, string placeholder)
		{
			var context = Context;
			var linearLayout = new LinearLayout(context)
			{
				LayoutParameters = new LP(LP.MatchParent, LP.MatchParent),
				Orientation = Orientation.Horizontal
			};

			int padding = (int)context.ToPixels(8);

			var searchButton = _searchButton = new ImageButton(context);
			searchButton.SetPadding(0, 0, 0, 0);
			SetImage(searchButton, searchImage, Resource.Drawable.abc_ic_search_api_material);
			searchButton.LayoutParameters = new LinearLayout.LayoutParams(LP.WrapContent, LP.MatchParent)
			{
				LeftMargin = padding
			};
			searchButton.SetBackground(null);

			var textBlock = _textBlock = new EditText(context);
			textBlock.LayoutParameters = new LinearLayout.LayoutParams(0, LP.MatchParent)
			{
				Gravity = GravityFlags.Fill,
				Weight = 1
			};
			textBlock.SetBackground(null);
			textBlock.SetPadding(padding, 0, padding, 0);
			textBlock.SetSingleLine(true);
			textBlock.ImeOptions = ImeAction.Done;

			textBlock.Text = query;
			textBlock.Hint = placeholder;

			var clearButton = _clearButton = new ImageButton(context);
			clearButton.SetPadding(0, 0, 0, 0);
			SetImage(clearButton, clearImage, Resource.Drawable.abc_ic_clear_material);
			clearButton.LayoutParameters = new LinearLayout.LayoutParams(LP.WrapContent, LP.MatchParent)
			{
				RightMargin = padding
			};
			clearButton.SetBackground(null);

			var clearPlaceholderButton = _clearPlaceholderButton = new ImageButton(context);
			clearPlaceholderButton.SetPadding(0, 0, 0, 0);
			SetImage(clearPlaceholderButton, clearPlaceholderImage, -1);
			clearPlaceholderButton.LayoutParameters = new LinearLayout.LayoutParams(LP.WrapContent, LP.MatchParent)
			{
				RightMargin = padding
			};
			clearPlaceholderButton.SetBackground(null);

			linearLayout.AddView(searchButton);
			linearLayout.AddView(textBlock);
			linearLayout.AddView(clearButton);
			linearLayout.AddView(clearPlaceholderButton);

			UpdateClearButtonState();

			// hook all events down here to avoid getting events while doing setup
			textBlock.AfterTextChanged += TextChanged;
			textBlock.SetOnEditorActionListener(this);
			clearButton.Click += OnClearButtonClicked;
			clearPlaceholderButton.Click += OnClearPlaceholderButtonClicked;
			searchButton.Click += OnSearchButtonClicked;

			AddView(linearLayout);
		}

		bool TextView.IOnEditorActionListener.OnEditorAction(TextView v, ImeAction actionId, KeyEvent e)
		{
			// Fire Completed and dismiss keyboard for hardware / physical keyboards
			if (actionId == ImeAction.Done || (actionId == ImeAction.ImeNull && e.KeyCode == Keycode.Enter && e.Action == KeyEventActions.Up))
			{
				_textBlock.ClearFocus();
				v.HideKeyboard();
				SearchPressed?.Invoke(this, EventArgs.Empty);
			}

			return true;
		}

		protected override async void OnAttachedToWindow()
		{
			base.OnAttachedToWindow();

			// need to wait so keyboard will show
			await Task.Delay(200);

			_textBlock.RequestFocus();
			Context.ShowKeyboard(_textBlock);
		}

		protected virtual void OnSearchButtonClicked(object sender, EventArgs e)
		{
		}

		protected virtual void OnClearButtonClicked(object sender, EventArgs e)
		{
			ClearPressed?.Invoke(this, EventArgs.Empty);
			((IShellSearchView)this).Query = "";
		}

		protected virtual void OnClearPlaceholderButtonClicked(object sender, EventArgs e)
		{
			ClearPlaceholderPressed?.Invoke(this, EventArgs.Empty);
		}

		protected virtual void TextChanged(object sender, global::Android.Text.AfterTextChangedEventArgs e)
		{
			UpdateClearButtonState();

			QueryChanged?.Invoke(this, EventArgs.Empty);
		}

		private void UpdateClearButtonState()
		{
			if (string.IsNullOrEmpty(_textBlock.Text))
			{
				_clearButton.Visibility = ViewStates.Gone;
				if (_clearPlaceholderImage != null)
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
	}
}