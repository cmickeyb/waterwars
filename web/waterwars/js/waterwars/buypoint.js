function hideParcelInfo()
{
    $('#parcel-info').hide();
    $('#parcel-info-unsold').hide();
    $('#parcel-info-unsold-alt').hide();
}

function hideParcelButtons()
{
    $('#buy-rights-button').hide();
    $('#sell-rights-button').hide();
}

function handleParcelUpdate()
{
    $('#parcel-info').show();

    waterRightsOwnerId = lastLocation.WaterRightsOwner.Uuid.Guid;

    hideFieldButtons();
    hideGameAssetButtons();

    $('#parcel-development-rights-owner').html(lastLocation.DevelopmentRightsOwner.Name);
    $('#parcel-water-rights-owner').html(lastLocation.WaterRightsOwner.Name);

    // Sell rights button
    if (lastLocation.DevelopmentRightsOwner.Uuid.Guid == selfId || lastLocation.WaterRightsOwner.Uuid.Guid == selfId)
    {       
        if (lastLocation.Game.State === GameStateType.Build
            && (lastLocation.OwnerActions.SellDevelopmentRights || lastLocation.OwnerActions.SellWaterRights))
        {
            $('#sell-rights-button').attr('disabled', false);
        }
        else
        {
            $('#sell-rights-button').attr('disabled', true);
        }
        
        $('#sell-rights-button').show();
    }
    else
    {
        $('#sell-rights-button').hide();
        $('#sell-rights-form').hide();
    }
    
    // Buy rights button
    if (lastLocation.DevelopmentRightsOwner.Uuid.Guid === nullId)
    {
        $('#parcel-water-entitlement').html(getWaterUnitsText(lastLocation.InitialWaterRights));
        $('#parcel-price').html(getMoneyUnitsText(lastLocation.CombinedPrice));
        $('#parcel-info-unsold').show();
            
        if (lastLocation.NonOwnerActions.BuyDevelopmentRights)
        {
            $('#buy-rights-button').attr('disabled', false);                               
        }
        else
        {
            $('#buy-rights-button').attr('disabled', true);          
        }
        
        $('#buy-rights-button').show();        
    }
    else
    {
        $('#parcel-info-unsold').hide();
        $('#buy-rights-button').hide();
    }

    if (lastLocation.DevelopmentRightsOwner.Uuid.Guid == selfId)
    {
        // Messy since we don't preserve state
        // There is probably a bug here if we switch between owned assets (e.g. name won't reappear)
        // This could be solved by a uuid comparison with lastlocation if we still have that available
        if ($('#name-edit-box').css('display') == "none")
        {
            $('#name').show();
            $('#name-edit-button').show();
        }
    }
    else
    {
        $('#name').show();
        $('#name-edit-box').hide();
        $('#name-edit-button').hide();
        $('#name-save-button').hide();
        $('#name-cancel-button').hide();
    }
}

function changeName(newName)
{
    post("buypoint/" + lastLocation.Uuid.Guid, {
        Name: newName
    });
}

function buyRights()
{
    var req =
    {
        DevelopmentRightsOwner: {
            Uuid: {
                Guid: selfId
            }
        },
        WaterRightsOwner: {
            Uuid: {
                Guid: selfId
            }
        }
    };

    // $('#debug').html(serialisedJson);
    post("buypoint/" + lastLocation.Uuid.Guid, req);
}

function showSellRightsForm()
{
    var rightsOptions;
    var isDevelopmentRightsOwner = lastLocation.DevelopmentRightsOwner.Uuid.Guid == selfId;
    var isWaterRightsOwner = lastLocation.WaterRightsOwner.Uuid.Guid == selfId;

    var html = "<form id='sell-rights-form'>\n";
    html += "<table>\n";
    html += "  <tr>\n";
    html += "    <td>Asking price</td>\n";
    html += "    <td><input id='sell-rights-price' type='text'/></td>\n";
    html += "    <td><div id='sell-rights-price-alert' class='alert'/></td>\n";
    html += "  </tr>\n";    
    html += "  <tr>\n";
    html += "    <td>Player</td>\n";
    html += "    <td><select id='sell-rights-players'>";

    get("player", function (data, textStatus)
    {
        //$('#debug').html("data:" + data);
        var playerOptions;

        $.each(data, function (i, p)
        {
            if (p.Uuid.Guid != selfId) playerOptions += "<option value=\"" + p.Uuid.Guid + "\">" + p.Name + "</option>";
        });

        $('#sell-rights-players').html(playerOptions);
    });

    html += "</select></td>\n";
    html += "  </tr>\n";
    html += "  <tr>\n";
    html += "    <td><input id='sell-rights-back-button' type='button' value='Back'/></td>\n";
    html += "    <td><input id='sell-rights-submit-button' type='button' value='Make offer'/></td>\n";
    html += "  </tr>\n";
    html += "</table>\n";
    html += "</form>\n";

    $('#sell-rights-form').replaceWith(html);
    $('#sell-rights-back-button').click(function ()
    {
        changeObjectView(ObjectViewType.Main);
    });
    
    $('#sell-rights-price').unbind('keyup');
    $('#sell-rights-price').keyup(checkSellRightsPrice);  
        
    $('#sell-rights-submit-button').click(sellRights);
    changeObjectView(ObjectViewType.SellRights);
}

function checkSellRightsPrice(event)
{
    var rawAmount = $('#sell-rights-price').val();

    var alertText = "";
    if (rawAmount != "")
    {
        var amount = parseInt(rawAmount);
        if (isNaN(amount))
            alertText = "Price is not a number";
        else if (amount < 0)
            alertText = "Price is negative";
    }
        
    $('#sell-rights-price-alert').html(alertText);
}

function sellRights()
{
    var request = new Object();
    var playerChosen = $('#sell-rights-players').val();
    var askingPrice = $('#sell-rights-price').val();

    request.DevelopmentRightsOwner =
    {
        Uuid: {
            Guid: playerChosen
        }
    };
    request.DevelopmentRightsPrice = parseInt(askingPrice);

    put("buypoint/" + lastLocation.Uuid.Guid, request);

    //$('#sell-rights-form').slideUp("fast");
}