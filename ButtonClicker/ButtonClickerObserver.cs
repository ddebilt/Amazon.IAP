using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Com.Amazon.Inapp.Purchasing;
using System.Threading.Tasks;

namespace com.amazon.sample.buttonclicker
{
	public class ButtonClickerObserver : BasePurchasingObserver
	{
		private const string OFFSET = "offset";
		private const string START_DATE = "startDate";
		private const string TAG = "Amazon-IAP";
		private ButtonClickerActivity baseActivity;

		/**
		 * Creates new instance of the ButtonClickerObserver class.
		 * 
		 * @param buttonClickerActivity Activity context
		 */
		public ButtonClickerObserver(ButtonClickerActivity buttonClickerActivity)
			: base(buttonClickerActivity)
		{
			this.baseActivity = buttonClickerActivity;
		}

		/**
		 * Invoked once the observer is registered with the Puchasing Manager If the boolean is false, the application is
		 * receiving responses from the SDK Tester. If the boolean is true, the application is live in production.
		 * 
		 * @param isSandboxMode
		 *            Boolean value that shows if the app is live or not.
		 */

		public override void OnSdkAvailable(bool isSandboxMode)
		{
			Android.Util.Log.Info(TAG, "onSdkAvailable recieved: Response -" + isSandboxMode);
			PurchasingManager.InitiateGetUserIdRequest();
		}

		/**
		 * Invoked once the call from initiateGetUserIdRequest is completed.
		 * On a successful response, a response object is passed which contains the request id, request status, and the
		 * userid generated for your application.
		 * 
		 * @param getUserIdResponse
		 *            Response object containing the UserID
		 */
		public override void OnGetUserIdResponse(GetUserIdResponse getUserIdResponse)
		{
			Android.Util.Log.Info(TAG, "onGetUserIdResponse recieved: Response -" + getUserIdResponse);
			Android.Util.Log.Info(TAG, "RequestId:" + getUserIdResponse.RequestId);
			Android.Util.Log.Info(TAG, "IdRequestStatus:" + getUserIdResponse.UserIdRequestStatus);
			
			var task = Task.Factory.StartNew<bool>(() =>
			{
				if (getUserIdResponse.UserIdRequestStatus == Com.Amazon.Inapp.Purchasing.GetUserIdResponse.GetUserIdRequestStatus.Successful) 
				{					
					// Each UserID has their own shared preferences file, and we'll load that file when a new user logs in.
					baseActivity.SetCurrentUser(getUserIdResponse.UserId);
					return true;
				} 
				
				Android.Util.Log.Info(TAG, "onGetUserIdResponse: Unable to get user ID.");
				return false;

			});
			
			task.ContinueWith(t =>
				{
					if (t.Result)
					{
						PurchasingManager.InitiatePurchaseUpdatesRequest(Offset.FromString(baseActivity.ApplicationContext
							.GetSharedPreferences(baseActivity.GetCurrentUser(), FileCreationMode.Private)
							.GetString(OFFSET, Offset.Beginning.ToString())));
					}
				});
		}

		/**
		 * Invoked once the call from initiateItemDataRequest is completed.
		 * On a successful response, a response object is passed which contains the request id, request status, and a set of
		 * item data for the requested skus. Items that have been suppressed or are unavailable will be returned in a
		 * set of unavailable skus.
		 * 
		 * @param itemDataResponse
		 *            Response object containing a set of purchasable/non-purchasable items
		 */
		public override void OnItemDataResponse(ItemDataResponse itemDataResponse)
		{
			Android.Util.Log.Info(TAG, "onItemDataResponse recieved");
			Android.Util.Log.Info(TAG, "ItemDataRequestStatus" + itemDataResponse.GetItemDataRequestStatus());
			Android.Util.Log.Info(TAG, "ItemDataRequestId" + itemDataResponse.RequestId);


			var task = Task.Factory.StartNew(() =>
			{
				var status = itemDataResponse.GetItemDataRequestStatus();

				if (status.Class.Name == Com.Amazon.Inapp.Purchasing.ItemDataResponse.ItemDataRequestStatus.SuccessfulWithUnavailableSkus.Class.Name)
				{
						// Skus that you can not purchase will be here.
					foreach (string s in itemDataResponse.UnavailableSkus) 
							Android.Util.Log.Info(TAG, "Unavailable SKU:" + s);

					foreach (KeyValuePair<string, Item> item in itemDataResponse.ItemData)
						Android.Util.Log.Info(TAG, String.Format("Item: {0}\n Type: {1}\n SKU: {2}\n Price: {3}\n Description: {4}\n",
							item.Value.Title, item.Value.GetItemType(), item.Value.Sku, item.Value.Price, item.Value.Description));
						
				}
				else if (status.Class.Name == Com.Amazon.Inapp.Purchasing.ItemDataResponse.ItemDataRequestStatus.Successful.Class.Name)
				{
					// Information you'll want to display about your IAP items is here
					// In this example we'll simply log them.						
					foreach (KeyValuePair<string, Item> item in itemDataResponse.ItemData)
						Android.Util.Log.Info(TAG, String.Format("Item: {0}\n Type: {1}\n SKU: {2}\n Price: {3}\n Description: {4}\n", 
							item.Value.Title, item.Value.GetItemType(), item.Value.Sku, item.Value.Price, item.Value.Description));
				}
				else if (status.Class.Name ==  Com.Amazon.Inapp.Purchasing.ItemDataResponse.ItemDataRequestStatus.Failed.Class.Name)
				{
					//fail gracefully
				}
			});
				
		}

		/**
		 * Is invoked once the call from initiatePurchaseRequest is completed.
		 * On a successful response, a response object is passed which contains the request id, request status, and the
		 * receipt of the purchase.
		 * 
		 * @param purchaseResponse
		 *            Response object containing a receipt of a purchase
		 */
		public override void OnPurchaseResponse(PurchaseResponse purchaseResponse)
		{
			Android.Util.Log.Info(TAG, "onPurchaseResponse recieved");
			Android.Util.Log.Info(TAG, "PurchaseRequestStatus:" + purchaseResponse.GetPurchaseRequestStatus());

			var task = Task.Factory.StartNew<bool>(() =>
			{
				if (purchaseResponse.UserId != baseActivity.GetCurrentUser()) 
				{
					// currently logged in user is different than what we have so update the state
					baseActivity.SetCurrentUser(purchaseResponse.UserId);                
					PurchasingManager.InitiatePurchaseUpdatesRequest(Offset.FromString(baseActivity.GetSharedPreferences(baseActivity.GetCurrentUser(), FileCreationMode.Private)
																					   .GetString(OFFSET, Offset.Beginning.ToString())));                
				}

				var settings = GetSharedPreferencesForCurrentUser();
				var editor = GetSharedPreferencesEditor();

				var status = purchaseResponse.GetPurchaseRequestStatus();

				if (status.Class.Name == Com.Amazon.Inapp.Purchasing.PurchaseResponse.PurchaseRequestStatus.Successful.Class.Name)
				{
					/*
					 * You can verify the receipt and fulfill the purchase on successful responses.
					 */
					var receipt = purchaseResponse.Receipt;
					var key = "";
               
					if (receipt.ItemType.Class.Name == Com.Amazon.Inapp.Purchasing.Item.ItemType.Consumable.Class.Name)
					{
						int numClicks = settings.GetInt(ButtonClickerActivity.NUM_CLICKS, 0);
						editor.PutInt(ButtonClickerActivity.NUM_CLICKS, numClicks + 10);
					}
					else if (receipt.ItemType.Class.Name == Com.Amazon.Inapp.Purchasing.Item.ItemType.Entitled.Class.Name)
					{
						key = GetKey(receipt.Sku);
						editor.PutBoolean(key, true);
					}
					else if (receipt.ItemType.Class.Name == Com.Amazon.Inapp.Purchasing.Item.ItemType.Subscription.Class.Name)
					{
						key = GetKey(receipt.Sku);
						editor.PutBoolean(key, true);
						editor.PutLong(START_DATE, DateTime.Now.Ticks);
					}

					editor.Commit();
					PrintReceipt(purchaseResponse.Receipt);
					return true;
				}

				if (status.Class.Name == Com.Amazon.Inapp.Purchasing.PurchaseResponse.PurchaseRequestStatus.AlreadyEntitled.Class.Name)
				{
					/*
					 * If the customer has already been entitled to the item, a receipt is not returned.
					 * Fulfillment is done unconditionally, we determine which item should be fulfilled by matching the
					 * request id returned from the initial request with the request id stored in the response.
					 */
					var requestId = purchaseResponse.RequestId;
					editor.PutBoolean(baseActivity.requestIds[requestId], true);
					editor.Commit();
					return true;
				}

				if (status.Class.Name == Com.Amazon.Inapp.Purchasing.PurchaseResponse.PurchaseRequestStatus.Failed.Class.Name)
				{
					/*
					 * If the purchase failed for some reason, (The customer canceled the order, or some other
					 * extraneous circumstance happens) the application ignores the request and logs the failure.
					 */
					Android.Util.Log.Info(TAG, "Failed purchase for request" + baseActivity.requestIds[purchaseResponse.RequestId]);
					return false;
				}

				if (status.Class.Name == Com.Amazon.Inapp.Purchasing.PurchaseResponse.PurchaseRequestStatus.InvalidSku.Class.Name)
				{
					/*
					 * If the sku that was purchased was invalid, the application ignores the request and logs the failure.
					 * This can happen when there is a sku mismatch between what is sent from the application and what
					 * currently exists on the dev portal.
					 */
					Android.Util.Log.Info(TAG, "Invalid Sku for request " + baseActivity.requestIds[purchaseResponse.RequestId]);
					return false;
				}
                
            return false;

			});

			task.ContinueWith(t =>
			{
				if (t.Result)
				{
					Application.SynchronizationContext.Post(new System.Threading.SendOrPostCallback(_ =>
						{
							baseActivity.Update();
						}), null);
				}
			});
		}

		/**
		 * Is invoked once the call from initiatePurchaseUpdatesRequest is completed.
		 * On a successful response, a response object is passed which contains the request id, request status, a set of
		 * previously purchased receipts, a set of revoked skus, and the next offset if applicable. If a user downloads your
		 * application to another device, this call is used to sync up this device with all the user's purchases.
		 * 
		 * @param purchaseUpdatesResponse
		 *            Response object containing the user's recent purchases.
		 */
		public override void OnPurchaseUpdatesResponse(PurchaseUpdatesResponse purchaseUpdatesResponse)
		{
			Android.Util.Log.Info(TAG, "onPurchaseUpdatesRecived recieved: Response -" + purchaseUpdatesResponse);
			Android.Util.Log.Info(TAG, "PurchaseUpdatesRequestStatus:" + purchaseUpdatesResponse.GetPurchaseUpdatesRequestStatus());
			Android.Util.Log.Info(TAG, "RequestID:" + purchaseUpdatesResponse.RequestId);

			var task = Task.Factory.StartNew<bool>(_ =>
			{
				var editor = GetSharedPreferencesEditor();
				var userId = baseActivity.GetCurrentUser();
           
				if (purchaseUpdatesResponse.UserId != userId) 
					return false;
            
				/*
				 * If the customer for some reason had items revoked, the skus for these items will be contained in the
				 * revoked skus set.
				 */
				foreach (string sku in purchaseUpdatesResponse.RevokedSkus) 
				{
					Android.Util.Log.Info(TAG, "Revoked Sku:" + sku);
					var key = GetKey(sku);
					editor.PutBoolean(key, false);
					editor.Commit();
				}

				var status = purchaseUpdatesResponse.GetPurchaseUpdatesRequestStatus();

				if (status.Class.Name == PurchaseUpdatesResponse.PurchaseUpdatesRequestStatus.Successful.Class.Name)
				{
					SubscriptionPeriod latestSubscriptionPeriod = null;
					LinkedList<SubscriptionPeriod> currentSubscriptionPeriods = new LinkedList<SubscriptionPeriod>();
                
					foreach (Receipt receipt in purchaseUpdatesResponse.Receipts) 
					{
						var sku = receipt.Sku;
						var key = GetKey(sku);
                    
						if (receipt.ItemType.Class.Name == Item.ItemType.Entitled.Class.Name)
						{
							/*
							 * If the receipt is for an entitlement, the customer is re-entitled.
							 */
							editor.PutBoolean(key, true);
							editor.Commit();
						}
						else if (receipt.ItemType.Class.Name == Item.ItemType.Subscription.Class.Name)
						{
							/*
							 * Purchase Updates for subscriptions can be done in one of two ways:
							 * 1. Use the receipts to determine if the user currently has an active subscription
							 * 2. Use the receipts to create a subscription history for your customer.
							 * This application checks if there is an open subscription the application uses the receipts
							 * returned to determine an active subscription.
							 * Applications that unlock content based on past active subscription periods, should create
							 * purchasing history for the customer.
							 * For example, if the customer has a magazine subscription for a year,
							 * even if they do not have a currently active subscription,
							 * they still have access to the magazines from when they were subscribed.
							 */
							var subscriptionPeriod = receipt.SubscriptionPeriod;
							var startDate = subscriptionPeriod.StartDate;
							/*
							 * Keep track of the receipt that has the most current start date.
							 * Store a container of duplicate subscription periods.
							 * If there is a duplicate, the duplicate is added to the list of current subscription periods.
							 */
							if (latestSubscriptionPeriod == null || startDate.After(latestSubscriptionPeriod.StartDate)) 
							{
								currentSubscriptionPeriods.Clear();
								latestSubscriptionPeriod = subscriptionPeriod;
								currentSubscriptionPeriods.AddLast(latestSubscriptionPeriod);
							} 
							else if (startDate.Equals(latestSubscriptionPeriod.StartDate)) 
								currentSubscriptionPeriods.AddLast(receipt.SubscriptionPeriod);

						}

						PrintReceipt(receipt);
                    }

                    
                
					/*
					 * Check the latest subscription periods once all receipts have been read, if there is a subscription
					 * with an existing end date, then the subscription is not active.
					 */
					if (latestSubscriptionPeriod != null) 
					{
						var hasSubscription = true;
						foreach (SubscriptionPeriod subscriptionPeriod in currentSubscriptionPeriods) {
							if (subscriptionPeriod.EndDate != null) 
							{
								hasSubscription = false;
								break;
							}
						}
						editor.PutBoolean(ButtonClickerActivity.HAS_SUBSCRIPTION, hasSubscription);
						editor.Commit();
					}

					/*
					 * Store the offset into shared preferences. If there has been more purchases since the
					 * last time our application updated, another initiatePurchaseUpdatesRequest is called with the new
					 * offset.
					 */
					var newOffset = purchaseUpdatesResponse.Offset;
					editor.PutString(OFFSET, newOffset.ToString());
					editor.Commit();
					if (purchaseUpdatesResponse.IsMore) 
					{
						Android.Util.Log.Info(TAG, "Initiating Another Purchase Updates with offset: " + newOffset.ToString());
						PurchasingManager.InitiatePurchaseUpdatesRequest(newOffset);
					}
					return true;
				}
				else if (status.Class.Name == PurchaseUpdatesResponse.PurchaseUpdatesRequestStatus.Failed.Class.Name)
				{
					/*
					 * On failed responses the application will ignore the request.
					 */
				}
           
				return false;

			}, Application.SynchronizationContext);

			task.ContinueWith(t =>
			{
				if (t.Result)
				{
					Application.SynchronizationContext.Post(new System.Threading.SendOrPostCallback(_ =>
						{
							baseActivity.Update();
						}), null);
				}
			});
		}

		/*
		 * Helper method to print out relevant receipt information to the log.
		 */
		private void PrintReceipt(Receipt receipt)
		{
			Android.Util.Log.Info(TAG, string.Format("Receipt: ItemType: {0} Sku: {1} SubscriptionPeriod: {2}", receipt.ItemType, receipt.Sku, receipt.SubscriptionPeriod));
		}

		/*
		 * Helper method to retrieve the correct key to use with our shared preferences
		 */
		private string GetKey(string sku)
		{
			if (sku.Equals(baseActivity.Resources.GetString(Resource.String.consumable_sku)))
				return ButtonClickerActivity.NUM_CLICKS;
			else if (sku.Equals(baseActivity.Resources.GetString(Resource.String.entitlement_sku_blue)))
				return ButtonClickerActivity.BLUE_BUTTON;
			else if (sku.Equals(baseActivity.Resources.GetString(Resource.String.entitlement_sku_purple)))
				return ButtonClickerActivity.PURPLE_BUTTON;
			else if (sku.Equals(baseActivity.Resources.GetString(Resource.String.entitlement_sku_green)))
				return ButtonClickerActivity.GREEN_BUTTON;
			else if (sku.Equals(baseActivity.Resources.GetString(Resource.String.parent_subscription_sku)) ||
				sku.Equals(baseActivity.Resources.GetString(Resource.String.child_subscription_sku_monthly)))
				return ButtonClickerActivity.HAS_SUBSCRIPTION;
			else
				return "";

		}

		private ISharedPreferences GetSharedPreferencesForCurrentUser()
		{
			return baseActivity.GetSharedPreferences(baseActivity.GetCurrentUser(), FileCreationMode.Private);
		}

		private ISharedPreferencesEditor GetSharedPreferencesEditor()
		{
			return GetSharedPreferencesForCurrentUser().Edit();
		}
	}
}