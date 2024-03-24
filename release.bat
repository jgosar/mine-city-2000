cd release
del mc2k-release.rar
del mc2k-release-cli.rar

cd ..\MineCity2000
rmdir bin /S /Q
dotnet publish
cd bin\Release\net8.0-windows10.0.19041.0
copy default_args.txt publish
xcopy buildings publish\buildings\* /s /e /h
cd publish
rar a -r ..\..\..\..\..\release\mc2k-release-cli.rar .\*.*

cd ..\..\..\..\..\MineCity2000-GUI
rmdir bin /S /Q
cd ..
dotnet publish -f net8.0-windows10.0.19041.0
cd MineCity2000-GUI\bin\Release\net8.0-windows10.0.19041.0\win10-x64
copy default_args.txt publish
xcopy buildings publish\buildings\* /s /e /h
cd publish
rar a -r ..\..\..\..\..\..\release\mc2k-release.rar .\*.*
