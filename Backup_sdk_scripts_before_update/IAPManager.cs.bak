using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class IAPManager : MonoBehaviour, IStoreListener, IDetailedStoreListener
{
    public static IAPManager instan;
    // Items list, configurable via inspector
    //[HideInInspector]
    //public List<CatalogItem> Catalog;

    // The Unity Purchasing system
    private static IStoreController m_StoreController;
    IExtensionProvider _mStoreExtensionProvider;

    private void Awake()
    {
        if (instan == null)
            instan = this;
        else
            Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }
    // Bootstrap the whole thing
    public void Start()
    {
        // Make PlayFab log in
        //Login();
        //InitializePurchasing();
    }

    //public void OnGUI()
    //{
    //    // This line just scales the UI up for high-res devices
    //    // Comment it out if you find the UI too large.
    //    GUI.matrix = Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.identity, new Vector3(3, 3, 3));

    //    // if we are not initialized, only draw a message
    //    if (!IsInitialized)
    //    {
    //        GUILayout.Label("Initializing IAP and logging in...");
    //        return;
    //    }

    //    // Draw menu to purchase items
    //    foreach (var item in Catalog)
    //    {
    //        if (GUILayout.Button("Buy " + item.DisplayName))
    //        {
    //            // On button click buy a product
    //            BuyProductID(item.ItemId);
    //        }
    //    }
    //}

    // This is invoked manually on Start to initiate login ops
    //private void Login()
    //{
    //    // Login with Android ID
    //    PlayFabClientAPI.LoginWithAndroidDeviceID(new LoginWithAndroidDeviceIDRequest()
    //    {
    //        CreateAccount = true,
    //        AndroidDeviceId = SystemInfo.deviceUniqueIdentifier
    //    }, result => {
    //        Debug.Log("Logged in");
    //        // Refresh available items
    //        RefreshIAPItems();
    //    }, error => Debug.LogError(error.GenerateErrorReport()));
    //}

    //private void RefreshIAPItems()
    //{
    //    PlayFabClientAPI.GetCatalogItems(new GetCatalogItemsRequest(), result => {
    //        Catalog = result.Catalog;

    //        // Make UnityIAP initialize
    //        InitializePurchasing();
    //    }, error => Debug.LogError(error.GenerateErrorReport()));
    //}

    // This is invoked manually on Start to initialize UnityIAP
    public void InitializePurchasing()
    {
        // If IAP is already initialized, return gently
        if (IsInitialized || !ConfigManager.iap) return;

        // Create a builder for IAP service
#if UNITY_ANDROID
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance(AppStore.GooglePlay));
#elif UNITY_IOS
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
#endif

        // Register each item from the catalog
        foreach (var id in GameManager.instan.keyProductIds)
        {
            //if (item.VirtualCurrencyPrices.ContainsKey("RM") && item.VirtualCurrencyPrices["RM"] > 0 && )
            if (id.Contains("remove_ads"))
            {
                builder.AddProduct(id, ProductType.NonConsumable);
            }
            else
                builder.AddProduct(id, ProductType.Consumable);
        }

        // Trigger IAP service initialization
        UnityPurchasing.Initialize(this, builder);
        Debug.Log("Initing");
    }

    // We are initialized when StoreController and Extensions are set and we are logged in
    public bool IsInitialized
    {
        get
        {
            return m_StoreController != null;
        }
    }

    // This is automatically invoked automatically when IAP service is initialized
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        m_StoreController = controller;
        _mStoreExtensionProvider = extensions;
        Debug.Log("IAP Inited");
    }

    // This is automatically invoked automatically when IAP service failed to initialized
    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.Log("OnInitializeFailed InitializationFailureReason:" + error);
    }

    // This is automatically invoked automatically when purchase failed
    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
    }


    // This is invoked automatically when successful purchase is ready to be processed
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
    {
        // NOTE: this code does not account for purchases that were pending and are
        // delivered on application start.
        // Production code should account for such case:
        // More: https://docs.unity3d.com/ScriptReference/Purchasing.PurchaseProcessingResult.Pending.html

        if (!IsInitialized)
        {
            return PurchaseProcessingResult.Complete;
        }

        // Test edge case where product is unknown
        if (e.purchasedProduct == null)
        {
            Debug.LogWarning("Attempted to process purchase with unknown product. Ignoring");
            return PurchaseProcessingResult.Complete;
        }

        // Test edge case where purchase has no receipt
        if (string.IsNullOrEmpty(e.purchasedProduct.receipt))
        {
            Debug.LogWarning("Attempted to process purchase with no receipt: ignoring");
            return PurchaseProcessingResult.Complete;
        }

        Debug.Log("Processing transaction: " + e.purchasedProduct.transactionID);


#if UNITY_ANDROID
        // Deserialize receipt
        var googleReceipt = GooglePurchase.FromJson(e.purchasedProduct.receipt);

        GameManager.instan.AddPurchase(e.purchasedProduct.definition.id, googleReceipt.PayloadData.json);

        if (string.IsNullOrEmpty(e.purchasedProduct.definition.id) || googleReceipt.PayloadData.JsonData == null || !ConfigManager.reciptCodeAccept.Contains(googleReceipt.PayloadData.JsonData.purchaseState.ToString())
            || !googleReceipt.PayloadData.JsonData.orderId.StartsWith("GPA."))
        {
            Debug.LogWarning("Not yet payment");
            return PurchaseProcessingResult.Complete;
        }


        #region Android
        // Invoke receipt validation
        // This will not only validate a receipt, but will also grant player corresponding items
        // only if receipt is valid.
        PlayFabClientAPI.ValidateGooglePlayPurchase(new ValidateGooglePlayPurchaseRequest()
        {
            // Pass in currency code in ISO format
            CurrencyCode = e.purchasedProduct.metadata.isoCurrencyCode,
            // Convert and set Purchase price
            PurchasePrice = (uint)(e.purchasedProduct.metadata.localizedPrice * 100),
            // Pass in the receipt
            ReceiptJson = googleReceipt.PayloadData.json,
            // Pass in the signature
            Signature = googleReceipt.PayloadData.signature
        }, result =>
        {
            Debug.Log("Validation successful!");

            OnValidateBuy(e);

        },
           error => Debug.Log("Validation failed: " + error.GenerateErrorReport())
        );

        #endregion
#endif

#if UNITY_IOS
        #region iOS
        // Invoke receipt validation
        // This will not only validate a receipt, but will also grant player corresponding items
        // only if receipt is valid.
        //PlayFabClientAPI.ValidateIOSReceipt(new ValidateIOSReceiptRequest()
        //{
        //    // Pass in currency code in ISO format
        //    CurrencyCode = e.purchasedProduct.metadata.isoCurrencyCode,
        //    // Convert and set Purchase price
        //    PurchasePrice = (int)(e.purchasedProduct.metadata.localizedPrice * 100),
        //    ReceiptData = e.purchasedProduct.receipt
        //}, result =>
        //{
        //    Debug.Log("Validation successful!");
        //    OnValidateBuy(e);
        //},
        //   error => Debug.Log("Validation failed: " + error.GenerateErrorReport())
        //);
        OnValidateBuy(e);
        #endregion
#endif

        return PurchaseProcessingResult.Complete;
    }

    public static bool isFirstBuy { get => PlayerPrefs.GetInt("IAP_FIRST_BUY", 1) == 1; set => PlayerPrefs.SetInt("IAP_FIRST_BUY", value ? 1 : 0); }
    private void OnValidateBuy(PurchaseEventArgs e)
    {
        if (GameManager.instan.keyProductIds.Contains(e.purchasedProduct.definition.id))
        {
            ItemId itemId = (ItemId)Enum.Parse(typeof(ItemId), e.purchasedProduct.definition.id);
            ItemConfig it = ConfigManager.GetItemConfig(itemId);
            if (isFirstBuy)
            {
                GameManager.instan.firebaseController.LogEvent("buy_iap");
            }

            GameManager.instan.firebaseController.LogEvent("buy_iap_item", "id", e.purchasedProduct.definition.id);

            //if(itemConfig == null) return;
            //CatalogItem it = itemConfig.catalogItem; //GameManager.instan.ca.Find(i => i.ItemId.Equals(e.purchasedProduct.definition.id));
            if (it != null && it.slot == ItemType.Pack)
            {
                PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
                {

                    GeneratePlayStreamEvent = true,
                    FunctionName = "BuyPack",
                    FunctionParameter = new { itemId = it.itemId.ToString() }

                }, result2 =>
                {
                    GameManager.instan.GetUserInventory();
                    RewardData rewardData = new RewardData();
                    if(it.itemProperties.packData != null)
                    {
                        if (it.itemProperties.packData.currncies != null)
                            rewardData.virtualCurrncies = new List<VirtualCurrncy>(it.itemProperties.packData.currncies);
                        if (it.itemProperties.packData.itemDatas != null)
                            rewardData.itemDatas = new List<ItemData>(it.itemProperties.packData.itemDatas);
                    }
                    //{
                    //    virtualCurrncies = new List<VirtualCurrncy>(it.itemProperties.packData.currncies),
                    //    itemDatas = new List<ItemData>(it.itemProperties.packData.itemDatas),
                    //};
                    GUIManager.instan.CreatePopup<PopupReward>(rewardData);
                    if (popupWaitingFocus) popupWaitingFocus.Dismiss();
                    //GameManagerAll.instan.user.diamond += int.Parse(it.Description);
                    //string _title = $"successfully purchase\n{it.DisplayName}";
                    //HomeManager.instan.getPanel.Create(_title, LayoutManagerAll.instan.packIcon);
                },
       error => Debug.Log(error.GenerateErrorReport()));
            }
            else if (it != null)
            {
                GameManager.instan.GetUserInventory();
                // string _title = $"successfully purchase\n{it.DisplayName}";
                // if (it.ItemId.Equals("remove_ads_2"))
                //     HomeManager.instan.getPanel.Create(_title, LayoutManagerAll.instan.removeAds);
                // else
                //     HomeManager.instan.getPanel.Create(_title);
                ItemData itemData = new ItemData()
                {
                    itemId = itemId,
                    quantity = 1
                };
                RewardData rewardData = new RewardData()
                {
                    itemDatas = new List<ItemData>() { itemData }
                };
                GUIManager.instan.CreatePopup<PopupReward>(rewardData);
                if (popupWaitingFocus) popupWaitingFocus.Dismiss();

            }
        }
        else
            GameManager.instan.GetUserInventory();
    }

    PopupWaitingFocus popupWaitingFocus;
    // This is invoked manually to initiate purchase
    public void BuyProductID(string productId)
    {
        // If IAP service has not been initialized, fail hard
        if (!IsInitialized) throw new Exception("IAP Service is not initialized!");
#if UNITY_IOS
        popupWaitingFocus = GUIManager.instan.CreatePopup<PopupWaitingFocus>();
#endif
        // Pass in the product id to initiate purchase
        m_StoreController.InitiatePurchase(productId);

    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.Log("OnInitializeFailed InitializationFailureReason:" + message + " " + error.ToString());
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        throw new NotImplementedException();
    }

    public void RestorePurchasesIOS(Action callback)
    {
        if (!IsInitialized || GameManager.instan.userData.GetUserItem(ItemId.brawl_valley_remove_ads) != null)
        {
            callback();
            return;
        }

        var apple = _mStoreExtensionProvider.GetExtension<IAppleExtensions>();


        apple.RestoreTransactions((result, s) => {
            Debug.Log("RestorePurchases continuing: " + s + ". If no further messages, no purchases available to restore.");
            if (m_StoreController.products.WithID(ItemId.brawl_valley_remove_ads.ToString()).hasReceipt || m_StoreController.products.WithID(ItemId.brawl_valley_pack_2.ToString()).hasReceipt)
            {
                GameManager.instan.RestorePurchase(ItemId.brawl_valley_remove_ads, callback);
            }
            else
            {
                callback();
            }

        });
    }
}


// The following classes are used to deserialize JSON results provided by IAP Service
// Please, note that JSON fields are case-sensitive and should remain fields to support Unity Deserialization via JsonUtilities
public class JsonData
{
    // JSON Fields, ! Case-sensitive

    public string orderId;
    public string packageName;
    public string productId;
    public long purchaseTime;
    public int purchaseState;
    public string purchaseToken;
}

public class PayloadData
{
    public JsonData JsonData;

    // JSON Fields, ! Case-sensitive
    public string signature;
    public string json;

    public static PayloadData FromJson(string json)
    {
        var payload = JsonUtility.FromJson<PayloadData>(json);
        payload.JsonData = JsonUtility.FromJson<JsonData>(payload.json);
        return payload;
    }
}

public class GooglePurchase
{
    public PayloadData PayloadData;

    // JSON Fields, ! Case-sensitive
    public string Store;
    public string TransactionID;
    public string Payload;

    public static GooglePurchase FromJson(string json)
    {
        var purchase = JsonUtility.FromJson<GooglePurchase>(json);
        purchase.PayloadData = PayloadData.FromJson(purchase.Payload);
        return purchase;
    }
}
