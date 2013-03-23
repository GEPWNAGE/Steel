<?php
include("config.php");

error_reporting(E_ALL ^ E_NOTICE);

$xml = new XmlWriter();
$xml->openMemory();
$xml->startDocument('1.0', 'UTF-8');
$xml->startElement('games');


$db = new SQLite3('db/playing.db');
// remove old data
$db->exec("DELETE FROM players WHERE timestamp < ".(time()-600));

function write(XMLWriter $xml, $data){
    foreach($data as $key => $value){
        if(is_array($value)){
            if(substr($key,0,5) == 'game_'){ $key = 'game'; }
            if(substr($key,0,3) == 'exe' && $key != 'exes'){ $key = 'exe'; }
            $xml->startElement($key);
            write($xml, $value);
            $xml->endElement();
            continue;
        }
        $xml->writeElement($key, $value);
    }
}

$gameId = 0;
foreach(glob(GAMES."*.torrent") as $torrent){
  $gamename = trim(basename($torrent,'.torrent')); 
  $xmlFile = str_replace('.torrent','.xml',$torrent);
  if(!file_exists($xmlFile)){
    continue;
  }

  $data = simplexml_load_file($xmlFile);

  $xmlexes = array();
  $i = 0;


  foreach($data->exes[0] as $exe){
    $file = (string)$exe->file;  
    $icon = (string)$exe->icon;
    $name = (string)$exe->name;
   
    if(strlen(str_replace(array(' ','.'),'',$file)) == 0){ continue; }

    $xmlexes['exe'.$i++] = array("file" => trim($file), "icon" => trim($icon), "name" => trim($name));
  }

  if(count($xmlexes) == 0){ continue; }


  // fetch current players
  $result = $db->query("SELECT nickname  FROM players WHERE game = '".$gamename."'");
  $players = array();
  while($row = $result->fetchArray()){
    $players[] = $row['nickname'];
  }
   

  if(count($players)>0){
    $playerList = implode(', ',$players);
  } else {
    $playerList = '';
  }
  

  $output['game_'.($gameId++)]= array("title" => $gamename, "size" => (string)$data->size, "message" => (string)$data->message, "exes" => $xmlexes, "players" => $playerList);
}


header("Content-Type: text/xml");
write($xml, $output);
$xml->endElement();
echo $xml->outputMemory(true);
