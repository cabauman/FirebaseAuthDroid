using System;
using Android.App;
using Android.Gms.Tasks;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using Firebase;
using Firebase.Auth;

namespace FirebaseAuthDroid
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        // Update: Works in 70.1602.0-preview1 but not 60.*
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

            // ======================== REPLACE THE VALUES BELOW ==================================
            FirebaseOptions options = new FirebaseOptions
                .Builder()
                .SetApiKey("AIzaSyCpgkUs5ymPgZ9Q7pV51F4o4BjAeGKbxVo")
                .SetApplicationId("1:447724422483:android:87fcb66cc6ca320a")
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
                new PhoneAuthCallbacks(this, _tvStatus, _btnSigninAnonymous, _btnLinkPhone));
        }
    }



    public class PhoneAuthCallbacks : PhoneAuthProvider.OnVerificationStateChangedCallbacks, IOnCompleteListener
    {
        private Activity _activity;
        private TextView _tvStatus;
        private Button _btnSigninAnonymous;
        private Button _btnLinkPhone;

        public PhoneAuthCallbacks(Activity activity, TextView tvStatus, Button btnSigninAnonymous, Button btnLinkPhone)
        {
            _activity = activity;
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
                //FirebaseAuth.Instance.CurrentUser
                //    .LinkWithCredential(credential)
                //    .AddOnCompleteListener(_activity, this);
                result = await FirebaseAuth.Instance.CurrentUser.LinkWithCredentialAsync(credential);
            }
            catch (FirebaseAuthException ex)
            {
                _tvStatus.Text = ex.Message;
                _btnLinkPhone.Enabled = true;
                return;
            }

            _tvStatus.Text = "Account link successful. " + result.User.Uid;
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

        public void OnComplete(Task task)
        {
            if (!task.IsSuccessful)
            {
                _tvStatus.Text = task.Exception.Message;
                Toast.MakeText(_activity, "Authentication failed.", ToastLength.Long).Show();
            }
        }
    }
}
