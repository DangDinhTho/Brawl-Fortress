using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

/// <summary>
/// Unity IAP v5 (StoreController API).
/// Flow: Connect -> OnStoreConnected -> FetchProducts -> OnProductsFetched -> FetchPurchases.
/// Purchases arrive in OnPurchasePending and must be finished with ConfirmPurchase.
/// </summary>
public class IAPManager : MonoBehaviour
{
    public static IAPManager instan;

    StoreController m_StoreController;
    bool m_ProductsFetched;
    readonly HashSet<string> m_ProcessingTransactions = new HashSet<string>();

    private void Awake()
    {
        if (instan == null)
            instan = this;
        else
            Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    public void Start()
    {
        //InitializePurchasing();
    }

    // We are initialized once the store is connected and the products are fetched
    public bool IsInitialized => m_StoreController != null && m_ProductsFetched;

    public async void InitializePurchasing()
    {
        // If IAP is already initializing / initialized, return gently
        if (m_StoreController != null || !ConfigManager.iap) return;

        m_StoreController = UnityIAPServices.StoreController();

        m_StoreController.OnStoreConnected += OnStoreConnected;
        m_StoreController.OnStoreDisconnected += OnStoreDisconnected;
        m_StoreController.OnProductsFetched += OnProductsFetched;
        m_StoreController.OnProductsFetchFailed += OnProductsFetchFailed;
        m_StoreController.OnPurchasesFetched += OnPurchasesFetched;
        m_StoreController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
        m_StoreController.OnPurchasePending += OnPurchasePending;
        m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;
        m_StoreController.OnPurchaseFailed += OnPurchaseFailed;
        m_StoreController.OnPurchaseDeferred += OnPurchaseDeferred;

        Debug.Log("IAP: connecting to store");
        try
        {
            await m_StoreController.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError("IAP: Connect failed: " + ex);
        }
    }

    #region Store / products
    void OnStoreConnected()
    {
        Debug.Log("IAP: store connected");
        var products = new List<ProductDefinition>();
        foreach (var id in GameManager.instan.keyProductIds)
        {
            var type = id.Contains("remove_ads") ? ProductType.NonConsumable : ProductType.Consumable;
            products.Add(new ProductDefinition(id, type));
        }
        m_StoreController.FetchProducts(products);
    }

    void OnStoreDisconnected(StoreConnectionFailureDescription description)
    {
        Debug.LogWarning("IAP: store disconnected: " + description.message);
    }

    void OnProductsFetched(List<Product> products)
    {
        m_ProductsFetched = true;
        Debug.Log($"IAP Inited ({products.Count} products)");
        // Deliver purchases that were not finished in a previous session (and restored ones).
        m_StoreController.FetchPurchases();
    }

    void OnProductsFetchFailed(ProductFetchFailed failure)
    {
        Debug.LogError($"IAP: products fetch failed for {failure.FailedFetchProducts.Count} products: {failure.FailureReason}");
    }

    void OnPurchasesFetched(Orders orders)
    {
        Debug.Log("IAP: purchases fetched");
    }

    void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
    {
        Debug.LogWarning("IAP: purchases fetch failed: " + failure.message);
    }
    #endregion

    #region Purchase flow
    static Product GetFirstProduct(Order order)
    {
        return order?.CartOrdered?.Items().FirstOrDefault()?.Product;
    }

    // Replaces the legacy ProcessPurchase(PurchaseEventArgs)
    void OnPurchasePending(PendingOrder order)
    {
        var product = GetFirstProduct(order);

        // Edge case: product is unknown
        if (product == null)
        {
            Debug.LogWarning("Attempted to process purchase with unknown product. Ignoring");
            m_StoreController.ConfirmPurchase(order);
            return;
        }

        var receipt = order.Info.Receipt;
        // Edge case: purchase has no receipt
        if (string.IsNullOrEmpty(receipt))
        {
            Debug.LogWarning("Attempted to process purchase with no receipt: ignoring");
            m_StoreController.ConfirmPurchase(order);
            return;
        }

        var transactionId = order.Info.TransactionID ?? string.Empty;
        if (!m_ProcessingTransactions.Add(transactionId))
            return; // already being validated

        Debug.Log("Processing transaction: " + transactionId);

#if UNITY_ANDROID
        // Deserialize receipt
        var googleReceipt = GooglePurchase.FromJson(receipt);

        GameManager.instan.AddPurchase(product.definition.id, googleReceipt.PayloadData.json);

        if (string.IsNullOrEmpty(product.definition.id) || googleReceipt.PayloadData.JsonData == null || !ConfigManager.reciptCodeAccept.Contains(googleReceipt.PayloadData.JsonData.purchaseState.ToString())
            || !googleReceipt.PayloadData.JsonData.orderId.StartsWith("GPA."))
        {
            Debug.LogWarning("Not yet payment");
            FinishOrder(order, transactionId);
            return;
        }

        // Validate the receipt on PlayFab, grant items only if it is valid.
        PlayFabClientAPI.ValidateGooglePlayPurchase(new ValidateGooglePlayPurchaseRequest()
        {
            CurrencyCode = product.metadata.isoCurrencyCode,
            PurchasePrice = (uint)(product.metadata.localizedPrice * 100),
            ReceiptJson = googleReceipt.PayloadData.json,
            Signature = googleReceipt.PayloadData.signature
        }, result =>
        {
            Debug.Log("Validation successful!");
            OnValidateBuy(product);
            FinishOrder(order, transactionId);
        },
        error =>
        {
            Debug.Log("Validation failed: " + error.GenerateErrorReport());
            FinishOrder(order, transactionId);
        });
#elif UNITY_IOS
        OnValidateBuy(product);
        FinishOrder(order, transactionId);
#else
        FinishOrder(order, transactionId);
#endif
    }

    void FinishOrder(PendingOrder order, string transactionId)
    {
        m_ProcessingTransactions.Remove(transactionId);
        m_StoreController.ConfirmPurchase(order);
    }

    void OnPurchaseConfirmed(Order order)
    {
        var id = GetFirstProduct(order)?.definition.id;
        if (order is FailedOrder failed)
            Debug.LogWarning($"IAP: confirmation failed - Product: '{id}', reason: {failed.FailureReason}, details: {failed.Details}");
        else
            Debug.Log($"IAP: purchase confirmed - Product: {id}");
    }

    void OnPurchaseFailed(FailedOrder order)
    {
        var id = GetFirstProduct(order)?.definition.id;
        Debug.Log($"OnPurchaseFailed: FAIL. Product: '{id}', PurchaseFailureReason: {order.FailureReason}, Details: {order.Details}");
        if (popupWaitingFocus) popupWaitingFocus.Dismiss();
    }

    void OnPurchaseDeferred(DeferredOrder order)
    {
        Debug.Log($"IAP: purchase deferred - Product: {GetFirstProduct(order)?.definition.id}");
        if (popupWaitingFocus) popupWaitingFocus.Dismiss();
    }
    #endregion

    public static bool isFirstBuy { get => PlayerPrefs.GetInt("IAP_FIRST_BUY", 1) == 1; set => PlayerPrefs.SetInt("IAP_FIRST_BUY", value ? 1 : 0); }

    private void OnValidateBuy(Product product)
    {
        string productId = product.definition.id;
        if (GameManager.instan.keyProductIds.Contains(productId))
        {
            ItemId itemId = (ItemId)Enum.Parse(typeof(ItemId), productId);
            ItemConfig it = ConfigManager.GetItemConfig(itemId);
            if (isFirstBuy)
            {
                GameManager.instan.firebaseController.LogEvent("buy_iap");
            }

            GameManager.instan.firebaseController.LogEvent("buy_iap_item", "id", productId);

            if (it != null && it.slot == ItemType.Pack)
            {
                PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
                {
                    GeneratePlayStreamEvent = true,
                    FunctionName = "BuyPack",
                    FunctionParameter = new { itemId = it.itemId.ToString() }
                }, result2 =>
                {
                    if (result2.Error != null)
                    {
                        Debug.LogError($"BuyPack CloudScript error: {result2.Error.Error} - {result2.Error.Message}");
                        GameManager.instan.GetUserInventory();
                        if (popupWaitingFocus) popupWaitingFocus.Dismiss();
                        return;
                    }
                    GameManager.instan.GetUserInventory();
                    RewardData rewardData = new RewardData();
                    if (it.itemProperties.packData != null)
                    {
                        if (it.itemProperties.packData.currncies != null)
                            rewardData.virtualCurrncies = new List<VirtualCurrncy>(it.itemProperties.packData.currncies);
                        if (it.itemProperties.packData.itemDatas != null)
                            rewardData.itemDatas = new List<ItemData>(it.itemProperties.packData.itemDatas);
                    }
                    GUIManager.instan.CreatePopup<PopupReward>(rewardData);
                    if (popupWaitingFocus) popupWaitingFocus.Dismiss();
                },
                error => Debug.Log(error.GenerateErrorReport()));
            }
            else if (it != null)
            {
                GameManager.instan.GetUserInventory();
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
        if (!IsInitialized)
        {
            Debug.LogError("IAP Service is not initialized!");
            return;
        }
#if UNITY_IOS
        popupWaitingFocus = GUIManager.instan.CreatePopup<PopupWaitingFocus>();
#endif
#if Admob
        // The store sheet sends the app to background: don't show an app open ad when it returns.
        if (AdmobManager.insan != null) AdmobManager.insan.SuppressNextAppOpen();
#endif
        m_StoreController.PurchaseProduct(productId);
    }

    /// <summary>Store-localized price (e.g. "25.000 ₫"), or null when the product is not fetched yet.</summary>
    public string GetLocalizedPrice(string productId)
    {
        if (m_StoreController == null || !m_ProductsFetched) return null;
        var price = m_StoreController.GetProductById(productId)?.metadata?.localizedPriceString;
        return string.IsNullOrEmpty(price) ? null : price;
    }

    bool HasPurchased(string productId)
    {
        if (m_StoreController.GetProductById(productId)?.hasReceipt == true) return true;
        return m_StoreController.GetPurchases().Any(o =>
            (o is ConfirmedOrder || o is PendingOrder) &&
            o.CartOrdered.Items().Any(i => i.Product?.definition.id == productId));
    }

    public void RestorePurchasesIOS(Action callback)
    {
        if (!IsInitialized || GameManager.instan.userData.GetUserItem(ItemId.brawl_valley_remove_ads) != null)
        {
            callback();
            return;
        }

#if Admob
        if (AdmobManager.insan != null) AdmobManager.insan.SuppressNextAppOpen();
#endif
        m_StoreController.RestoreTransactions((success, message) =>
        {
            Debug.Log("RestorePurchases continuing: " + success + " " + message + ". If no further messages, no purchases available to restore.");
            if (HasPurchased(ItemId.brawl_valley_remove_ads.ToString()) || HasPurchased(ItemId.brawl_valley_pack_2.ToString()))
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
