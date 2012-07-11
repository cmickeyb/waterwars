function handlePlayerUpdateForPAndLTab(p)
{
    var html = "<table cellpadding='7'>\n";
        
    var d = new Date(p.Game.CurrentDateAsMs);
    var y = d.getFullYear();
    
    html += "<tr><th/>";
    html += "<th>" + y + "</th>";
    
    if (p.Game.CurrentRound > 1)
    {
        html += "<th>" + (y - 1) + "</th>";
        
        if (p.Game.CurrentRound > 2)
            html += "<th>" + (y - 2) + "</th>";
    }                    
    
    html += "</tr>\n";
    
    if (p.Role.Type === RoleType.Farmer)
        html += getFarmerPAndL(p);
    else if (p.Role.Type === RoleType.Manufacturer)
        html += getManufacturerPAndL(p);
    else if (p.Role.Type === RoleType.Developer)
        html += getDeveloperPAndL(p);
           
    html += "</table>\n";

    $('#pandl-view').html(html);            
}


function showPAndL()
{
    put("player/" + selfId + "/selected/player/" + selfId, {});
    changeObjectView(ObjectViewType.PAndL);
}

function getDeveloperPAndL(p)
{
    var html = "";
    
    html += getLine(p, "House sale revenue", "BuildRevenueThisTurn");
    html += getLine(p, "+ Revenue from leasing out water", "WaterRevenueThisTurn");
    html += getLine(p, "- Cost of building houses", "BuildCostsThisTurn");
    html += getLine(p, "- Cost of maintaining houses", "MaintenanceCosts");    
    html += getLine(p, "- Cost of leasing water", "WaterCostsThisTurn");
    html += getLine(p, "- Cost of living", "CostOfLiving");
    html += getLine(p, "Profit (loss)", "Profit");        
                
    return html;
}

function getManufacturerPAndL(p)
{
    var html = "";
    
    html += getLine(p, "Factory revenue", "ProjectedRevenueFromProducts");
    html += getLine(p, "+ Revenue from leasing out water", "WaterRevenueThisTurn");    
    html += getLine(p, "- Cost of maintaining factories", "MaintenanceCosts");
    html += getLine(p, "- Cost of leasing water", "WaterCostsThisTurn");
    html += getLine(p, "- Cost of living", "CostOfLiving");    
    html += getLine(p, "Profit (loss)", "Profit");
                
    return html;
}

function getFarmerPAndL(p)
{
    var html = "";
    
    html += getLine(p, "Crop revenue", "ProjectedRevenueFromProducts");
    html += getLine(p, "+ Revenue from leasing out water", "WaterRevenueThisTurn");    
    html += getLine(p, "- Cost of planting", "BuildCostsThisTurn");
    html += getLine(p, "- Cost of leasing water", "WaterCostsThisTurn");
    html += getLine(p, "- Cost of living", "CostOfLiving");
    html += getLine(p, "Profit (loss)", "Profit");    
                
    return html;
}

function getLine(p, key, attr)
{
    var html = "<tr>";
    html += "<td>" + key + "</td>";
    
    html += "<td>" + getMoneyUnitsText(p[attr]) + "</td>";
    
    if (p.Game.CurrentRound > 1)
    {
        html += "<td>" + getMoneyUnitsText(p.History[p.Game.CurrentRound - 1][attr]) + "</td>";
        
        if (p.Game.CurrentRound > 2)
            html += "<td>" + getMoneyUnitsText(p.History[p.Game.CurrentRound - 2][attr]) + "</td>";
    }
    
    html += "</tr>\n";
    
    return html;
}