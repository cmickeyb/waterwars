/**
 * Initialize game asset events
 */
function initGameAssetEvents()
{
}

function hideAssetInfo(assetType)
{
    $('#asset-sellable-info').hide();
    $('#asset-info').hide();
}

function hideGameAssetButtons()
{
    $('#continue-build-asset-button').hide();
    $('#sell-asset-to-economy-button').hide();
    $('#remove-asset-button').hide();
    $('#upgrade-asset-button').hide();
    $('#allocate-water-button').hide();
    $('#undo-allocate-water-button').hide();
    $('#allocate-amount').hide();
    $('#allocate-amount-dropdown').hide();
}

function handleGameAssetUpdate()
{
    hideFieldButtons();
    hideParcelButtons();         
       
    if (lastLocation.Type === AbstractGameAssetType.Houses)
    {       
        var marketPriceHtml = getMoneyUnitsText(lastLocation.MarketPrice);
         
        var maintenanceHtml;   
        if (lastLocation.IsSoldToEconomy)
            maintenanceHtml = "n/a";
        else
            maintenanceHtml = getMoneyUnitsText(lastLocation.AccruedMaintenanceCost + "  (" + getMoneyUnitsText(lastLocation.MaintenanceCost) + " per turn)");
                   
        var profitHtml;
        if (lastLocation.IsSoldToEconomy || lastLocation.Game.State !== GameStateType.Build || !lastLocation.IsBuilt)
        {
            profitHtml = "n/a";
        }
        else
        {
            var profitPerWu = lastLocation.Profit / lastLocation.WaterUsage;    
            profitHtml = getMoneyUnitsText(lastLocation.Profit) + "  (" + getMoneyUnitsText(profitPerWu.toFixed(2)) + " per " + WATER_UNIT + ")";
        }
                
        $('#asset-sellable-info').show();
        $('#asset-info').hide();
        $('#asset-sellable-owner').html(lastLocation.OwnerName);
        $('#asset-sellable-build-status').html(getBuildStatus(lastLocation));
        $('#asset-sellable-water-rights-required').html(getWaterUnitsText(lastLocation.WaterUsage));
        $('#asset-sellable-market-price').html(marketPriceHtml);
        $('#asset-sellable-construction-cost').html(getMoneyUnitsText(lastLocation.ConstructionCostPerBuildStep * lastLocation.StepsBuilt));
        $('#asset-sellable-maintenance-cost').html(maintenanceHtml);
        $('#asset-sellable-nominal-maximum-profit').html(profitHtml);
    }
    else
    {        
        var possibleRevenuePerWu = lastLocation.RevenueThisTurn / lastLocation.WaterUsage;
        var possibleRevenueHtml
            = getMoneyUnitsText(lastLocation.RevenueThisTurn) + "  (" + getMoneyUnitsText(possibleRevenuePerWu.toFixed(2)) + " per " + WATER_UNIT + ")";
                
        var maintenanceHtml;
        if (lastLocation.Type === AbstractGameAssetType.Crops)
        {
            if (lastLocation.TimeToLive === infiniteTimeToLive)
                maintenanceHtml = getWaterUnitsText(lastLocation.WaterUsage);
            else
                maintenanceHtml = "n/a";
        }
        else    
        {
            maintenanceHtml = getMoneyUnitsText(lastLocation.MaintenanceCost);
        }  
        
        var profitHtml;
        if (lastLocation.Game.State !== GameStateType.Water || !lastLocation.IsBuilt)
            profitHtml = "n/a";
        else    
            profitHtml = getMoneyUnitsText(lastLocation.Profit);
            
        var waterAllocated;
        var projectedRevenue;
        if (lastLocation.Game.State !== GameStateType.Water)
        {
            waterAllocated = "n/a";
            projectedRevenue = "n/a";
        }
        else
        {
            waterAllocated = getWaterUnitsText(lastLocation.WaterAllocated);
            projectedRevenue = getMoneyUnitsText(lastLocation.ProjectedRevenue);
        }
            
        $('#asset-sellable-info').hide();
        $('#asset-info').show();
        $('#asset-owner').html(lastLocation.OwnerName);
        $('#asset-build-status').html(getBuildStatus(lastLocation));
        $('#asset-water-usage').html(getWaterUnitsText(lastLocation.WaterUsage));
        $('#asset-water-allocated').html(waterAllocated);
        $('#asset-revenue').html(possibleRevenueHtml);
        $('#projected-asset-revenue').html(projectedRevenue);
        $('#asset-maintenance-cost').html(maintenanceHtml);
        $('#asset-nominal-maximum-profit').html(profitHtml);
    }

    // Owner options
    if (lastLocation.OwnerUuid.Guid == selfId)
    {
        // Build options
        if (lastLocation.OwnerActions.Remove) 
            $('#remove-asset-button').attr('disabled', false);
        else 
            $('#remove-asset-button').attr('disabled', true);
                            
        $('#remove-asset-button').show();             

        if (lastLocation.CanBeSoldToEconomy)
        {
            if (lastLocation.OwnerActions.SellToEconomy) 
                $('#sell-asset-to-economy-button').attr('disabled', false);
            else 
                $('#sell-asset-to-economy-button').attr('disabled', true);
                
            $('#sell-asset-to-economy-button').show();                    
        }     
        else
        {
            $('#sell-asset-to-economy-button').hide();
        }            
            
        if (lastLocation.IsMultiStepBuild)
        {
            if (lastLocation.OwnerActions.ContinueBuild)
                $('#continue-build-asset-button').attr('disabled', false);
            else
                $('#continue-build-asset-button').attr('disabled', true); 
    
            $('#continue-build-asset-button').show();
        }
        else
        {
            $('#continue-build-asset-button').hide();
        }
        
        if (lastLocation.CanUpgradeInPrinciple)
        {
            if (lastLocation.OwnerActions.Upgrade)
            {
                $('#upgrade-asset-button').attr('disabled', false);
            }
            else
            {
                $('#upgrade-asset-button').attr('disabled', true); 
            }
            
            $('#upgrade-asset-button').show();
        }
        else
        {
            $('#upgrade-asset-button').hide();
        }

        // Water options
        if (lastLocation.OwnerActions.AllocateWater)
        {
            $('#allocate-water-button').unbind('click');
            if (lastLocation.CanPartiallyAllocateWater)
            {
                $('#allocate-water-button').click(function()
                {
                    amount = parseInt($('#allocate-amount').val());
                    allocateWater(amount);
                });
                $('#allocate-water-button').attr('disabled', false);
                        
                var amount = 0;

                if (lastLocation.WaterAllocated == 0) 
                    amount = Math.min(lastLocation.Field.Owner.Water, lastLocation.WaterUsage);
                else 
                    amount = lastLocation.WaterAllocated;

                $('#allocate-amount').val(amount);
                $('#allocate-amount').unbind('keyup');
                $('#allocate-amount').keyup(checkWaterAllocationAmount);
                $('#allocate-amount').show();
                $('#allocate-amount-dropdown').hide();
            }
            else
            {
                $('#allocate-water-button').unbind('click');
                $('#allocate-water-button').click(function()
                {
                    var amount = 0;
                    
                    if ($('#allocate-amount-dropdown').val() === "Full")
                        amount = lastLocation.WaterUsage;
    
                    allocateWater(amount);
                });

                $('#allocate-amount-dropdown').unbind('change');
                $('#allocate-amount-dropdown').change(function()
                {
                    var dropdownMatchesCurrent = false;
                    var selectedAllocationIsFull = $('#allocate-amount-dropdown').val() === "Full";
                    
                    if (selectedAllocationIsFull && lastLocation.WaterAllocated == lastLocation.WaterUsage)
                        dropdownMatchesCurrent = true;
                    else if (!selectedAllocationIsFull && lastLocation.WaterAllocated != lastLocation.WaterUsage)
                        dropdownMatchesCurrent = true;
                        
                    $('#allocate-water-button').attr('disabled', dropdownMatchesCurrent);
                });
                                                        
                $('#allocate-amount-dropdown').val("Full");
                $('#allocate-amount-dropdown').triggerHandler('change');                
                $('#allocate-amount').hide();
                $('#allocate-amount-dropdown').show();
            }
        }
        else
        {
            $('#allocate-amount').hide();
            $('#allocate-amount-dropdown').hide();
            $('#allocate-water-button').attr('disabled', true);
        }
        
        if (lastLocation.CanBeAllocatedWater)
            $('#allocate-water-button').show();
        else
            $('#allocate-water-button').hide();
    }
    else
    {
        hideGameAssetButtons();
    }
}

/*
 * Get build status information about the given asset
 */
function getBuildStatus(asset)
{
    var status;
    if (asset.IsBuilt)
    {
        status = "Complete";
    }
    else
    {
        if (asset.StepBuiltThisTurn)
        {
            status = "Building step " + asset.StepsBuilt + " of " + asset.StepsToBuild
                + " (cost " + getMoneyUnitsText(lastLocation.ConstructionCostPerBuildStep) + ")";
        }
        else
        {
            status 
                = "Awaiting step " + asset.StepsBuilt + " of " + asset.StepsToBuild 
                    + " (cost " + getMoneyUnitsText(lastLocation.ConstructionCostPerBuildStep) + ")";
        }
    }
                
    return status;
}

function checkWaterAllocationAmount(event)
{
    var rawAmount = $('#allocate-amount').val();

    if (event.keyCode == 13 && rawAmount != "")
    { 
        $('#allocate-water-button').click();
    } 
    else
    {
        var alertText = "";
        if (rawAmount != "")
        {
            var amount = parseInt(rawAmount);
            if (isNaN(amount))
                alertText = "Amount is not a number";
            else if (amount < 0)
                alertText = "Amount is negative";
            else if (amount > lastLocation.WaterUsage)
                alertText = "Amount is larger than required";
            else if (amount - lastLocation.WaterAllocated > lastLocation.Field.Owner.Water)
                alertText = "You don't have that much water";
        }
            
        $('#view-alert').html(alertText);
    }
}

// Only factories are currently upgradeable
function showUpgradeAssetForm()
{
    var html = "<form id='upgrade-asset-form'>\n";
    html += "<table cellpadding='10' rules='rows'>\n";

    html += "<tr>\n";

    // col 1
    html += "<td valign='top'>\n";
    html += "    <b>" + lastLocation.Name + " (CURRENT)</b><br/>\n";
    html += "</td>\n";

    // col 2
    html += "<td valign='top'>\n";
    if (lastLocation.Type == AbstractGameAssetType.Houses)
    {
        html += "    Rights&nbsp;req&nbsp;to&nbsp;sell:<br/>\n";
    }
    else
    {
        html += "    Water&nbsp;usage:<br/>\n";
        html += "    Upkeep:<br/>\n";
    }

    html += "</td>\n";

    // col 3
    html += "<td valign='top'>\n";
    html += getWaterUnitsText(lastLocation.WaterUsage) + "<br/>\n";

    if (lastLocation.Type != AbstractGameAssetType.Houses) 
        html += getMoneyUnitsText(lastLocation.MaintenanceCost) + "<br/>\n";

    html += "</td>\n";

    // col 4
    html += "<td valign='top'>\n";

    if (lastLocation.Type == AbstractGameAssetType.Houses) 
        html += "    Market&nbsp;value:<br/>\n";
    else 
        html += "    Revenue:<br/>\n";

    html += "</td>\n";

    // col 5
    html += "<td valign='top'>\n";
    html 
        += getMoneyUnitsText(Math.ceil(lastLocation.NormalRevenue 
            * lastLocation.Game.EconomicActivity[AbstractGameAssetTypeNames[lastLocation.Type]][lastLocation.Level])) 
            + "<br/>\n";
    html += "</td>\n";

    html += "</tr>\n";

    var i;
    for (i = lastLocation.Level + 1; i <= lastLocation.MaxLevel; i++)
    {
        var canAfford = lastLocation.Field.Owner.Money >= lastLocation.UpgradeCosts[i];

        html += "<tr" + ((!canAfford) ? " class='greyout'" : "") + ">\n";

        // col 1
        html += "<td valign='top'>\n";
        html += "    <b>" + lastLocation.InitialNames[i] + "</b><br/>\n";
        html += "</td>\n";

        // col 2
        html += "<td valign='top'>\n";
        html += "    Upgrade&nbsp;cost:<br/>\n";
//        html += "    Turns&nbsp;to&nbsp;upgrade:<br/>\n";

        if (lastLocation.Type == AbstractGameAssetType.Houses)
        {
            html += "    Rights&nbsp;req&nbsp;to&nbsp;sell:<br/>\n";
        }
        else
        {
            html += "    Water&nbsp;usage:<br/>\n";
            html += "    Upkeep:<br/>\n";
        }

        html += "</td>\n";

        // col 3
        html += "<td valign='top'>\n";

        html += getMoneyUnitsText(lastLocation.UpgradeCosts[i]) + "<br/>\n";
//        html += lastLocation.StepsToBuilds[i] + "<br/>\n";
        html += getWaterUnitsText(lastLocation.WaterUsages[i]) + "<br/>\n";

        if (lastLocation.Type != AbstractGameAssetType.Houses) html += getMoneyUnitsText(lastLocation.MaintenanceCosts[i]) + "<br/>\n";

        html += "</td>\n";

        // col 4
        html += "<td valign='top'>\n";

        if (lastLocation.Type == AbstractGameAssetType.Houses) 
            html += "Market&nbsp;value:<br/>\n";
        else 
            html += "    Revenue:<br/>\n";

        html += "</td>\n";

        // col 5
        html += "<td valign='top'>\n";
        html 
            += getMoneyUnitsText(Math.ceil(lastLocation.NormalRevenues[i] 
                * lastLocation.Game.EconomicActivity[AbstractGameAssetTypeNames[lastLocation.Type]][i])) 
                + "<br/>\n";        
        html += "</td>\n";

        // col 6
        if (canAfford)
        {
            html += "  <td>\n";
            html += "    <input type='radio' name='upgrade-asset-level' value='" + i + "'/>\n";
            html += "  </td>\n";
        }

        html += "</tr>\n";
    }

    html += "<tr><td/><td/><td/><td/>";
    html += "<td><input id='upgrade-asset-back-button' type='button' value='Back'/></td>";
    html += "<td><input id='upgrade-asset-submit-button' type='button' value='Upgrade'/></td></tr>";
    html += "</table>\n";
    html += "</form>\n";

    $('#upgrade-asset-form').replaceWith(html);
    $('#upgrade-asset-back-button').click(function() { changeObjectView(ObjectViewType.Main); });
    $('#upgrade-asset-submit-button').click(upgradeAsset);
    changeObjectView(ObjectViewType.UpgradeAsset);
}

function sellAssetToEconomy()
{
    put("buypoint/" + lastLocation.BuyPointUuid.Guid + "/assets/" + lastLocation.Uuid.Guid, {
        OwnerUuid: {
            Guid: "Economy"
        }
    });
}

/*
 * Continue the build of a game asset
 */
function continueBuild()
{
    var req =
    {
        TurnsBuilt: lastLocation.TurnsBuild++
    };
    put("buypoint/" + lastLocation.BuyPointUuid.Guid + "/assets/" + lastLocation.Uuid.Guid, req);
}

function removeAsset()
{
    del("buypoint/" + lastLocation.BuyPointUuid.Guid + "/assets/" + lastLocation.Uuid.Guid);

    $('#remove-asset-button').hide();
    changeObjectView(ObjectViewType.Main);
}

function upgradeAsset()
{
    var req =
    {
        Level: parseInt($("input[name='upgrade-asset-level']:checked").val())
    };
    put("buypoint/" + lastLocation.BuyPointUuid.Guid + "/assets/" + lastLocation.Uuid.Guid, req);

    changeObjectView(ObjectViewType.Main);
}

function allocateWater(amount)
{
    put("buypoint/" + lastLocation.BuyPointUuid.Guid + "/assets/" + lastLocation.Uuid.Guid, {
        WaterAllocated: parseInt(amount)
    });
}