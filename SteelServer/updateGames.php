<?php
echo "Updating torrentfiles...\n";

foreach(glob("Games\\*.arc") as $file){
  $basename = basename($file,'.arc');
  $torrent = 'Games\\'.$basename.'.torrent';
  if(!file_exists($torrent)){
    $cmd = 'mktorrent\\mktorrent.exe -a http://STEEL_ANNOUNCE_SERVER -o "'.$torrent.'" "'.$file.'"';
    system($cmd);
  }
}

echo "Done!\n";