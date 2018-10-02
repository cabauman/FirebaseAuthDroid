using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Firebase;
using Firebase.Auth;

namespace FirebaseAuthDroid
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        // Bug: Attempting to link a phone number that's already linked to another account doesn't throw an exception.
        //      It just hangs indefinitely.
        // Expected behavior: Throw a FirebaseAuthUserCollisionException

        // Setup is required in your Firebase console.
        // 1. Set your API key and application ID in the OnCreate method.
        // 2. Enable phone and anonymous authentication.
        // 3. Add a test phone number and sms code (matching the variables below)
        public static readonly string PhoneNumber = "+1 650-555-4321";
        public static readonly string SmsCode = "654321";

        public TextView _tvStatus;
        public Button _btnSigninAnonymous;
        public Button _btnLinkPhone;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            // ======================== REPLACE THE VALUES BELOW ==================================
            FirebaseOptions options = new FirebaseOptions
                .Builder()
                .SetApiKey("<API_KEY>")
                .SetApplicationId("<APPLICATION_ID>")
                .Build();
            // ====================================================================================

            FirebaseApp.InitializeApp(this, options);

            _tvStatus = FindViewById<TextView>(Resource.Id.tv_status);
            _btnSigninAnonymous = FindViewById<Button>(Resource.Id.btn_signin_anonymous);
            _btnLinkPhone = FindViewById<Button>(Resource.Id.btn_link_phone);

            _btnSigninAnonymous.Click += _btnSigninAnonymous_Click;
            _btnLinkPhone.Click += _btnLinkPhone_Click;
        }

        private async void _btnSigninAnonymous_Click(object sender, EventArgs e)
        {
            _tvStatus.Text = "Signing in...";
            _btnSigninAnonymous.Enabled = false;

            IAuthResult result = null;
            try
            {
                result = await FirebaseAuth.Instance.SignInAnonymouslyAsync();
            }
            catch (FirebaseAuthException ex)
            {
                _tvStatus.Text = ex.Message;
                _btnSigninAnonymous.Enabled = true;
                return;
            }

            _tvStatus.Text = string.Format("Sign in successful. uid: {0}", result.User.Uid);
            _btnSigninAnonymous.Visibility = ViewStates.Gone;
            _btnLinkPhone.Visibility = ViewStates.Visible;
            _btnLinkPhone.Enabled = true;
        }

        private void _btnLinkPhone_Click(object sender, EventArgs e)
        {
            _tvStatus.Text = "Verifying phone number...";
            _btnLinkPhone.Enabled = false;

            PhoneAuthProvider.Instance.VerifyPhoneNumber(
                PhoneNumber,
                60,
                Java.Util.Concurrent.TimeUnit.Seconds,
                this,
                new PhoneAuthCallbacks(_tvStatus, _btnSigninAnonymous, _btnLinkPhone));
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            View view = (View) sender;
            Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
                .SetAction("Action", (Android.Views.View.IOnClickListener)null).Show();
        }
    }



    public class PhoneAuthCallbacks : PhoneAuthProvider.OnVerificationStateChangedCallbacks
    {
        private TextView _tvStatus;
        private Button _btnSigninAnonymous;
        private Button _btnLinkPhone;

        public PhoneAuthCallbacks(TextView tvStatus, Button btnSigninAnonymous, Button btnLinkPhone)
        {
            _tvStatus = tvStatus;
            _btnSigninAnonymous = btnSigninAnonymous;
            _btnLinkPhone = btnLinkPhone;
        }

        public override async void OnCodeSent(string verificationId, PhoneAuthProvider.ForceResendingToken forceResendingToken)
        {
            _tvStatus.Text = "Sms code received. Now linking...";
            var credential = PhoneAuthProvider.GetCredential(verificationId, MainActivity.SmsCode);
            IAuthResult result = null;
            try
            {
                result = await FirebaseAuth.Instance.CurrentUser.LinkWithCredentialAsync(credential);
            }
            catch (FirebaseAuthException ex)
            {
                _tvStatus.Text = ex.Message;
                _btnLinkPhone.Enabled = true;
                return;
            }

            _tvStatus.Text = "Account link successful.";
            _btnLinkPhone.Visibility = ViewStates.Gone;
            _btnSigninAnonymous.Visibility = ViewStates.Visible;
            _btnSigninAnonymous.Enabled = true;
        }

        public override void OnVerificationCompleted(PhoneAuthCredential credential)
        {
            _tvStatus.Text = "Verification completed.";
        }

        public override void OnVerificationFailed(FirebaseException exception)
        {
            _tvStatus.Text = exception.Message;
        }
    }
}
