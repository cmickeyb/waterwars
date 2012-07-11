// Keep a record of the last asset radio selected for purchased, so that we can select it again on the next buy
var lastBuyAssetLevelChecked = 1;

function hideFieldInfo()
{
    $('#field-info').hide();
}

function hideFieldButtons()
{
    $('#buy-assets-button').hide();
}

function handleFieldUpdate()
{
    //var submitButtonText = (template.Type === AbstractGameAssetType.Crops ? "Plant" : "Build");
    var buildButtonText = (lastLocation.Owner.Role.AllowedAssets[0].Type === AbstractGameAssetType.Crops ? "Plant" : "Build")
    $('#buy-assets-button').val(buildButtonText);
    //$('#buy-assets-button').val("Plant");
    $('#field-info').show();

    hideGameAssetButtons();
    hideParcelButtons();

    $('#field-owner').html(lastLocation.Owner.Name);

    //if (lastLocation.Type != AbstractGameAssetType.Field && lastLocation.OwnerUuid.Guid == selfId)
    if (lastLocation.Owner.Uuid.Guid == selfId)
    {
        if (lastLocation.OwnerActions.BuyAsset)
        {
            $('#buy-assets-button').attr('disabled', false);            
        }
        else
        {
            $('#buy-assets-button').attr('disabled', true);
        }
        
        $('#buy-assets-button').show();
    }
    else
    {
        hideFieldButtons();
    }
}

function showBuyAssetsForm()
{
    var html = "<form id='buy-assets-form'>\n";
    html += "<table cellpadding='10' rules='rows'>\n";
    var template = lastLocation.Owner.Role.AllowedAssets[0];

    var i;
    for (i = template.MinLevel; i <= template.MaxLevel; i++)
    {
        var canAfford = lastLocation.Owner.Money >= template.ConstructionCostsPerBuildStep[i];

        html += "<tr" + ((!canAfford) ? " class='greyout'" : "") + ">\n";

        // col 1
        html += "<td valign='top'>\n";
        html += "    <b>" + template.InitialNames[i] + "</b><br/>\n";
        html += "</td>\n";

        // col 2
        html += "<td valign='top'>\n";
        
        if (template.Type === AbstractGameAssetType.Crops) 
        {
            html += "    Planting&nbsp;cost:<br/>\n";
        }
        else
        {                    
            html += "    Total&nbsp;build&nbsp;cost:<br/>\n";
            html += "    Build&nbsp;steps:<br/>\n";
            html += "    Cost&nbsp;per&nbsp;step:<br/>\n";
        }                    
        
        html += "</td>\n";

        // col 3
        html += "<td valign='top'>\n";
        html += getMoneyUnitsText(template.ConstructionCosts[i]) + "<br/>\n";
        
        if (template.Type !== AbstractGameAssetType.Crops) 
        {        
            html += template.StepsToBuilds[i] + "<br/>\n";
            html += getMoneyUnitsText(template.ConstructionCostsPerBuildStep[i]) + "<br/>\n";
        }
        
        html += "</td>\n";

        // col 4
        html += "<td valign='top'>\n";

        if (template.Type == AbstractGameAssetType.Houses)
        {
            html += "Rights&nbsp;req&nbsp;to&nbsp;sell:<br/>\n"; 
            html += "Market&nbsp;value:<br/>\n";
            html += "Upkeep:<br/>\n";
        }
        else 
        {
            if (template.Type == AbstractGameAssetType.Crops) 
                html += "Matures:<br/>\n";
                        
            html += "Water&nbsp;usage:<br/>\n";
            html += "Revenue:<br/>\n";
            
            if (template.Type != AbstractGameAssetType.Crops)             
                html += "Upkeep:<br/>\n";
        }        
        html += "</td>\n";

        // col 5
        html += "<td valign='top'>\n";
        if (template.Type == AbstractGameAssetType.Crops)
        {
            var ttl = template.InitialTimesToLive[i];
            
            if (ttl === infiniteTimeToLive)
            {
                html += "perennial";
            }
            else
            {
                html += ttl + "&nbsp;turn" + ((ttl > 1) ? "s" : "");
            }
            
            html += "<br/>\n";
        }        
        html += getWaterUnitsText(template.WaterUsages[i]) + "<br/>\n";
        
        // FIXME: This is extremely naughty - we should be pulling these numbers off the template
        // rather than calculating them ourselves        
        html 
            += getMoneyUnitsText(Math.ceil(template.NormalRevenues[i] 
                * lastLocation.Game.EconomicActivity[AbstractGameAssetTypeNames[template.Type]][i])) 
                + "<br/>\n";
        if (template.Type != AbstractGameAssetType.Crops) 
            html += getMoneyUnitsText(template.MaintenanceCosts[i]) + "<br/>\n";        
        html += "</td>\n";

        // col 6
        if (canAfford)
        {
            html += "<td>\n";
            html += "    <input type='radio' name='buy-assets-level' value='" + i + "'";
            if (lastBuyAssetLevelChecked == i)
                html += " checked='checked'";
            html += "/>\n";
            html += "</td>\n";
        }

        html += "</tr>\n";
    }

    var submitButtonText = (template.Type === AbstractGameAssetType.Crops ? "Plant" : "Build");              
    html += "<tr><td/><td/><td/><td/>";
    html += "<td><input id='buy-assets-back-button' type='button' value='Back'/></td>";
    html += "<td><input id='buy-assets-submit-button' type='button' value='" + submitButtonText + "'/></td></tr>\n";
    html += "</table>\n";
    html += "</form>\n";

    $('#buy-assets-form').replaceWith(html);
    $('#buy-assets-back-button').click(function() { changeObjectView(ObjectViewType.Main); });    
    $('#buy-assets-submit-button').click(buyAssets);
    changeObjectView(ObjectViewType.BuyAssets);
}

function buyAssets()
{
    var level = parseInt($("input[name='buy-assets-level']:checked").val());
    lastBuyAssetLevelChecked = level;
     
    var req =
    {
        Level: level
    };

    post("buypoint/" + lastLocation.BuyPointUuid.Guid + "/field/" + lastLocation.Uuid.Guid, req);

    $('#buy-assets-button').hide();
    changeObjectView(ObjectViewType.Main);
}