using System.Configuration;
using System.Data;
using System.Windows;

namespace Frontend;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	public App()
	{
		this.DispatcherUnhandledException += App_DispatcherUnhandledException;
	}

	private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
	{
		MessageBox.Show($"Unhandled exception: {e.Exception}", "Error");
		e.Handled = true;
	}
}

