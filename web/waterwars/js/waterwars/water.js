/*
 * For now, we're going to assume that player updates only apply to the water tab 
 */

function handlePlayerUpdateForWaterTab(data)
{
    var waterAvailable;
    if (data.Game.State !== GameStateType.Water)
        waterAvailable = "n/a";
    else
        waterAvailable = getWaterUnitsText(data.Water);
        
    $('#water-entitlement').html(getWaterUnitsText(data.WaterEntitlement));
    $('#water-required').html(getWaterUnitsText(data.WaterRequired));
    $('#water-available').html(waterAvailable);
    $('#water-forecast').html(data.Game.Forecast.Water);

    if (data.OwnerActions.SellWaterRights) 
        $('#wat-sell-rights-button').attr('disabled', false);
    else 
        $('#wat-sell-rights-button').attr('disabled', true);

    if (data.OwnerActions.SellWater) 
        $('#wat-sell-water-button').attr('disabled', false);
    else 
        $('#wat-sell-water-button').attr('disabled', true);

    $('#wat-request-rights-button').attr('disabled', !data.OwnerActions.RequestWaterRights);
    $('#wat-request-water-button').attr('disabled', !data.OwnerActions.RequestWater);       
        
    $('#wat-sell-rights-button').show();
    $('#wat-sell-water-button').show();
    $('#wat-request-rights-button').show();
    $('#wat-request-water-button').show();        
}

function showWater()
{
    //get("player/" + selfId, handlePlayerUpdate);
    put("player/" + selfId + "/selected/player/" + selfId, {});
    changeObjectView(ObjectViewType.Water);
}

function showSellWaterForm()
{
    get("player", function (data, textStatus)
    {
        var playerOptions;
        var water;

        $.each(data, function (i, p)
        {
            if (p.Uuid.Guid != selfId) 
                playerOptions += "<option value=\"" + p.Uuid.Guid + "\">" + p.Name + "</option>";
            else 
                water = p.Water;
        });

        var html = "<form id='sell-water-form'>\n";
        html += "<table>\n";
        html += "  <tr>\n";
        html += "    <td>Amount available</td>\n";
        html += "    <td>" + getWaterUnitsText(water) + "</td>\n";
        html += "  </tr>\n";
        html += "  <tr>\n";
        html += "    <td>Amount to lease</td>\n";
        html += "    <td><input id='sell-water-amount'/></td>\n";
        html += "    <td><div id='sell-water-amount-alert' class='alert'/></td>\n";
        html += "  </tr>\n";
        html += "  <tr>\n";
        html += "    <td>Price</td>\n";
        html += "    <td><input id='sell-water-price'/></td>\n";
        html += "    <td><div id='sell-water-price-alert' class='alert'/></td>\n";
        html += "  </tr>\n";
        html += "  <tr>\n";
        html += "    <td>Player</td>\n";
        html += "    <td><select id='sell-water-players'>" + playerOptions + "</select></td>\n";
        html += "  </tr>\n";
        html += "  <tr>\n";
        html += "    <td><input id='sell-water-back-button' type='button' value='Back'/></td>\n";
        html += "    <td><input id='sell-water-submit-button' type='button' value='Make offer'/></td>\n";
        html += "  </tr>\n";
        html += "</table>\n";
        html += "</form>\n";

        $('#sell-water-form').replaceWith(html);
        
        $('#sell-water-amount').unbind('keyup');
        $('#sell-water-amount').keyup(
            function(event) 
                { checkSellWaterAmount(event, "#sell-water-amount", "#sell-water-amount-alert"); });
        $('#sell-water-price').unbind('keyup');
        $('#sell-water-price').keyup(
            function(event) { checkSellWaterPrice(event, "#sell-water-price", "#sell-water-price-alert"); });        
                
        $('#sell-water-back-button').click(function ()
        {
            changeObjectView(ObjectViewType.Water);
        });
        $('#sell-water-submit-button').click(sellWater);
        changeObjectView(ObjectViewType.SellWater);
    });
}

function checkSellWaterAmount(event, rawAmountId, alertId)
{
    var rawAmount = $(rawAmountId).val();

    var alertText = "";
    if (rawAmount != "")
    {
        var amount = parseInt(rawAmount);
        if (isNaN(amount))
            alertText = "Amount is not a number";
        else if (amount < 0)
            alertText = "Amount is negative";
        else if (amount > lastLocation.Water)
            alertText = "You don't have that much water";
    }
        
    $(alertId).html(alertText);
}

function checkSellWaterPrice(event, rawAmountId, alertId)
{
    var rawAmount = $(rawAmountId).val();

    var alertText = "";
    if (rawAmount != "")
    {
        var amount = parseInt(rawAmount);
        if (isNaN(amount))
            alertText = "Price is not a number";
        else if (amount < 0)
            alertText = "Price is negative";
    }
        
    $(alertId).html(alertText);
}

function sellWater()
{
    var request = new Object();
    var askingPrice = parseInt($('#sell-water-price').val());
    var water = parseInt($('#sell-water-amount').val());
    var playerChosen = $('#sell-water-players').val();

    request.WaterBuyer =
    {
        Uuid: {
            Guid: playerChosen
        }
    };
    request.DeltaWater = water;
    request.DeltaMoney = -askingPrice;

    put("player/" + selfId, request);

    changeObjectView(ObjectViewType.Water);
}

function showSellWaterRightsForm()
{
    get("player", function (data, textStatus)
    {
        var playerOptions;
        var water;

        $.each(data, function (i, p)
        {
            if (p.Uuid.Guid != selfId) 
                playerOptions += "<option value=\"" + p.Uuid.Guid + "\">" + p.Name + "</option>";
            else 
                water = p.WaterEntitlement;
        });        
            
        var html = "<form id='sell-water-rights-form'>\n";
        html += "<table>\n";
        html += "  <tr>\n";
        html += "    <td>Rights available</td>\n";
        html += "    <td>" + getWaterUnitsText(water) + "</td>\n";
        html += "  </tr>\n";    
        html += "  <tr>\n";
        html += "    <td>Amount to sell</td>\n";
        html += "    <td><input id='sell-water-rights-amount' type='text'/></td>\n";
        html += "    <td><div id='sell-water-rights-amount-alert' class='alert'/></td>\n";
        html += "  </tr>\n";
        html += "  <tr>\n";
        html += "    <td>Asking price</td>\n";
        html += "    <td><input id='sell-water-rights-price' type='text'/></td>\n";
        html += "    <td><div id='sell-water-rights-price-alert' class='alert'/></td>\n";
        html += "  </tr>\n";    
        html += "  <tr>\n";
        html += "    <td>Player</td>\n";
        html += "    <td><select id='sell-water-rights-players'>" + playerOptions + "</select></td>\n";    
        html += "  </tr>\n";
        html += "  <tr>\n";
        html += "    <td><input id='sell-water-rights-back-button' type='button' value='Back'/></td>\n";
        html += "    <td><input id='sell-water-rights-submit-button' type='button' value='Make offer'/></td>\n";
        html += "  </tr>\n";
        html += "</table>\n";
        html += "</form>\n";
    
        $('#sell-water-rights-form').replaceWith(html);
        $('#sell-water-rights-back-button').click(function ()
        {
            changeObjectView(ObjectViewType.Water);
        });
        
        $('#sell-water-rights-amount').unbind('keyup');
        $('#sell-water-rights-amount').keyup(
            function(event) 
                { checkSellWaterRightsAmount(
                    event, "#sell-water-rights-amount", "#sell-water-rights-amount-alert"); });
        $('#sell-water-rights-price').unbind('keyup');
        $('#sell-water-rights-price').keyup(
            function(event) { checkSellWaterPrice(event, "#sell-water-rights-price", "#sell-water-rights-price-alert"); });  
                    
        $('#sell-water-rights-submit-button').click(sellWaterRights);
        changeObjectView(ObjectViewType.SellWaterRights);
    });
}

function checkSellWaterRightsAmount(event, rawAmountId, alertId)
{
    var rawAmount = $(rawAmountId).val();

    var alertText = "";
    if (rawAmount != "")
    {
        var amount = parseInt(rawAmount);
        if (isNaN(amount))
            alertText = "Amount is not a number";
        else if (amount < 0)
            alertText = "Amount is negative";
        else if (amount > lastLocation.WaterEntitlement)
            alertText = "You don't have that much water rights";
    }
        
    $(alertId).html(alertText);
}

function sellWaterRights()
{
    var request = new Object();
    var playerChosen = $('#sell-water-rights-players').val();
    var amount = $('#sell-water-rights-amount').val();
    var askingPrice = $('#sell-water-rights-price').val();

    request.WaterRightsBuyer =
    {
        Uuid: {
            Guid: playerChosen
        }
    };
    request.DeltaMoney = -parseInt(askingPrice);
    request.DeltaWaterEntitlement = parseInt(amount);

    put("player/" + selfId, request);

    changeObjectView(ObjectViewType.Water);
}

function showRequestWaterRightsForm()
{            
    var html = "<form id='request-water-rights-form'>\n";
    html += "<table>\n";  
    html += "  <tr>\n";
    html += "    <td>Rights to request</td>\n";
    html += "    <td><input id='request-water-rights-amount' type='text'/></td>\n";
    html += "  </tr>\n";
    html += "  <tr>\n";
    html += "    <td><input id='request-water-rights-back-button' type='button' value='Back'/></td>\n";
    html += "    <td><input id='request-water-rights-submit-button' type='button' value='Make request'/></td>\n";
    html += "  </tr>\n";        
    html += "</table>\n";
    html += "</form>\n";

    $('#request-water-rights-form').replaceWith(html);
    $('#request-water-rights-back-button').click(function ()
    {
        changeObjectView(ObjectViewType.Water);
    }); 
                
    $('#request-water-rights-submit-button').click(requestWaterRights);
    changeObjectView(ObjectViewType.RequestWaterRights);
}

function requestWaterRights()
{
    var request = new Object();
    request.DeltaWaterEntitlement = parseInt($('#request-water-rights-amount').val());

    put("player/" + selfId, request);

    changeObjectView(ObjectViewType.Water);
}

function showRequestWaterForm()
{            
    var html = "<form id='request-water-form'>\n";
    html += "<table>\n";  
    html += "  <tr>\n";
    html += "    <td>Amount to request</td>\n";
    html += "    <td><input id='request-water-amount' type='text'/></td>\n";
    html += "  </tr>\n";
    html += "  <tr>\n";
    html += "    <td><input id='request-water-back-button' type='button' value='Back'/></td>\n";
    html += "    <td><input id='request-water-submit-button' type='button' value='Make request'/></td>\n";
    html += "  </tr>\n";        
    html += "</table>\n";
    html += "</form>\n";

    $('#request-water-form').replaceWith(html);
    $('#request-water-back-button').click(function ()
    {
        changeObjectView(ObjectViewType.Water);
    }); 
                
    $('#request-water-submit-button').click(requestWater);
    changeObjectView(ObjectViewType.RequestWater);
}

function requestWater()
{
    var request = new Object();
    request.DeltaWater = parseInt($('#request-water-amount').val());

    put("player/" + selfId, request);

    changeObjectView(ObjectViewType.Water);
}