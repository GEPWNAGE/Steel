<?php
include("config.php");

echo "Updating torrentfiles...\n";

chdir(GAMES);

foreach(glob("*.arc") as $file){
  $basename = basename($file,'.arc');
  $torrent = $basename.'.torrent';
  $xml = $basename.'.xml';

  // create torrent file  
  if(!file_exists($torrent)){
    $cmd = 'ctorrent -t -u http://STEEL_ANNOUNCE_SERVER -l 1048576 -s "'.$torrent.'" "'.$file.'"';   
    system($cmd);
  }
    
  // create xml file with (extracted) size in it
  if(!file_exists($xml)){
    $cmd = 'arc l "'.$file.'"';
    exec($cmd,$ret);

    list(,$size) = explode(', ',$ret[count($ret)-3]);
    $size = str_replace(array('.',' ','bytes'),'',$size);   

    $data = "<xml>\n";
    $data .= "  <size>".$size."</size>\n";
    $data .= "  <exes>\n";
    $data .= "    <exe><file> .. </file> <icon> .. </icon><name> .. </name></exe>\n";
    $data .= "  </exes>\n";
    $data .= "  <message> </message>\n";
    $data .= "</xml>\n";

    file_put_contents($xml,$data);
  }

  echo "Done with $basename\n";
}

echo "Done!\n";
