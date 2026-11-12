// =====================================================================
// Brawl Fortress - rewarded-ad handlers (appended to CloudScript rev 6)
//   ClaimDoubleReward : x2 end-game / daily reward (one claim, 10 min)
//   GetTicketByAds    : watch ad -> tickets for a map (daily cap)
// EndGame and AddDailyReward call savePendingDouble(...) before returning.
// =====================================================================

var ADS_DOUBLE_EXPIRE_MS = 10 * 60 * 1000;
var ADS_DEFAULT_MAX_TICKET_PER_DAY = 5;

function adsGetInternal(keys) {
    return server.GetUserInternalData({ PlayFabId: currentPlayerId, Keys: keys }).Data || {};
}

function adsSetInternal(data, removeKeys) {
    var req = { PlayFabId: currentPlayerId, Data: data || {} };
    if (removeKeys && removeKeys.length) req.KeysToRemove = removeKeys;
    server.UpdateUserInternalData(req);
}

// Adds uses to a consumable item, same pattern as EndGame (one stacked instance).
// Durable items (no RemainingUses) are skipped - doubling them makes no sense.
function adsAddConsumable(itemId, quantity, inventory) {
    var inst = inventory.find(function (e) { return e.ItemId == itemId; });
    if (inst) {
        if (inst.RemainingUses === undefined || inst.RemainingUses === null) return 0;
        server.ModifyItemUses({ PlayFabId: currentPlayerId, ItemInstanceId: inst.ItemInstanceId, UsesToAdd: quantity });
        return quantity;
    }
    var granted = server.GrantItemsToUser({ PlayFabId: currentPlayerId, ItemIds: [itemId] }).ItemGrantResults[0];
    if (!granted) return 0;
    var got = granted.RemainingUses || 0;
    if (got === 0 && (granted.RemainingUses === undefined || granted.RemainingUses === null)) return 1; // durable, first copy
    if (quantity - got != 0) {
        server.ModifyItemUses({ PlayFabId: currentPlayerId, ItemInstanceId: granted.ItemInstanceId, UsesToAdd: quantity - got });
    }
    return quantity;
}

function savePendingDouble(source, rewardData) {
    if (!rewardData) return;
    try {
        var data = {};
        data["pending_double_" + source] = JSON.stringify({ reward: rewardData, expires: Date.now() + ADS_DOUBLE_EXPIRE_MS });
        adsSetInternal(data);
    } catch (e) { log.error("savePendingDouble " + e); }
}

handlers.ClaimDoubleReward = function (args, context) {
    var source = args && args.source;
    if (source !== "endgame" && source !== "daily") return { messageValue: "error" };
    var key = "pending_double_" + source;
    var data = adsGetInternal([key]);
    if (!data[key] || !data[key].Value) return { messageValue: "error" };
    adsSetInternal(null, [key]); // one-time claim

    var pending;
    try { pending = JSON.parse(data[key].Value); } catch (e) { pending = null; }
    if (!pending || !pending.reward || Date.now() > pending.expires) return { messageValue: "error" };

    var given = { itemDatas: [], virtualCurrncies: [] };
    var reward = pending.reward;
    (reward.virtualCurrncies || []).forEach(function (vc) {
        if (!(vc.quantity > 0)) return;
        server.AddUserVirtualCurrency({ PlayFabId: currentPlayerId, VirtualCurrency: vc.currency, Amount: vc.quantity });
        given.virtualCurrncies.push({ currency: vc.currency, quantity: vc.quantity });
    });
    if (reward.itemDatas && reward.itemDatas.length) {
        var inventory = server.GetUserInventory({ PlayFabId: currentPlayerId }).Inventory || [];
        reward.itemDatas.forEach(function (it) {
            var q = it.quantity > 0 ? it.quantity : 1;
            var added = adsAddConsumable(it.itemId, q, inventory);
            if (added > 0) given.itemDatas.push({ itemId: it.itemId, quantity: added });
        });
    }
    if (!given.itemDatas.length && !given.virtualCurrncies.length) return { messageValue: "error" };
    return { messageValue: given };
};

function adsFindStickPerPlay(raw, mapName, mapId) {
    try {
        var cfg = JSON.parse(raw);
        var list = Array.isArray(cfg) ? cfg : Object.keys(cfg).map(function (k) {
            var v = cfg[k]; if (v && typeof v === "object" && v.map === undefined) v.map = k; return v;
        });
        for (var i = 0; i < list.length; i++) {
            var m = list[i];
            if (m && (m.map == mapName || m.map == mapId || m.name == mapName))
                return m.stickPerPlay > 0 ? m.stickPerPlay : 1;
        }
    } catch (e) { }
    return 1;
}

handlers.GetTicketByAds = function (args, context) {
    var mapName = args && args.map;
    var mapId = args && args.mapId;
    if (!mapName) return { messageValue: "error" };

    var td = server.GetTitleData({ Keys: ["can_get_ticket_by_ads", "max_ticket_ads_per_day", "map_config"] }).Data || {};
    if (td.can_get_ticket_by_ads && String(td.can_get_ticket_by_ads).toLowerCase() === "false") return { messageValue: "error" };
    var maxPerDay = parseInt(td.max_ticket_ads_per_day, 10);
    if (!(maxPerDay >= 0)) maxPerDay = ADS_DEFAULT_MAX_TICKET_PER_DAY;

    var today = new Date().toISOString().substring(0, 10);
    var cnt = adsGetInternal(["ticket_ads_day"]);
    var state = { day: today, count: 0 };
    if (cnt.ticket_ads_day && cnt.ticket_ads_day.Value) {
        try { var s = JSON.parse(cnt.ticket_ads_day.Value); if (s.day === today) state = s; } catch (e) { }
    }
    if (state.count >= maxPerDay) return { messageValue: "limit" };

    var catalog = server.GetCatalogItems({}).Catalog || [];
    var ticket = null;
    for (var i = 0; i < catalog.length; i++) {
        var c = catalog[i];
        if ((c.ItemClass || "").toLowerCase() !== "ticket") continue;
        if ((c.Tags || []).indexOf(mapName) >= 0) { ticket = c; break; }
    }
    if (!ticket) return { messageValue: "error" };

    var amount = adsFindStickPerPlay(td.map_config, mapName, mapId);
    var inventory = server.GetUserInventory({ PlayFabId: currentPlayerId }).Inventory || [];
    adsAddConsumable(ticket.ItemId, amount, inventory);

    state.count++;
    adsSetInternal({ ticket_ads_day: JSON.stringify(state) });
    return { messageValue: "success" };
};
