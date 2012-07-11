/*
 * For now, we're going to assume that player updates only apply to the water tab 
 */

function handlePlayerUpdateForEconomyTab(data)
{
    var economicForecasts = data.Game.Forecast.Economic;
    $('#factory-output-demand-forecast').html(
        economicForecasts !== null ? economicForecasts.Factory[1] : "Available next build phase");
    $('#houses-level-1-demand-forecast').html(
        economicForecasts !== null ? economicForecasts.Houses[1] : "Available next build phase");
    $('#houses-level-2-demand-forecast').html(
        economicForecasts !== null ? economicForecasts.Houses[2] : "Available next build phase");
    $('#houses-level-3-demand-forecast').html(
        economicForecasts !== null ? economicForecasts.Houses[3] : "Available next build phase");                
}

function showEconomy()
{
    //get("player/" + selfId, handlePlayerUpdate);
    put("player/" + selfId + "/selected/player/" + selfId, {});
    changeObjectView(ObjectViewType.Economy);
}