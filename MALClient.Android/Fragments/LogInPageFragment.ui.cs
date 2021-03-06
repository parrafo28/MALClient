using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using GalaSoft.MvvmLight.Helpers;
using Java.Lang;
using MALClient.Android.BindingConverters;
using MALClient.Android.Resources;
using MALClient.Models.Enums;

namespace MALClient.Android.Fragments
{
    public partial class LogInPageFragment : MalFragmentBase
    {



        private EditText _usernameInput;
        private EditText _passwordInput;
        private Button _signInButton;
        private ProgressBar _loginPageLoadingSpinner;
        private Button _loginPageRegisterButton;
        private Button _loginPageLogOutButton;
        private Button _loginPageProblemsButton;
        private FrameLayout _bottomButtonsSection;

        public EditText UsernameInput => _usernameInput ?? (_usernameInput = FindViewById<EditText>(Resource.Id.UsernameInput));

        public EditText PasswordInput => _passwordInput ?? (_passwordInput = FindViewById<EditText>(Resource.Id.PasswordInput));

        public Button SignInButton => _signInButton ?? (_signInButton = FindViewById<Button>(Resource.Id.SignInButton));

        public ProgressBar LoginPageLoadingSpinner => _loginPageLoadingSpinner ?? (_loginPageLoadingSpinner = FindViewById<ProgressBar>(Resource.Id.LoginPageLoadingSpinner));

        public Button LoginPageRegisterButton => _loginPageRegisterButton ?? (_loginPageRegisterButton = FindViewById<Button>(Resource.Id.LoginPageRegisterButton));

        public Button LoginPageLogOutButton => _loginPageLogOutButton ?? (_loginPageLogOutButton = FindViewById<Button>(Resource.Id.LoginPageLogOutButton));

        public Button LoginPageProblemsButton => _loginPageProblemsButton ?? (_loginPageProblemsButton = FindViewById<Button>(Resource.Id.LoginPageProblemsButton));

        public FrameLayout BottomButtonsSection => _bottomButtonsSection ?? (_bottomButtonsSection = FindViewById<FrameLayout>(Resource.Id.BottomButtonsSection));


    }
}