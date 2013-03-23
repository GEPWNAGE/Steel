move msxmlenu.exe msxmlenu.msi
vcredist_x86_2010.exe /passive /norestart
dxwebsetup.exe /Q
msiexec /i msxmlenu.msi /quiet /log install.log
move msxmlenu.msi msxmlenu.exe