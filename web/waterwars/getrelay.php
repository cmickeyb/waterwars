<?php

//while (list($result_nme, $result_val) = each($_POST)) { echo "$result_nme:$result_val"; }

$url = $_GET['url'];
#$url = "http://192.168.1.2:9000/waterwars/index";

$firstValue = true;

foreach ($_GET as $key => $val)
{
  if ($key != "url")
  {
    if ($firstValue)
    {
      $url .= "?";
      $firstValue = false;
    }
    else
    {
      $url .= "&";
    }

    $url .= "$key=$val";
  }
}
# echo "posting to " . $url;
# $ch = curl_init("$url?" . implode("&", array_values($_GET)));

$ch = curl_init($url);
# curl_setopt($ch, CURLOPT_POST, 1);
# curl_setopt($ch, CURLOPT_POSTFIELDS, $_POST);
#curl_setopt($ch, CURLOPT_POSTFIELDS, array('hello' => 'arnold'));
curl_setopt($ch, CURLOPT_HEADER, false);

// We can't directly insert the output using curl_exec because then we won't be able to write the status header
// Bizarrely, even if the source returns a non-200 status code we still end up with 200 in the output
// hence we have to replace the header with the proper code later on
curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
$data = curl_exec($ch);
header("HTTP/1.1 " . curl_getinfo($ch, CURLINFO_HTTP_CODE));

//header("HTTP/1.1 404 Not Found");
// For some reason, just passing through the headers with CURL_OPTHEADER does not send the response code correctly
//header("HTTP/1.1 " . "404" . " Not Found");

//echo "STATUS " + curl_getinfo($ch, CURLINFO_HTTP_CODE);
curl_close($ch);

echo $data;
?>
