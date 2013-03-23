<?php
include("Server/htdocs/config.php");

echo "Updating torrentfiles...\n";

// mktorrent basename
$mktorrent = getcwd().'\\ctorrent\\ctorrent.exe';
chdir(GAMES);

foreach(glob("*.arc") as $file){
  $basename = basename($file,'.arc');
  $torrent = $basename.'.torrent';
    
  // create torrent file  
  if(!file_exists($torrent)){
    $cmd = $mktorrent.' -t -u http://STEEL_ANNOUNCE_SERVER -l 1048576 -s "'.$torrent.'" "'.$file.'"';   
    system($cmd);
      
    // determine size of file
    $cmd = 'arc l "'.$file.'" > tmp.txt';
    system($cmd);

    $tmp = file('tmp.txt');
    list(,$size) = explode(', ',$tmp[count($tmp)-2]);
    $size = str_replace(' ','',str_replace('bytes','',str_replace(',','',$size)));

    // write filesize
    $info = '';
    if(file_exists($basename.'.txt')){
    	$info = file_get_contents($basename.'.txt');
	}
	$info = $size."\r\n".$info;
	file_put_contents($basename.'.txt',$info);

	unlink('tmp.txt');
  }
}

echo "Done!\n";