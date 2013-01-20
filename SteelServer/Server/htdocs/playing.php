<?php


if(isset($_GET['game']) && $_GET['nickname']){
	$game = sqlite_escape_string(htmlentities($_GET['game']));
	$nickname = sqlite_escape_string(htmlentities($_GET['nickname']));

	// put data in sqlite table
	$db = new SQLite3('db/playing.db');
	$db->exec("INSERT OR REPLACE INTO players VALUES ('".$nickname."','".$game."',".time().")");
}

?>