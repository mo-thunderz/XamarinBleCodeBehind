using Xamarin.Forms;

namespace Jenx.Bluetooth.UartOverGatt.Client
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // The lines below define which page opens when the app is started
            var navigationPage = new NavigationPage(new BtDevPage());
            MainPage = navigationPage;
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}