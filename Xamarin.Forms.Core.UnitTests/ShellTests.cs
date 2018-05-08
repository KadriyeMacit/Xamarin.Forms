using System;
using NUnit.Framework;

namespace Xamarin.Forms.Core.UnitTests
{
	[TestFixture]
	public class ShellTests : BaseTestFixture
	{
		[Test]
		public void DefaultState()
		{
			var shell = new Shell();

			Assert.IsEmpty(shell.Items);
			Assert.IsEmpty(shell.MenuItems);
		}

		[Test]
		public void CurrentItemAutoSets()
		{
			var shell = new Shell();
			var shellItem = new ShellItem();
			var shellTabItem = new ShellTabItem();
			shellItem.Items.Add(shellTabItem);
			shell.Items.Add(shellItem);

			Assert.That(shell.CurrentItem, Is.EqualTo(shellItem));
		}

		[Test]
		public void CurrentItemDoesNotChangeOnSecondAdd()
		{
			var shell = new Shell();
			var shellItem = new ShellItem();
			var shellTabItem = new ShellTabItem();
			shellItem.Items.Add(shellTabItem);
			shell.Items.Add(shellItem);

			Assume.That(shell.CurrentItem, Is.EqualTo(shellItem));

			shell.Items.Add(new ShellItem());

			Assert.AreEqual(shellItem, shell.CurrentItem);
		}

		[Test]
		public void SimpleGoTo()
		{
			var shell = new Shell();
			shell.Route = "s";

			var one = new ShellItem { Route = "one" };
			var two = new ShellItem { Route = "two" };

			var tabone = new ShellTabItem { Route = "tabone" };
			var tabtwo = new ShellTabItem { Route = "tabtwo" };
			var tabthree = new ShellTabItem { Route = "tabthree" };
			var tabfour = new ShellTabItem { Route = "tabfour" };

			one.Items.Add(tabone);
			one.Items.Add(tabtwo);

			two.Items.Add(tabthree);
			two.Items.Add(tabfour);

			shell.Items.Add(one);
			shell.Items.Add(two);

			Assert.That(shell.CurrentState.Location.ToString(), Is.EqualTo("app:///s/one/tabone/"));

			shell.GoToAsync(new ShellNavigationState("app:///s/two/tabfour/"));

			Assert.That(shell.CurrentState.Location.ToString(), Is.EqualTo("app:///s/two/tabfour/"));
		}

		[Test]
		public void CancelNavigation()
		{
			var shell = new Shell();
			shell.Route = "s";

			var one = new ShellItem { Route = "one" };
			var two = new ShellItem { Route = "two" };

			var tabone = new ShellTabItem { Route = "tabone" };
			var tabtwo = new ShellTabItem { Route = "tabtwo" };
			var tabthree = new ShellTabItem { Route = "tabthree" };
			var tabfour = new ShellTabItem { Route = "tabfour" };

			one.Items.Add(tabone);
			one.Items.Add(tabtwo);

			two.Items.Add(tabthree);
			two.Items.Add(tabfour);

			shell.Items.Add(one);
			shell.Items.Add(two);

			Assume.That(shell.CurrentState.Location.ToString(), Is.EqualTo("app:///s/one/tabone/"));

			shell.Navigating += (s, e) =>
			{
				e.Cancel();
			};

			shell.GoToAsync(new ShellNavigationState("app:///s/two/tabfour/"));

			Assert.That(shell.CurrentState.Location.ToString(), Is.EqualTo("app:///s/one/tabone/"));
		}
	}
}
