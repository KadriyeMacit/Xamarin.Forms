using System;
using AView = Android.Views.View;

namespace Xamarin.Forms.Platform.Android
{
	public interface IShellSearchView : IDisposable
	{
		AView View { get; }

		string Placeholder { get; set; }

		string Query { get; set; }

		void SetSearchImage(ImageSource searchImage);

		void SetClearImage(ImageSource clearImage);

		void SetClearPlaceholderImage(ImageSource clearPlaceholderImage);

		void LoadView();

		event EventHandler QueryChanged;

		event EventHandler SearchPressed;

		event EventHandler ClearPressed;

		event EventHandler ClearPlaceholderPressed;
	}
}