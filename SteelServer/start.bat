@echo off

Server\php.exe updateGames.php

:loop
echo Starting Steel Server...
cd Server
LightTPD.exe
cd ..

goto loop