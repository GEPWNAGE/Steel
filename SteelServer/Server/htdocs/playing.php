<?php
$current = file_get_contents('tmp.txt');

echo $current;
file_put_contents('tmp.txt',$current."\n".$_SERVER['REMOTE_ADDR'].' - '.$_GET['game']);