const relayUrl = "http://localhost:9000/waterwars/";
const PLAYER_PATH = "player";
const LOGIN_PATH = "login";
const NEWS_PATH = "news";
const UPDATE_PATH = "update";
const MONEY_UNIT = "$";
const WATER_UNIT = "wu";

ObjectViewType =
{
    None: "none",
    Main: "#main-view",
    SellRights: "#sell-rights-view",
    BuyAssets: "#buy-assets-view",
    UpgradeAsset: "#upgrade-asset-view",
    Water: "#water-view",
    Economy: "#economy-view",
    PAndL: "#pandl-view",
    SellWater: "#sell-water-view",
    SellWaterRights: "#sell-water-rights-view",
    RequestWater: "#request-water-view",
    RequestWaterRights: "#request-water-rights-view"    
}

GameStateType =
{
    None : 0,
    Registration : 1,
    Game_Starting : 2,
    Build : 3,
    Allocation : 4,
    Water : 5,
    Revenue : 6,
    Game_Ended : 7,
    Game_Resetting : 8
};
    
AbstractGameAssetType =
{
    Crops: 0,
    Houses: 1,
    Factory: 2,
    Field: 3,
    Parcel: 4,
    Player: 5,
    None: 6
}

RoleType =
{
    Developer: 0,
    Economy: 1,
    Farmer: 2,
    Manufacturer: 3,
    WaterMaster: 4
}

/*
 * Translate game asset types into name.  This is separate from AbstractGameAssetType to avoid tight coupling between
 * the enumeration name and the display name.
 */
AbstractGameAssetTypeNames =
{
    0: "Crops",
    1: "Houses",
    2: "Factory",
    3: "Field",
    4: "Parcel Control",
    5: "Player",
    6: "None"
}

RoleTypeNames =
{
    0: "Developer",
    1: "Economy",
    2: "Farmer",
    3: "Manufacturer",
    4: "WaterMaster"
}

var lastLocation = "None";
var selfId = $.query.get('selfId');
var nullId = "00000000-0000-0000-0000-000000000000";
var authToken;
var infiniteTimeToLive = -999;

/*
 * <summary>
 * Get string for given amount with appropriate water units symbol attached
 * </summary>
 * <param name="amount"></param>
 * <returns></returns>
 */
function getWaterUnitsText(amount)
{
    return amount + "&nbsp;" + WATER_UNIT;
}

/*
 * <summary>
 * Get string for given amount with appropriate money units symbol attached
 * </summary>
 * <param name="amount"></param>
 * <returns></returns>
 */
function getMoneyUnitsText(amount)
{
    return MONEY_UNIT + amount;
}

// GET from the Water Wars server
function get(path, func)
{
    var realUrl = relayUrl + path;
    var url = "getrelay.php?url=" + escape(realUrl) + "&randval=" + Math.random();
    $.getJSON(url, func);
}

// POST to the Water Wars server
// Because of same domain name security restrictions, we have to relay this via our Apache instance
function post(path, req)
{
    // This won't work because of the browser security model
    // Firefox, for instance, does an OPTIONS http request rather than a POST
    //var url = "http://192.168.1.2:9000/waterwars/buypoint/" + lastLocation.Uuid.Guid;
    var realUrl = relayUrl + path + "?_method=post";
    var url = "postrelay.php?url=" + realUrl;
    $.post(url, {
        json: JSON.stringify(req),
        url: realUrl
    });
}

// PUT to the Water Wars server
function put(path, req)
{
    var realUrl = relayUrl + path + "?_method=put";
    var url = "postrelay.php";
    $.post(url, {
        json: JSON.stringify(req),
        url: realUrl
    });
}

// DELETE to the Water Wars server
function del(path)
{
    var realUrl = relayUrl + path + "?_method=delete";
    var url = "postrelay.php";
    $.post(url, {
        url: realUrl
    });
}

function makeGetRelayUrl(realUrl)
{
    return "getrelay.php?url=" + escape(realUrl);
}

// Change the object view, showing and hiding divs as appropriate
// @param newView - an ObjectViewType such as ObjectViewType.SellRights
function changeObjectView(newView)
{
    foundView = false;

    for (var view in ObjectViewType)
    {
        if (ObjectViewType[view] === newView)
        {
            $(ObjectViewType[view]).show();
            foundView = true;
        }
        else
        {
            $(ObjectViewType[view]).hide();
        }
    }

    if (!foundView) alert("No such view " + newView + " in changeObjectView()");
}

/*
 * Show the information that's common to all components.  For historical reasons, this function also
 * hides the irrelevant information from other asset types.
 */
function showMainInfo(assetType)
{
    $('#name').html(lastLocation.Name);
    $('#type').html(AbstractGameAssetTypeNames[lastLocation.Type]);

    if (assetType == AbstractGameAssetType.Parcel)
    {
        hideFieldInfo();
        hideAssetInfo();
    }
    else if (assetType == AbstractGameAssetType.Field)
    {
        hideParcelInfo();
        hideAssetInfo();
    }
    else if (assetType == AbstractGameAssetType.Crops || assetType == AbstractGameAssetType.Houses || assetType == AbstractGameAssetType.Factory)
    {
        hideParcelInfo();
        hideFieldInfo();
    }
    else if (assetType == AbstractGameAssetType.None)
    {
        hideParcelInfo();
        hideFieldInfo();
        hideAssetInfo();
        hideAllButtons();
    }
}

function hideAllButtons()
{
    hideParcelButtons();
    hideGameAssetButtons();
    hideFieldButtons();
}

// Temporary!
var months = new Array("Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec");

function handleNewsUpdate(rss)
{
    $(rss).find("item").each(function ()
    {
        /*
         ts = new Date();
         day = ts.getDate();
         if (day < 10) { day = "0" + day; }
         hours = ts.getHours();
         if (hours < 10) { hours = "0" + hours; }
         minutes = ts.getMinutes();
         if (minutes < 10) { minutes = "0" + minutes; }
         seconds = ts.getSeconds();
         if (seconds < 10) { seconds = "0" + seconds; }
         */

        //$('#news').prepend("\n  " + day + " " + months[ts.getMonth()] + " " + ts.getFullYear() + ", " + hours + ":" + minutes + ":" + seconds + " - " + $(this).find("title").text() + "<br/>");
        // ARGH, Mozilla GRE 1.8.1.21 as found in LL Second Life client 1.23.5 doesn't seem to return anything for .find("pubDate")
        //pubDate = Date.parse($(this).find("pubDate").text());
        // The date text that we will actually output
        var cookedPubDate;

        // We won't have a pubDate if the game has not yet begun
        if ($(this).children("pubDate").length > 0)
        {
            rawPubDate = $(this).children("pubDate").text();
            pubDate = Date.parse(rawPubDate);

            if (pubDate != null) cookedPubDate = pubDate.toString('MMMM d, yyyy');
            else cookedPubDate = rawPubDate;
        }
        else
        {
            cookedPubDate = "#### ##, ####";
        }

        //$('#news').prepend("\n " + pubDate + " - " + $(this).find("title").text() + "<br/>");
        $('#news').prepend("\n " + cookedPubDate + " - " + $(this).find("title").text() + "<br/>");
    });
}

/*
 * Process an object update received from the server
 */
function handleLastSelectedUpdate(data)
{
    //if (lastLocation == "None" || lastLocation.Uuid.Guid != data.Uuid.Guid)
    if (lastLocation == "None" || !compareObj(lastLocation, data))
    {                
        lastLocation = data;

        if (lastLocation.Type == AbstractGameAssetType.Player)
        {
            handlePlayerUpdateForWaterTab(data);
            handlePlayerUpdateForEconomyTab(data);
            handlePlayerUpdateForPAndLTab(data);
        }
        else
        {
            // Something of a hack to always clear an alert box if we receive an object update
            $('#view-alert').html("");
            
            // If the player selects a game object then make sure that we are switched on to the view tab, rather than
            // possibly the water tab
            // If we do it after handling updates (which slide various buttons up and down) then these slides don't
            // work properly.  Therefore, we must change the object view before sliding around
            changeObjectView(ObjectViewType.Main);
            $('#tabs').tabs("select", "#view-tab");
                    
            showMainInfo(lastLocation.Type);

            if (lastLocation.Type == AbstractGameAssetType.Parcel)
            {
                handleParcelUpdate();
            }
            else
            {
                $('#name').show();
                $('#name-edit-box').hide();
                $('#name-edit-button').hide();
                $('#name-save-button').hide();
                $('#name-cancel-button').hide();

                if (lastLocation.Type == AbstractGameAssetType.Crops || lastLocation.Type == AbstractGameAssetType.Houses || lastLocation.Type == AbstractGameAssetType.Factory)
                {
                    handleGameAssetUpdate();
                }
                else if (lastLocation.Type == AbstractGameAssetType.Field)
                {
                    handleFieldUpdate();
                }
            }
        }
    }
}

function loginSuccess()
{
    getUpdate(false);
}

/*
 * Get the next selection update from OpenSim
 * @param wait - If false, then demand an immediate update from the server.  If true, then long poll
 */

function getUpdate(wait)
{
    var realUrl = relayUrl + [PLAYER_PATH, selfId, UPDATE_PATH].join("/");
    //var serialisedJson = JSON.stringify({ Force: force });
    var requestUrl = "getrelay.php?url=" + escape(realUrl) + "&wait=" + wait + "&randval=" + Math.random();

    $.getJSON(requestUrl, function (update)
    {
        // One situation in which update null is when the page is being closed, when neither update no getUpdate() will exist
        // Might be able to deal with this better by examining textStatus though this is tricky
        if (update != null)
        {
            // If the update fails for some reason then just try again.
            try
            {
                handleLastSelectedUpdate(update.LastSelected);
                //handleNewsUpdate($(update.News));
            }
            catch (e)
            {
                // The alert won't show up in the viewer embedded browser but that's what we want right now
                // Later on we may reveals some information here for debugging purposes....
                // The mozilla embedded in the 1.x viewer really isn't happy with this alert - it stops anything further from executing
                //alert(e);
            }

            getUpdate(true);
        }
    });
}

$(document).ready(function ()
{
    $('#tabs').tabs();
    $('[title]').aToolTip(
    {
        yOffset: -60,
        outSpeed: 0,
        delay: 900
    });

    showMainInfo(AbstractGameAssetType.None);
    hideAllButtons();

    //$('#name').ajaxError(function(event, request, settings, e) { alert("Error requesting " + settings.url + ".  Exception " + e); });
    var loginUrl = relayUrl + PLAYER_PATH + "/" + selfId + "/" + LOGIN_PATH;

    $.ajax(
    {
        url: makeGetRelayUrl(loginUrl) + "&loginToken=" + $.query.get('loginToken'),
        //cache: false,
        //complete: function(xhr, textStatus) { $('#name').html("Complete status code " + xhr.status) },
        // success: function(data, textStatus, xhr) { $('#debug').html("Success code " + xhr.status) },
        success: loginSuccess,
        error: function (xhr)
        {
            $('#debug').html("Failure code " + xhr.status)
        }
    });

    /*
     $('#name').editable(function(value, settings)
     {
     changeName(value);
     $('#name-edit-button').show();
     return value;
     },
     {
     submit:"save",
     cancel:"cancel",
     onreset:function() { $('#name-edit-button').show(); }
     });
     $('#name-edit-button').bind('mouseup', function() { $('#name-edit-button').hide(); $('#name').triggerHandler('click'); });
     */

    $('#name-edit-button').bind('mouseup', function ()
    {
        var origName = $('#name').text();
        //$('#name-edit-button').data('origName', origName);
        // Unfortunately, focus does not work in Mozilla GRE 1.8.1.21 as found in LL Second Life client 1.23.5
        // The setTimeout trick doesn't work either
        $('#name-edit-box').val(origName).show().focus();
        //$('#name').replaceWith("<input type='text' id='name' value='" + origName + "'/>"); 
        $('#name').hide();
        $('#name-edit-button').hide();
        $('#name-save-button').show();
        $('#name-cancel-button').show();
    });

    $('#name').bind('click', function ()
    {
        $('#name-edit-button').triggerHandler('mouseup');
    });
    $('#name-edit-box').keydown(

    function (e)
    {
        if (e.which == 13)
        {
            $('#name-save-button').triggerHandler('mouseup');
        }
        else if (e.which == 27)
        {
            $('#name-cancel-button').triggerHandler('mouseup');
        }
    });

    $('#name-save-button').bind('mouseup', function ()
    {
        var newName = $('#name-edit-box').val();
        changeName(newName);
        //$('#name').replaceWith("<b id='name'>" + newName + "</b>");
        $('#name').text(newName);
        $('#name').show();
        $('#name-edit-button').show();
        $('#name-edit-box').hide();
        $('#name-save-button').hide();
        $('#name-cancel-button').hide();
    });

    $('#name-cancel-button').bind('mouseup', function ()
    {
        //$('#name').replaceWith("<b id='name'>" + $('#name-edit-button').data('origName') + "</b>");
        $('#name').show();
        $('#name-edit-button').show();
        $('#name-edit-box').hide();
        $('#name-save-button').hide();
        $('#name-cancel-button').hide();
    });

    $('#buy-rights-button').click(buyRights);
    $('#sell-rights-button').click(showSellRightsForm);
    $('#buy-assets-button').click(showBuyAssetsForm);
    $('#continue-build-asset-button').click(continueBuild);
    $('#sell-asset-to-economy-button').click(sellAssetToEconomy);
    $('#remove-asset-button').click(removeAsset);
    $('#upgrade-asset-button').click(showUpgradeAssetForm);

    $('#view').click(function ()
    {
        changeObjectView(ObjectViewType.Main);
    });

    $('#wat').click(showWater);
    $('#wat-sell-water-button').click(showSellWaterForm);
    $('#wat-sell-rights-button').click(showSellWaterRightsForm);
    $('#wat-request-water-button').click(showRequestWaterForm);
    $('#wat-request-rights-button').click(showRequestWaterRightsForm);    
    
    $('#econ').click(showEconomy);

    $('#ovr').click(showOverview);
    
    $('#pandl').click(showPAndL);
    
    initGameAssetEvents();    
});