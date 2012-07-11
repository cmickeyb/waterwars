<?php

//while (list($result_nme, $result_val) = each($_POST)) { echo "$result_nme:$result_val"; }

$url = $_POST['url'];
#$url = "http://192.168.1.2:9000/waterwars/index";
echo "posting to " . $url;
$ch = curl_init($url);
curl_setopt($ch, CURLOPT_POST, 1);
curl_setopt($ch, CURLOPT_POSTFIELDS, $_POST);
#curl_setopt($ch, CURLOPT_POSTFIELDS, array('hello' => 'arnold'));
curl_exec($ch);
curl_close($ch);

?>
