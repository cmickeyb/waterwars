function showOverview()
{
    //$('#overview-tab').html("<p>Nothing here yetgreen giant</p>\n");
    get("player", function (data, textStatus)
    {
        var html = "<form id='overview-form'><table cellpadding='10'>\n";
        html += "<tr>\n";
        html += "<th align='left'>Player</th>";
        html += "<th align='left'>Role</th>";
        html += "<th align='left'>Parcels</th>";
        html += "<th align='left'>Water rights</th>";
        html += "<th align='left'>Water available</th>";
        html += "<th align='left'>Cash</th>\n";
        html += "</tr>\n";

        $.each(data, function (i, p)
        {
            html += "<tr>\n";
            html += "<td>" + p.Name + "</td>";
            html += "<td>" + RoleTypeNames[p.Role.Type] + "</td>";
            html += "<td>" + p.DevelopmentRightsOwnedCount + "</td>";
            html += "<td>" + getWaterUnitsText(p.WaterEntitlement) + "</td>";
            html += "<td>" + getWaterUnitsText(p.Water) + "</td>";
            html += "<td>" + getMoneyUnitsText(p.Money) + "</td>\n";
            html += "</tr>\n";
        });
        
//        html += "<tr><td><td><td><td><td>";
//        html += "<td><input id='overview-refresh-button' type='button' value='Refresh'/></td>";
//        html += "</tr>\n";

        html += "</table></form>\n";        

        $('#overview-view').html(html);  
        
        // For some reason, if we setup the click handler here then the web browser becomes very slow to respond.
        // If we both create the button and assign the handle then the SL built-in web viewer crashes
        // Not sure why this is - calling the same function should set up something different
        // If we assign the handle outside of this function then everything is okay.
        //$('#overview-refresh-button').click(showOverview);                       
    });
    
    $('#overview-refresh-button').click(showOverview); 
}