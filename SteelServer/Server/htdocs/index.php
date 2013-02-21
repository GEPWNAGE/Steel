<style>
* { font-family:arial; font-size:12pt; }
</style>
<?php
include("config.php");
echo '<ul>';
foreach(glob(GAMES."*.torrent") as $torrent){
  $name = str_replace('.torrent','',basename($torrent));
  $xmlFile = str_replace('.torrent','.xml',$torrent);
  $data = simplexml_load_file($xmlFile);
  
  $icon = '';
  foreach($data->exes[0] as $exe){
    $icon = $exe->icon;
    break;
  }

  echo '<li><img src="icon.php?file='.$icon.'" width="16"> '.$name.'</li>';


  
}
echo '</ul>';

