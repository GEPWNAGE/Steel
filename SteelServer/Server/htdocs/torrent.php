<?php
include("config.php");


$file = GAMES.basename(substr($_SERVER['QUERY_STRING'],5)).'.torrent';
$file = urldecode($file);

if(!file_exists($file)){
	exit("File not found");
}

$torrent = file_get_contents($file);
$host = $_SERVER['HTTP_HOST'];
$torrent = str_replace('28:http://STEEL_ANNOUNCE_SERVER',(strlen($host)+20).':http://'.$host.'/announce.php',$torrent);

header('Content-type: application/x-bittorrent');
header('Content-Disposition: attachment; filename="'.basename($file).'"');

echo $torrent;
?>