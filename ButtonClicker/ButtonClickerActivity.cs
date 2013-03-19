/*
 * Button Clicker
 * Sample Implementation of the In-App Purchasing APIs
 * 
 * © 2012, Amazon.com, Inc. or its affiliates.
 * All Rights Reserved.
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 * http://aws.amazon.com/apache2.0/
 * or in the "license" file accompanying this file.
 * This file is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
 * implied.
 * See the License for the specific language governing permissions and limitations under the License.
 */


using Android.App;
using Android.Widget;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using Android.Content;
using Com.Amazon.Inapp.Purchasing;


namespace com.amazon.sample.buttonclicker
{
	public class ButtonClickerActivity : Activity
	{
		// Keys for our shared prefrences
		public const string BLUE_BUTTON = "hasBlueButton";
		public const string PURPLE_BUTTON = "hasPurpleButton";
		public const string GREEN_BUTTON = "hasGreenButton";
		public const string NUM_CLICKS = "numClicks";
		public const string HAS_SUBSCRIPTION = "hasSubscription";

		// UI Elements
		private Button blueSwatch;
		private Button purpleSwatch;
		private Button greenSwatch;
		private Button centerButton;
		private TextView clicksLeft;

		// currently logged in user
		private string m_currentUser;

		// Mapping of our requestIds to unlockable content
		public Dictionary<string, string> requestIds;

		// State of the activity color of the button and the number of clicks left.
		public string buttonColor;
		public int numClicks;

		/**
		 * When the app is first created the views are cached and the requestId mapping is created.
		 */
		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			SetContentView(Resource.Layout.Main);

			requestIds = new Dictionary<string, string>();

			blueSwatch = (Button)FindViewById(Resource.Id.blueswatch);
			blueSwatch.Click += new System.EventHandler(blueSwatch_Click);
			
			purpleSwatch = (Button)FindViewById(Resource.Id.purpleswatch);
			purpleSwatch.Click += new System.EventHandler(purpleSwatch_Click);

			greenSwatch = (Button)FindViewById(Resource.Id.greenswatch);
			greenSwatch.Click += new System.EventHandler(greenSwatch_Click);

			clicksLeft = (TextView)FindViewById(Resource.Id.numClicks);
			centerButton = (Button)FindViewById(Resource.Id.button);
			centerButton.Click += new System.EventHandler(centerButton_Click);

			var redSwatch = (Button)FindViewById(Resource.Id.redswatch);
			redSwatch.Click += new System.EventHandler(redSwatch_Click);

			var buyMore = (Button)FindViewById(Resource.Id.buyClicks);
			buyMore.Click += new System.EventHandler(buyMore_Click);
		}
		
		/**
		 * Whenever the application regains focus, the observer is registered again.
		 */
		protected override void OnStart()
		{
			base.OnStart();
			var buttonClickerObserver = new ButtonClickerObserver(this);
			PurchasingManager.RegisterObserver(buttonClickerObserver);
		}

		/**
		 * When the application resumes the application checks which customer is signed in.
		 */
		protected override void OnResume()
		{
			base.OnResume();
			PurchasingManager.InitiateGetUserIdRequest();
		}

		/**
		 * Update the UI for any purchases the customer has made.
		 */
		public void Update()
		{
			// Display the lock overlay on each swatch unless the customer has purchased it.
			var settings = GetSharedPreferencesForCurrentUser();

			if (settings.GetBoolean(BLUE_BUTTON, false))
				blueSwatch.SetBackgroundDrawable(Resources.GetDrawable(Resource.Drawable.swatchblue));
			else
				blueSwatch.SetBackgroundDrawable(Resources.GetDrawable(Resource.Drawable.swatchbluelocked));


			if (settings.GetBoolean(PURPLE_BUTTON, false))
				purpleSwatch.SetBackgroundDrawable(Resources.GetDrawable(Resource.Drawable.swatchpurple));
			else
				purpleSwatch.SetBackgroundDrawable(Resources.GetDrawable(Resource.Drawable.swatchpurplelocked));


			if (settings.GetBoolean(GREEN_BUTTON, false))
				greenSwatch.SetBackgroundDrawable(Resources.GetDrawable(Resource.Drawable.swatchgreen));
			else
				greenSwatch.SetBackgroundDrawable(Resources.GetDrawable(Resource.Drawable.swatchgreenlocked));


			// Display the number of remaining clicks
			numClicks = settings.GetInt(NUM_CLICKS, 5);
			clicksLeft.SetText(numClicks.ToString(), Android.Widget.TextView.BufferType.Normal);
		}

		/**
		 * Called when the customer presses the "Buy More" button.
		 * 
		 * @param view
		 *            View Object for the Buy More button
		 */
		void buyMore_Click(object sender, System.EventArgs e)
		{
			if (IsSubscribed())
			{
				var requestId = PurchasingManager.InitiatePurchaseRequest(Resources.GetString(Resource.String.consumable_sku));
				StoreRequestId(requestId, NUM_CLICKS);
			}
			else
				GenerateSubscribeDialog();
		}

		/**
		 * Called when the customer presses the red swatch
		 * Since the Red Button is unlocked by default, we simply change the color and update the UI
		 * 
		 * @param view
		 *            View Object for the Red Swatch
		 */
		void redSwatch_Click(object sender, System.EventArgs e)
		{
			if (IsSubscribed())
			{
				centerButton.SetBackgroundDrawable(Resources.GetDrawable(Resource.Drawable.redbutton));
				Update();
			}
			else
				GenerateSubscribeDialog();
		}

		/**
		 * Called when the customer presses the blue swatch
		 * If the customer has not purchased it, then the app will initiate a purchase.
		 * If the customer has purchased the color, the button changes to that color.
		 * 
		 * @param view
		 *            View Object for the Blue Swatch
		 */
		void blueSwatch_Click(object sender, System.EventArgs e)
		{
			if (IsSubscribed())
			{
				var settings = GetSharedPreferencesForCurrentUser();
				var entitled = settings.GetBoolean(BLUE_BUTTON, false);
				if (!entitled)
				{
					var requestId = PurchasingManager.InitiatePurchaseRequest(Resources.GetString(Resource.String.entitlement_sku_blue));
					StoreRequestId(requestId, BLUE_BUTTON);
				}
				else
					centerButton.SetBackgroundDrawable(Resources.GetDrawable(Resource.Drawable.bluebutton));
			}
			else
				GenerateSubscribeDialog();
		}
				

		/**
		 * Called when the customer presses the purple swatch.
		 * If the customer has not purchased it, then the app will initiate a purchase.
		 * If the customer has purchased the color, then the button changes to that color.
		 * 
		 * @param view
		 *            View Object for the Purple Swatch
		 */
		void purpleSwatch_Click(object sender, System.EventArgs e)
		{
			if (IsSubscribed())
			{
				var settings = GetSharedPreferencesForCurrentUser();
				var entitled = settings.GetBoolean(PURPLE_BUTTON, false);
				if (!entitled) 
				{
				    var requestId = PurchasingManager.InitiatePurchaseRequest(Resources.GetString(Resource.String.entitlement_sku_purple));
				    StoreRequestId(requestId, PURPLE_BUTTON);
				    Android.Util.Log.Info("Amazon-IAP",
						string.Format("Sending Request for Sku: {0} Request ID: {1}", Resources.GetString(Resource.String.entitlement_sku_purple), requestId));
				} 
				else 
					centerButton.SetBackgroundDrawable(Resources.GetDrawable(Resource.Drawable.purplebutton));
			}
			else
				GenerateSubscribeDialog();
		}

		/**
		 * Called when the customer presses the green swatch.
		 * If the customer has not purchased it, then the app will initiate a purchase.
		 * If the customer has purchased the color, then the button changes to that color.
		 * 
		 * @param view
		 *            View Object for the Green Swatch
		 */
		void greenSwatch_Click(object sender, System.EventArgs e)
		{
			if (IsSubscribed())
			{
				var settings = GetSharedPreferencesForCurrentUser();
				var entitled = settings.GetBoolean(GREEN_BUTTON, false);
				if (!entitled)
				{
					var requestId = PurchasingManager.InitiatePurchaseRequest(Resources.GetString(Resource.String.entitlement_sku_green));
					StoreRequestId(requestId, GREEN_BUTTON);
				}
				else
					centerButton.SetBackgroundDrawable(Resources.GetDrawable(Resource.Drawable.greenbutton));
			}
			else
				GenerateSubscribeDialog();
		}

		/**
		 * Called when the customer presses the "Click Me" button.
		 * This consumes the number of clicks the customer has by 1.
		 * If the customer no longer has clicks, then a dialog will ask them if they would like to purchase more clicks.
		 * 
		 * @param view
		 *            View Object for the Click Me Button
		 */
		void centerButton_Click(object sender, System.EventArgs e)
		{
			if (IsSubscribed())
			{
				if (numClicks > 0)
				{
					numClicks--;
					var settings = GetSharedPreferencesForCurrentUser();
					var editor = GetSharedPreferencesEditor();
					editor.PutInt(NUM_CLICKS, numClicks);
					editor.Commit();
					Update();
				}
				else
				{
					new DialogCommandWrapper().CreateConfirmationDialog(this, "You don't have any presses left!",
						"Buy More", "Bummer", new System.Threading.Tasks.Task(() =>
						{
							PurchasingManager.InitiatePurchaseRequest(Resources.GetString(Resource.String.consumable_sku));
						})).Show();
				}
			}
			else
				GenerateSubscribeDialog();
		}

		/**
		 * Helper method to associate request ids to shared preference keys
		 * 
		 * @param requestId
		 *            Request ID returned from a Purchasing Manager Request
		 * @param key
		 *            Key used in shared preferrences file
		 */
		private void StoreRequestId(string requestId, string key)
		{
			requestIds[requestId] = key;
		}

		/**
		 * Helper method to check if the customer is subscribed.
		 * 
		 * @return Returns whether or not the customer is subscribed
		 */
		private bool IsSubscribed()
		{
			var settings = GetSharedPreferencesForCurrentUser();
			return settings.GetBoolean(HAS_SUBSCRIPTION, false);
		}

		/**
		 * Helper method to surface a subscribe dialog.
		 */
		private void GenerateSubscribeDialog()
		{
			new DialogCommandWrapper().CreateConfirmationDialog(this, "Subscribe to button clicker to press the button!",
			    "Subscribe", "No Thanks", new System.Threading.Tasks.Task(() =>
					{
						PurchasingManager.InitiatePurchaseRequest(Resources.GetString(Resource.String.child_subscription_sku_monthly));
			        })).Show();
		}

		/**
		 * Get the SharedPreferences file for the current user.
		 * @return SharedPreferences file for a user.
		 */
		ISharedPreferences GetSharedPreferencesForCurrentUser()
		{
			var settings = this.GetSharedPreferences(m_currentUser, Android.Content.FileCreationMode.Private);
			return settings;
		}

		/**
		 * Generate a SharedPreferences.Editor object. 
		 * @return editor for Shared Preferences file.
		 */
		ISharedPreferencesEditor GetSharedPreferencesEditor()
		{
			return GetSharedPreferencesForCurrentUser().Edit();
		}

		/**
		 * Gets current logged in user
		 * @return current user
		 */
		public string GetCurrentUser()
		{
			return m_currentUser;
		}

		/**
		 * Sets current logged in user
		 * @param currentUser current user to set
		 */
		public void SetCurrentUser(string currentUser)
		{
			m_currentUser = currentUser;
		}
	}
}
