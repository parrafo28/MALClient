﻿using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using MalClient.Shared.Models.MalSpecific;
using MalClient.Shared.NavArgs;
using MalClient.Shared.ViewModels.Main;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MALClient.Pages.Messages
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MalMessageDetailsPage : Page
    {
        private MalMessageDetailsNavArgs _lastArgs;

        public MalMessageDetailsPage()
        {
            InitializeComponent();
            Loaded += (sender, args) => ViewModel.Init(_lastArgs);
        }

        private MalMessageDetailsViewModel ViewModel => DataContext as MalMessageDetailsViewModel;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _lastArgs = e.Parameter as MalMessageDetailsNavArgs;
            base.OnNavigatedTo(e);
        }
    }
}