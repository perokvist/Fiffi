@echo "Builing and publishing nuget"
dotnet restore
dotnet build
dotnet test
cd src\Fiffi
cd nupkgs
del *.nupkg
cd ..
dotnet pack -o nupkgs
dotnet nuget push nupkgs\*.nupkg -k oy2nuatumdmsjfqczhmq7hu7lv2lnvhelkhn4jc2bt2vvu --source https://www.nuget.org/api/v2/package
cd ..
@echo "Done."
