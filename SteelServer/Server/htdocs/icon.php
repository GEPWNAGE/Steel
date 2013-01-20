<?php
include("config.php");

$file = ICONS.basename($_GET['file']);
if(!file_exists($file) || strlen($_GET['file']) == 0){
	exit("File not found");
}

$icon = file_get_contents($file);

header('Content-type: image/x-icon');
header('Content-Disposition: attachment; filename="'.basename($file).'"');

echo $icon;
?>