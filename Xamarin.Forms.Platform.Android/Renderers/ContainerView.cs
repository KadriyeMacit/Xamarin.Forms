using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using System;

namespace Xamarin.Forms.Platform.Android
{
	public class ContainerView : ViewGroup
	{
		private Context _context;
		private View _view;
		private IVisualElementRenderer _renderer;

		public View View => _view;

		public ContainerView(Context context, View view) : base(context)
		{
			_context = context;
			_view = view;

			_renderer = Platform.CreateRenderer(view, context);
			Platform.SetRenderer(view, _renderer);

			AddView(_renderer.View);
		}

		public ContainerView(Context context, IAttributeSet attribs) : base(context, attribs)
		{
		}

		public ContainerView(Context context, IAttributeSet attribs, int defStyleAttr) : base(context, attribs, defStyleAttr)
		{
		}

		protected ContainerView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
		}

		protected override void OnLayout(bool changed, int l, int t, int r, int b)
		{
			if (_renderer == null)
				return;

			var width = _context.FromPixels(r - l);
			var height = _context.FromPixels(b - t);

			_view.Layout(new Rectangle (0, 0, width, height));
			_renderer.UpdateLayout();
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			_renderer.View.Measure(widthMeasureSpec, heightMeasureSpec);

			var width = MeasureSpecFactory.GetSize(widthMeasureSpec);

			var sizeReq = _view.Measure(_context.FromPixels(width), double.PositiveInfinity);

			SetMeasuredDimension(width, (int)_context.ToPixels(sizeReq.Request.Height));
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				_renderer.Dispose();
				_renderer = null;
				_view = null;
				_context = null;
			}
		}
	}
}